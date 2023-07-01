using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WUG.Database.Models.Entities;
using WUG.Database.Models.Economy;
using WUG.Web;
using System.Text.Json.Serialization;

namespace WUG.Database.Models.Items;

public class SVItemOwnership : IHasOwner
{
    [Key]
    [Column("id")]
    public long Id {get; set; }

    [Column("ownerid")]
    public long OwnerId { get; set; }

    [NotMapped]
    [JsonIgnore]
    public BaseEntity Owner => BaseEntity.Find(OwnerId)!;
    
    [Column("definitionid")]
    public long DefinitionId { get; set; }
    
    [NotMapped]
    [JsonIgnore]
    public ItemDefinition Definition => DBCache.Get<ItemDefinition>(DefinitionId)!;

    [Column("amount", TypeName = "numeric(16, 2)")]
    public double Amount { get; set;}
}