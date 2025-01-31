﻿using WUG.Scripting;
using WUG.Scripting.LuaObjects;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WUG.Database.Models.Nations;
public class StaticModifier
{
    public bool Decay { get; set; }
    public DateTime StartDate { get; set; }

    /// <summary>
    /// In hours (ticks)
    /// </summary>
    public int? Duration { get; set; }
    public decimal ScaleBy { get; set; }
    public string LuaStaticModifierObjId { get; set; }

    [NotMapped]
    [JsonIgnore]
    public LuaStaticModifier BaseStaticModifiersObj => GameDataManager.BaseStaticModifiersObjs[LuaStaticModifierObjId];

    public string GenerateHtmlForTooltip(Nation Nation, Province? province = null) {
        var html = $"<span class='modifier-tooltip-name'><b>{BaseStaticModifiersObj.Name}</b></span>";
        if (BaseStaticModifiersObj.Description is not null)
            html += $"<br/><span class='modifier-tooltip-description'>{BaseStaticModifiersObj.Description}</span>";
        var state = new ExecutionState(Nation, province);
        foreach (var item in BaseStaticModifiersObj.ModifierNodes) {
            html += "<br/>"+item.GenerateHTMLForListing(state);
        }
        if (Duration is not null && Duration > 0)
            html += $"<br/><span class'modifier-tooltip-remainingtime'>1 Week left</span>";
        return html;
    }

    public Shared.Models.Nations.StaticModifier ToModel()
    {
        return new()
        {
            Decay = Decay,
            StartDate = StartDate,
            Duration = Duration,
            ScaleBy = ScaleBy,
            LuaStaticModifierObjId = LuaStaticModifierObjId
        };
    }
}