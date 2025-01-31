using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WUG.Database.Models.Entities;
using WUG.Database.Models.Items;
using WUG.Database.Managers;

namespace WUG.Database.Models.Factories;

public class Mine : ProducingBuilding
{
    public override BuildingType BuildingType { get => BuildingType.Mine; }

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