using System.Text.Json;
using System.Reflection;
using Microsoft.AspNetCore.Hosting.Server;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using SV2.WUGAI.Commands;
using System.Drawing;
//using Valour.Api.Models.Economy;

namespace WUG.WUGVAI;
public class VoopAI
{
    public static bool prod;
    public static List<string> prefixes;
    public static List<string> RankNames = new() { "Washed Up", "Expert", "Enjoyer", "Fan", "Noob", "Unranked" };
    public static Dictionary<string, ulong> RankRoleIds = new();
    public static Dictionary<string, DiscordRole> Roles = new();
    public static Dictionary<string, DiscordRole> NationRoles = new();
    public static DiscordClient Discord { get; set; }
    public static DiscordGuild Server { get; set; }
    public static DiscordChannel EcoChannel { get; set; }

    public static string ConvertRankTypeToString(Rank rank)
    {
        if (rank == Rank.WashedUp)
            return "Washed Up";
        return rank.ToString();
    }

    public static string GenerateRgba(string backgroundColor, decimal backgroundOpacity)
    {
        System.Drawing.Color color = ColorTranslator.FromHtml(backgroundColor);
        int r = Convert.ToInt16(color.R);
        int g = Convert.ToInt16(color.G);
        int b = Convert.ToInt16(color.B);
        return string.Format("rgba({0}, {1}, {2}, {3});", r, g, b, backgroundOpacity);
    }

    public static async Task Main()
    {
        Discord = new DiscordClient(new DiscordConfiguration()
        {
            Token = DiscordConfig.instance.BotToken,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContent
        });

        await Discord.ConnectAsync();

        Discord.MessageCreated += async (s, e) =>
        {
            AccountCommandsModule.OnMessage(e.Message);
        };

        var commands = Discord.UseCommandsNext(new CommandsNextConfiguration()
        {
            StringPrefixes = new List<string>() { "!", "/", "c/"}
        });

        commands.RegisterCommands<AccountCommandsModule>();
        commands.RegisterCommands<EconomyCommandsModule>();
        commands.RegisterCommands<TestCommandsModule>();

        EcoChannel = await Discord.GetChannelAsync(1125513097042464778);
        Server = await Discord.GetGuildAsync(1124495105131294743);
        var roles = Server.Roles.Values.ToList();

        foreach (var role in roles.Where(x => RankNames.Contains(x.Name)))
        {
            RankRoleIds[role.Name] = role.Id;
            Roles[role.Name] = role;
        }
        
        var nationsnames = DBCache.GetAll<Nation>().Select(x => x.Name).ToList();
        foreach (var role in roles.Where(x => nationsnames.Contains(x.Name.Replace(" Nation", ""))))
        {
            NationRoles[role.Name] = role;
            RankRoleIds[role.Name] = role.Id;
            Roles[role.Name] = role;
        }

        await CheckRoles();

        //OnMessageRecieved += MessageHandler;

        //await Task.Delay(-1);
    }

    public static string GetRankColor(Rank? rank)
    {
        if (rank is null)
        {
            return "ffffff";
        }
        switch (rank)
        {
            case Rank.WashedUp:
                return "414aff";
            case Rank.Expert:
                return "e05151";
            case Rank.Enjoyer:
                return "00ff23";
            case Rank.Fan:
                return "b400ff";
            case Rank.Noob:
                return "f1ff00";
            case Rank.Unranked:
                return "ffffff";
        }
        return "ffffff";
    }

    /// <summary>
    /// Checks roles on the server if needed
    /// </summary>
    public static async Task CheckRoles()
    {
        var roles = Server.Roles.Values.ToList();
        foreach (var role in roles.Where(x => RankNames.Contains(x.Name)))
            RankRoleIds[role.Name] = role.Id;

        //foreach (var role in (await (await Planet.FindAsync(PlanetId)).GetRolesAsync()).Where(x => Nationsnames.Contains(x.Name + " Nation")))
        //    RankRoleIds[role.Name] = role.Id;
    }

    public static async Task UpdateRanks()
    {
        Console.WriteLine("Doing rank job");

        using var dbctx = WashedUpDB.DbFactory.CreateDbContext();

        var users = DBCache.GetAll<User>().OrderByDescending(x => x.Xp);

        double c = users.Count();
        int washedupcount = (int)Math.Floor(c / 100);
        c -= washedupcount;
        int expertcount = (int)Math.Floor(c / 20);
        c -= expertcount;
        int enjoyercount = (int)Math.Floor(c / 10);
        c -= enjoyercount;
        int fancount = (int)Math.Floor(c / 4);
        c -= fancount;
        int noobcount = (int)Math.Floor(c / 2);
        c -= noobcount;
        int unrankedcount = (int)c;

        var InactivityTaxPolicy = DBCache.GetAll<TaxPolicy>().FirstOrDefault(x => x.NationId == 100 && x.taxType == TaxType.Inactivity);

        foreach (var user in users)
        {
            var member = await VoopAI.Server.GetMemberAsync(user.DiscordUserId);
            if (member is null)
                continue;
            if (washedupcount > 0)
            {
                washedupcount -= 1;
                user.Rank = Rank.WashedUp;
            }
            else if (expertcount > 0)
            {
                expertcount -= 1;
                user.Rank = Rank.Expert;
            }
            else if (enjoyercount > 0)
            {
                enjoyercount -= 1;
                user.Rank = Rank.Enjoyer;
            }
            else if (fancount > 0)
            {
                fancount -= 1;
                user.Rank = Rank.Fan;
            }
            else if (noobcount > 0)
            {
                noobcount -= 1;
                user.Rank = Rank.Noob;
            }
            else
            {
                user.Rank = Rank.Unranked;
            }

            //Console.WriteLine($"Setting {user.Name}'s rank to {user.Rank}");

            // inactivity tax
            if (Math.Abs(user.LastSentMessage.Subtract(DateTime.UtcNow).TotalDays) > 14 && InactivityTaxPolicy is not null)
            {
                decimal tax = InactivityTaxPolicy.GetTaxAmount(user.Money);

                var tran = new Transaction(user, BaseEntity.Find(100), tax, TransactionType.TaxPayment, "Inactivity Tax");

                await tran.Execute(true);

                if (tran.Result.Succeeded)
                {
                    InactivityTaxPolicy.Collected += tax;
                }
                continue;
            }
            else
            {
                // most if not all of the member data should be cached so this call should be really fast to execute
                await user.CheckRoles(member);
            }

            // set Nation
        }

        // TODO: add patron role management

        Console.WriteLine("Finished rank job");
    }
}