using Microsoft.AspNetCore.Mvc;
using WUG.Models;
using System.Diagnostics;
using WUG.Database;
using WUG.Database.Models.Entities;
using WUG.Web;
using WUG.Database.Models;
using WUG.Database.Models.Economy;
using Microsoft.AspNetCore.Cors;

namespace WUG.API
{
    [EnableCors("ApiPolicy")]
    public class EcoAPI : BaseAPI
    {
        public static void AddRoutes(WebApplication app)
        {
            app.MapGet   ("api/eco/transaction/send", SendTransaction).RequireCors("ApiPolicy");
            app.MapGet   ("api/eco/nation/{id}/gdp", GetNationGDP).RequireCors("ApiPolicy");
            app.MapGet   ("api/eco/state/{id}/gdp", GetStateGDP).RequireCors("ApiPolicy");
        }

        private static async Task GetNationGDP(HttpContext ctx, long id)
        {
            Nation? nation = DBCache.Get<Nation>(id);
            if (nation is null && id != 100) {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsJsonAsync(new TaskResult(false, $"Could not find nation with id {id}"));
                return;
            }
            if (id == 100) {
                await ctx.Response.WriteAsync(UN.GDP.ToString());
                return;
            }
            
            await ctx.Response.WriteAsync(nation.GDP.ToString());
        }

        private static async Task GetStateGDP(HttpContext ctx, long id)
        {
            State? state = DBCache.Get<State>(id);
            if (state is null) {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsJsonAsync(new TaskResult(false, $"Could not find state with id {id}"));
                return;
            }
            
            await ctx.Response.WriteAsync(state.GDP.ToString());
        }

        private static async Task SendTransaction(HttpContext ctx, WashedUpDB db, long fromid, long toid, string apikey, decimal amount, string detail, TransactionType trantype, bool? isanexpense = null)
        {
            // get Entity with the api key
            BaseEntity? entity = await BaseEntity.FindByApiKey(apikey, db);

            BaseEntity? fromentity = BaseEntity.Find(fromid);
            if (fromentity == null)
            {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsJsonAsync(new TaskResult(false, $"Could not find entity with id {fromid}"));
                return;
            }

            BaseEntity? toentity = BaseEntity.Find(toid);
            if (toentity == null)
            {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsJsonAsync(new TaskResult(false, $"Could not find entity with id {toid}"));
                return;
            }

            if (entity.Id != fromentity.Id)
            {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsJsonAsync(new TaskResult(false, $"You can not use one entity's api key or oauth key to send a transaction from another entity!"));
                return;
            }

            if (!fromentity.HasPermission(entity, GroupPermissions.Eco))
            {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsJsonAsync(new TaskResult(false, $"You lack permission to send transactions!"));
                return;
            }

            if (amount <= 0) {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsJsonAsync(new TaskResult(false, $"Amount must be greater than 0!"));
                return;
            }

            if (isanexpense is not null) 
            {
                // only groups with the CanSetTransactionsExpenseStatus flag can set the isanexpense
                if (toentity.EntityType != EntityType.Group && toentity.EntityType != EntityType.Corporation)
                {
                    ctx.Response.StatusCode = 401;
                    await ctx.Response.WriteAsJsonAsync(new TaskResult(false, "Only groups can use isanexpense!"));
                    return;
                }
                Group group = (Group)toentity;
                if (!group.Flags.Contains(GroupFlag.CanSetTransactionsExpenseStatus))
                {
                    ctx.Response.StatusCode = 401;
                    await ctx.Response.WriteAsJsonAsync(new TaskResult(false, "Only groups with the CanSetTransactionsExpenseStatus flag can use isanexpense!"));
                    return;
                }
            }

            var tran = new Transaction(fromentity, toentity, amount, trantype, detail);
            if (isanexpense is not null)
                tran.IsAnExpense = isanexpense;

            TaskResult result = await tran.Execute();

            await ctx.Response.WriteAsJsonAsync(result);
        }
    }
}