using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalesDataAnalyzer.Models;

[Table("HourlySales")]
public class HourlySales
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public int SiteId { get; set; }
    
    [Required]
    public int Hour { get; set; }
    
    public decimal ItemCount { get; set; }
    
    public int MerchOnlyCount { get; set; }
    
    public decimal MerchOnlyAmount { get; set; }
    
    public int MerchFuelCount { get; set; }
    
    public decimal MerchFuelAmount { get; set; }
    
    public int FuelOnlyCount { get; set; }
    
    public decimal FuelOnlyAmount { get; set; }
    
    public int? RegisterId { get; set; }
    
    [Required]
    public DateTime PeriodBeginDate { get; set; }
    
    [Required]
    public DateTime PeriodEndDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}