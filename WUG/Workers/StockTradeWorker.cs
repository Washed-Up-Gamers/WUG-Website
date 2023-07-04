using WUG.Database;
using WUG.Database.Models.Groups;
using WUG.Database.Models.Economy;
using WUG.Database.Models.Users;
using WUG.Web;

namespace WUG.Workers;

public class StockTradeWorker : BackgroundService
{
    public readonly ILogger<StockTradeWorker> _logger;

    public StockTradeWorker(ILogger<StockTradeWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Task task = Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        if (!(await StockTradeManager.Run()))
                        {
                            await Task.Delay(10);
                        }
                    }
                    catch(System.Exception e)
                    {
                        Console.WriteLine("FATAL STOCK TRADE WORKER ERROR:");
                        Console.WriteLine(e.Message);
                    }
                }
            });

            while (!task.IsCompleted)
            {
                _logger.LogInformation("STOCK TRADE Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(60000);
            }

            _logger.LogInformation("STOCK TRADE Worker task stopped at: {time}", DateTimeOffset.Now);
            _logger.LogInformation("Restarting.", DateTimeOffset.Now);
        }
    }
}