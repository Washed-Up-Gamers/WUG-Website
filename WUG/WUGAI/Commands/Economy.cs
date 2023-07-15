using System.Threading.Tasks;
using WUG.Database.Models.Economy;
using WUG.Database.Models.Entities;
using WUG.Database.Models.Users;
using System.Linq;
using WUG.Web;

namespace SV2.WUGAI.Commands;

class EconomyCommandsModule : BaseCommandModule
{
    [Command("balance")]
    public async Task Balance(CommandContext ctx)
    {
        User? user = DBCache.GetAll<User>().FirstOrDefault(x => x.DiscordUserId == ctx.User.Id);
        if (user is null)
        {
            await ctx.RespondAsync("You do not have a WUG account!");
            return;
        }
        await ctx.RespondAsync($"{ctx.Member.Nickname}'s balance: ${Math.Round(user.Money, 2)}");
    }

    [Command("balance")]
    public async Task Balance(CommandContext ctx, [RemainingText] string entityname)
    {
        BaseEntity? entity = DBCache.GetAll<User>().FirstOrDefault(x => x.Name == entityname);
        if (entity is null)
            entity = DBCache.GetAll<Group>().FirstOrDefault(x => x.Name == entityname);
        if (entity is null)
        {
            await ctx.RespondAsync($"Could not find entity (group or user) with name: {entityname}");
            return;
        }
        await ctx.RespondAsync($"{entityname}'s balance: ${Math.Round(entity.Money, 2)}");
    }

    [Command("budget")]
    public async Task SeeNationTaxInfo(CommandContext ctx, [RemainingText] string nationname)
    {
        var nation = DBCache.GetAll<Nation>().FirstOrDefault(x => x.Name.ToLower() == nationname.ToLower());
        if (nation is null)
        {
            await ctx.RespondAsync($"Could not find nation with name: {nationname}");
            return;
        }

        var buildings = DBCache.GetAllProducingBuildings().Where(x => x.NationId == nation.Id && x.BuildingType != BuildingType.Infrastructure && x.OwnerId != nation.GroupId);

        var embed = new DiscordEmbedBuilder
        {
            Color = DiscordColor.SpringGreen,
            Title = $"{nation.Name} Income (monthly)"
        };
        embed.AddField("Buildings", $"{buildings.Count():n0}", true);
        embed.AddField("Total Building Levels", $"{buildings.Sum(x => x.Size):n0}", true);
        embed.AddField("Base Property Taxes", $"${buildings.Sum(x => nation.BasePropertyTax ?? 0):n0}", false);
        embed.AddField("Per Level Property Taxes", $"${buildings.Sum(x => nation.PropertyTaxPerSize * x.Size * x.GetThroughputFromUpgrades() ?? 0):n0}", false);

        await ctx.RespondAsync(embed);
    }

    [Command("addmoney")]
    public async Task CreateAccount(CommandContext ctx, decimal amount)
    {
        if (ctx.User.Id != 259004891148582914)
        {
            await ctx.RespondAsync("Only Jacob can use this command!");
            return;
        }
        User? user = DBCache.GetAll<User>().FirstOrDefault(x => x.DiscordUserId == ctx.User.Id);
        if (user is not null)
            user.Money += amount;
        await ctx.RespondAsync($"Added ${amount} to Jacob's balance.");
    }

    [Command("pay")]
    public async Task Pay(CommandContext ctx, decimal amount, DiscordMember member)
    {
        User? from = DBCache.GetAll<User>().FirstOrDefault(x => x.DiscordUserId == ctx.User.Id);
        if (from is null)
        {
            await ctx.RespondAsync("You do not have a WUG account!");
            return;
        }

        User? to = DBCache.GetAll<User>().FirstOrDefault(x => x.DiscordUserId == member.Id);
        if (from is null)
        {
            await ctx.RespondAsync("The user you are trying to send credits to lacks a WUG account!");
            return;
        }
        var tran = new Transaction(from, to, amount, TransactionType.Payment, "Payment from Valour");
        await ctx.RespondAsync((await tran.Execute()).Info);
    }

    [Command("pay")]
    public async Task Pay(CommandContext ctx, decimal amount, DiscordMember member, [RemainingText] string groupname)
    {
        User? fromuser = DBCache.GetAll<User>().FirstOrDefault(x => x.DiscordUserId == ctx.User.Id);
        if (fromuser is null)
        {
            await ctx.RespondAsync("You do not have a WUG account!");
            return;
        }

        Group? from = DBCache.GetAll<Group>().FirstOrDefault(x => x.Name == groupname);
        if (from is null)
        {
            await ctx.RespondAsync($"Could not find {groupname}");
            return;
        }

        if (!from.HasPermission(fromuser, GroupPermissions.Eco))
        {
            await ctx.RespondAsync($"You lack permission to send credits using this group!");
            return;
        }

        User? to = DBCache.GetAll<User>().FirstOrDefault(x => x.DiscordUserId == member.Id);
        if (to is null)
        {
            await ctx.RespondAsync("The user you are trying to send credits to lacks a WUG account!");
            return;
        }
        var tran = new Transaction(from, to, amount, TransactionType.Payment, "Payment from Valour");
        await ctx.RespondAsync((await tran.Execute()).Info);
    }

    [Command("pay")]
    public async Task Pay(CommandContext ctx, decimal amount, [RemainingText] string groupname)
    {
        User? fromuser = DBCache.GetAll<User>().FirstOrDefault(x => x.DiscordUserId == ctx.User.Id);
        if (fromuser is null)
        {
            await ctx.RespondAsync("You do not have a WUG account! Create one by doing /create account");
            return;
        }

        Group? to = null;
        Transaction transaction = null;

        if (groupname.Contains(","))
        {
            string[] splited = groupname.Split(",");
            to = DBCache.GetAll<Group>().FirstOrDefault(x => x.Name == splited[0]);
            if (to is null)
            {
                await ctx.RespondAsync($"Could not find {splited[0]}");
                return;
            }
            Group? from = DBCache.GetAll<Group>().FirstOrDefault(x => x.Name == splited[1] || splited[1][0] == ' ' && x.Name == splited[1].Substring(1, splited[1].Length - 1));
            if (from is null)
            {
                await ctx.RespondAsync($"Could not find {splited[1]}");
                return;
            }

            if (!from.HasPermission(fromuser, GroupPermissions.Eco))
            {
                await ctx.RespondAsync($"You lack permission to send credits using this group!");
                return;
            }
            transaction = new Transaction(from, to, amount, TransactionType.Payment, "Payment from Valour");
            await ctx.RespondAsync((await transaction.Execute()).Info);
            return;
        }

        to = DBCache.GetAll<Group>().FirstOrDefault(x => x.Name == groupname);
        if (to is null)
        {
            await ctx.RespondAsync($"Could not find {groupname}");
            return;
        }
        transaction = new Transaction(fromuser, to, amount, TransactionType.Payment, "Payment from Valour");
        await ctx.RespondAsync((await transaction.Execute()).Info);
    }
}