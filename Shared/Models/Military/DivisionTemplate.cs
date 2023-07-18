using Shared.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Shared.Models.Military;

public enum DivisionModifierType
{
    Attack = 0,
    Health = 1,
    Speed = 2,
    AttackFactor = 3
}
public class DivisionTemplate
{
    public long Id { get; set; }
    public long NationId { get; set; }
    public string Name { get; set; }
    public List<RegimentTemplate> RegimentsTemplates { get; set; }

    [NotMapped]
    public Dictionary<DivisionModifierType, double> Modifiers { get; set; }

    public double GetModifierValue(DivisionModifierType modifierType)
    {
        if (!Modifiers.ContainsKey(modifierType))
            return 0;
        return Modifiers[modifierType];
    }

    public void UpdateOrAddModifier(DivisionModifierType type, double value)
    {
        if (!Modifiers.ContainsKey(type))
            Modifiers[type] = value;
        else
            Modifiers[type] += value;
    }

    public async ValueTask UpdateModifiers()
    {
        Modifiers = new();

        foreach (var regiment in RegimentsTemplates)
        {
            await regiment.UpdateModifiers();
            foreach (var pair in regiment.Modifiers)
                UpdateOrAddModifier(pair.Key, pair.Value);
        }
    }

    public int GetSoldierCount() {
        var total = 0;
        foreach (var regiment in RegimentsTemplates)
            total += regiment.GetSoldierCount();
        return total;
    }
}

public class RegimentTemplate
{
    public long Id { get; set; }
    public string Name { get; set; }
    public RegimentType Type { get; set; }

    // number of things in this regiment
    // for example in an Infantry Regiment, Count will be the number of soldiers
    // only allowed values are in 1k increments for infantry and 1 increments for everything else
    public int Count { get; set; }
    public long WeaponItemDefinitionId { get; set; }
    public long AmmoItemDefinitionId { get; set; }

    [NotMapped]
    [JsonIgnore]
    public Dictionary<DivisionModifierType, double> Modifiers { get; set; }

    public async ValueTask<ItemDefinition> GetWeaponItemDefinitionAsync() => await ItemDefinition.FindAsync(WeaponItemDefinitionId);
    public async ValueTask<ItemDefinition> GetAmmoItemDefinitionAsync() => await ItemDefinition.FindAsync(AmmoItemDefinitionId);

    public double GetModifierValue(DivisionModifierType modifierType)
    {
        if (!Modifiers.ContainsKey(modifierType))
            return 0;
        return Modifiers[modifierType];
    }

    public int GetSoldierCount() {
        if (Type is RegimentType.Infantry)
            return Count * 1000;
        return Count;
    }

    public void UpdateOrAddModifier(DivisionModifierType type, double value)
    {
        if (!Modifiers.ContainsKey(type))
            Modifiers[type] = value;
        else
            Modifiers[type] += value;
    }

    public static Dictionary<ItemModifierType, DivisionModifierType> ConvertItemModifierToDivisionModifier = new()
    {
        { ItemModifierType.Attack, DivisionModifierType.Attack },
        { ItemModifierType.AttackFactor, DivisionModifierType.AttackFactor}
    };

    public async ValueTask UpdateModifiers()
    {
        Modifiers = new();
        if (WeaponItemDefinitionId != 0)
        {
            foreach (var pair in (await GetWeaponItemDefinitionAsync())!.Modifiers) {
                var key = ConvertItemModifierToDivisionModifier[pair.Key];
                if (key.ToString().Contains("Factor"))
                    UpdateOrAddModifier(key, pair.Value);
                else
                    UpdateOrAddModifier(key, pair.Value * Count);
            }
        }

        if (AmmoItemDefinitionId != 0)
        {
            foreach (var pair in (await GetAmmoItemDefinitionAsync())!.Modifiers) {
                var key = ConvertItemModifierToDivisionModifier[pair.Key];
                if (key.ToString().Contains("Factor"))
                    UpdateOrAddModifier(key, pair.Value);
                else
                    UpdateOrAddModifier(key, pair.Value * Count);
            }
        }
    }
}