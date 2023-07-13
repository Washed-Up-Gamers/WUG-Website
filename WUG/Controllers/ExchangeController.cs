using Microsoft.AspNetCore.Mvc;
using WUG.Extensions;
using WUG.Helpers;
using WUG.Models;
using System.Diagnostics;
using WUG.Models.Global;
using Microsoft.AspNetCore.Mvc.Rendering;
using WUG.Database.Models.Economy;
using WUG.Web;
using WUG.Managers;
using WUG.Database.Models.Economy.Stocks;
using WUG.Models.Exchange;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Principal;
using WUG.Database.Models.Users;

namespace WUG.Controllers;

public class ExchangeController : SVController
{
    private readonly ILogger<ExchangeController> _logger;
    private readonly WashedUpDB _dbctx;

    public ExchangeController(ILogger<ExchangeController> logger, WashedUpDB dbctx)
    {
        _logger = logger;
        _dbctx = dbctx;
    }

    public async Task<IActionResult> Index(int page = 0, string sort = "", long? accountid = null)
    {
        User? user = UserManager.GetUser(HttpContext);
        List<Security> securities = new();
        if (sort == "") sort = "Balance";
        int view = 14;

        if (sort == "Ticker")
            securities = await _dbctx.Securities.OrderByDescending(x => x.Ticker).Skip(page * view).Take(view).ToListAsync();
        else if (sort == "Price")
            securities = await _dbctx.Securities.OrderByDescending(x => x.Price).Skip(page * view).Take(view).ToListAsync();
        else if (sort == "Balance")
            securities = await _dbctx.Securities.OrderByDescending(x => x.Balance).Skip(page * view).Take(view).ToListAsync();

        BaseEntity chosenEntity = null;
        if (accountid is null)
        {
            if (user is not null)
                chosenEntity = user;
        }
        else
        {
            chosenEntity = BaseEntity.Find(accountid);
            if (user is null)
                chosenEntity = null;
            else if (!chosenEntity.HasPermission(user, GroupPermissions.Eco))
                chosenEntity = user;
        }

        return View(new ExchangeIndexModel()
        {
            ChosenAccount = chosenEntity,
            AccountId = chosenEntity.Id,
            Sort = sort,
            Page = page
        });
    }

    [UserRequired]
    public async Task<IActionResult> SelectAccount(long accountid = 0)
    {
        return View(accountid);
    }

    [HttpPost("/Exchange/Buy")]
    [UserRequired]
    [ValidateAntiForgeryToken]
    public async Task<TaskResult> Buy(string ticker, long accountid, long amount)
    {
        var caller = HttpContext.GetUser();

        BaseEntity entity = BaseEntity.Find(accountid);

        if (!entity.HasPermission(caller, GroupPermissions.Eco))
            return new TaskResult(false, $"You lack eco group permission!");

        if (!DBCache.SecuritiesByTicker.ContainsKey(ticker))
            return new TaskResult(false, $"Could not find security with ticker {ticker}");

        var security = DBCache.SecuritiesByTicker[ticker];
        var traderesult = await security.BuyAsync(amount, entity, _dbctx);
        return traderesult;
    }

    [HttpPost("/Exchange/Sell")]
    [UserRequired]
    [ValidateAntiForgeryToken]
    public async Task<TaskResult> Sell(string ticker, long accountid, long amount)
    {
        var caller = HttpContext.GetUser();

        BaseEntity entity = BaseEntity.Find(accountid);

        if (!entity.HasPermission(caller, GroupPermissions.Eco))
            return new TaskResult(false, $"You lack eco group permission!");

        if (!DBCache.SecuritiesByTicker.ContainsKey(ticker))
            return new TaskResult(false, $"Could not find security with ticker {ticker}");

        var security = DBCache.SecuritiesByTicker[ticker];
        var traderesult = await security.SellAsync(amount, entity, _dbctx);
        return traderesult;
    }

    public async Task<IActionResult> Trade(string ticker, long? accountid = null)
    {
        if (ticker != null) ticker = ticker.ToUpper();

        User? user = UserManager.GetUser(HttpContext);

        // Allow an account to be specified
        BaseEntity chosenEntity = null;
        if (accountid is null)
        {
            if (user is not null)
                chosenEntity = user;
        }
        else
        {
            chosenEntity = BaseEntity.Find(accountid);
            if (user is null)
                chosenEntity = null;
            else if (!chosenEntity.HasPermission(user, GroupPermissions.Eco))
                chosenEntity = user;
        }

        DBCache.SecuritiesByTicker.TryGetValue(ticker, out Security security);

        if (security is null)
        {
            StatusMessage = $"Failed to find the ticker {ticker}";
            return RedirectToAction("Index");
        }

        return View(new ExchangeTradeModel() { Chosen_Account = chosenEntity, Security = security });
    }
}