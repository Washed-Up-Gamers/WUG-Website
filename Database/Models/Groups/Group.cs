using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SV2.Database.Models.Entities;
using SV2.Database.Models.Permissions;
using SV2.Database.Models.Users;
using Microsoft.EntityFrameworkCore;
using SV2.Web;
using Valour.Api.Models;

namespace SV2.Database.Models.Groups;

public enum GroupTypes
{
    Company,
    // a corporation is a company that is listed on SVSE or a company on a private stock exchange that the CFV has determined is a corporation
    Corporation,
    NonProfit,
    PoliticalParty,
    District
}

public enum GroupFlag
{
    // is only given by the CFV
    Charity,
    // is only given by the MOJ
    News
}

public class Group : BaseEntity, IHasOwner
{
    public GroupTypes GroupType { get; set; }

    // use the PostgreSQL Array datatype
    public List<GroupFlag> Flags { get; set; }

    // if the group is open to the public
    public bool Open { get; set; }

    public List<long> MembersIds { get; set; }

    public override EntityType EntityType
    {
        get
        {
            if (GroupType == GroupTypes.Corporation)
                return EntityType.Corporation;
            return EntityType.Group;
        }
    }

    [NotMapped]
    public IEnumerable<BaseEntity> Members => MembersIds.Select(x => BaseEntity.Find(x));

    [NotMapped]
    public IEnumerable<GroupRole> Roles => DBCache.GetAll<GroupRole>().Where(x => x.GroupId == Id).ToList();

    public long OwnerId { get; set; }

    [NotMapped]

    public BaseEntity Owner => BaseEntity.Find(OwnerId)!;

    public bool IsInGroup(SVUser user)
    {
        return MembersIds.Contains(user.Id);
    }

    public Group()
    {

    }

    public Group(string name, long ownerId)
    {
        Id = IdManagers.GroupIdGenerator.Generate();
        Name = name;
        ApiKey = Guid.NewGuid().ToString();
        Credits = 0.0m;
        TaxAbleBalance = 0.0m;
        TaxAbleBalanceYesterday = 0.0m;
        CreditSnapshots = new();
        OwnerId = ownerId;
        Open = false;
        Flags = new();
        GroupType = GroupTypes.Company;
        MembersIds = new() { OwnerId };
    }

    public GroupRole? GetHighestRole(BaseEntity entity)
    {
        GroupRole? role = DBCache.GetAll<GroupRole>().Where(x => x.GroupId == Id && x.MembersIds.Contains(entity.Id)).OrderByDescending(x => x.Authority).FirstOrDefault();
        if (role is null)
        {
            return GroupRole.Default;
        }
        return role;
    }

    public GroupRole GetHighestRoleWithPermission(BaseEntity user, GroupPermission permission)
    {
        GroupRole role = DBCache.GetAll<GroupRole>().Where(x => x.GroupId == Id && x.MembersIds.Contains(user.Id) && HasPermission(user, permission)).OrderByDescending(x => x.Authority).First();
        return role;
    }

    public bool HasPermissionWithKey(string apikey, GroupPermission permission)
    {
        if (apikey == ApiKey)
        {
            return true;
        }

        // add oauth key handling
        return false;

    }

    public bool HasPermission(BaseEntity entity, GroupPermission permission)
    {
        if (entity.Id == OwnerId)
        {
            return true;
        }

        foreach (GroupRole role in DBCache.GetAll<GroupRole>().Where(x => x.GroupId == Id && x.MembersIds.Contains(entity.Id)).OrderByDescending(x => x.Authority))
        {
            PermissionCode code = new PermissionCode(role.PermissionValue, permission.Value);
            PermissionState state = code.GetState(permission);

            // this should NEVER happen
            if (state == PermissionState.Undefined)
            {
                continue;
            }
            else if (state == PermissionState.True)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        return false;

    }

    public async Task<IEnumerable<Group>> GetOwnedGroupsAsync()
    {
        List<Group> groups = new List<Group>();

        using var dbctx = VooperDB.DbFactory.CreateDbContext();

        var topGroups = await dbctx.Groups.Where(x => x.OwnerId == Id).ToListAsync();

        foreach (Group group in topGroups)
        {
            groups.Add(group);
            groups.AddRange(await group.GetOwnedGroupsAsync());
        }

        return groups;
    }

    public static Group? Find(long Id)
    {
        return DBCache.Get<Group>(Id);
    }

    public List<GroupRole> GetMemberRoles(BaseEntity entity)
    {
        List<GroupRole> roles = new();
        roles.AddRange(Roles.Where(x => x.MembersIds.Contains(entity.Id)));
        return roles;
    }

    public int GetAuthority(BaseEntity target)
    {
        if (target is null)
            return int.MinValue;

        if (OwnerId == target.Id)
            return int.MaxValue;

        List<GroupRole> roles = GetMemberRoles(target);

        if (roles is null || roles.Count == 0)
            return int.MinValue;

        return roles.Max(r => r.Authority);
    }

    public TaskResult AddEntityToRole(BaseEntity caller, BaseEntity target, GroupRole role)
    {
        // Validate arguments
        TaskResult validate = CommonValidation(caller, target, GroupPermissions.AddMembersToRoles);
        if (!validate.Succeeded) { return validate; }

        // Authority check
        if (role.Authority > GetAuthority(target))
            return new TaskResult(false, $"{role.Name} has more authority than you!");

        if (role is null)
            return new TaskResult(false, "Error: The role value was empty.");

        if (Roles.Any(x => x.MembersIds.Contains(target.Id)))
            return new TaskResult(false, "Error: The entity already has this role.");

        if (role.GroupId != Id)
            return new TaskResult(false, "Error: The role does not belong to this group!");

        role.MembersIds.Add(target.Id);
        return new(true, $"Successfully added {target.Name} to {role.Name}");
    }

    public TaskResult RemoveEntityFromRole(BaseEntity caller, BaseEntity target, GroupRole role)
    {
        // Validate arguments
        TaskResult validate = CommonValidation(caller, target, GroupPermissions.RemoveMembersFromRoles);
        if (!validate.Succeeded) { return validate; }

        // Authority check
        if (role.Authority > GetAuthority(target))
            return new TaskResult(false, $"{role.Name} has more authority than you!");

        if (role is null)
            return new TaskResult(false, "Error: The role value was empty.");

        if (!Roles.Any(x => x.MembersIds.Contains(target.Id)))
            return new TaskResult(false, "Error: The entity does has this role.");

        if (role.GroupId != Id)
            return new TaskResult(false, "Error: The role does not belong to this group!");

        role.MembersIds.Remove(target.Id);
        return new(true, $"Successfully removed {target.Name} from {role.Name}");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="caller">The entity who is trying to execute this operation</param>
    /// <param name="target">The entity we are doing an operation on</param>
    /// <param name="permission">The permission that the <paramref name="caller"/> is required to have</param>
    /// <returns></returns>
    public TaskResult CommonValidation(BaseEntity caller, BaseEntity target, GroupPermission permission)
    {
        if (caller is null)
            return new TaskResult(false, $"Error: Please log in!");

        if (target is null)
            return new TaskResult(false, $"Error: Target user does not exist!");

        if (HasPermission(caller, permission))
            return new TaskResult(false, $"Error: You don't have permission to do that!");

        return new TaskResult(true, $"Validated!");
    }
}