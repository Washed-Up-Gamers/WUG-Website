using Microsoft.AspNetCore.Mvc;
using WUG.Models;
using WUG.Managers;
using WUG.Database.Models.Users;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Valour.Api.Models;
using WUG.Models.Leaderboard;
using Microsoft.EntityFrameworkCore;
using WUG.Helpers;

namespace WUG.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class LeaderboardController : SVController 
{
    private readonly ILogger<LeaderboardController> _logger;

    public LeaderboardController(ILogger<LeaderboardController> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Index(int id)
    {
        var model = new LeaderboardIndexModel()
        {
            Users = DBCache.GetAll<SVUser>().OrderByDescending(x => x.Xp).ToList(),
            Page = id,
            Amount = 25
        };

        return View(model);
    }

    public async Task<IActionResult> EconomicScore()
    {
        return View();
    }
}