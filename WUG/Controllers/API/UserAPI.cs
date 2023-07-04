using Microsoft.AspNetCore.Mvc;
using WUG.Models;
using System.Diagnostics;
using WUG.Database;
using WUG.Database.Models.Entities;
using Microsoft.AspNetCore.Cors;
using WUG.Extensions;

namespace WUG.API;

[EnableCors("ApiPolicy")]
public class UserAPI : BaseAPI
{
    public static void AddRoutes(WebApplication app)
    {
        app.MapGet   ("api/users/{id}", GetAsync).RequireCors("ApiPolicy");
        app.MapGet   ("api/users/self", GetSelfAsync).RequireCors("ApiPolicy");
    }

    private static async Task<IResult> GetAsync(HttpContext ctx, long id)
    {
        User? user = User.Find(id);
        if (user is null)
            return ValourResult.NotFound($"Could not find user with id {id}");

        return Results.Json(user);
    }

    private static async Task<IResult> GetSelfAsync(HttpContext ctx)
    {
        User? user = ctx.GetUser();
        if (user is null)
            return ValourResult.NotFound($"Could not find your account! Try logging in or relogging in.");

        return Results.Json(user);
    }
}