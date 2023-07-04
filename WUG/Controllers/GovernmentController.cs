using Microsoft.AspNetCore.Mvc;
using WUG.Models;
using WUG.Managers;
using WUG.Database.Models.Users;
using System.Diagnostics;
using WUG.Models.Government;
using Microsoft.EntityFrameworkCore;
using WUG.Helpers;

namespace WUG.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class GovernmentController : SVController {
    private readonly ILogger<GovernmentController> _logger;
    
    [TempData]
    public string StatusMessage { get; set; }

    public GovernmentController(ILogger<GovernmentController> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        GovernmentIndexModel model = new GovernmentIndexModel();

        using var dbctx = WashedUpDB.DbFactory.CreateDbContext();
        model.Emperor = DBCache.GetAll<User>().FirstOrDefault(x => x.DiscordUserId == 259004891148582914);
        model.Justices = new();
        foreach (User user in DBCache.GetAll<User>())
        {
            if (await user.IsPrimeMinister())
                model.PrimeMinister = user;
            if (await user.IsSupremeCourtJustice())
                model.Justices.Add(user);
        }
        model.CFV = DBCache.GetAll<User>().FirstOrDefault(x => x.DiscordUserId == 259004891148582914);
        model.Senators = DBCache.GetAll<CouncilMember>().ToList();

        return View(model);
    }

    public async Task<IActionResult> Map()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}