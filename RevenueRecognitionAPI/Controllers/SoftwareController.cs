using Microsoft.AspNetCore.Mvc;
using RevenueRecognitionAPI.Attributes;
using RevenueRecognitionAPI.DTOs;
using RevenueRecognitionAPI.Services;

namespace RevenueRecognitionAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[RequireUser]
public class SoftwareController : ControllerBase
{
    private readonly IRevenueService _revenueService;

    public SoftwareController(IRevenueService revenueService)
    {
        _revenueService = revenueService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllSoftware()
    {
        var software = await _revenueService.GetAllSoftware();
        var response = software.Select(s => new SoftwareResponseDTO
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            CurrentVersion = s.CurrentVersion,
            Category = s.Category,
            UpfrontPrice = s.UpfrontPrice
        }).ToList();

        return Ok(response);
    }

    [HttpGet("{softwareId}")]
    public async Task<IActionResult> GetSoftware(int softwareId)
    {
        var software = await _revenueService.GetSoftwareById(softwareId);
        if (software == null)
            return NotFound($"Software with ID {softwareId} not found");

        var response = new SoftwareResponseDTO
        {
            Id = software.Id,
            Name = software.Name,
            Description = software.Description,
            CurrentVersion = software.CurrentVersion,
            Category = software.Category,
            UpfrontPrice = software.UpfrontPrice
        };

        return Ok(response);
    }
}