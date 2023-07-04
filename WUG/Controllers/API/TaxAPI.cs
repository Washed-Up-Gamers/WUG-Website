using Microsoft.AspNetCore.Mvc;
using WUG.Models;
using System.Diagnostics;
using WUG.Database;
using WUG.Database.Models.Entities;
using Microsoft.AspNetCore.Cors;
using System.Text.Json;
using WUG.Helpers;
using WUG.Extensions;
using System.Text.RegularExpressions;

namespace WUG.API;

[EnableCors("ApiPolicy")]
public class TaxAPI : BaseAPI
{
    public static void AddRoutes(WebApplication app)
    {
        app.MapGet   ("api/districts/{districtid}/taxpolicies/all", GetAllForDistrictAsync).RequireCors("ApiPolicy");
        app.MapPost  ("api/taxpolicies", CreateOrUpdateAsync).RequireCors("ApiPolicy").AddEndpointFilter<UserRequiredAttribute>();
    }

    private static async Task GetAllForDistrictAsync(HttpContext ctx, long districtid)
    {
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(DBCache.GetAll<TaxPolicy>().Where(x => x.NationId == districtid).ToList()));
    }

    public static Regex rg = new Regex(@"^[a-zA-Z0-9\s,.-]*$");

    private static async Task<IResult> CreateOrUpdateAsync(HttpContext ctx, [FromBody] TaxPolicy policy)
    {
        var user = ctx.GetUser();
        var district = DBCache.Get<Nation>(policy.NationId);
        if (district.GovernorId != user.Id)
            return ValourResult.Forbid("Only the Governor can create/edit tax policies!");

        if (policy.taxType == TaxType.ResourceMined && policy.Rate > 20.0m)
            return ValourResult.BadRequest("Resource Mined Tax Rate must be �20 or lower!");

        if (policy.taxType != TaxType.ResourceMined && policy.Rate > 80m)
            return ValourResult.BadRequest("Tax rate must be 80% or lower!");

        var oldpolicy = DBCache.Get<TaxPolicy>(policy.Id);
        if (oldpolicy is null)
        {
            oldpolicy = new()
            {
                Id = IdManagers.GeneralIdGenerator.Generate(),
                Collected = 0.00m,
                taxType = policy.taxType,
                Target = policy.taxType == TaxType.ResourceMined ? policy.Target : null,
                NationId = district.Id
            };
        }

        if (policy.Name.Length < 4) return ValourResult.BadRequest("Tax Policy name must be 4 chars or longer!");

        if (!rg.IsMatch(policy.Name))
            return ValourResult.BadRequest("Recipe name can only contain letters and numbers!");

        oldpolicy.Minimum = policy.Minimum;
        oldpolicy.Maximum = policy.Maximum;
        oldpolicy.Rate = policy.Rate;
        oldpolicy.Name = policy.Name;

        if (policy.Id == 0)
            DBCache.AddNew(oldpolicy.Id, oldpolicy);

        return Results.Json(oldpolicy);
    }
}