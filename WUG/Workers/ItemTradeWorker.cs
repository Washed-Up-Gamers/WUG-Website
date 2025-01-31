using WUG.Database;
using WUG.Database.Models.Groups;
using WUG.Database.Models.Economy;
using WUG.Database.Models.Users;
using WUG.Web;

namespace WUG.Workers
{
    public class ItemTradeWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        public readonly ILogger<ItemTradeWorker> _logger;
        private static WashedUpDB dbctx;
        private static DateTime LastTime = DateTime.UtcNow;

        public ItemTradeWorker(ILogger<ItemTradeWorker> logger,
                            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            dbctx = WashedUpDB.DbFactory.CreateDbContext();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Task task = Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            if (!(await ItemTradeManager.Run(dbctx)))
                            {
                                await Task.Delay(10);
                            }
                        }
                        catch(System.Exception e)
                        {
                            Console.WriteLine("FATAL TRANSACTION WORKER ERROR:");
                            Console.WriteLine(e.Message);
                        }
                        if (ItemTradeManager.itemTradeQueue.IsEmpty || (DateTime.UtcNow - LastTime).TotalMinutes >= 1)
                        {
                            await dbctx.SaveChangesAsync();
                            LastTime = DateTime.UtcNow;
                        }
                    }
                });

                while (!task.IsCompleted)
                {
                    //_logger.LogInformation("ITEM TRADE Worker running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(60000);
                }

                _logger.LogInformation("ITEM TRADE Worker task stopped at: {time}", DateTimeOffset.Now);
                _logger.LogInformation("Restarting.", DateTimeOffset.Now);
            }
        }
    }
}