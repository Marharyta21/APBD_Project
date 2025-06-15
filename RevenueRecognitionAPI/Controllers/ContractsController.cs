using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevenueRecognitionAPI.Attributes;
using RevenueRecognitionAPI.DTOs;
using RevenueRecognitionAPI.Models;
using RevenueRecognitionAPI.Services;

namespace RevenueRecognitionAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[RequireUser]
public class ContractsController : ControllerBase
{
    private readonly IRevenueService _revenueService;

    public ContractsController(IRevenueService revenueService)
    {
        _revenueService = revenueService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateContract(CreateContractDTO request)
    {
        if (!await _revenueService.DoesClientExist(request.ClientId))
            return NotFound($"Client with ID {request.ClientId} not found");
        
        if (!await _revenueService.DoesSoftwareExist(request.SoftwareId))
            return NotFound($"Software with ID {request.SoftwareId} not found");
        
        if (await _revenueService.HasActiveContractForSoftware(request.ClientId, request.SoftwareId))
            return Conflict("Client already has an active contract for this software");

        var software = await _revenueService.GetSoftwareById(request.SoftwareId);
        var isReturningClient = await _revenueService.IsReturningClient(request.ClientId);
        
        var basePrice = software!.UpfrontPrice + (request.AdditionalSupportYears * 1000);
        var finalPrice = await _revenueService.CalculateDiscountedPrice(
            basePrice, request.SoftwareId, isReturningClient);

        var contract = new Contract
        {
            ClientId = request.ClientId,
            SoftwareId = request.SoftwareId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(request.DurationDays),
            Price = finalPrice,
            SoftwareVersion = request.SoftwareVersion,
            AdditionalSupportYears = request.AdditionalSupportYears
        };

        var createdContract = await _revenueService.CreateContract(contract);
        var response = await MapContractToResponse(createdContract);

        return CreatedAtAction(nameof(GetContract), new { contractId = createdContract.Id }, response);
    }

    [HttpGet("{contractId}")]
    public async Task<IActionResult> GetContract(int contractId)
    {
        var contract = await _revenueService.GetContractById(contractId);
        if (contract == null)
            return NotFound($"Contract with ID {contractId} not found");

        var response = await MapContractToResponse(contract);
        return Ok(response);
    }

    [HttpGet("client/{clientId}")]
    public async Task<IActionResult> GetClientContracts(int clientId)
    {
        if (!await _revenueService.DoesClientExist(clientId))
            return NotFound($"Client with ID {clientId} not found");

        var contracts = await _revenueService.GetClientContracts(clientId);
        var response = new List<ContractResponseDTO>();

        foreach (var contract in contracts)
        {
            response.Add(await MapContractToResponse(contract));
        }

        return Ok(response);
    }

    [HttpPost("{contractId}/payments")]
    public async Task<IActionResult> ProcessPayment(int contractId, ProcessPaymentDTO request)
    {
        var contract = await _revenueService.GetContractById(contractId);
        if (contract == null)
            return NotFound($"Contract with ID {contractId} not found");

        if (!contract.IsPaymentWindowOpen)
            return BadRequest("Payment window for this contract has closed");

        if (contract.IsFullyPaid)
            return BadRequest("Contract is already fully paid");

        if (request.Amount > contract.RemainingAmount)
            return BadRequest($"Payment amount exceeds remaining balance of {contract.RemainingAmount:C}");

        var success = await _revenueService.ProcessContractPayment(contractId, request.Amount);
        if (!success)
            return BadRequest("Unable to process payment");
        
        var updatedContract = await _revenueService.GetContractById(contractId);
        var response = await MapContractToResponse(updatedContract!);

        return Ok(response);
    }

    [HttpPost("cancel-expired")]
    public async Task<IActionResult> CancelExpiredContracts()
    {
        await _revenueService.CancelExpiredContracts();
        return Ok(new { Message = "Expired contracts have been cancelled and payments refunded" });
    }

    private async Task<ContractResponseDTO> MapContractToResponse(Contract contract)
    {
        var client = contract.Client ?? await _revenueService.GetClientById(contract.ClientId);
        var software = contract.Software ?? await _revenueService.GetSoftwareById(contract.SoftwareId);

        return new ContractResponseDTO
        {
            Id = contract.Id,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            Price = contract.Price,
            SoftwareVersion = contract.SoftwareVersion,
            AdditionalSupportYears = contract.AdditionalSupportYears,
            IsSigned = contract.IsSigned,
            IsCancelled = contract.IsCancelled,
            CreatedAt = contract.CreatedAt,
            TotalPaid = contract.TotalPaid,
            RemainingAmount = contract.RemainingAmount,
            IsPaymentWindowOpen = contract.IsPaymentWindowOpen,
            Client = new ClientSummaryDTO
            {
                Id = client!.Id,
                Name = client switch
                {
                    IndividualClient ind => $"{ind.FirstName} {ind.LastName}",
                    CompanyClient comp => comp.CompanyName,
                    _ => "Unknown"
                },
                Email = client.Email,
                ClientType = client switch
                {
                    IndividualClient => "Individual",
                    CompanyClient => "Company",
                    _ => "Unknown"
                }
            },
            Software = new SoftwareSummaryDTO
            {
                Id = software!.Id,
                Name = software.Name,
                CurrentVersion = software.CurrentVersion,
                Category = software.Category
            },
            Payments = contract.Payments.Select(p => new PaymentResponseDTO
            {
                Id = p.Id,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                IsRefunded = p.IsRefunded,
                RefundDate = p.RefundDate
            }).ToList()
        };
    }
}