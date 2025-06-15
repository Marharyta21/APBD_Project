using System.ComponentModel.DataAnnotations;
using RevenueRecognitionAPI.Models;

namespace RevenueRecognitionAPI.DTOs;

public class RevenueCalculationRequestDTO
{
    public int? SoftwareId { get; set; }
    [MaxLength(3)]
    public string? Currency { get; set; }
}

public class RevenueResponseDTO
{
    public decimal CurrentRevenue { get; set; }
    public decimal PredictedRevenue { get; set; }
    public string Currency { get; set; } = "PLN";
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public int? SoftwareId { get; set; }
    public string? SoftwareName { get; set; }
}

public class SoftwareResponseDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal UpfrontPrice { get; set; }
}

public class LoginRequestDTO
{
    [Required]
    [MaxLength(100)]
    public string Login { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public EmployeeDTO? Employee { get; set; }
}

public class EmployeeDTO
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}