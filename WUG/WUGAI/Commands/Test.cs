using System.Threading.Tasks;
using IdGen;
using WUG.Database.Models.Items;
using WUG.Managers;

namespace SV2.WUGAI.Commands;

class TestCommandsModule : BaseCommandModule
{
    [Command("ping")]
    public async Task Ping(CommandContext ctx)
    {
        ctx.RespondAsync("Pong!");
    }

    [Command("provincestartingbalancepayment")]
    public async Task provincestartingbalancepayment(CommandContext ctx)
    {
        if (ctx.User.Id != 259004891148582914)
        {
            await ctx.RespondAsync("Only Jacob can use this command!");
            return;
        }
        
        foreach (var nation in DBCache.GetAll<Nation>()) {
            nation.Group.Money += 1_500.0m * nation.Provinces.Count();
        }

        await ctx.RespondAsync($"Did");
    }

    [Command("createresource")]
    public async Task CreateResource(CommandContext ctx, string resource, int amount)
    {
        if (ctx.User.Id != 259004891148582914)
        {
            await ctx.RespondAsync("Only Jacob can use this command!");
            return;
        }
        User? user = DBCache.GetAll<User>().FirstOrDefault(x => x.DiscordUserId == ctx.User.Id);
        var itemdefid = GameDataManager.ResourcesToItemDefinitions[resource].Id;
        ItemTrade itemtrade = new(ItemTradeType.Server, null, user.Id, amount, itemdefid, "From Valour - /creatresource command");
        itemtrade.NonAsyncExecute(true);
        await ctx.RespondAsync($"Added {amount} of {resource} to Jacob.");
    }

    [Command("createresource")]
    public async Task CreateResource(CommandContext ctx, int amount, long svid, [RemainingText] string resource)
    {
        if (ctx.User.Id != 259004891148582914)
        {
            await ctx.RespondAsync("Only Jacob can use this command!");
            return;
        }
        BaseEntity? entity = BaseEntity.Find(svid);
        var itemdefid = GameDataManager.ResourcesToItemDefinitions[resource].Id;
        ItemTrade itemtrade = new(ItemTradeType.Server, null, entity.Id, amount, itemdefid, "From Discord - /creatresource command");
        itemtrade.NonAsyncExecute(true);
        await ctx.RespondAsync($"Added {amount} of {resource} to {entity.Name}.");
    }

    [Command("minevoucher")]
    public async Task MineVouchers(CommandContext ctx, int amount, long svid)
    {
        if (ctx.User.Id != 259004891148582914 && ctx.User.Id != 185190742988292096)
        {
            await ctx.RespondAsync("Only TalkinTurtle can use this command!");
            return;
        }
        BaseEntity? entity = BaseEntity.Find(svid);
        var resources = new Dictionary<string, int>()
        {
            { "steel", 2000 },
            { "simple_components", 2000 },
            { "advanced_components", 200 }
        };

        foreach (var resource in resources)
        {
            var itemdefid = GameDataManager.ResourcesToItemDefinitions[resource.Key].Id;
            ItemTrade itemtrade = new(ItemTradeType.Server, null, entity.Id, resource.Value * amount, itemdefid, "From Discord - /minevoucher command");
            itemtrade.NonAsyncExecute(true);
        }
        await ctx.RespondAsync($"Gave {amount} mine voucher to {entity.Name}.");
    }

    [Command("factoryvoucher")]
    public async Task FactoryVouchers(CommandContext ctx, int amount, long svid)
    {
        if (ctx.User.Id != 259004891148582914 && ctx.User.Id != 185190742988292096)
        {
            await ctx.RespondAsync("Only TalkinTurtle can use this command!");
            return;
        }
        BaseEntity? entity = BaseEntity.Find(svid);
        var resources = new Dictionary<string, int>()
        {
            { "steel", 10000 },
            { "simple_components", 7500 },
            { "advanced_components", 1000 }
        };

        foreach (var resource in resources)
        {
            var itemdefid = GameDataManager.ResourcesToItemDefinitions[resource.Key].Id;
            ItemTrade itemtrade = new(ItemTradeType.Server, null, entity.Id, resource.Value * amount, itemdefid, "From Valour - /factoryvoucher command");
            itemtrade.NonAsyncExecute(true);
        }
        await ctx.RespondAsync($"Gave {amount} factory voucher to {entity.Name}.");
    }

    [Command("givexp")]
    public async Task CreateResource(CommandContext ctx, int amount, long svid)
    {
        if (ctx.User.Id != 259004891148582914)
        {
            await ctx.RespondAsync("Only Jacob can use this command!");
            return;
        }
        User? user = User.Find(svid);
        user.MessageXp += amount;
        await ctx.RespondAsync($"Added {amount}xp to {user.Name}.");
    }
}