using WUG.Web;
using WUG.Database.Models.Economy.Stocks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WUG.Database.Models.Economy.Stocks;

public enum StockTradeType
{
    Buy = 0,
    Sell = 1
}

public class StockTrade
{
    [Key]
    public long Id { get; set; }
    public string Ticker { get; set; }
    public long SecurityId { get; set; }
    public long Amount { get; set; }
    public StockTradeType Type { get; set; }
    public DateTime Time { get; set; }
    public long EntityId { get; set; }

    [NotMapped]
    [JsonIgnore]
    public BaseEntity Entity { get; set; }

    [NotMapped]

    public bool IsCompleted = false;

    [NotMapped]

    public TaskResult? Result = null;

    [NotMapped]

    public bool Force = false;

    public StockTrade(string ticker, long securityId, long amount, StockTradeType type, BaseEntity entity)
    {
        Id = IdManagers.GeneralIdGenerator.Generate();
        Ticker = ticker;
        SecurityId = securityId;
        Amount = amount;
        Type = type;
        Entity = entity;
        EntityId = entity.Id;
        Time = DateTime.UtcNow;
    }

    public async Task<TaskResult> Execute(bool force = false)
    {
        Force = force;
        StockTradeManager.stocktradeQueue.Enqueue(this);

        while (!IsCompleted) await Task.Delay(1);

        return Result!;
    }

    public void NonAsyncExecute(bool force = false)
    {
        Force = force;
        StockTradeManager.stocktradeQueue.Enqueue(this);
    }

    public async Task<TaskResult> ExecuteFromManager(WashedUpDB dbctx)
    {
        var security = DBCache.Get<Security>(SecurityId);
        if (security is null)
            return new(false, $"Could not find security with id {SecurityId}");

        if (Type == StockTradeType.Buy)
            return await security.BuyAsync(Amount, Entity, dbctx, Force);
        else
            return await security.SellAsync(Amount, Entity, dbctx, Force);
    }
}
