using System.ComponentModel.DataAnnotations;

namespace RevenueRecognitionAPI.Models;

public abstract class Client
{
    [Key]
    public int Id { get; set; }
    [Required]
    [MaxLength(200)]
    public string Address { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;
    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Contract> Contracts { get; set; } = new HashSet<Contract>();
}

public class IndividualClient : Client
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    [MaxLength(120)]
    public string LastName { get; set; } = string.Empty;
    [Required]
    [MaxLength(11)]
    public string PESEL { get; set; } = string.Empty;
}

public class CompanyClient : Client
{
    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;
    [Required]
    [MaxLength(14)]
    public string KRS { get; set; } = string.Empty;
}