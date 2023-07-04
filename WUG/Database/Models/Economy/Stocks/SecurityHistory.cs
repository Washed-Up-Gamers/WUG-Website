using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WUG.Database.Models.Economy.Stocks;

public enum HistoryType { 
    Minute = 0,
    Hour = 1,
    Day = 2
}

[Index(nameof(Time))]
[Index(nameof(HistoryType))]
[Index(nameof(Id))]
public class SecurityHistory
{
    [Key]
    public long Id { get; set; }
    public long SecurityId { get; set; }
    public long Balance { get; set; }
    public long Shares { get; set; }
    public long OpenShares { get; set; }
    public decimal Price { get; set; }
    public long SellVolumeThisHour { get; set; }
    public long BuyVolumeThisHour { get; set; }
    public long SellWorthTradedThisHour { get; set; }
    public long BuyWorthTradedThisHour { get; set; }
    public DateTime Time { get; set; }
    public HistoryType HistoryType { get; set; }
}
