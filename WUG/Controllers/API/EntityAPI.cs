using Microsoft.AspNetCore.Mvc;
using WUG.Models;
using System.Diagnostics;
using WUG.Database;
using WUG.Database.Models.Entities;
using Microsoft.AspNetCore.Cors;
using WUG.Extensions;
using Microsoft.EntityFrameworkCore;

namespace WUG.API;

[EnableCors("ApiPolicy")]
public class EntityAPI : BaseAPI
{
    public static void AddRoutes(WebApplication app)
    {
        app.MapGet   ("api/entity/{svid}/name", GetName).RequireCors("ApiPolicy");
        app.MapGet   ("api/entity/{svid}/credits", GetCredits).RequireCors("ApiPolicy");
        app.MapGet   ("api/entities/{svid}", GetEntity).RequireCors("ApiPolicy");
        app.MapGet   ("api/entity/search", Search).RequireCors("ApiPolicy");
        app.MapGet   ("api/entities/{entityid}/taxablebalancehistory", GetTaxableBalanceHistory).RequireCors("ApiPolicy");
        app.MapGet   ("api/entities/{id}/ownedgroups", GetOwnedGroupsAsync).RequireCors("ApiPolicy");
    }

    private static async Task<IResult> GetOwnedGroupsAsync(HttpContext ctx, long id)
    {
        BaseEntity? entity = BaseEntity.Find(id);
        if (entity is null)
            return ValourResult.NotFound($"Could not find entity with id {id}");

        return Results.Json(entity.GetGroupsOwned());
    }

    private static async Task GetTaxableBalanceHistory(HttpContext ctx, WashedUpDB dbctx, long entityid, int days)
    {
        if (days > 90)
            days = 90;
        BaseEntity? entity = BaseEntity.Find(entityid);
        if (entity is null) {
            await ctx.Response.WriteAsJsonAsync(ValourResult.NotFound($"Could not find entity with svid {entityid}"));
            return;
        }

        var records = await dbctx.EntityBalanceRecords.Where(x => x.EntityId == entityid).OrderByDescending(x => x.Time).Take(days).ToListAsync();
        await ctx.Response.WriteAsJsonAsync(records);
    }

    private static async Task GetEntity(HttpContext ctx, long svid)
    {
        BaseEntity? entity = BaseEntity.Find(svid);
        if (entity is null)
            await ctx.Response.WriteAsJsonAsync(ValourResult.NotFound($"Could not find entity with svid {svid}"));

        await ctx.Response.WriteAsJsonAsync(entity);
    }

    private static async Task GetName(HttpContext ctx, long svid)
    {
        BaseEntity? entity = BaseEntity.Find(svid);
        if (entity == null)
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsync($"Could not find entity with svid {svid}");
            return;
        }

        await ctx.Response.WriteAsync(entity.Name);
    }

    private static async Task GetCredits(HttpContext ctx, long svid)
    {
        BaseEntity? account = BaseEntity.Find(svid);
        if (account == null)
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsync($"Could not find entity with svid {svid}");
            return;
        }

        await ctx.Response.WriteAsync((account.Money).ToString());
    }

    private static async Task<IEnumerable<SearchResult>> Search(string name, int amount = 20)
    {
        List<BaseEntity> entities = new();

        // Cap at 20
        if (amount > 20)
            amount = 20;

        if (name == null)
            return new List<SearchResult>();

        name = name.ToLower();

        var users = DBCache.GetAll<User>().Where(x => x.Name.ToLower().Contains(name));
        var groups = DBCache.GetAll<Group>().Where(x => x.Name.ToLower().Contains(name));

        entities.AddRange(users);
        entities.AddRange(groups);

        var top = entities.OrderBy(x => x.Name.ToLower().StartsWith(name.ToLower())).TakeLast(amount).ToList();

        //snaps.Reverse();

        var results = new List<SearchResult>();
        results.AddRange(entities.Select(x => new SearchResult()
        {
            Name = x.Name,
            Id = x.Id.ToString(),
            imageUrl = x.ImageUrl,
            EntityType = x.EntityType
        }));

        return results;
    }
}

public class SearchResult
{
    public string Name { get; set; }
    public string Id { get; set; }
    public string imageUrl { get; set; }
    public EntityType EntityType { get; set; }
}