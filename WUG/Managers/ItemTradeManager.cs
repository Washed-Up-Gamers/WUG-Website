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

namespace WUG.Managers;

public static class ItemTradeManager
{
    static public HashSet<long> ActiveSvids = new();

    static public ConcurrentQueue<ItemTrade> itemTradeQueue = new();

    static public async Task<bool> Run(VooperDB dbctx)
    {
        if (itemTradeQueue.IsEmpty) return false;

        ItemTrade trade;
        bool dequeued = itemTradeQueue.TryDequeue(out trade);

        if (!dequeued) return false;

        TaskResult result = await trade.ExecuteFromManager(dbctx, trade.Force);

        trade.Result = result;

        trade.IsCompleted = true;

        string success = "SUCC";
        if (!result.Succeeded) 
            success = "FAIL";

        Console.WriteLine($"[{success}] Processed {trade.Details}");

        return true;

        // Notify SignalR
        //string json = JsonConvert.SerializeObject(request);

        //await TransactionHub.Current.Clients.All.SendAsync("NotifyTransaction", json);
    }
}