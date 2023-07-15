using Microsoft.AspNetCore.Mvc;
using WUG.Models;
using System.Diagnostics;
using WUG.Database;
using WUG.Database.Models.Entities;
using Microsoft.AspNetCore.Cors;
using WUG.Helpers;
using WUG.Extensions;
using Microsoft.AspNetCore.Http;

namespace WUG.API;

[EnableCors("ApiPolicy")]
public class GroupAPI : BaseAPI
{
    public static void AddRoutes(WebApplication app)
    {
        app.MapGet   ("api/groups/{id}", GetAsync).RequireCors("ApiPolicy");
        app.MapGet   ("api/groups/{id}/ownershipchainasids", GetOwnershipChainAsIdsAsync).RequireCors("ApiPolicy");
        app.MapGet   ("api/groups/{id}/ownershipchain", GetOwnershipChainAsync).RequireCors("ApiPolicy");
        app.MapGet   ("api/groups/mine/all/withperm/{permissionname}", MineAllWithPerm).RequireCors("ApiPolicy").AddEndpointFilter<UserRequiredAttribute>();
    }

    private static async Task<IResult> GetOwnershipChainAsync(HttpContext ctx, long id)
    {
        Group? group = Group.Find(id);
        if (group is null)
            return ValourResult.NotFound($"Could not find group with id {id}");

        List<BaseEntity> ownershipChain = new();

        BaseEntity owner = group.Owner;
        if (owner.EntityType == EntityType.User)
            ownershipChain.Add(owner);
        else {

            // While the owner is a group
            while (owner is Group)
            {
                ownershipChain.Add(owner);

                // Move up to next layer of ownership
                owner = BaseEntity.Find(((Group)owner).OwnerId);
            }

            // At this point the owner must be a user
            if (owner != null)
            {
                ownershipChain.Add(owner);
            }
        }

        return Results.Json(ownershipChain);
    }

    private static async Task<IResult> GetOwnershipChainAsIdsAsync(HttpContext ctx, long id)
    {
        Group? group = Group.Find(id);
        if (group is null)
            return ValourResult.NotFound($"Could not find group with id {id}");

        List<long> ownershipChainEntityIds = new();

        BaseEntity owner = group.Owner;
        if (owner.EntityType == EntityType.User)
            ownershipChainEntityIds.Add(group.OwnerId);
        else {

            // While the owner is a group
            while (owner is Group)
            {
                ownershipChainEntityIds.Add(owner.Id);

                // Move up to next layer of ownership
                owner = BaseEntity.Find(((Group)owner).OwnerId);
            }

            // At this point the owner must be a user
            if (owner != null)
            {
                ownershipChainEntityIds.Add(owner.Id);
            }
        }

        return Results.Json(ownershipChainEntityIds);
    }

    private static async Task<IResult> GetAsync(HttpContext ctx, long id)
    {
        Group? group = Group.Find(id);
        if (group is null)
            return ValourResult.NotFound($"Could not find group with id {id}");

        return Results.Json(group);
    }

    private static async Task<IResult> MineAllWithPerm(HttpContext ctx, string permissionname)
    {
        var user = ctx.GetUser();

        var permission = GroupPermissions.Permissions.FirstOrDefault(x => x.Name == permissionname);
        if (permission is null) {
            return ValourResult.BadRequest($"Could not find group permission with name {permissionname}!");
        }

        return Results.Json(DBCache.GetAll<Group>().Where(x => x.HasPermission(user, permission)).ToList());
    }

}