using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RevenueRecognitionAPI.Models;

public class Payment
{
    [Key]
    public int Id { get; set; }
    [Required]
    [Precision(10, 2)]
    public decimal Amount { get; set; }
    [Required]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public bool IsRefunded { get; set; } = false;
    public DateTime? RefundDate { get; set; }
    
    [Required]
    public int ContractId { get; set; }

    [ForeignKey(nameof(ContractId))]
    public Contract Contract { get; set; } = null!;
}