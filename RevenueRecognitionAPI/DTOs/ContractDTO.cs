using System.ComponentModel.DataAnnotations;

namespace RevenueRecognitionAPI.DTOs;

public class CreateContractDTO
{
    [Required]
    public int ClientId { get; set; }
    [Required]
    public int SoftwareId { get; set; }
    [Required]
    [Range(3, 30, ErrorMessage = "Contract duration must be between 3 and 30 days")]
    public int DurationDays { get; set; }
    [Required]
    [MaxLength(50)]
    public string SoftwareVersion { get; set; } = string.Empty;
    [Range(0, 3, ErrorMessage = "Additional support years must be between 0 and 3")]
    public int AdditionalSupportYears { get; set; } = 0;
}

public class ContractResponseDTO
{
    public int Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Price { get; set; }
    public string SoftwareVersion { get; set; } = string.Empty;
    public int AdditionalSupportYears { get; set; }
    public bool IsSigned { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public bool IsPaymentWindowOpen { get; set; }
    
    public ClientSummaryDTO Client { get; set; } = null!;
    public SoftwareSummaryDTO Software { get; set; } = null!;
    public ICollection<PaymentResponseDTO> Payments { get; set; } = new List<PaymentResponseDTO>();
}

public class ProcessPaymentDTO
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than 0")]
    public decimal Amount { get; set; }
}

public class PaymentResponseDTO
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public bool IsRefunded { get; set; }
    public DateTime? RefundDate { get; set; }
}

public class ClientSummaryDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ClientType { get; set; } = string.Empty;
}

public class SoftwareSummaryDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}