using WUG.Database.Managers;
using WUG.Scripting.LuaObjects;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WUG.Scripting;
using WUG.Database.Models.Nations;
using System.Text.Json.Serialization;
using Shared.Models;
using WUG.Scripting.Parser;

namespace WUG.Database.Models.Buildings;

public enum BuildingType
{
    Mine = 0,
    Farm = 3,
    Factory = 1,
    Recruitment_Center = 2,
    Infrastructure = 4,
    ResearchLab = 5,
    PowerPlant = 6,
    Battery = 7
}

public interface ITickable
{
    public ValueTask Tick();
}

public abstract class BuildingBase : IHasOwner, ITickable
{
    [Key]
    public long Id { get; set; }
    public string? Name { get; set; }
    public long NationId { get; set; }
    public int Size { get; set; }
    public string PrevRecipeId { get; set; }
    public string RecipeId { get; set; }
    public abstract BuildingType BuildingType { get; }
    public string LuaBuildingObjId { get; set; }
    public string? Description { get; set; }
    public long ProvinceId { get; set; }
    public long OwnerId { get; set; }

    [JsonIgnore]
    public BaseEntity Owner => BaseEntity.Find(OwnerId)!;

    [NotMapped]
    [JsonIgnore]
    public Province Province => DBCache.Get<Province>(ProvinceId)!;

    [NotMapped]
    public Recipe Recipe => DBCache.Recipes[RecipeId];

    [NotMapped]
    public LuaBuilding BuildingObj => GameDataManager.BaseBuildingObjs[LuaBuildingObjId];

    [NotMapped]
    [JsonIgnore]
    public Nation Nation => DBCache.Get<Nation>(NationId)!;

    [NotMapped]
    public double ThroughputLossFromPowerGrid = 0.0;

    public static BuildingBase Find(long? id)
    {
        if (id == null) return null;

        BuildingBase obj = DBCache.Get<Factory>(id)!;
        if (obj is not null) return obj;

        obj = DBCache.Get<Mine>(id)!;
        if (obj is not null) return obj;

        return null;
    }

    public bool SuccessfullyTicked { get; set; }

    public virtual async ValueTask Tick() { }
}

public class BuildingUpgrade
{
    public string LuaBuildingUpgradeId { get; set; }

    [NotMapped]
    public LuaBuildingUpgrade LuaBuildingUpgradeObj => GameDataManager.BaseBuildingUpgradesObjs[LuaBuildingUpgradeId];

    public int Level { get; set; }
}

[JsonDerivedType(typeof(Factory), typeDiscriminator: 1)]
[JsonDerivedType(typeof(Mine), typeDiscriminator: 2)]
[JsonDerivedType(typeof(Farm), typeDiscriminator: 3)]
[JsonDerivedType(typeof(Infrastructure), typeDiscriminator: 4)]
[JsonDerivedType(typeof(PowerPlant), typeDiscriminator: 6)]
public abstract class ProducingBuilding : BuildingBase
{
    public ProducingBuilding() { }
    public long? EmployeeId { get; set; }

    /// <summary>
    /// The id of the group role that employees are auto-added to and get paid from
    /// </summary>
    [Column("employeegrouproleid")]
    public long? EmployeeGroupRoleId { get; set; }
    public double Quantity { get; set; }

    [NotMapped]
    public User? Employee => DBCache.Get<User>(EmployeeId);

    [NotMapped]
    public Dictionary<BuildingModifierType, double> Modifiers { get; set; }

    [Column("upgrades", TypeName = "jsonb[]")]
    public List<BuildingUpgrade>? Upgrades { get; set; } = new();

    [Column("staticmodifiers", TypeName = "jsonb[]")]
    public List<StaticModifier>? StaticModifiers { get; set; }

    [NotMapped]
    public double QuantityHourlyGrowth {
        get {
            double quantitychange = Defines.NProduction["BASE_QUANTITY_GROWTH_RATE"] / 24;
            quantitychange *= (QuantityCap * QuantityCap) / Quantity;
            return quantitychange * QuantityGrowthRateFactor;
        }
    }

    [NotMapped]
    public double Efficiency
    {
        get
        {
            double eff = 1.0;
            if (BuildingObj.BaseEfficiency is not null)
                eff = (double)BuildingObj.BaseEfficiency.GetValue(new ExecutionState(Nation, Province));

            if (BuildingType == BuildingType.Factory) {
                eff -= (((Size - 1) * Defines.NProduction["FACTORY_INPUT_EFFICIENCY_LOSS_PER_SIZE"]) - Defines.NProduction["FACTORY_INPUT_EFFICIENCY_LOSS_PER_SIZE"]);
                eff += Nation.GetModifierValue(NationModifierType.FactoryEfficiency);
                eff *= 1 + Nation.GetModifierValue(NationModifierType.FactoryEfficiencyFactor);
            }
            eff *= GetModifierValue(BuildingModifierType.EfficiencyFactor) + 1.00;
            return eff;
        }
    }

    [NotMapped]
    public double QuantityGrowthRateFactor
    {
        get
        {
            string type = BuildingType.ToString().ToUpper();
            return Defines.NProduction[$"BASE_{type}_QUANTITY_GROWTH_RATE_FACTOR"];
        }
    }

    [NotMapped]
    public double ThroughputFactor
    {
        get
        {
            var basevalue = BuildingType switch
            {
                BuildingType.Farm => 1 + Nation.GetModifierValue(NationModifierType.FarmThroughputFactor),
                BuildingType.Mine => 1 + Nation.GetModifierValue(NationModifierType.MineThroughputFactor),
                BuildingType.Factory => 1 + Nation.GetModifierValue(NationModifierType.FactoryThroughputFactor),
                _ => 1
            };
            basevalue *= BuildingType switch
            {
                BuildingType.Farm => 1 + Province.GetModifierValue(ProvinceModifierType.FarmThroughputFactor),
                BuildingType.Mine => 1 + Province.GetModifierValue(ProvinceModifierType.MineThroughputFactor),
                BuildingType.Factory => 1 + Province.GetModifierValue(ProvinceModifierType.FactoryThroughputFactor),
                _ => 1
            };

            if (BuildingObj.ApplyStackingBonus)
                basevalue *= 1+Math.Min(Defines.NProduction["STACKING_THROUGHPUT_BONUS"] * Size, Defines.NProduction["MAX_STACKING_THROUGHPUT_BONUS"]);

            if (BuildingType is BuildingType.Factory or BuildingType.PowerPlant)
            {
                var start = 6.0;
                var end = 1;
                var diff = start - end;
                var startdate = new DateTime(2023, 7, 20);
                var hourstotal = 24 * 7 * 6;
                var progress = Math.Max(0, (DateTime.UtcNow - startdate).TotalHours);
                var muit = end + (diff * (1 - (progress / hourstotal)));
                basevalue *= Math.Max(end, muit);
            }

            else if (BuildingType == BuildingType.Mine) {
                var start = 3.0;
                var end = 1;
                var diff = start - end;
                var startdate = new DateTime(2023, 7, 20);
                var hourstotal = 24 * 7 * 6;
                var progress = Math.Max(0, (DateTime.UtcNow - startdate).TotalHours);
                var muit = end + (diff * (1 - (progress / hourstotal)));
                basevalue *= Math.Max(end, muit);
            }

            if (LuaBuildingObjId == "building_pothium_factory")
                basevalue *= 0.1;

            basevalue *= GetModifierValue(BuildingModifierType.ThroughputFactor) + 1.00;
            basevalue *= Province.GetModifierValue(ProvinceModifierType.AllProducingBuildingThroughputFactor) + 1.00;
            basevalue *= Nation.GetModifierValue(NationModifierType.AllProducingBuildingThroughputFactor) + 1.00;

            if (EmployeeId is not null)
                basevalue *= 1.15;
            
            basevalue *= (1 - ThroughputLossFromPowerGrid);
            
            return basevalue;
        }
    }

    [NotMapped]
    public double QuantityCap
    {
        get
        {
            string type = BuildingType.ToString().ToUpper();
            return Defines.NProduction[$"BASE_{type}_QUANTITY_CAP"] + BuildingType switch
            {
                BuildingType.Farm => Nation.GetModifierValue(NationModifierType.FarmQuantityCap),
                BuildingType.Mine => Nation.GetModifierValue(NationModifierType.MineQuantityCap),
                BuildingType.Factory => Nation.GetModifierValue(NationModifierType.FactoryQuantityCap),
                BuildingType.PowerPlant => Nation.GetModifierValue(NationModifierType.PowerPlantQuantityCap),
                _ => 1
            };
        }
    }

    public double GetThroughputFromUpgrades()
    {
        double total = 1.0;
        foreach (var upgrade in Upgrades)
        {
            foreach (var node in upgrade.LuaBuildingUpgradeObj.ModifierNodes)
            {
                if (node.buildingModifierType == BuildingModifierType.ThroughputFactor)
                {
                    total *= ((double)node.GetValue(new(null, null, building: this, buildingUpgrade: upgrade)) * upgrade.Level) + 1.0;
                }
            }
        }
        return total;
    }

    public double GetProductionSpeed(bool useQuantity = true)
    {
        string type = BuildingType.ToString().ToUpper();
        double rate = Defines.NProduction[$"BASE_{type}_THROUGHPUT"];
        if (useQuantity)
            rate *= Quantity;

        rate *= ThroughputFactor;
        rate *= Recipe.PerHour;
        return rate;
    }

    public double GetRateForProduction() {
        double rate = 1;

        rate *= Size;

        rate *= Recipe.PerHour;

        rate *= Defines.NProduction[$"BASE_{BuildingType.ToString().ToUpper()}_THROUGHPUT"];

        rate *= Quantity;

        rate *= ThroughputFactor;

        return rate;
    }

    public double GetModifierValue(BuildingModifierType modifierType)
    {
        if (!Modifiers.ContainsKey(modifierType))
            return 0;
        return Modifiers[modifierType];
    }

    public double GetHourlyProduction(bool useQuantity = true) {
        return GetProductionSpeed(useQuantity) * Size;
    }

    public double MiningOutputFactor() {
        if (!Province.Metadata.Resources.ContainsKey(BuildingObj.MustHaveResource)) return 0.0;
        return Province.Metadata.Resources[BuildingObj.MustHaveResource]/10550.0;
    }

    public void UpdateOrAddModifier(BuildingModifierType type, double value, bool multiplier = false)
    {
        if (multiplier)
        {
            if (!Modifiers.ContainsKey(type))
                Modifiers[type] = value;
            else
                Modifiers[type] *= value + 1;
        }
        else
        {
            if (!Modifiers.ContainsKey(type))
                Modifiers[type] = value;
            else
                Modifiers[type] += value;
        }
    }

    public void UpdateModifiers()
    {
        Modifiers = new();
        var value_executionstate = new ExecutionState(Nation, Province, parentscopetype: ScriptScopeType.Building, building: this);
        //var scaleby_executionstate = new ExecutionState(Nation, this);
        foreach (var staticmodifier in StaticModifiers)
        {
            foreach (var modifiernode in staticmodifier.BaseStaticModifiersObj.ModifierNodes)
            {
                var value = (double)modifiernode.GetValue(value_executionstate, staticmodifier.ScaleBy);
                UpdateOrAddModifier((BuildingModifierType)modifiernode.buildingModifierType!, value);
            }
            if (staticmodifier.BaseStaticModifiersObj.EffectBody is not null)
            {
                staticmodifier.BaseStaticModifiersObj.EffectBody.Execute(new(Nation, Province, parentscopetype: ScriptScopeType.Building, building: this));
            }
        }

        value_executionstate = new ExecutionState(Nation, Province, parentscopetype: ScriptScopeType.Building, building: this);
        //var scaleby_executionstate = new ExecutionState(Nation, this);
        foreach (var upgrade in Upgrades)
        {
            foreach (var modifiernode in upgrade.LuaBuildingUpgradeObj.ModifierNodes)
            {
                var value = (double)modifiernode.GetValue(value_executionstate, upgrade.Level);
                UpdateOrAddModifier((BuildingModifierType)modifiernode.buildingModifierType!, value, true);
            }
        }
    }

    public async ValueTask<TaskResult> TickRecipe() {
        UpdateModifiers();

        if (PrevRecipeId != "" && PrevRecipeId is not null && PrevRecipeId != RecipeId && PrevRecipeId != BuildingObj.Recipes.FirstOrDefault()?.Id)
            Quantity /= 2.0;

        PrevRecipeId = RecipeId;

        if (BuildingType is BuildingType.PowerPlant or BuildingType.Battery)
            return new(true, "");

        double rate = GetRateForProduction();
        if (!Recipe.BaseRecipe.Inputcost_Scaleperlevel)
            rate /= Size;
        double rate_for_input = rate * (1/Efficiency);
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
                return new(false, "Owner lacks enough resources to tick this building");
            }
        }
        foreach (var resourcename in Recipe.Inputs.Keys) {
            double amount = rate_for_input * Recipe.Inputs[resourcename];
            await Owner.ChangeResourceAmount(resourcename, -amount, $"Input for building {Name} ({BuildingObj.PrintableName})");
        }

        // do output handling now
        foreach (var resourcename in Recipe.Outputs.Keys) {
            double amount = rate * Recipe.Outputs[resourcename];
            if (BuildingObj.MustHaveResource is not null)
            {
                amount *= MiningOutputFactor();
                var policies = DBCache.GetAll<TaxPolicy>().Where(x => (x.NationId == NationId || x.NationId == 100) && x.taxType == TaxType.ResourceMined && x.Target == BuildingObj.MustHaveResource);
                foreach (var policy in policies)
                {
                    if (policy is not null)
                    {
                        decimal due = policy.GetTaxAmountForResource((decimal)amount);
                        policy.Collected += due;
                        var taxtrans = new Transaction(Owner, BaseEntity.Find(policy.NationId), due, TransactionType.TaxPayment, $"Tax payment for transaction id: {Id}, Tax Id: {policy.Id}, Tax Type: {policy.taxType}");
                        taxtrans.NonAsyncExecute(true);
                    }
                }
            }
            await Owner.ChangeResourceAmount(resourcename, amount, $"Output for building {Name} ({BuildingObj.PrintableName})");
        }

        SuccessfullyTicked = true;

        return new(true, "");
    }

    public double OutputPerHourPerSize(string resource)
    {
        return 0;
        //return Recipe.Outputs.FirstOrDefault(x => x.Key == resource).Value * GetProductionSpeed();
    }

    public bool CanManage(BaseEntity entity)
    {
        if (OwnerId == entity.Id || (Owner.EntityType != EntityType.User && ((Group)Owner).HasPermission(entity, GroupPermissions.ManageBuildings)))
            return true;
        return false;
    }
}

public enum BuildingModifierType
{
    ThroughputFactor,
    EfficiencyFactor
}