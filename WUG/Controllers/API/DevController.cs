using Microsoft.AspNetCore.Mvc;
using WUG.Models;
using System.Diagnostics;
using WUG.Database;
using WUG.Database.Models.Entities;
using WUG.Web;
using WUG.Database.Models.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Cors;

namespace WUG.API
{
    public class DevAPI : BaseAPI
    {
        public static void AddRoutes(WebApplication app)
        {
            //app.MapGet   ("api/dev/database/sql", GetSQL);
            app.MapGet("api/dev/lackaccess", LackAccess).RequireCors("ApiPolicy");
            app.MapGet("api/dev/getNationproduction", GetNationProduction).RequireCors("ApiPolicy");
            app.MapGet("api/dev/gettime", GetTime).RequireCors("ApiPolicy");
        }

        private static async Task LackAccess(HttpContext ctx) {
            await ctx.Response.WriteAsync("You lack access to SV 2.0. SV 2.0 is currently in private early alpha, public early alpha is expected in a few weeks to months.");
        }

        private static async Task GetSQL(HttpContext ctx, WashedUpDB db, bool drop = false)
        {
            if (drop && false) {
                WashedUpDB.Instance.Database.EnsureDeleted();
                WashedUpDB.Instance.Database.EnsureCreated();
                await WashedUpDB.Instance.SaveChangesAsync();
            }
            await ctx.Response.WriteAsync(WashedUpDB.GenerateSQL());
        }

        private static async Task GetNationProduction(HttpContext ctx, WashedUpDB db, string name,string resource)
        {
            var Nation = DBCache.GetAll<Nation>().FirstOrDefault(x => x.Name == name);
            if (Nation is null)
            {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsync($"Could not find Nation with name of {name}!");
                return;
            }

            // / 10550.0 * buildingobj.Recipes.First().PerHour * buildingobj.Recipes.First().Outputs.First().Value

            var buildingobj = GameDataManager.BaseBuildingObjs.Values.First(x => x.MustHaveResource == resource);
            var second_part = buildingobj.Recipes.First().PerHour * buildingobj.Recipes.First().Outputs.First().Value;
            double sum = 0;
            foreach (var province in Nation.Provinces)
            {
                sum += province.GetMiningResourceProduction(resource) / 10550.0 * second_part;
            }

            await ctx.Response.WriteAsync(sum.ToString());
        }

        private static async Task GetTime(HttpContext ctx)
        {
            await ctx.Response.WriteAsync(DBCache.CurrentTime.Time.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds.ToString());
        }
    }
}