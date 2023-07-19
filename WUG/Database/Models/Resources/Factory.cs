using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WUG.Database.Models.Entities;
using WUG.Database.Models.Items;
using WUG.Managers;
using WUG.Database.Managers;

namespace WUG.Database.Models.Factories;

public class Factory : ProducingBuilding
{
    public override BuildingType BuildingType { get => BuildingType.Factory; }

    /// <summary>
    /// This function is called every IRL hour
    /// </summary>
    public override async ValueTask Tick()
    {
        if (Quantity <= 0.1)
            Quantity = 0.1;

        if (Quantity < QuantityCap)
        {
            Quantity += QuantityHourlyGrowth;
        }
        if (Quantity > QuantityCap)
            Quantity = QuantityCap;
    }
}