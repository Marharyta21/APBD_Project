using System.ComponentModel.DataAnnotations;

namespace RevenueRecognitionAPI.Models;

public class Employee
{
    [Key]
    public int Id { get; set; }
    [Required]
    [MaxLength(100)]
    public string Login { get; set; } = string.Empty;
    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;
    [Required]
    public EmployeeRole Role { get; set; }
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    [MaxLength(120)]
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum EmployeeRole
{
    StandardUser = 1,
    Admin = 2
}