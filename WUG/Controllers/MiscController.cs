using Microsoft.AspNetCore.Mvc;
using WUG.Helpers;

namespace WUG.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class MiscController : SVController
{
    public IActionResult NetResourcesForRecipes()
    {
        return View();
    }
    public IActionResult TechTree()
    {
        return View();
    }
    public async Task<IActionResult> BlazorMapTest()
    {
        return View();
    }
}
