﻿using Shared.Models.Nations;
using WUG.Database.Models.Groups;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WUG.Database.Models.Nations;
public class State {
    [Column("id")]
    public long Id { get; set; }

    [Column("name", TypeName = "VARCHAR(64)")]
    public string? Name { get; set; }

    [Column("description", TypeName = "VARCHAR(512)")]
    public string? Description { get; set; }

    [Column("mapcolor")]
    public string MapColor { get; set; }

    [Column("groupid")]
    public long GroupId { get; set; }

    [NotMapped]
    public Group Group => DBCache.Get<Group>(GroupId)!;

    [Column("Nationid")]
    public long NationId { get; set; }

    [NotMapped]
    [JsonIgnore]
    public Nation Nation => DBCache.Get<Nation>(NationId)!;

    [Column("governorid")]
    public long? GovernorId { get; set; }

    [NotMapped]
    public BaseEntity? Governor => BaseEntity.Find(GovernorId);

    /// <summary>
    /// In monthly rate
    /// </summary>
    public double? BasePropertyTax { get; set; }

    /// <summary>
    /// In monthly rate
    /// </summary>
    public double? PropertyTaxPerSize { get; set; }

    [NotMapped]
    public decimal GDP = 0.0m;

    [NotMapped]
    public IEnumerable<Province> Provinces => DBCache.GetAll<Province>().Where(x => x.StateId == Id);

    [NotMapped]
    public long Population => Provinces.Sum(x => x.Population);

    public bool CanEdit(BaseEntity entity) {
        if (entity.Id == Nation.GovernorId) return true;
        if (Governor is not null) {
            if (Governor.EntityType == EntityType.User)
                return GovernorId == entity.Id;
            else {
                Group governorasgroup = (Group)Governor;

                return governorasgroup.HasPermission(entity, GroupPermissions.FullControl);
            }
        }
        return false;
    }

    public bool CanManageBuildingRequests(BaseEntity entity) {
        if (entity.Id == Nation.GovernorId) return true;
        if (Governor is not null) {
            if (Governor.EntityType == EntityType.User)
                return GovernorId == entity.Id;
            else {
                Group governorasgroup = (Group)Governor;
                return governorasgroup.HasPermission(entity, GroupPermissions.ManageBuildingRequests);
            }
        }
        return false;
    }
}