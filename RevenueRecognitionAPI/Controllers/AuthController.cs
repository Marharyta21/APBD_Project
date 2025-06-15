using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevenueRecognitionAPI.DTOs;
using RevenueRecognitionAPI.Models;
using RevenueRecognitionAPI.Services;

namespace RevenueRecognitionAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IRevenueService _revenueService;

    public AuthController(IRevenueService revenueService)
    {
        _revenueService = revenueService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDTO request)
    {
        var isValid = await _revenueService.ValidateEmployeeCredentials(request.Login, request.Password);
        
        if (!isValid)
        {
            return Ok(new LoginResponseDTO
            {
                Success = false,
                Message = "Invalid login credentials"
            });
        }

        var employee = await _revenueService.GetEmployeeByLogin(request.Login);
        
        return Ok(new LoginResponseDTO
        {
            Success = true,
            Message = "Login successful",
            Employee = new EmployeeDTO
            {
                Id = employee!.Id,
                Login = employee.Login,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Role = employee.Role.ToString()
            }
        });
    }

    [HttpGet("validate/{login}")]
    public async Task<IActionResult> ValidateEmployee(string login)
    {
        var employee = await _revenueService.GetEmployeeByLogin(login);
        
        if (employee == null)
            return NotFound($"Employee with login '{login}' not found");

        return Ok(new EmployeeDTO
        {
            Id = employee.Id,
            Login = employee.Login,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Role = employee.Role.ToString()
        });
    }
}