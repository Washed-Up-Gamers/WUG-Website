using System.Threading.Tasks;
using WUG.Database.Models.Groups;
using WUG.Database.Models.Economy;
using WUG.Database.Models.Factories;
using WUG.Database.Models.Users;
using System.Collections.Concurrent;
using WUG.Database.Models.Items;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using WUG.Web;
using WUG.Database.Models.Economy.Stocks;

namespace WUG.Managers;

public static class StockTradeManager
{
    public static ConcurrentQueue<StockTrade> stocktradeQueue = new();

    // TODO: add support for sending bulk transactions
    static public async Task<bool> Run()
    {
        if (stocktradeQueue.IsEmpty) return false;

        StockTrade trade;
        bool dequeued = stocktradeQueue.TryDequeue(out trade);

        if (!dequeued) return false;

        using var dbctx = WashedUpDB.DbFactory.CreateDbContext();

        TaskResult result = await trade.ExecuteFromManager(dbctx);

        trade.Result = result;

        trade.IsCompleted = true;

        string success = "SUCC";
        if (!result.Succeeded) success = "FAIL";

        Console.WriteLine($"[{success}] Processed {trade.Type} order for {trade.Amount} shares of ${trade.Ticker}. INFO: {result.Info}");

        await dbctx.SaveChangesAsync();

        return true;

        // Notify SignalR
        //string json = JsonConvert.SerializeObject(request);

        //await TransactionHub.Current.Clients.All.SendAsync("NotifyTransaction", json);
    }
}