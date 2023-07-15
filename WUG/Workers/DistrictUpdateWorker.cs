using WUG.Database;
using WUG.Database.Models.Groups;
using WUG.Database.Models.Economy;
using WUG.Database.Models.Users;
using WUG.Web;
using System.Diagnostics;

namespace WUG.Workers;

public class NationUpdateWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    public readonly ILogger<NationUpdateWorker> _logger;
    private static WashedUpDB dbctx;
    private static DateTime LastTime = DateTime.UtcNow;

    public NationUpdateWorker(ILogger<NationUpdateWorker> logger,
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
                    int times = 0;
                    try
                    {
                        foreach (var nation in DBCache.GetAll<Nation>()) {
                            nation.ProvincesByDevelopmnet = nation.Provinces.OrderByDescending(x => x.DevelopmentValue).ToList();
                            nation.ProvincesByMigrationAttraction = nation.Provinces.OrderByDescending(x => x.MigrationAttraction).ToList();
                            nation.UpdateModifiers();
                        }
                        Stopwatch sw = Stopwatch.StartNew();
                        for (int i = 0; i < 1; i++)
                        {
                            foreach (var province in DBCache.GetAll<Province>())
                            {
                                await province.HourlyTick();
                            }
                        }
                        sw.Stop();
                        Console.WriteLine($"Time took to tick provinces: {(int)(sw.Elapsed.TotalMilliseconds)}ms");

                        sw = Stopwatch.StartNew();
                        foreach(var Nation in DBCache.GetAll<Nation>())
                        {
                            Nation.HourlyTick();
                        }
                        sw.Stop();
                        Console.WriteLine($"Time took to tick Nations: {(int)(sw.Elapsed.TotalMilliseconds)}ms");
                        if (times%168 == 0)
                            Console.WriteLine(times);

                        if (DateTime.UtcNow.Subtract(DBCache.UpdateTimeStuff.LastProvinceSlotGiven).TotalHours >= 2)
                        {
                            DBCache.UpdateTimeStuff.LastProvinceSlotGiven = DateTime.UtcNow;
                            foreach (var nation in DBCache.GetAll<Nation>())
                                nation.ProvinceSlotsLeft += 1;
                        }
                        //await Task.Delay(1000 * 60 * 60);
                        await Task.Delay(1000 * 60 * 60);
                    }
                    catch(System.Exception e)
                    {
                        Console.WriteLine("FATAL Nation UPDATING WORKER ERROR:");
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                        if (e.InnerException is not null) {
                            Console.WriteLine(e.InnerException);
                            Console.WriteLine(e.InnerException.StackTrace);
                        }
                    }
                }
            });

            while (!task.IsCompleted)
            {
                _logger.LogInformation("Nation Updating Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(60000);
            }

            _logger.LogInformation("Nation Updating Worker task stopped at: {time}", DateTimeOffset.Now);
            _logger.LogInformation("Restarting.", DateTimeOffset.Now);
        }
    }
}