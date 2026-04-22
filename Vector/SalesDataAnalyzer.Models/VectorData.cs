using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalesDataAnalyzer.Models;

[Table("VectorData")]
public class VectorData
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public int SiteId { get; set; }
    
    [Required]
    public DateTime PeriodDate { get; set; }
    
    [Required]
    [StringLength(100)]
    public string DataType { get; set; } = string.Empty;
    
    public string MetadataJson { get; set; } = string.Empty;
    
    public string ChromaDocumentId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}