using System.Threading.Tasks;
using Valour.Net;
using Valour.Net.ModuleHandling;
using Valour.Net.CommandHandling;
using Valour.Net.CommandHandling.Attributes;
using WUG.Database.Models.Groups;
using WUG.Database.Models.Districts;
using WUG.Database.Models.Economy;
using WUG.Database.Models.Users;
using WUG.Database.Models.Entities;
using System.Linq;
using WUG.Web;
using Valour.Api.Models.Messages.Embeds;
using Valour.Api.Models.Messages.Embeds.Styles.Basic;

namespace WUG.VoopAI.Commands;
class UBICommands : CommandModuleBase
{
    [Interaction(EmbedIteractionEventType.FormSubmitted)]
    public async Task ChangeUBIInteraction(InteractionContext ctx)
    {
        // Element_Id is the id of the button that was clicked
        string EventId = ctx.Event.ElementId;
        if (EventId.Contains("ChangeUBI")) {
            await ctx.ReplyAsync(ctx.Event.ToString());
        }
    }

    [Command("UBI")]
    public async Task ViewUBI(CommandContext ctx) 
    {
        await ShowUBI(ctx, 100);
    }

    [Command("UBI")]
    public async Task ViewUBI(CommandContext ctx, [Remainder] string district) 
    {
        District? _district = DBCache.GetAll<District>().FirstOrDefault(x => x.Name == district);
        if (_district == null) {
            await ctx.ReplyAsync($"Could not find district {district}!");
            return;
        }
        await ShowUBI(ctx, _district.Id);
    }

    public async Task ShowUBI(CommandContext ctx, long districtid)
    {
        IEnumerable<UBIPolicy> policies = DBCache.GetAll<UBIPolicy>().Where(x => x.DistrictId == districtid);
        
        var embed = new EmbedBuilder();
        string name = "";
        District? district = null;
        if (districtid != 100) {
            district = DBCache.Get<District>(districtid);
        }
        if (district is not null) {
            name = district.Name;
        }
        else {
            name = "Vooperia";
        }
        embed.AddPage($"UBI For {name}").AddRow();
        foreach (UBIPolicy policy in policies.OrderByDescending(x => x.Rate))
        {
            string rankname = "";
            string rankcolor = "";
            if (policy.ApplicableRank is null) {
                rankname = "Everyone";
                rankcolor = "ffffff";
            }
            else {
                rankname = policy.ApplicableRank.ToString()!;
                rankcolor = VoopAI.GetRankColor(policy.ApplicableRank);
            }
            embed.AddText($"¢{Math.Round(policy.Rate)} daily");
            embed.WithName(rankname).WithStyles(new TextColor(rankcolor));
            embed.AddRow();
        }
        await ctx.ReplyAsync(embed);
    }
}