using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WUG.Database.Models.Entities;
using WUG.Database.Models.Items;
using WUG.Managers;
using WUG.Database.Managers;
using WUG.Scripting.Parser;

namespace WUG.Database.Models.Factories;

public interface IPowerProducingBuilding
{
    /// <summary>
    /// In $ per mWh
    /// </summary>
    public decimal SellingPrice { get; set; }
    public double PowerBrought { get; set; }
    public double MaxPowerProduction { get; }
    public Recipe Recipe { get; }
    public string Name { get; set; }
    public long Id { get; set; }
    public BaseEntity Owner { get; }
}

public class PowerPlant : ProducingBuilding, IPowerProducingBuilding
{
    /// <summary>
    /// In $ per mWh
    /// </summary>
    [DecimalType(4)]
    public decimal SellingPrice { get; set; }
    public double PowerBrought { get; set; }

    [NotMapped]
    public double MaxPowerProduction => GetRateForProduction() * Recipe.PowerOutput;
    public override BuildingType BuildingType { get => BuildingType.PowerPlant; }

    /// <summary>
    /// This function is called every IRL hour
    /// </summary>
    public override async ValueTask Tick()
    {
        UpdateModifiers();
        if (Quantity <= 0.1)
            Quantity = 0.1;

        if (Quantity < QuantityCap)
        {
            Quantity += QuantityHourlyGrowth;
        }
        
        double rate = GetRateForProduction();
        double rate_for_input = rate * (1/Efficiency);
        var currentMaxPowerProduction = rate_for_input;
        var ratio = PowerBrought / currentMaxPowerProduction;
        
        // power plants consume a min of 25% even when no power demand for the plant
        rate_for_input *= Math.Min(0.25, ratio);

        SuccessfullyTicked = false;
        foreach (var resourcename in Recipe.Inputs.Keys) {
            double amount = rate_for_input * Recipe.Inputs[resourcename];
            if (!await Owner.HasEnoughResource(resourcename, amount)) {
                var notification = new Notification() {
                    Id = IdManagers.GeneralIdGenerator.Generate(),
                    Title = "Building has ran out of resources!",
                    Content = $"{Owner.Name}'s {Name} ({BuildingObj.PrintableName}) has ran out of {DBCache.Get<ItemDefinition>(resourcename).Name}, and as a result could not produce anything!",
                    TimeSent = DateTime.UtcNow,
                    Seen = false,
                    Button1Text = "View Building",
                    Button1Link = $"/Building/Managae/{Id}"
                };
                if (Owner.EntityType is EntityType.User)
                    notification.UserId = Owner.Id;
                else
                    notification.UserId = ((Group)Owner).GetOwnershipChain().Last().Id;
                NotificationManager.notificationQueue.Enqueue(notification);
                return;
            }
        }
        foreach (var resourcename in Recipe.Inputs.Keys) {
            double amount = rate_for_input * Recipe.Inputs[resourcename];
            await Owner.ChangeResourceAmount(resourcename, -amount, $"Input for building {Name} ({BuildingObj.PrintableName})");
        }

        SuccessfullyTicked = true;
    }
}