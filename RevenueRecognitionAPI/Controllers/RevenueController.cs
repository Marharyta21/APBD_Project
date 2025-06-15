using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevenueRecognitionAPI.DTOs;
using RevenueRecognitionAPI.Services;

namespace RevenueRecognitionAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RevenueController : ControllerBase
{
    private readonly IRevenueService _revenueService;

    public RevenueController(IRevenueService revenueService)
    {
        _revenueService = revenueService;
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> CalculateRevenue([FromBody] RevenueCalculationRequestDTO? request = null)
    {
        request ??= new RevenueCalculationRequestDTO();
        
        if (request.SoftwareId.HasValue && !await _revenueService.DoesSoftwareExist(request.SoftwareId.Value))
            return NotFound($"Software with ID {request.SoftwareId} not found");
        
        var currency = request.Currency?.ToUpper() ?? "PLN";
        if (!string.IsNullOrEmpty(request.Currency) && !IsValidCurrency(currency))
            return BadRequest($"Invalid currency code: {request.Currency}");

        var currentRevenue = await _revenueService.CalculateCurrentRevenue(request.SoftwareId, currency);
        var predictedRevenue = await _revenueService.CalculatePredictedRevenue(request.SoftwareId, currency);

        string? softwareName = null;
        if (request.SoftwareId.HasValue)
        {
            var software = await _revenueService.GetSoftwareById(request.SoftwareId.Value);
            softwareName = software?.Name;
        }

        var response = new RevenueResponseDTO
        {
            CurrentRevenue = currentRevenue,
            PredictedRevenue = predictedRevenue,
            Currency = currency,
            SoftwareId = request.SoftwareId,
            SoftwareName = softwareName
        };

        return Ok(response);
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentRevenue([FromQuery] int? softwareId = null, [FromQuery] string? currency = null)
    {
        if (softwareId.HasValue && !await _revenueService.DoesSoftwareExist(softwareId.Value))
            return NotFound($"Software with ID {softwareId} not found");
        
        var currencyCode = currency?.ToUpper() ?? "PLN";
        if (!string.IsNullOrEmpty(currency) && !IsValidCurrency(currencyCode))
            return BadRequest($"Invalid currency code: {currency}");

        var revenue = await _revenueService.CalculateCurrentRevenue(softwareId, currencyCode);

        return Ok(new
        {
            Revenue = revenue,
            Currency = currencyCode,
            CalculatedAt = DateTime.UtcNow,
            SoftwareId = softwareId
        });
    }

    [HttpGet("predicted")]
    public async Task<IActionResult> GetPredictedRevenue([FromQuery] int? softwareId = null, [FromQuery] string? currency = null)
    {
        if (softwareId.HasValue && !await _revenueService.DoesSoftwareExist(softwareId.Value))
            return NotFound($"Software with ID {softwareId} not found");
        
        var currencyCode = currency?.ToUpper() ?? "PLN";
        if (!string.IsNullOrEmpty(currency) && !IsValidCurrency(currencyCode))
            return BadRequest($"Invalid currency code: {currency}");

        var revenue = await _revenueService.CalculatePredictedRevenue(softwareId, currencyCode);

        return Ok(new
        {
            Revenue = revenue,
            Currency = currencyCode,
            CalculatedAt = DateTime.UtcNow,
            SoftwareId = softwareId
        });
    }

    private static bool IsValidCurrency(string currency)
    {
        var validCurrencies = new HashSet<string>
        {
            "PLN", "USD", "EUR", "GBP", "CHF", "JPY", "CAD", "AUD", "SEK", "NOK", "DKK", "CZK", "HUF"
        };

        return validCurrencies.Contains(currency);
    }
}