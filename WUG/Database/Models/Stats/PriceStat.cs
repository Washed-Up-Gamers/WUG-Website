using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class PriceStat 
{
    [Key]
    public long Id { get; set; }
    
    [Column(TypeName = "jsonb")]
    public Dictionary<long, decimal> ResourcePrices { get; set; }

    public decimal PPI { get; set; }
    public DateTime Time { get; set; }
}