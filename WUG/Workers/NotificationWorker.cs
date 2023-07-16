using WUG.Database;
using WUG.Database.Models.Groups;
using WUG.Database.Models.Economy;
using WUG.Database.Models.Users;
using WUG.Web;

namespace WUG.Workers;

public class NotificationWorker : BackgroundService
{
    public readonly ILogger<NotificationWorker> _logger;

    public NotificationWorker(ILogger<NotificationWorker> logger)
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
                        if (!(await NotificationManager.Run()))
                        {
                            await Task.Delay(50);
                        }
                    }
                    catch(System.Exception e)
                    {
                        Console.WriteLine("FATAL NOTIFICATION WORKER WORKER ERROR:");
                        Console.WriteLine(e.Message);
                    }
                }
            });

            while (!task.IsCompleted)
            {
                await Task.Delay(60000);
            }

            _logger.LogInformation("NOTIFICATION Worker task stopped at: {time}", DateTimeOffset.Now);
            _logger.LogInformation("Restarting.", DateTimeOffset.Now);
        }
    }
}