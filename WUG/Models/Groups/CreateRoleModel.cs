﻿using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace WUG.Models.Groups;

public class CreateRoleModel
{
    // The ID of the role
    [Key]
    public long RoleId { get; set; }

    // The name of the role
    [MaxLength(64, ErrorMessage = "Name should be under 64 characters.")]
    [RegularExpression("^[a-zA-Z0-9, ]*$", ErrorMessage = "Please use only letters, numbers, and commas.")]
    [Display(Name = "Name", Description = "Use the name 'Default' for the permissions given to those without any roles! (Only make one default role!!!)")]
    [Required]
    public string Name { get; set; }

    // Things this role is allowed to do
    public long PermissionValue { get; set; }

    // Hexcode for role color
    [MaxLength(6, ErrorMessage = "Color should be a hex code (ex: #ffffff)")]
    [Display(Name = "Color", Description = "The hex color code for the role. #??????")]
    public string Color { get; set; }

    // ids of members of this role
    public long MembersIds { get; set; }

    public List<BaseEntity> Members { get; set; }

    // The group this role belongs to
    public long GroupId { get; set; }

    [Display(Name = "Salary", Description = "This will be paid to this role every hour.")]
    public decimal Salary { get; set; }

    // Weight of the role
    [Required]
    [RegularExpression("^[0-9]*$", ErrorMessage = "Numbers only!")]
    [Display(Name = "Authority", Description = "The 'Power' of a role. A role with less power cannot be banned, kicked, or give themselves a higher role.")]
    public int Authority { get; set; }

    [Display(Name = "Create Role", Description = "The ability to create a role. This is the most powerful and dangerous permission, " +
        "as there are no limits to the roles created. This also lets you set the default role.")]
    public bool CreateRole { get; set; }

    [Display(Name = "Delete Role", Description = "The ability to delete a role. This is a most powerful and dangerous permission")]
    public bool DeleteRole { get; set; }

    [Display(Name = "Add To Role", Description = "The ability to add a entity to a role.")]
    public bool AddRole { get; set; }

    [Display(Name = "Remove From Role", Description = "The ability to remove a entity from a role.")]
    public bool RemoveRole { get; set; }

    [Display(Name = "Manage Invites", Description = "The ability to invite a entity to the group, and remove invites.")]
    public bool ManageInvites { get; set; }

    [Display(Name = "Manage Membership", Description = "The ability to kick or ban a entity from the group.")]
    public bool ManageMembership { get; set; }

    [Display(Name = "Edit", Description = "The ability to get onto the 'Edit' page of a group. This doesn't mean you get " +
        "permissions for everything in Edit!")]
    public bool Edit { get; set; }

    [Display(Name = "Post", Description = "The ability to post in a group's category.")]
    public bool Post { get; set; }

    [Display(Name = "Economy", Description = "The ability to use group finances.")]
    public bool Eco { get; set; }

    [Display(Name = "News", Description = "The ability to post news stories.")]
    public bool News { get; set; }

    [Display(Name = "Manage Building Requests", Description = "The ability to accept or deny building requests on provinces that this group has governorship over.")]
    public bool ManageBuildingRequests { get; set; }

    [Display(Name = "Manage Provinces", Description = "The ability to edit/manage provinces that this group has governorship over.")]
    public bool ManageProvinces { get; set; }

    [Display(Name = "Build", Description = "The ability to submit building requests as this group.")]
    public bool Build { get; set; }

    [Display(Name = "Manage Buildings", Description = "The ability to manage buildings owned by this group.")]
    public bool ManageBuildings { get; set; }

    [Display(Name = "Resources", Description = "The ability to trade resources as this group.")]
    public bool Resources { get; set; }

    [Display(Name = "Recipes", Description = "The ability to create and edit recipes owned by this group.")]
    public bool Recipes { get; set; }

    [Display(Name = "ManageDivisions", Description = "The ability to create, edit, and delete division templates, to start the training of new divisions, and delete divisions.")]
    public bool ManageDivisions { get; set; }

    [Display(Name = "ManageMilitary", Description = "The ability to create, edit, and delete Army Groups, Field Armies, etc and to add/remove commanders and divisions from them, and the ability to create, edit, and delete division templates, to start the training of new divisions, and delete divisions.")]
    public bool ManageMilitary { get; set; }

    public static CreateRoleModel FromExisting(GroupRole role)
    {
        CreateRoleModel model = new CreateRoleModel()
        {
            Color = role.Color,
            GroupId = role.GroupId,
            RoleId = role.Id,
            Authority = role.Authority,
            Name = role.Name,
            Salary = role.Salary
        };

        model.CreateRole = role.HasPermission(GroupPermissions.CreateRole);
        model.DeleteRole = role.HasPermission(GroupPermissions.DeleteRole);
        model.AddRole = role.HasPermission(GroupPermissions.AddMembersToRoles);
        model.RemoveRole = role.HasPermission(GroupPermissions.RemoveMembersFromRoles);
        model.ManageInvites = role.HasPermission(GroupPermissions.ManageInvites);
        model.ManageMembership = role.HasPermission(GroupPermissions.ManageMembership);
        model.Edit = role.HasPermission(GroupPermissions.Edit);
        model.Post = role.HasPermission(GroupPermissions.Post);;
        model.Eco = role.HasPermission(GroupPermissions.Eco);
        model.News = role.HasPermission(GroupPermissions.News);
        model.ManageBuildingRequests = role.HasPermission(GroupPermissions.ManageBuildingRequests);
        model.ManageProvinces = role.HasPermission(GroupPermissions.ManageProvinces);
        model.Build = role.HasPermission(GroupPermissions.Build);
        model.ManageBuildings = role.HasPermission(GroupPermissions.ManageBuildings);
        model.Resources = role.HasPermission(GroupPermissions.Resources);
        model.Recipes = role.HasPermission(GroupPermissions.Recipes);
        model.ManageMilitary = role.HasPermission(GroupPermissions.ManageMilitary);

        return model;
    }
}