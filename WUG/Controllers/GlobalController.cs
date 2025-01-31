﻿using Microsoft.AspNetCore.Mvc;
using WUG.Extensions;
using WUG.Helpers;
using WUG.Models;
using System.Diagnostics;
using WUG.Models.Global;
using Microsoft.AspNetCore.Mvc.Rendering;
using WUG.Database.Models.Economy;
using WUG.Web;
using WUG.Managers;

namespace WUG.Controllers;

public class GlobalController : SVController {
    private readonly ILogger<GlobalController> _logger;

    public GlobalController(ILogger<GlobalController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [UserRequired]
    public async Task<IActionResult> Pay()
    {
        var user = HttpContext.GetUser();

        List<BaseEntity> cansendas = new() { user };
        cansendas.AddRange(DBCache.GetAll<Group>().Where(x => x.HasPermission(user, GroupPermissions.Eco)));
        return View(new PayModel()
        {
            CanSendAs = cansendas.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString(), x.Id == user.Id)).ToList(),
            FromEntityId = user.Id,
            ToEntityId = 0,
            Amount = 0.0m
        });
    }

    [HttpPost]
    [UserRequired]
    public async Task<IActionResult> Pay(PayModel model)
    {
        var user = HttpContext.GetUser();

        var fromentity = BaseEntity.Find(model.FromEntityId);
        var toentity = BaseEntity.Find(model.ToEntityId);

        if (!fromentity.HasPermission(user, GroupPermissions.Eco))
            return RedirectBack($"You lack permission to send transactions!");

        if (model.Amount <= 0)
            return RedirectBack($"Amount must be greater than 0!");

        var tran = new Transaction(fromentity, toentity, model.Amount, TransactionType.Payment, "Payment from /Global/Pay");

        TaskResult result = await tran.Execute();

        return RedirectBack($"{result.Info}");
    }

    [HttpGet]
    [UserRequired]
    public async Task<IActionResult> Trade()
    {
        var user = HttpContext.GetUser();

        List<BaseEntity> cansendas = new() { user };
        cansendas.AddRange(DBCache.GetAll<Group>().Where(x => x.HasPermission(user, GroupPermissions.Resources)));
        return View(new TradeModel()
        {
            CanSendAs = cansendas.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString(), x.Id == user.Id)).ToList(),
            Resources = GameDataManager.Resources.OrderBy(x => x.Value.Name).Select(x => new SelectListItem(x.Value.Name, x.Key)).ToList(),
            FromEntityId = user.Id,
            ToEntityId = 0,
            ResourceId = "",
            Amount = 0.0
        });
    }

    [HttpPost]
    [UserRequired]
    public async Task<IActionResult> Trade(TradeModel model)
    {
        var user = HttpContext.GetUser();

        var fromentity = BaseEntity.Find(model.FromEntityId);
        var toentity = BaseEntity.Find(model.ToEntityId);
        var resource = GameDataManager.Resources.FirstOrDefault(x => x.Key == model.ResourceId).Value;

        if (!fromentity.HasPermission(user, GroupPermissions.Resources))
            return RedirectBack($"You lack permission to send resource trades!");

        if (model.Amount <= 0)
            return RedirectBack($"Amount must be greater than 0!");


        SVItemOwnership? item = DBCache.GetAll<SVItemOwnership>().FirstOrDefault(x => x.OwnerId == fromentity.Id && x.DefinitionId == resource.ItemDefinition.Id);
        if (item is null)
            return RedirectBack("You lack the resources to send this!");

        ItemTrade trade = new()
        {
            Id = IdManagers.GeneralIdGenerator.Generate(),
            Amount = model.Amount,
            FromId = fromentity.Id,
            ToId = toentity.Id,
            Time = DateTime.UtcNow,
            DefinitionId = item.DefinitionId,
            Details = "Resource trade from /Global/Trade",
        };

        TaskResult result = await trade.Execute();

        return RedirectBack($"{result.Info}");
    }
}