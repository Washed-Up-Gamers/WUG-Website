using Shared.Models.Nations;
using Shared.Models.Nations.Modifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Lua.Scripting;

public class SyntaxModifierNode : SyntaxNode
{
    public NationModifierType? nationModifierType { get; set; }
    public ProvinceModifierType? provinceModifierType { get; set; }
    public EntityModifierType? entityModifierType { get; set; }
    public BuildingModifierType? buildingModifierType { get; set; }
    public ItemModifierType? itemModifierType { get; set; }
    public SyntaxNode Value { get; set; }

    public string GetColorClass(bool flip, decimal value)
    {
        bool Good = false;
        if (value > 0.0m)
            Good = true;
        if (flip) Good = !Good;
        if (Good) return "modifier-tooltip-modifier-listitem-good";
        else return "modifier-tooltip-modifier-listitem-bad";
    }

    public string GetHumanReadableName()
    {
        if (nationModifierType is not null)
        {
            return nationModifierType switch
            {
                NationModifierType.AllProducingBuildingThroughputFactor => "Buildings' Throughput",
                NationModifierType.BuildingSlotsExponent => "Exponent for Building Slots from Population",
                NationModifierType.BuildingSlotsFactor => "Building Slots",
                NationModifierType.OverPopulationModifierExponent => "Exponent for Overpopulation",
                _ => "[No Loc]"
            };
        }
        else if (entityModifierType is not null)
        {
            return entityModifierType switch
            {
                EntityModifierType.FactoryEfficiencyFactor => "Factories' Efficiency",
                EntityModifierType.FactoryThroughputFactor => "Factories' Throughput",
                EntityModifierType.FactoryQuantityCapFactor => "Factories' Quantity Cap",
                _ => "[No Loc]"
            };
        }
        else if (itemModifierType is not null)
        {
            return itemModifierType switch
            {
                ItemModifierType.Attack => "Attack",
                ItemModifierType.AttackFactor => "Attack Factor",
                _ => "[No Loc]"
            };
        }
        else
        {
            return provinceModifierType switch
            {
                ProvinceModifierType.AllProducingBuildingThroughputFactor => "Buildings' Throughput",
                ProvinceModifierType.BuildingSlotsExponent => "Exponent for Building Slots from Population",
                ProvinceModifierType.BuildingSlotsFactor => "Building Slots",
                ProvinceModifierType.OverPopulationModifierExponent => "Exponent for Overpopulation",
                _ => "[No Loc]"
            };
        }
    }

    public string GetColorClassForModifier(decimal value)
    {
        if (nationModifierType is not null)
        {
            return nationModifierType switch
            {
                NationModifierType.AllProducingBuildingThroughputFactor => GetColorClass(false, value),
                NationModifierType.BuildingSlotsExponent => GetColorClass(false, value),
                NationModifierType.BuildingSlotsFactor => GetColorClass(false, value),
                NationModifierType.OverPopulationModifierExponent => GetColorClass(true, value),
                _ => "modifier-tooltip-modifier-listitem-neutral"
            };
        }
        else if (entityModifierType is not null)
        {
            return entityModifierType switch
            {
                EntityModifierType.FactoryEfficiencyFactor => GetColorClass(false, value),
                EntityModifierType.FactoryQuantityCapFactor => GetColorClass(false, value),
                EntityModifierType.FactoryThroughputFactor => GetColorClass(false, value),
                _ => "modifier-tooltip-modifier-listitem-neutral"
            };
        }
        else if (itemModifierType is not null)
        {
            return itemModifierType switch
            {
                ItemModifierType.Attack => GetColorClass(false, value),
                ItemModifierType.AttackFactor => GetColorClass(false, value),
                _ => "modifier-tooltip-modifier-listitem-neutral"
            };
        }
        else
        {
            return provinceModifierType switch
            {
                ProvinceModifierType.AllProducingBuildingThroughputFactor => GetColorClass(false, value),
                ProvinceModifierType.BuildingSlotsExponent => GetColorClass(false, value),
                ProvinceModifierType.BuildingSlotsFactor => GetColorClass(false, value),
                ProvinceModifierType.OverPopulationModifierExponent => GetColorClass(true, value),
                _ => "modifier-tooltip-modifier-listitem-neutral"
            };
        }
    }

    public string GenerateHTMLForListing(ExecutionState state)
    {
        var value = Value.GetValue(state);
        var sign = "+";
        if (value < 0.0m) sign = "";
        string valuestring = "";
        if (nationModifierType is not null && nationModifierType.ToString().Contains("Factor")
            || provinceModifierType is not null && provinceModifierType.ToString().Contains("Factor")
            || entityModifierType is not null && entityModifierType.ToString().Contains("Factor")
            || itemModifierType is not null && itemModifierType.ToString().Contains("Factor"))
            valuestring = $"{value * 100:n2}%";
        else
            valuestring = $"{value:n3}";

        return $"<span class='{GetColorClassForModifier(value)}'>{sign}{valuestring}</span><span class='modifier-tooltip-listitem-name'> {GetHumanReadableName()}</span>";
    }

    public SyntaxModifierNode()
    {
        NodeType = NodeType.MODIFIER;
    }

    public override decimal GetValue(ExecutionState state)
    {
        return Value.GetValue(state);
    }

    public decimal GetValue(ExecutionState state, decimal scaleby)
    {
        return GetValue(state) * scaleby;
    }
}
