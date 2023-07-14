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
using WUG.Database.Models.Economy.Stocks;

namespace WUG.Workers;

public class SecurityHistoryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    public readonly ILogger<SecurityHistoryWorker> _logger;

    private readonly WashedUpDB _dbctx;

    public SecurityHistoryWorker(ILogger<SecurityHistoryWorker> logger,
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
                        SecurityHistory? lasthistory = await _dbctx.SecurityHistories.Where(x => x.HistoryType == HistoryType.Minute).OrderByDescending(x => x.Time).FirstOrDefaultAsync();
                        if (lasthistory is null || DateTime.UtcNow.Subtract(lasthistory.Time).TotalSeconds >= 60)
                        {
                            var histories = new List<SecurityHistory>();
                            foreach (var security in DBCache.GetAll<Security>())
                            {
                                histories.Add(new()
                                {
                                    Id = IdManagers.SecurityHistoryIdGenerator.Generate(),
                                    SecurityId = security.Id,
                                    Balance = (long)security.Balance,
                                    Shares = security.Shares,
                                    OpenShares = security.OpenShares,
                                    Price = security.Price,
                                    SellVolumeThisHour = security.SellVolumeThisHour,
                                    BuyVolumeThisHour = security.BuyVolumeThisHour,
                                    SellWorthTradedThisHour = security.SellWorthTradedThisHour,
                                    BuyWorthTradedThisHour = security.BuyWorthTradedThisHour,
                                    Time = DateTime.UtcNow,
                                    HistoryType = HistoryType.Minute
                                });
                            }
                            _dbctx.SecurityHistories.AddRange(histories);
                        }

                        lasthistory = await _dbctx.SecurityHistories.Where(x => x.HistoryType == HistoryType.Hour).OrderByDescending(x => x.Time).FirstOrDefaultAsync();
                        if (lasthistory is null || DateTime.UtcNow.Subtract(lasthistory.Time).TotalMinutes >= 60)
                        {
                            var histories = new List<SecurityHistory>();
                            foreach (var security in DBCache.GetAll<Security>())
                            {
                                histories.Add(new()
                                {
                                    Id = IdManagers.SecurityHistoryIdGenerator.Generate(),
                                    SecurityId = security.Id,
                                    Balance = (long)security.Balance,
                                    Shares = security.Shares,
                                    OpenShares = security.OpenShares,
                                    Price = security.Price,
                                    SellVolumeThisHour = security.SellVolumeThisHour,
                                    BuyVolumeThisHour = security.BuyVolumeThisHour,
                                    SellWorthTradedThisHour = security.SellWorthTradedThisHour,
                                    BuyWorthTradedThisHour = security.BuyWorthTradedThisHour,
                                    Time = DateTime.UtcNow,
                                    HistoryType = HistoryType.Hour
                                });
                            }
                            _dbctx.SecurityHistories.AddRange(histories);
                        }

                        lasthistory = await _dbctx.SecurityHistories.Where(x => x.HistoryType == HistoryType.Day).OrderByDescending(x => x.Time).FirstOrDefaultAsync();
                        if (lasthistory is null || DateTime.UtcNow.Subtract(lasthistory.Time).TotalHours >= 24)
                        {
                            var histories = new List<SecurityHistory>();
                            foreach (var security in DBCache.GetAll<Security>())
                            {
                                histories.Add(new()
                                {
                                    Id = IdManagers.SecurityHistoryIdGenerator.Generate(),
                                    SecurityId = security.Id,
                                    Balance = (long)security.Balance,
                                    Shares = security.Shares,
                                    OpenShares = security.OpenShares,
                                    Price = security.Price,
                                    SellVolumeThisHour = security.SellVolumeThisHour,
                                    BuyVolumeThisHour = security.BuyVolumeThisHour,
                                    SellWorthTradedThisHour = security.SellWorthTradedThisHour,
                                    BuyWorthTradedThisHour = security.BuyWorthTradedThisHour,
                                    Time = DateTime.UtcNow,
                                    HistoryType = HistoryType.Day
                                });
                            }
                            _dbctx.SecurityHistories.AddRange(histories);
                        }

                        await _dbctx.SaveChangesAsync();


                        //await Task.Delay(1000 * 60 * 60);
                        await Task.Delay(1000 * 20);
                    }
                    catch(System.Exception e)
                    {
                        Console.WriteLine("FATAL SECURITY HISTORY WORKER ERROR:");
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

            _logger.LogInformation("Security History Worker task stopped at: {time}", DateTimeOffset.Now);
            _logger.LogInformation("Restarting.", DateTimeOffset.Now);
        }
    }
}