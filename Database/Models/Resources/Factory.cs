using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SV2.Database.Models.Entities;
using SV2.Database.Models.Items;
using SV2.Managers;

namespace SV2.Database.Models.Factories;

public class Factory : IHasOwner, IBuilding
{
    [Key]
    public long Id {get; set; }

    [VarChar(64)]
    public string? Name { get; set; }

    [VarChar(1024)]
    public string? Description { get; set; }

    public long OwnerId { get; set; }

    [NotMapped]
    public IEntity Owner { 
        get {
            return IEntity.Find(OwnerId)!;
        }
    }

    public long? RecipeId { get; set; }
    
    [NotMapped]
    public Recipe? recipe {
        get {
            if (RecipeId is null)
                return null;
            return DBCache.Get<Recipe>((long)RecipeId);
        }
    }

    public long? EmployeeId { get; set; }

    // effects production speed, grows over time, min value is 10%
    public double Quantity { get; set; }

    // base is 1x
    public double QuantityGrowthRate { get; set; }


    public double QuantityCap { get; set; }

    public double Efficiency { get; set; }

    // default is 1, size directly increases output, but harms efficiency
    // max value is 10, and at 10, it costs 4x more input to produce the same output

    public int Size { get; set; }

    public int HoursSinceChangedProductionRecipe { get; set; }

    // every tick (1 hour), Age increases by 1
    public int Age { get; set; }

    public double LeftOver { get; set; }

    [NotMapped]
    public BuildingType buildingType { 
        get {
            return BuildingType.Factory;
        }
    }

    public long ProvinceId { get; set; } 

    [NotMapped]
    public Province Province {
        get {
            return DBCache.Get<Province>(ProvinceId)!;
        }
    }

    public string GetProduction()
    {
        if (recipe is null)
        {
            return "";
        }
        string output = "";
        output += $"{recipe.Output.Key}, ";
        if (output != "") {
            output = output.Substring(0, output.Length-2);
        }
        return output;
    }

    public Factory()
    {
        
    }

    public Factory(long ownerid, long provinceid)
    {
        // why so many variables
        Id = IdManagers.FactoryIdGenerator.Generate();
        OwnerId = ownerid;
        ProvinceId = provinceid;
        Quantity = 0.1;
        QuantityCap = 1;
        QuantityGrowthRate = 1;
        Efficiency = 1;
        Size = 1;
        HoursSinceChangedProductionRecipe = 1;
        Age = 1;
    }

    /// <summary>
    /// This function is called every IRL hour
    /// </summary>

    public async Task Tick(List<TradeItem> tradeItems)
    {

        if (RecipeId is null) {
            return;
        }

        // TODO: when we add district stats (industal stat, etc) update this
        double rate = Size;

        if (EmployeeId != null) {
            // 2.5x production boost if this factory has an employee
            rate *= 2.5;
        };

        // ((A2^1.2/1.6)-1)/1000

        // ex:
        // 10 days : 1% lost
        // 100 days: 15.8% lost
        // 300 days: 58.8% lost

        double AgeProductionLost = ( (Math.Pow(Age, 1.2) / 1.6)-1 ) / 1000;

        rate *= 1-AgeProductionLost;

        // tick Quantity system

        // ex:
        // 3 days : 26.24%
        // 11 days: 57.28%
        // 32 days: 82.78% 

        QuantityCap = 1+Province.Owner.GetModifier(DistrictModifierType.FactoryQuantityCap);

        if (Quantity < QuantityCap) {
            HoursSinceChangedProductionRecipe += 1;
            double days = HoursSinceChangedProductionRecipe/24;
            double newQuantity = Math.Max(QuantityCap, Math.Log10( Math.Pow(days, 20) / 40));
            newQuantity = Math.Min(0.1+Province.Owner.GetModifier(DistrictModifierType.FactoryBaseQuantity), newQuantity);
            newQuantity *= QuantityGrowthRate*Province.Owner.GetModifier(DistrictModifierType.FactoryQuantityGrowthRateFactor);

            Quantity = newQuantity;
        }

        rate *= Quantity;

        rate *= recipe.HourlyProduction;

        // apply district modifers
        rate *= Province.Owner.GetModifier(DistrictModifierType.FactorySpeedFactor);

        // update Efficiency

        Efficiency = 1;
        // apply size debuff to Efficiency
        // we subtract 0.4 since at size 1 there is no debuff
        Efficiency += Size*0.4-0.4;

        Efficiency *= Province.Owner.GetModifier(DistrictModifierType.FactoryEfficiencyFactor);

        TradeItem? item = null;

        string output = recipe.Output.Key;
        // find the tradeitem
        item = tradeItems.FirstOrDefault(x => x.Definition.Name == output && x.Definition.OwnerId == 100 && x.OwnerId == OwnerId);
        if (item is null) {
            item = new()
            {
                Id = IdManagers.ItemIdGenerator.Generate(),
                OwnerId = OwnerId,
                Definition_Id = DBCache.GetAll<TradeItemDefinition>().FirstOrDefault(x => x.Name == output && x.OwnerId == 100)!.Id,
                Amount = 0
            };
            await DBCache.Put<TradeItem>(item.Id, item);
            await VooperDB.Instance.TradeItems.AddAsync(item);
            await VooperDB.Instance.SaveChangesAsync();
        }
        rate *= recipe.Output.Value;
        int wholerate = (int)Math.Floor(rate);
        LeftOver += rate-wholerate;
        if (LeftOver >= 1.0) {
            wholerate += 1;
            LeftOver -= 1.0;
        }
        foreach(string Resource in recipe.Inputs.Keys) 
        {
            item = tradeItems.FirstOrDefault(x => x.Definition.Name == Resource && x.Definition.OwnerId == 100 && x.OwnerId == OwnerId);
            if (item is null) {
                return;
            }
            int amountNeeded = (int)(recipe.Inputs[Resource]*wholerate/Efficiency);
            if (item.Amount < amountNeeded) {
                return;
            }
        }
        foreach(string Resource in recipe.Inputs.Keys) 
        {
            // find the tradeitem
            item = tradeItems.FirstOrDefault(x => x.Definition.Name == Resource && x.Definition.OwnerId == 100 && x.OwnerId == OwnerId);
            int amountNeeded = (int)(recipe.Inputs[Resource]*wholerate/Efficiency);
            item.Amount -= amountNeeded;
        }
        item.Amount += wholerate;
    }

}