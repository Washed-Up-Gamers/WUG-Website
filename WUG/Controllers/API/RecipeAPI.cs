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
public class RecipeAPI : BaseAPI
{
    public static void AddRoutes(WebApplication app)
    {
        app.MapGet   ("api/recipes/all", GetAllAsync).RequireCors("ApiPolicy");
        app.MapGet   ("api/baserecipes/all", GetAllRecipesAsync).RequireCors("ApiPolicy");
        app.MapGet   ("api/baserecipes/{id}", GetAsync).RequireCors("ApiPolicy");
        app.MapPost  ("api/recipes", CreateAsync).RequireCors("ApiPolicy").AddEndpointFilter<UserRequiredAttribute>();
    }

    private static async Task GetAllAsync(HttpContext ctx)
    {
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(DBCache.GetAll<Recipe>().ToList()));
    }

    private static async Task GetAllRecipesAsync(HttpContext ctx)
    {
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(GameDataManager.BaseRecipeObjs.Values.ToList()));
    }

    private static async Task<IResult> GetAsync(HttpContext ctx, string id)
    {
        if (!GameDataManager.BaseRecipeObjs.ContainsKey(id))
            return ValourResult.NotFound($"Could not find baserecipe with id {id}");

        var obj = GameDataManager.BaseRecipeObjs[id];
        return Results.Json(obj);
    }

    public static Regex rg = new Regex(@"^[a-zA-Z0-9\s,.-]*$");

    private static async Task<IResult> CreateAsync(HttpContext ctx, [FromBody] Recipe recipe)
    {
        var user = ctx.GetUser();
        var owner = BaseEntity.Find(recipe.OwnerId);
        if (!owner.HasPermission(user, GroupPermissions.Recipes))
            return ValourResult.Forbid("");

        if (recipe.Name.Length < 4) return ValourResult.BadRequest("Recipe name must be 4 chars or longer!");
        if (recipe.OutputItemName.Length < 4) return ValourResult.BadRequest("Output item name must be 4 chars or longer!");

        if (!rg.IsMatch(recipe.Name))
            return ValourResult.BadRequest("Recipe name can only contain letters and numbers!");

        if (!rg.IsMatch(recipe.OutputItemName))
            return ValourResult.BadRequest("Output item name can only contain letters and numbers!");

        if (DBCache.GetAll<ItemDefinition>().Any(x => x.Name == recipe.Name))
            return ValourResult.BadRequest("This output item name has already been taken!");

        if (DBCache.Recipes.ContainsKey(recipe.StringId) || DBCache.Recipes.Values.Any(x => x.Name == recipe.Name))
            return ValourResult.BadRequest("This recipe name has already been taken!");

        recipe.UpdateInputs();
        recipe.UpdateModifiers();
        recipe.Id = IdManagers.GeneralIdGenerator.Generate();
        recipe.StringId = recipe.Name.ToLower().Replace(" ", "_");
        recipe.EntityIdsThatCanUseThisRecipe = new();
        recipe.PerHour = recipe.BaseRecipe.PerHour;
        recipe.HasBeenUsed = false;

        var itemdef = new ItemDefinition(recipe.OwnerId, recipe.OutputItemName);
        itemdef.BaseItemDefinitionId = GameDataManager.ResourcesToItemDefinitions[recipe.BaseRecipe.OutputWithCustomItem.Value.Key].Id;
        itemdef.Transferable = true;
        DBCache.AddNew(itemdef.Id, itemdef);

        recipe.CustomOutputItemDefinitionId = itemdef.Id;
        recipe.UpdateOutputs();
        recipe.Created = DateTime.UtcNow;
        itemdef.Modifiers = recipe.Modifiers;

        DBCache.AddNew(recipe.Id, recipe);
        DBCache.Recipes[recipe.StringId] = recipe;

        await itemdef.UpdateModifiers();

        return Results.Json(recipe);
    }
}