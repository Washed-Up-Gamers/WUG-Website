using System.Threading.Tasks;
using Valour.Net;
using Valour.Net.ModuleHandling;
using Valour.Net.CommandHandling;
using Valour.Net.CommandHandling.Attributes;
using Valour.Api.Models.Messages;
using Valour.Api.Models.Messages.Embeds;
using WUG.Database.Models.Groups;
using WUG.Database.Models.Economy;
using WUG.Database.Models.Districts;
using WUG.Database.Models.Users;
using System.Linq;
using WUG.Web;

namespace WUG.VoopAI.Commands;

class DistrictCommands : CommandModuleBase
{
    [Group("district")]
    public class DistrictGroup : CommandModuleBase
    {

        
    }
}