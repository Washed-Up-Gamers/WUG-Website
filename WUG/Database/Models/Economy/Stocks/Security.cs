using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WUG.Web;
using SV2.Hubs;
using SV2.Models.Exchange;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Linq;
using WUG.Database.Models.Corporations;
using static MudBlazor.Colors;

namespace WUG.Database.Models.Economy.Stocks;

public class Security
{
    [Key]
    public long Id { get; set; }
    public decimal Balance { get; set; }
    public long Shares { get; set; }
    public long OpenShares { get; set; }

    [DecimalType(8)]
    public decimal Price { get; set; }
    public string? Resource { get; set; }

    // if set, means that this security's shares have voting power
    public long? CorporationShareClassId { get; set; }
    public long? CorporationId { get; set; }
    public long? GroupId { get; set; }
    public string Ticker { get; set; }
    public string? Description { get; set; }
    public long SellVolumeThisHour { get; set; }
    public long BuyVolumeThisHour { get; set; }
    public long SellWorthTradedThisHour { get; set; }
    public long BuyWorthTradedThisHour { get; set; }

    [NotMapped]
    public CorporationShareClass? CorporationShareClass => DBCache.Get<CorporationShareClass>(CorporationShareClassId);

    [NotMapped]
    public long VolumeThisHour
    {
        get
        {
            return BuyVolumeThisHour + SellVolumeThisHour;
        }
    }

    [NotMapped]
    public long WorthTradedThisHour
    {
        get
        {
            return BuyWorthTradedThisHour + SellWorthTradedThisHour;
        }
    }

    public async Task<decimal> GetChangeFrom7d(WashedUpDB dbctx)
    {
        var price7dhago = (await dbctx.SecurityHistories.Where(x => x.SecurityId == Id && x.HistoryType == HistoryType.Hour).OrderByDescending(x => x.Time).Take(168).FirstOrDefaultAsync())?.Price ?? Price;
        var change = Math.Round(Price, 2) - Math.Round(price7dhago, 2);
        return change / Math.Round(price7dhago, 2);
    }

    public async Task<decimal> GetChangeFrom24h(WashedUpDB dbctx)
    {
        var price24hago = (await dbctx.SecurityHistories.Where(x => x.SecurityId == Id && x.HistoryType == HistoryType.Hour).OrderByDescending(x => x.Time).Take(24).LastOrDefaultAsync())?.Price ?? Price;
        var change = Math.Round(Price, 2) - Math.Round(price24hago, 2);
        return change / Math.Round(price24hago, 2);
    }

    public async Task<TaskResult> SellAsync(long amount, BaseEntity entity, WashedUpDB dbctx, bool Force = false)
    {
        if (amount <= 0)
            return new(false, "Amount must above 0!");

        var ownership = await dbctx.SecurityOwnerships.FirstOrDefaultAsync(x => x.OwnerId == entity.Id && x.SecurityId == Id);
        if (ownership is null || ownership.Amount < amount)
            return new(false, "You can not sell more shares than you own!");

        var priceresult = GetSellPrice(amount);
        OpenShares += amount;
        SellVolumeThisHour += amount;
        SellWorthTradedThisHour += (long)priceresult.total;
        Balance -= priceresult.total;

        ownership.Amount -= amount;

        Price = Balance / Shares * 5;

        var tran = new Transaction(DBCache.FindEntity(99), entity, priceresult.total, TransactionType.StockSale, $"{entity.Name}: sold {amount} of {Ticker}");
        tran.NonAsyncExecute(true);

        await dbctx.SaveChangesAsync();

        var modal = new StockTradeModel()
        {
            Ticker = Ticker,
            Amount = amount,
            TotalShares = Shares,
            SharesAvailable = OpenShares,
            Price = Price,
            StockBalance = Balance,
            EntityId = entity.Id.ToString(),
            Type = "Sell"
        };

        VoopAI.EcoChannel.SendMessageAsync($":chart_with_downwards_trend: {amount} {Ticker} @ ${Math.Round(priceresult.total / amount, 2)} has been sold by {entity.Name}");

        await ExchangeHub.Current.Clients.All.SendAsync("StockTrade", JsonConvert.SerializeObject(modal));

        return new(true, $"Successfully sold {amount} shares of {Ticker} for ${Math.Round(priceresult.total, 2)}");
    }

    public async Task<TaskResult> BuyAsync(long amount, BaseEntity entity, WashedUpDB dbctx, bool Force = false)
    {
        if (amount <= 0)
            return new(false, "Amount must above 0!");

        if (OpenShares < amount)
            return new(false, "Not enough stock available!");

        var priceresult = GetBuyPrice(amount);

        if (entity.Money < priceresult.total && !Force)
            return new(false, $"You lack the funds to buy {amount} of {Ticker}! You need ${Math.Round(priceresult.total - entity.Money, 2)} more!");

        var tran = new Transaction(entity, DBCache.FindEntity(99), priceresult.total, TransactionType.StockBrought, $"{entity.Name}: bought {amount} of {Ticker}");
        tran.NonAsyncExecute(true);

        OpenShares -= amount;
        BuyVolumeThisHour += amount;
        BuyWorthTradedThisHour += (long)priceresult.total;
        Balance += priceresult.total;

        var ownership = await dbctx.SecurityOwnerships.FirstOrDefaultAsync(x => x.SecurityId == Id && x.OwnerId == entity.Id);
        if (ownership is null)
        {
            ownership = new()
            {
                Id = IdManagers.GeneralIdGenerator.Generate(),
                OwnerId = entity.Id,
                SecurityId = Id,
                Amount = amount
            };
            dbctx.SecurityOwnerships.Add(ownership);
        }
        else
            ownership.Amount += amount;

        Price = Balance / Shares * 5;

        await dbctx.SaveChangesAsync();

        var modal = new StockTradeModel()
        {
            Ticker = Ticker,
            Amount = amount,
            SharesAvailable = OpenShares,
            TotalShares = Shares,
            Price = Price,
            StockBalance = Balance,
            EntityId = entity.Id.ToString(),
            Type = "Buy"
        };

        VoopAI.EcoChannel.SendMessageAsync($":chart_with_upwards_trend: {amount} {Ticker} @ ${Math.Round(priceresult.total/amount, 2)} has been bought by {entity.Name}");

        await ExchangeHub.Current.Clients.All.SendAsync("StockTrade", JsonConvert.SerializeObject(modal));

        return new(true, $"Successfully bought {amount} shares of {Ticker} for ${Math.Round(priceresult.total, 2)}");
    }

    public (decimal price, decimal total) GetSellPrice(long amount)
    {
        decimal p1 = GetBuyPrice(1).price;
        decimal p2 = GetBuyPrice(2).price;
        decimal price = Price;
        decimal balance = Balance;
        decimal muit = (Shares * p1) / ((Shares * p2) - p2);
        for (int i = 0; i < amount; i++)
        {
            balance -= (price + (price * (1 - muit)));
            price = balance / Shares * 5;
        }
        return (price, (Balance - balance)*0.9985m);
    }

    public (decimal price, decimal total) GetBuyPrice(long amount)
    {
        // convert to long because decimal is slow as a snail
        long price = (long)(Price * 1_000_000);
        long total = 0;
        long balance = (long)(Balance * 1_000_000);
        for (int i = 0; i < amount; i++)
        {
            balance += price;
            total += price;
            price = balance / Shares * 5; // apply 5x to make prices move more
        }

        return (((decimal)price) / 1_000_000.0m, (((decimal)total) / 1_000_000.0m) * 1.0015m);
    }
}
