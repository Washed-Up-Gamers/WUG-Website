using WUG.Database.Managers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WUG.Database.Models.Districts;

public class City
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; }
    public long DistrictId { get; set; }

    [NotMapped]
    public Nation District => DBCache.Get<Nation>(DistrictId);

    public long ProvinceId { get; set; }

    [NotMapped]
    public Province Province => DBCache.Get<Province>(ProvinceId);

    public bool IsCapitalCity { get; set; }

    //[NotMapped]
    // this is generated upon server start
    //public List<DistrictModifier> Modifiers { get; set; }

    //public DistrictModifier GetModifier(DistrictModifierType type) => Modifiers.First(x => x.ModifierType == type);

    
}
