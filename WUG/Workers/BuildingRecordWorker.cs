using WUG.Database;
using WUG.Database.Models.Groups;
using WUG.Database.Models.Economy;
using WUG.Database.Models.Users;
using WUG.Web;
using System.Collections.Concurrent;

namespace WUG.Workers;

public class BuildingRecordWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    public readonly ILogger<BuildingRecordWorker> _logger;
    private static WashedUpDB _dbctx;
    private static DateTime LastTime = DateTime.UtcNow;
    public static ConcurrentQueue<BuildingRecord> recordQueue = new();

    public BuildingRecordWorker(ILogger<BuildingRecordWorker> logger,
                        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _dbctx = WashedUpDB.DbFactory.CreateDbContext();
    }

    public bool ProcessRecord()
    {
        if (recordQueue.IsEmpty) return false;

        bool dequeued = recordQueue.TryDequeue(out var record);

        _dbctx.BuildingRecords.Add(record);

        if (!dequeued) return false;

        return true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Task task = Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    bool ProcessedRecord = false;
                    try
                    {
                        if (!(ProcessRecord()))
                        {
                            await Task.Delay(100);
                        }
                        else {
                            ProcessedRecord = true;
                        }
                    }
                    catch(System.Exception e)
                    {
                        Console.WriteLine("FATAL BUILDING RECORD WORKER ERROR:");
                        Console.WriteLine(e.Message);
                    }
                    if ((ProcessedRecord && recordQueue.IsEmpty) || (DateTime.UtcNow - LastTime).TotalSeconds >= 10)
                    {
                        await _dbctx.SaveChangesAsync();
                        LastTime = DateTime.UtcNow;
                    }
                }
            });

            while (!task.IsCompleted)
            {
                //_logger.LogInformation("ITEM TRADE Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(60000);
            }

            _logger.LogInformation("Building Record Worker task stopped at: {time}", DateTimeOffset.Now);
            _logger.LogInformation("Restarting.", DateTimeOffset.Now);
        }
    }
}