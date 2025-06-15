using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace RevenueRecognitionAPI.Models;

public class Software
{
    [Key]
    public int Id { get; set; }
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    [Required]
    [MaxLength(50)]
    public string CurrentVersion { get; set; } = string.Empty;
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;
    [Required]
    [Precision(10, 2)]
    public decimal UpfrontPrice { get; set; }

    public ICollection<Contract> Contracts { get; set; } = new HashSet<Contract>();
    public ICollection<Discount> Discounts { get; set; } = new HashSet<Discount>();
}