using Microsoft.AspNetCore.Mvc;
using WUG.Models;
using System.Diagnostics;
using WUG.Database;
using WUG.Database.Models.Entities;
using Microsoft.AspNetCore.Cors;
using System.Text.Json;

namespace WUG.API;

[EnableCors("ApiPolicy")]
public class NationAPI : BaseAPI
{
    public static void AddRoutes(WebApplication app)
    {
        app.MapGet   ("api/nations/{id}", GetNationAsync).RequireCors("ApiPolicy");
        app.MapGet   ("api/getallprovinces", GetAllProvincesAsync).RequireCors("ApiPolicy");
    }

    private static async Task GetNationAsync(HttpContext ctx, long id)
    {
        await ctx.Response.WriteAsJsonAsync(DBCache.Get<Nation>(id));
    }

    private static async Task GetAllProvincesAsync(HttpContext ctx)
    {
        await ctx.Response.WriteAsJsonAsync(DBCache.GetAll<Province>().Where(x => x.NationId != 100).ToList());
    }
}