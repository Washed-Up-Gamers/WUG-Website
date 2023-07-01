using Microsoft.AspNetCore.Mvc;
using WUG.Models;
using WUG.Managers;
using WUG.Database.Models.Users;
using System.Diagnostics;
using WUG.Models.Manage;
using Valour.Shared.Models;
using WUG.VoopAI;
using Valour.Shared.Authorization;
using Valour.Api.Client;
using System.Web;
using System.Text.Json;
using WUG.Helpers;
using Valour.Api.Nodes;
using WUG.NonDBO;
using Valour.Shared;

namespace WUG.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class TimeController : SVController 
{
    private readonly ILogger<TimeController> _logger;
    public TimeController(ILogger<TimeController> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        return View(DBCache.CurrentTime);
    }
}