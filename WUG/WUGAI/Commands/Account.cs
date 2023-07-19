using System.Threading.Tasks;
using WUG.Database.Models.Groups;
using WUG.Database.Models.Economy;
using WUG.Database.Models.Users;
using System.Linq;
using System.Collections.Concurrent;
using WUG.Web;
using WUG.Managers;

namespace SV2.WUGAI.Commands;

public class AccountCommandsModule : BaseCommandModule
{
    public static string RemoveWhitespace(string input)
    {
        return new string(input.ToCharArray()
            .Where(c => !char.IsWhiteSpace(c))
            .ToArray());
    }

    public static async Task OnMessage(DiscordMessage msg)
    {
        User? user = DBCache.GetAll<User>().FirstOrDefault(x => x.DiscordUserId == msg.Author.Id);
        if (user is not null)
        {
            user.NewMessage(msg);

            user.ImageUrl = msg.Author.AvatarUrl;
        }
    }

    [Command("svid")]
    public async Task ViewSVID(CommandContext ctx)
    {
        User? _user = DBCache.GetAll<User>().FirstOrDefault(x => x.DiscordUserId == ctx.User.Id);
        if (_user is null)
        {
            await ctx.RespondAsync("You do not have a WUG account!");
            return;
        }

        await ctx.RespondAsync(_user.Id.ToString());
    }

    [Command("xp")]
    [Aliases("do")]
    public async Task ViewXP(CommandContext ctx)
    {
        User? user = DBCache.GetAll<User>().FirstOrDefault(x => x.DiscordUserId == ctx.User.Id);
        if (user is null)
        {
            await ctx.RespondAsync("You do not have a WUG account!");
            return;
        }
        var embed = new DiscordEmbedBuilder
        {
            Color = DiscordColor.SpringGreen
        };
        embed.AddField("XP", $"{Math.Round(user.Xp, 1)}", true);
        embed.AddField("Rank", user.Rank.ToString(), true);
        embed.AddField("Messages", $"{user.Messages}", true);
        embed.AddField("Message To XP Ratio", $"1 : {Math.Round((double)user.MessageXp / user.Messages, 2)}", false);

        // get daily UBI

        // get vooperia's ubi
        decimal ubi = 0.0m;
        ubi += DBCache.GetAll<UBIPolicy>().FirstOrDefault(x => x.NationId == 100 && x.ApplicableRank == user.Rank)!.Rate;

        // get the user's Nation's UBI
        ubi += DBCache.GetAll<UBIPolicy>().Where(x => x.NationId == user.NationId && (x.ApplicableRank == user.Rank || x.ApplicableRank == null)).Sum(x => x.Rate);

        embed.AddField("Daily UBI", $"${Math.Round(ubi)}");
        await ctx.RespondAsync(embed);
    }
}