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
using Npgsql;

namespace WUG.Managers;

public static class NotificationManager
{
    public static ConcurrentQueue<Notification> notificationQueue = new();

    static public async Task<bool> Run()
    {
        if (notificationQueue.IsEmpty) return false;

        Notification notification;
        bool dequeued = notificationQueue.TryDequeue(out notification);

        if (!dequeued) return false;

        using var dbctx = WashedUpDB.DbFactory.CreateDbContext();

        dbctx.Notifications.Add(notification);

        await dbctx.SaveChangesAsync();

        return true;

        // Notify SignalR
        //string json = JsonConvert.SerializeObject(request);

        //await TransactionHub.Current.Clients.All.SendAsync("NotifyTransaction", json);
    }
}