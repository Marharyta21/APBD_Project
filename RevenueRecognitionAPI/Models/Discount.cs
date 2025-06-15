using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RevenueRecognitionAPI.Models;

public class Discount
{
    [Key]
    public int Id { get; set; }
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required]
    [Range(0.01, 100.0)]
    [Precision(5, 2)]
    public decimal Percentage { get; set; }
    [Required]
    public DateTime StartDate { get; set; }
    [Required]
    public DateTime EndDate { get; set; }
    public int? SoftwareId { get; set; }

    [ForeignKey(nameof(SoftwareId))]
    public Software? Software { get; set; }

    public bool IsActiveAt(DateTime date)
    {
        return date >= StartDate && date <= EndDate;
    }
}