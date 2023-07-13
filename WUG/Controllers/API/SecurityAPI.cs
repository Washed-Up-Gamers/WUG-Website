using Microsoft.AspNetCore.Mvc;
using WUG.Models;
using System.Diagnostics;
using WUG.Database;
using WUG.Database.Models.Entities;
using Microsoft.AspNetCore.Cors;
using System.Text.Json;
using WUG.Helpers;
using WUG.Extensions;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using WUG.Database.Models.Economy.Stocks;
using System.Text;
using WUG.Web;

namespace WUG.API;

[EnableCors("ApiPolicy")]
public class SecurityAPI : BaseAPI
{
    public static void AddRoutes(WebApplication app)
    {
        app.MapGet   ("api/securities/{ticker}/ownership", GetOwnershipAsync).RequireCors("ApiPolicy");
        app.MapGet   ("api/securities/{ticker}/history", GetHistoryAsync).RequireCors("ApiPolicy");
        app.MapGet   ("api/securities/buy", BuyAsync).RequireCors("ApiPolicy");
        app.MapGet   ("api/securities/sell", SellAsync).RequireCors("ApiPolicy");
        app.MapGet   ("api/securities/calcbuytotal", CalcBuyTotalAsync).RequireCors("ApiPolicy");
        app.MapGet   ("api/securities/calcselltotal", CalcSellTotalAsync).RequireCors("ApiPolicy");
    }

    private static async Task CalcBuyTotalAsync(HttpContext ctx, string ticker, long amount)
    {
        if (!DBCache.SecuritiesByTicker.ContainsKey(ticker))
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsJsonAsync(new TaskResult(false, $"Could not find security with ticker {ticker}"));
            return;
        }
        var security = DBCache.SecuritiesByTicker[ticker];
        await ctx.Response.WriteAsync(Math.Round(security.GetBuyPrice(amount).total, 2).ToString());
    }

    private static async Task CalcSellTotalAsync(HttpContext ctx, string ticker, long amount)
    {
        if (!DBCache.SecuritiesByTicker.ContainsKey(ticker))
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsJsonAsync(new TaskResult(false, $"Could not find security with ticker {ticker}"));
            return;
        }
        var security = DBCache.SecuritiesByTicker[ticker];
        await ctx.Response.WriteAsync(Math.Round(security.GetSellPrice(amount).total, 2).ToString());
    }

    private static async Task BuyAsync(HttpContext ctx, WashedUpDB dbctx, string ticker, long accountid, long amount, string apikey)
    {
        BaseEntity caller = await BaseEntity.FindByApiKey(apikey, dbctx);

        BaseEntity entity = BaseEntity.Find(accountid);

        if (!entity.HasPermission(caller, GroupPermissions.Eco))
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsJsonAsync(new TaskResult(false, $"You lack eco group permission!"));
            return;
        }

        if (!DBCache.SecuritiesByTicker.ContainsKey(ticker))
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsJsonAsync(new TaskResult(false, $"Could not find security with ticker {ticker}"));
            return;
        }
        var security = DBCache.SecuritiesByTicker[ticker];
        var traderesult = await security.BuyAsync(amount, entity, dbctx);
        await ctx.Response.WriteAsJsonAsync(traderesult);
    }

    private static async Task SellAsync(HttpContext ctx, WashedUpDB dbctx, string ticker, long accountid, long amount, string apikey)
    {
        BaseEntity caller = await BaseEntity.FindByApiKey(apikey, dbctx);

        BaseEntity entity = BaseEntity.Find(accountid);

        if (!entity.HasPermission(caller, GroupPermissions.Eco))
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsJsonAsync(new TaskResult(false, $"You lack eco group permission!"));
            return;
        }

        if (!DBCache.SecuritiesByTicker.ContainsKey(ticker))
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsJsonAsync(new TaskResult(false, $"Could not find security with ticker {ticker}"));
            return;
        }
        var security = DBCache.SecuritiesByTicker[ticker];
        var traderesult = await security.SellAsync(amount, entity, dbctx);
        await ctx.Response.WriteAsJsonAsync(traderesult);
    }

    private static async Task GetOwnershipAsync(HttpContext ctx, WashedUpDB dbctx, string ticker, long accountid)
    {
        if (!DBCache.SecuritiesByTicker.ContainsKey(ticker))
        {
            await NotFound($"Could not find a security with ticker ${ticker}", ctx);
            return;
        }
        var security = DBCache.SecuritiesByTicker[ticker];
        var ownership = await dbctx.SecurityOwnerships.FirstOrDefaultAsync(x => x.OwnerId == accountid && x.SecurityId == security.Id);
        await ctx.Response.WriteAsync(ownership is not null ? ownership.Amount.ToString() : "0");
    }

    private static async Task GetHistoryAsync(HttpContext ctx, WashedUpDB dbctx, string ticker, bool includetime, int count, bool gethours = false)
    {
        if (!DBCache.SecuritiesByTicker.ContainsKey(ticker))
        {
            await NotFound($"Could not find a security with ticker ${ticker}", ctx);
            return;
        }
        var security = DBCache.SecuritiesByTicker[ticker];
        if (count > 3000)
            count = 3000;
        count -= 1;
        List<List<object>> data = new();
        if (!gethours)
        {
            var history = await dbctx.SecurityHistories.Where(x => x.SecurityId == security.Id && x.HistoryType == HistoryType.Minute).OrderByDescending(x => x.Time).Take(count).ToListAsync();
            int i = 1;
            foreach (var obj in history)
            {
                data.Add(new() { 0 - i, Math.Round(obj.Price, 4) });
                i += 1;
            }
        }
        else
        {
            var history = await dbctx.SecurityHistories.Where(x => x.SecurityId == security.Id && x.HistoryType == HistoryType.Hour).OrderByDescending(x => x.Time).Take(count).ToListAsync();
            int i = 1;
            foreach (var obj in history)
            {
                data.Add(new() { 0 - i, Math.Round(obj.Price, 4) });
                i += 1;
            }
        }

        data.Reverse();
        data.Add(new() { 0, Math.Round(security.Price, 4) });

        JsonSerializerOptions options = new() { IncludeFields = true };
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(data, options: options));
    }

}