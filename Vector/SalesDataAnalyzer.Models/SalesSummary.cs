using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalesDataAnalyzer.Models;

[Table("SalesSummaries")]
public class SalesSummary
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public int SiteId { get; set; }
    
    public decimal InsideGrandStart { get; set; }
    
    public decimal InsideSalesStart { get; set; }
    
    public decimal OutsideGrandStart { get; set; }
    
    public decimal OutsideSalesStart { get; set; }
    
    public decimal InsideGrandEnd { get; set; }
    
    public decimal InsideSalesEnd { get; set; }
    
    public decimal OutsideGrandEnd { get; set; }
    
    public decimal OutsideSalesEnd { get; set; }
    
    public decimal InsideGrandDifference { get; set; }
    
    public decimal InsideSalesDifference { get; set; }
    
    public decimal OutsideGrandDifference { get; set; }
    
    public decimal OutsideSalesDifference { get; set; }
    
    public decimal NetSales { get; set; }
    
    public int ItemCount { get; set; }
    
    public int CustomerCount { get; set; }
    
    public int NoSaleCount { get; set; }
    
    public decimal FuelSales { get; set; }
    
    public decimal MerchSales { get; set; }
    
    public int? RegisterId { get; set; }
    
    public int? CashierId { get; set; }
    
    [StringLength(50)]
    public string? CashierName { get; set; }
    
    [Required]
    public DateTime PeriodBeginDate { get; set; }
    
    [Required]
    public DateTime PeriodEndDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}