using Microsoft.AspNetCore.Mvc;
using WUG.Extensions;
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

    [UserRequired]
    public async Task<IActionResult> GiveVouchers()
    {
        return Redirect($"/Group/View");
    }
}
