﻿using WUG.Database.Managers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WUG.Database.Models.Nations;

public class City
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; }
    public long NationId { get; set; }

    [NotMapped]
    public Nation Nation => DBCache.Get<Nation>(NationId);

    public long ProvinceId { get; set; }

    [NotMapped]
    public Province Province => DBCache.Get<Province>(ProvinceId);

    public bool IsCapitalCity { get; set; }

    //[NotMapped]
    // this is generated upon server start
    //public List<NationModifier> Modifiers { get; set; }

    //public NationModifier GetModifier(NationModifierType type) => Modifiers.First(x => x.ModifierType == type);

    
}
