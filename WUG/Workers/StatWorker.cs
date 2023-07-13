using IdGen;
using Microsoft.EntityFrameworkCore;
using WUG.Database;
using WUG.Database.Managers;
using WUG.Database.Models.Economy;
using WUG.Database.Models.Stats;
using WUG.Database.Models.Users;
using WUG.Web;
using System.Data;
using System.Diagnostics;
using TargetType = WUG.Database.Models.Stats.TargetType;

namespace WUG.Workers;

public class StatWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    public readonly ILogger<StatWorker> _logger;

    private readonly WashedUpDB _dbctx;

    public StatWorker(ILogger<StatWorker> logger,
                        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _dbctx = WashedUpDB.DbFactory.CreateDbContext();
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
                        var laststat = await _dbctx.Stats.OrderByDescending(x => x.Id).FirstOrDefaultAsync();
                        if (laststat is not null)
                        {
                            while (DateTime.UtcNow.Subtract(laststat.Date).TotalMinutes < 60)
                            {
                                await Task.Delay(60_000);
                            }
                        }

                        foreach (var user in DBCache.GetAll<User>())
                        {
                            _dbctx.Stats.Add(new Stat()
                            {
                                Date = DateTime.UtcNow,
                                Id = IdManagers.StatIdGenerator.Generate(),
                                TargetType = TargetType.User,
                                StatType = StatType.Xp,
                                Value = (long)user.Xp,
                                TargetId = user.Id
                            });
                        }

                        _dbctx.Stats.Add(new Stat() { 
                            Date = DateTime.UtcNow, 
                            Id = IdManagers.StatIdGenerator.Generate(), 
                            TargetType = TargetType.Global, 
                            StatType = StatType.Population,
                            Value = DBCache.GetAll<Province>().Where(x => x.NationId != 100).Sum(x => x.Population)});

                        _dbctx.Stats.Add(new Stat()
                        {
                            Date = DateTime.UtcNow,
                            Id = IdManagers.StatIdGenerator.Generate(),
                            TargetType = TargetType.Global,
                            StatType = StatType.UsedBuildingSlots,
                            Value = DBCache.GetAll<Province>().Where(x => x.NationId != 100).Sum(x => x.BuildingSlotsUsed)
                        });

                        _dbctx.Stats.Add(new Stat()
                        {
                            Date = DateTime.UtcNow,
                            Id = IdManagers.StatIdGenerator.Generate(),
                            TargetType = TargetType.Global,
                            StatType = StatType.TotalBuildingSlots,
                            Value = DBCache.GetAll<Province>().Where(x => x.NationId != 100).Sum(x => x.BuildingSlots)
                        });

                        foreach (var district in DBCache.GetAll<Nation>())
                        {
                            _dbctx.Stats.Add(new Stat()
                            {
                                Date = DateTime.UtcNow,
                                Id = IdManagers.StatIdGenerator.Generate(),
                                TargetType = TargetType.District,
                                StatType = StatType.TotalBuildingSlots,
                                Value = district.Provinces.Sum(x => x.BuildingSlots),
                                TargetId = district.Id
                            });
                            _dbctx.Stats.Add(new Stat()
                            {
                                Date = DateTime.UtcNow,
                                Id = IdManagers.StatIdGenerator.Generate(),
                                TargetType = TargetType.District,
                                StatType = StatType.UsedBuildingSlots,
                                Value = district.Provinces.Sum(x => x.BuildingSlotsUsed),
                                TargetId = district.Id
                            });
                            _dbctx.Stats.Add(new Stat()
                            {
                                Date = DateTime.UtcNow,
                                Id = IdManagers.StatIdGenerator.Generate(),
                                TargetType = TargetType.District,
                                StatType = StatType.Population,
                                Value = district.Provinces.Sum(x => x.Population),
                                TargetId = district.Id
                            });
                        }

                        await _dbctx.SaveChangesAsync();


                        //await Task.Delay(1000 * 60 * 60);
                        await Task.Delay(1000 * 60 * 60);
                    }
                    catch(System.Exception e)
                    {
                        Console.WriteLine("FATAL ECONOMY WORKER ERROR:");
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                        if (e.InnerException is not null)
                            Console.WriteLine(e.InnerException);
                    }
                }
            });

            while (!task.IsCompleted)
            {
                await Task.Delay(60_000, stoppingToken);
            }

            _logger.LogInformation("Economy Worker task stopped at: {time}", DateTimeOffset.Now);
            _logger.LogInformation("Restarting.", DateTimeOffset.Now);
        }
    }
}