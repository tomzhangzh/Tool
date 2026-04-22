using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalesDataAnalyzer.Models;

[Table("SalesCategories")]
public class SalesCategory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public int SiteId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string CategoryName { get; set; } = string.Empty;
    
    public int CategorySysId { get; set; }
    
    public int NetSalesCount { get; set; }
    
    public decimal NetSalesAmount { get; set; }
    
    public decimal NetSalesItemCount { get; set; }
    
    public decimal PercentOfSales { get; set; }
    
    [Required]
    public DateTime PeriodBeginDate { get; set; }
    
    [Required]
    public DateTime PeriodEndDate { get; set; }
    
    public int? RegisterId { get; set; }
    
    public int? CashierId { get; set; }
    
    [StringLength(50)]
    public string? CashierName { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}