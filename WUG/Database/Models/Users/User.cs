using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WUG.Database.Models.Entities;
using Microsoft.EntityFrameworkCore;
using WUG.Scripting.Tokens;
using System.Data;

namespace WUG.Database.Models.Users;

public enum Rank
{
    WashedUp = 1,
    Expert = 2,
    Enjoyer = 3,
    Fan = 4,
    Noob = 5,
    Unranked = 6
}

[Table("users")]
public class User : BaseEntity
{
    public ulong DiscordUserId { get; set; }
    public int ForumXp { get; set;}
    public float MessageXp { get; set;}
    public int CommentLikes { get; set;}
    public int PostLikes { get; set;}
    public int Messages { get; set;}

    // xp calc stuff

    public float PointsTotal { get; set; }
    public int ActiveMinutes { get; set; }
    public short PointsThisMinute { get; set; }
    public int TotalPoints { get; set; }
    public int TotalChars { get; set; }
    public DateTime LastActiveMinute { get; set; }
    public DateTime Joined { get; set;}
    public Rank Rank { get; set;}
    // the datetime that this user created their account
    public DateTime Created { get; set; }

    public DateTime LastSentMessage { get; set; }

    public DateTime LastMoved { get; set; }

    public string? OAuthToken { get; set; }

    [NotMapped]
    public float Xp => MessageXp + ForumXp;

    public override EntityType EntityType => EntityType.User;

    public async ValueTask<List<DiscordRole>> GetValourRolesAsync()
    {
        var member = await VoopAI.Server.GetMemberAsync(DiscordUserId);
        if (member is null) return new();
        return member.Roles.ToList();
    }

    public async ValueTask<bool> IsGovernmentAdmin() {
        return (await GetValourRolesAsync()).Any(x => x.Name == "Site Admin");
    }

    public int GetNumberOfJobSlotsFilled()
    {
        int number = 0;
        number += DBCache.ProducingBuildingsById.Values.Count(x => x.EmployeeId == Id);
        // TODO: when you add "signing" up for military job, update this
        return number;
    }

    public override async Task Create()
    {
        if (OAuthToken is null)
            return;
        Money = 50_000.0m;
    }

    public static string RemoveWhitespace(string input)
    {
        return new string(input.ToCharArray()
            .Where(c => !char.IsWhiteSpace(c))
            .ToArray());
    }

    public void NewMessage(DiscordMessage msg)
    {
        if (LastActiveMinute.AddSeconds(60) < DateTime.UtcNow)
        {
            if (PointsThisMinute <= 3)
                PointsThisMinute += 3;
            double xpgain = (Math.Log10(PointsThisMinute) - 1) * 3;
            xpgain = Math.Max(0.2, xpgain);
            MessageXp += (float)xpgain;
            ActiveMinutes += 1;
            PointsThisMinute = 0;
            LastSentMessage = DateTime.UtcNow;
            LastActiveMinute = DateTime.UtcNow;
        }

        string Content = RemoveWhitespace(msg.Content);

        Content = Content.Replace("*", "");

        short Points = 0;

        // each char grants 1 point
        Points += (short)Content.Length;

        // if there is media then add 150 points
        if (msg.Attachments.Any(x => x.MediaType.Contains("image/")))
        {
            Points += 150;
        }

        PointsThisMinute += Points;
        TotalChars += Content.Length;
        TotalPoints += Points;

        Messages += 1;
    }

    public bool IsMinister(string ministertype)
    {
        if (DBCache.UnitedNations.GetMemberRoles(this).Any(x => x.Name == ministertype))
            return true;
        return false;
    }

    public static User? FindByName(string name)
    {
        return DBCache.GetAll<User>().FirstOrDefault(x => x.Name == name);
    }
    
    public static User? Find(long Id)
    {
        return DBCache.Get<User>(Id);
    }

    public bool HasPermissionWithKey(string apikey, GroupPermission permission)
    {
        if (apikey == ApiKey) {
            return true;
        }
        return false;
    }

    public override bool HasPermission(BaseEntity entity, GroupPermission permission)
    {
        if (entity.Id == Id) {
            return true;
        }
        return false;
    }

    public User()
    {

    }

    public User(string name, ulong discorduserid)
    {
        Id = IdManagers.UserIdGenerator.Generate();
        DiscordUserId = discorduserid;
        Name = name;
        ForumXp = 0;
        MessageXp = 0;
        Messages = 0;
        PostLikes = 0;
        CommentLikes = 0;
        TaxAbleBalance = 0.00m;
        ApiKey = Guid.NewGuid().ToString();
        Rank = Rank.Unranked;
        Created = DateTime.UtcNow;
        Joined = DateTime.UtcNow;
        LastMoved = DateTime.UtcNow.AddDays(-100);
        SVItemsOwnerships = new();
        Create();
    }

    public async Task<IEnumerable<Group>> GetJoinedGroupsAsync()
    {
        using var dbctx = WashedUpDB.DbFactory.CreateDbContext();
        var groups = await dbctx.Groups.Where(x => x.MembersIds.Contains(Id)).ToListAsync();

        return groups;
    }

    public async Task<IEnumerable<Group>> GetOwnedGroupsAsync()
    {
        List<Group> groups = new List<Group>();

        using var dbctx = WashedUpDB.DbFactory.CreateDbContext();

        var topGroups = DBCache.GetAll<Group>().Where(x => x.IsOwner(this));

        foreach (Group group in topGroups)
        {
            groups.Add(group);
            groups.AddRange(await group.GetOwnedGroupsAsync());
        }

        return groups;
    }

    public async Task CheckRoles(DiscordMember member)
    {
        // check rank role
        var rankname = Rank.ToString();
        if (!member.Roles.Any(x => x.Name == rankname))
        {
            await member.GrantRoleAsync(VoopAI.Roles[VoopAI.ConvertRankTypeToString(Rank)]);
        }
        foreach (var role in member.Roles.Where(x => VoopAI.RankNames.Contains(x.Name)).ToList())
        {
            if (role.Name != rankname)
            {
                await member.RevokeRoleAsync(role);
            }
        }

        if (NationId is not null)
        {
            if (VoopAI.Roles.ContainsKey(Nation.Name))
            {
                var nationrole = VoopAI.Roles[Nation.Name];
                if (!member.Roles.Any(x => x.Id == nationrole.Id))
                {
                    await member.GrantRoleAsync(VoopAI.Roles[VoopAI.ConvertRankTypeToString(Rank)]);
                }
                foreach (var role in member.Roles.Where(x => VoopAI.NationRoles.ContainsKey(x.Name)).ToList())
                {
                    if (role.Id != nationrole.Id)
                    {
                        await member.GrantRoleAsync(role);
                    }
                }
            }
        }

        if (IsCouncilMember())
        {
            var vooperia = (Group)BaseEntity.Find(100);
            if (!vooperia.MembersIds.Contains(Id))
            {
                vooperia.MembersIds.Add(Id);
                vooperia.AddEntityToRole(vooperia, this, vooperia.Roles.First(x => x.Name == "Imperial Senator"), true);
            }
        }
        else
        {
            var vooperia = (Group)BaseEntity.Find(100);
            if (vooperia.MembersIds.Contains(Id))
            {
                vooperia.RemoveEntityFromRole(vooperia, this, vooperia.Roles.First(x => x.Name == "Imperial Senator"), true);
            }
        }

        if (member.Roles.Any(x => x.Name == "Council Member") && !IsCouncilMember())
            await member.GrantRoleAsync(member.Roles.First(x => x.Name == "Council Member"));
        if (!member.Roles.Any(x => x.Name == "Council Member") && IsCouncilMember())
            await member.GrantRoleAsync(VoopAI.Server.Roles.Values.First(x => x.Name == "Council Member"));
    }

    public async ValueTask<string> GetPfpRingColor()
    {
        if (IsEmperor()) return "4FEDF0";
        if (IsCFV()) return "1cbabd";
        if (await IsPrimeMinister()) return "03A1A4";
        if (await IsSupremeCourtJustice()) return "4FEDF0";
        if (IsCouncilMember()) return "1bf278";
        return "1bd9f2";
    }

    public bool IsEmperor() => DiscordUserId == 12200448886571008;
    public async ValueTask<bool> IsPrimeMinister() {
        return (await GetValourRolesAsync()).Any(x => x.Name == "Prime Minister");
    }
    public async ValueTask<bool> IsSupremeCourtJustice()
    {
        return (await GetValourRolesAsync()).Any(x => x.Name == "Supreme Court Justice");
    }

    public bool IsCFV() => DiscordUserId == 259004891148582914;
    public bool IsCouncilMember() => DBCache.GetAll<CouncilMember>().Any(x => x.UserId == Id);
}