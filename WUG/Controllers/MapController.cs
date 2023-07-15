using Microsoft.AspNetCore.Mvc;
using WUG.Models.Map;
using System.Text.Json.Serialization;
using System.Xml;
using System.Text;
using System.Text.Json;
using WUG.NonDBO;
using IdGen;
using WUG.Database.Managers;
using WUG.Helpers;

[ApiExplorerSettings(IgnoreApi = true)]
public class MapController : SVController {
    private readonly ILogger<MapController> _logger;

    public static List<MapState> MapStates = new List<MapState>();

    public static List<NationMap> NationMaps = new();

    [TempData]
    public string StatusMessage { get; set; }

    public MapController(ILogger<MapController> logger)
    {
        _logger = logger;
    }

    public IActionResult World()
    {
        return View(MapStates);
    }
}