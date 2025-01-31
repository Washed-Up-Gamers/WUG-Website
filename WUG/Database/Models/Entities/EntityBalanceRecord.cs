using Microsoft.EntityFrameworkCore;

namespace WUG.Database.Models.Entities;

[PrimaryKey(nameof(EntityId), nameof(Time))]
[Index(nameof(Time))]
[Index(nameof(EntityId))]
public class EntityBalanceRecord 
{
    public long EntityId { get; set; }
    public DateTime Time { get; set; }

    [DecimalType(2)]
    public decimal IncomeToday { get; set; }

    [DecimalType(2)]
    public decimal Balance { get; set; }

    [DecimalType(2)]
    /// <summary>
    /// Basically the all-time cumulative "profit" of the entity
    /// </summary>
    public decimal TaxableBalance { get; set; }
}