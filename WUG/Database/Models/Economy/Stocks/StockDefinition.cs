using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WUG.Database.Models.Entities;

namespace WUG.Database.Models.Economy.Stocks;

public class StockDefinition
{
    [Key]
    public string Ticker { get; set;}

    // The group that issued this stock
    public long GroupId { get; set; }

    // Current value estimate
    public decimal CurrentValue { get; set; }
}