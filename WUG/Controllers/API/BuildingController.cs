using Microsoft.AspNetCore.Mvc;
using WUG.Models;
using System.Diagnostics;
using WUG.Database;
using WUG.Database.Models.Entities;
using Microsoft.AspNetCore.Cors;

namespace WUG.API;

[EnableCors("ApiPolicy")]
public class BuildingAPI : BaseAPI
{
    public static void AddRoutes(WebApplication app)
    {
        app.MapGet   ("api/buildings/{ownerid}", GetAsync).RequireCors("ApiPolicy");
    }

    private static async Task<IResult> GetAsync(HttpContext ctx, long ownerid)
    {
        var buildings = DBCache.GetAllProducingBuildings().Where(x => x.OwnerId == ownerid).ToList();

        return Results.Json(buildings);
    }
}