using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevenueRecognitionAPI.DTOs;
using RevenueRecognitionAPI.Models;
using RevenueRecognitionAPI.Services;
using RevenueRecognitionAPI.Attributes;

namespace RevenueRecognitionAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[RequireUser]
public class ClientsController : ControllerBase
{
    private readonly IRevenueService _revenueService;

    public ClientsController(IRevenueService revenueService)
    {
        _revenueService = revenueService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllClients()
    {
        var clients = await _revenueService.GetAllClients();
        var response = clients.Select(MapClientToResponse).ToList();
        return Ok(response);
    }

    [HttpGet("{clientId}")]
    public async Task<IActionResult> GetClient(int clientId)
    {
        var client = await _revenueService.GetClientById(clientId);
        if (client == null)
            return NotFound($"Client with ID {clientId} not found");

        return Ok(MapClientToResponse(client));
    }

    [HttpPost("individual")]
    public async Task<IActionResult> CreateIndividualClient(CreateIndividualClientDTO request)
    {
        if (await _revenueService.DoesIndividualClientExist(request.PESEL))
            return Conflict($"Individual client with PESEL {request.PESEL} already exists");

        var client = new IndividualClient
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            PESEL = request.PESEL,
            Address = request.Address,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };

        var createdClient = await _revenueService.AddIndividualClient(client);
        return CreatedAtAction(nameof(GetClient), new { clientId = createdClient.Id }, 
            MapClientToResponse(createdClient));
    }

    [HttpPost("company")]
    public async Task<IActionResult> CreateCompanyClient(CreateCompanyClientDTO request)
    {
        if (await _revenueService.DoesCompanyClientExist(request.KRS))
            return Conflict($"Company client with KRS {request.KRS} already exists");

        var client = new CompanyClient
        {
            CompanyName = request.CompanyName,
            KRS = request.KRS,
            Address = request.Address,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };

        var createdClient = await _revenueService.AddCompanyClient(client);
        return CreatedAtAction(nameof(GetClient), new { clientId = createdClient.Id }, 
            MapClientToResponse(createdClient));
    }

    [HttpPut("{clientId}/individual")]
    [RequireAdmin]
    public async Task<IActionResult> UpdateIndividualClient(int clientId, UpdateIndividualClientDTO request)
    {
        if (!await _revenueService.DoesClientExist(clientId))
            return NotFound($"Client with ID {clientId} not found");

        var updatedClient = new IndividualClient
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Address = request.Address,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };

        var result = await _revenueService.UpdateIndividualClient(clientId, updatedClient);
        if (result == null)
            return BadRequest("Unable to update client or client is not an individual client");

        return Ok(MapClientToResponse(result));
    }

    [HttpPut("{clientId}/company")]
    [RequireAdmin]
    public async Task<IActionResult> UpdateCompanyClient(int clientId, UpdateCompanyClientDTO request)
    {
        if (!await _revenueService.DoesClientExist(clientId))
            return NotFound($"Client with ID {clientId} not found");

        var updatedClient = new CompanyClient
        {
            CompanyName = request.CompanyName,
            Address = request.Address,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };

        var result = await _revenueService.UpdateCompanyClient(clientId, updatedClient);
        if (result == null)
            return BadRequest("Unable to update client or client is not a company client");

        return Ok(MapClientToResponse(result));
    }

    [HttpDelete("{clientId}")]
    [RequireAdmin]
    public async Task<IActionResult> DeleteIndividualClient(int clientId)
    {
        if (!await _revenueService.DoesClientExist(clientId))
            return NotFound($"Client with ID {clientId} not found");

        var success = await _revenueService.SoftDeleteIndividualClient(clientId);
        if (!success)
            return BadRequest("Cannot delete this client - only individual clients can be deleted");

        return NoContent();
    }

    private static ClientResponseDTO MapClientToResponse(Client client)
    {
        return client switch
        {
            IndividualClient individual => new IndividualClientResponseDTO
            {
                Id = individual.Id,
                FirstName = individual.FirstName,
                LastName = individual.LastName,
                PESEL = individual.PESEL,
                Address = individual.Address,
                Email = individual.Email,
                PhoneNumber = individual.PhoneNumber,
                CreatedAt = individual.CreatedAt,
                ClientType = "Individual"
            },
            CompanyClient company => new CompanyClientResponseDTO
            {
                Id = company.Id,
                CompanyName = company.CompanyName,
                KRS = company.KRS,
                Address = company.Address,
                Email = company.Email,
                PhoneNumber = company.PhoneNumber,
                CreatedAt = company.CreatedAt,
                ClientType = "Company"
            },
            _ => throw new ArgumentException("Unknown client type")
        };
    }
}