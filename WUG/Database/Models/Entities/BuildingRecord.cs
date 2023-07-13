using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WUG.Database.Models.Entities;

public enum BuildingRecordType {
    NewlyBuilt = 0,
    Expansion = 1,
    Upgrade = 2,
    OwnershipChange = 3
}

[PrimaryKey(nameof(BuildingId), nameof(OwnerId), nameof(Time))]
[Index(nameof(Time))]
[Index(nameof(BuildingId))]
[Index(nameof(OwnerId))]
public class BuildingRecord 
{
    public long BuildingId { get; set; }
    public long OwnerId { get; set; }
    public DateTime Time { get; set; }
    public BuildingRecordType RecordType { get; set; }
    public int? BuildingUpradeNumId { get; set; }

    /// <summary>
    /// The number of levels either that have been added to the building or that have been added to the upgrade
    /// </summary>
    public int? LevelsChanged { get; set; }
    public long? PrevOwnerId { get; set; }
    public long? NewOwnerId { get; set; }

    [ForeignKey(nameof(BuildingId))]
    public ProducingBuilding Building { get; set; }
}