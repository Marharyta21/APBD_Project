using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RevenueRecognitionAPI.Models;

public class Contract
{
    [Key]
    public int Id { get; set; }
    [Required]
    public DateTime StartDate { get; set; }
    [Required]
    public DateTime EndDate { get; set; }
    [Required]
    [Precision(10, 2)]
    public decimal Price { get; set; }
    [Required]
    [MaxLength(50)]
    public string SoftwareVersion { get; set; } = string.Empty;
    [Required]
    [Range(0, 3)]
    public int AdditionalSupportYears { get; set; } = 0;
    public bool IsSigned { get; set; } = false;
    public bool IsCancelled { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public int ClientId { get; set; }
    [Required]
    public int SoftwareId { get; set; }

    [ForeignKey(nameof(ClientId))]
    public Client Client { get; set; } = null!;
    [ForeignKey(nameof(SoftwareId))]
    public Software Software { get; set; } = null!;

    public ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();

    public bool IsPaymentWindowOpen => DateTime.UtcNow <= EndDate && !IsCancelled;
    public decimal TotalPaid => Payments.Where(p => !p.IsRefunded).Sum(p => p.Amount);
    public bool IsFullyPaid => TotalPaid >= Price;
    public decimal RemainingAmount => Math.Max(0, Price - TotalPaid);
}