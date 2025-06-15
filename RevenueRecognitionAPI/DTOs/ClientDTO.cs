using System.ComponentModel.DataAnnotations;

namespace RevenueRecognitionAPI.DTOs;

public abstract class CreateClientDTO
{
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
}

public class CreateIndividualClientDTO : CreateClientDTO
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    [MaxLength(120)]
    public string LastName { get; set; } = string.Empty;
    [Required]
    [MaxLength(11)]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "PESEL must be exactly 11 digits")]
    public string PESEL { get; set; } = string.Empty;
}

public class CreateCompanyClientDTO : CreateClientDTO
{
    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;
    [Required]
    [MaxLength(14)]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "KRS must be exactly 10 digits")]
    public string KRS { get; set; } = string.Empty;
}

public abstract class UpdateClientDTO
{
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
}

public class UpdateIndividualClientDTO : UpdateClientDTO
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    [MaxLength(120)]
    public string LastName { get; set; } = string.Empty;
}

public class UpdateCompanyClientDTO : UpdateClientDTO
{
    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;
}

public abstract class ClientResponseDTO
{
    public int Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string ClientType { get; set; } = string.Empty;
}

public class IndividualClientResponseDTO : ClientResponseDTO
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PESEL { get; set; } = string.Empty;
}

public class CompanyClientResponseDTO : ClientResponseDTO
{
    public string CompanyName { get; set; } = string.Empty;
    public string KRS { get; set; } = string.Empty;
}