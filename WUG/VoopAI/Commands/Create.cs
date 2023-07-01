using System.Threading.Tasks;
using Valour.Net;
using Valour.Net.ModuleHandling;
using Valour.Net.CommandHandling;
using Valour.Net.CommandHandling.Attributes;
using WUG.Database.Models.Groups;
using WUG.Database.Models.Economy;
using WUG.Database.Models.Users;
using System.Linq;
using WUG.Web;
using Valour.Api.Models.Messages.Embeds;

namespace WUG.VoopAI.Commands;

public class CreateCommands : CommandModuleBase
{
    [Group("info")]
    public class InfoGroup : CommandModuleBase
    {
        [Command("xp")]
        public Task XpInfo(CommandContext ctx)
        {
            var embed = new EmbedBuilder().AddPage().AddRow()
                .AddText("Message Xp", "The more chars (numbers, letters, etc) you type in a given minute, the more xp you earn. However, each additional char adds a little less xp.").AddRow()
                .AddText("Element Xp", "By combining elements, you will earn xp depending on how difficult the combination was.");
            return ctx.ReplyAsync(embed);
        }
    }

    [Command("create")]
    public async Task GetInfoAsync(CommandContext ctx)
    {
        ctx.ReplyAsync("what");
    }
}