using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalesDataAnalyzer.Models;

[Table("PaymentMethods")]
public class PaymentMethod
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public int SiteId { get; set; }
    
    [Required]
    public int PaymentSysId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string PaymentName { get; set; } = string.Empty;
    
    public bool IsCardBased { get; set; }
    
    public int SaleCount { get; set; }
    
    public decimal SaleAmount { get; set; }
    
    public int CancelRefundCount { get; set; }
    
    public decimal CancelRefundAmount { get; set; }
    
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