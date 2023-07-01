using Microsoft.AspNetCore.Mvc;
using WUG.Models;
using System.Diagnostics;
using WUG.Database;
using WUG.Database.Models.Entities;
using Microsoft.AspNetCore.Cors;

namespace WUG.API;

[EnableCors("ApiPolicy")]
public class ItemDefinitionAPI : BaseAPI
{
    public static void AddRoutes(WebApplication app)
    {
        app.MapGet   ("api/itemdefinitions/all", GetAllAsync).RequireCors("ApiPolicy");
        app.MapGet   ("api/itemdefinitions/{defid}", GetAsync).RequireCors("ApiPolicy");
    }

    private static async Task GetAsync(HttpContext ctx, long defid)
    {
        ItemDefinition? itemdef = DBCache.Get<ItemDefinition>(defid);
        if (itemdef is null)
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsync($"Could not find item definition with id {defid}");
            return;
        }

        await ctx.Response.WriteAsJsonAsync(itemdef);
    }

    private static async Task GetAllAsync(HttpContext ctx)
    {
        var defs = DBCache.GetAll<ItemDefinition>().ToList();
        await ctx.Response.WriteAsJsonAsync(defs);
    }
}