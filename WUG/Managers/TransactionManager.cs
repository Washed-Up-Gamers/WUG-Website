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

public static class TransactionManager
{
    static public HashSet<long> ActiveSvids = new();

    static public ConcurrentQueue<Transaction> transactionQueue = new();
    static public WashedUpDB TransactionVooperDB;
    static public long TransactionsProcessed = 0;


    // TODO: add support for sending bulk transactions
    static public async Task<bool> Run(WashedUpDB dbctx)
    {
        if (transactionQueue.IsEmpty) return false;

        Transaction tran;
        bool dequeued = transactionQueue.TryDequeue(out tran);

        if (!dequeued) return false;

        TaskResult result = await tran.ExecuteFromManager(dbctx);

        tran.Result = result;

        tran.IsCompleted = true;

        string success = "SUCC";
        if (!result.Succeeded) success = "FAIL";

        Console.WriteLine($"[{success}] Processed {tran.Details} for {tran.Amount}. INFO: {result.Info}");

        TransactionsProcessed += 1;
        if (TransactionsProcessed % 100 == 0)
            Console.WriteLine($"Transactions Processed: {TransactionsProcessed:n0}");

        return true;

        // Notify SignalR
        //string json = JsonConvert.SerializeObject(request);

        //await TransactionHub.Current.Clients.All.SendAsync("NotifyTransaction", json);
    }
}