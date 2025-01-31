using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WUG.Database.Models.Entities;
using WUG.Database.Models.Economy;
using Microsoft.EntityFrameworkCore;
using WUG.Database.Managers;
using WUG.Scripting;
using NationModifier = Shared.Models.Nations.NationModifier;

namespace WUG.Database.Models.Nations;

public class Nation
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("name", TypeName = "VARCHAR(64)")]
    public string? Name { get; set; }

    [NotMapped]
    public string ScriptName => Name.ToLower().Replace(" ", "_");

    [Column("description", TypeName = "VARCHAR(512)")]
    public string? Description { get; set; }

    public int ProvinceSlotsLeft { get; set; }

    [NotMapped]
    public List<Province> Provinces { get; set; }

    [NotMapped]
    public List<City> Cities => DBCache.GetAll<City>().Where(x => x.NationId == Id).ToList();

    [NotMapped]
    public List<User> Citizens => DBCache.GetAll<User>().Where(x => x.NationId == Id).ToList();

    [NotMapped]
    public List<State> States => DBCache.GetAll<State>().Where(x => x.NationId == Id).ToList();

    [NotMapped]
    public decimal GDP = 0.0m;

    [NotMapped]
    public long TotalPopulation
    {
        get
        {
            if (Provinces is not null)
                return Provinces.Sum(x => x.Population);
            return 0;
        }
    }

    [NotMapped]
    public Group Group => DBCache.Get<Group>(GroupId)!;

    [Column("groupid")]
    public long GroupId { get; set; }

    [NotMapped]
    public CouncilMember? Senator => DBCache.Get<CouncilMember>(Id);

    public double BasePopulationFromUsers { get; set; }

    [NotMapped]
    public double BaseProvincePopulation => BasePopulationFromUsers / Provinces.Count;

    [Column("governorid")]
    public long? GovernorId { get; set; }

    [NotMapped]
    public User? Governor => DBCache.Get<User>(GovernorId);

    [Column("flagurl", TypeName = "VARCHAR(256)")]
    public string? FlagUrl { get; set; }

    [Column("basepropertytax")]
    public double? BasePropertyTax { get; set; }

    [Column("propertytaxpersize")]
    public double? PropertyTaxPerSize { get; set; }

    [Column("staticmodifiers", TypeName = "jsonb[]")]
    public List<StaticModifier> StaticModifiers { get; set; } = new();

    [Column("titleforprovince")]
    public string? TitleForProvince { get; set; }

    [Column("titleforstate")]
    public string? TitleForState { get; set; }

    [Column("titleforgovernorofaprovince")]
    public string? TitleForGovernorOfProvince { get; set; }

    [Column("titleforgovernorofastate")]
    public string? TitleForGovernorOfState { get; set; }

    [Column("capitalprovinceid")]
    public long? CapitalProvinceId { get; set; }

    [NotMapped]
    public string NameForState => TitleForState is null ? "State" : TitleForState;

    [NotMapped]
    public string NameForProvince => TitleForProvince is null ? "Province" : TitleForProvince;

    [NotMapped]
    public string NameForGovernorOfAProvince => TitleForGovernorOfProvince is null ? "Governor" : TitleForGovernorOfProvince;

    [NotMapped]
    public string NameForGovernorOfAState => TitleForGovernorOfState is null ? "Governor" : TitleForGovernorOfState;

    [NotMapped]
    public Dictionary<NationModifierType, NationModifier> Modifiers = new();

    [NotMapped]
    public double EconomicScore { get; set; }

    [NotMapped]
    public string Color => Name switch
    {
        "Archelon Republic" => "B7BCFC",
        "The Astarian Egis" => "B8B7FD",
        "United States of Qortos" => "FEEAB7",
        "Isurium" => "F4B7FD",
        "The Procrastin Nation" => "B7FDE5",
        "Oglar" => "FDB7B7",
        "Fraisia" => "D3FCB6",
        "Arkoros" => "EAB7FC",
        "United Corporations of Adramat" => "B6EEFD", 
        "The Sublime State of the Fíkret" => "FAFDB8"
    };

    public static Nation Find(long id)
    {
        return DBCache.GetAll<Nation>().FirstOrDefault(x => x.Id == id)!;
    }

    public double GetModifierValue(NationModifierType modifierType)
    {
        if (!Modifiers.ContainsKey(modifierType))
            return 0;
        return Modifiers[modifierType].Amount;
    }

    [NotMapped]
    public List<Province> ProvincesByDevelopmnet { get; set; }

    [NotMapped]
    public List<Province> ProvincesByMigrationAttraction { get; set; }

    public void HourlyTick()
    {
        var populationtarget = Citizens.Count() * 2_500_000.0;
        populationtarget += 500_000.0;
        populationtarget += Provinces.Count() * 10_000;
        var diff = populationtarget - BasePopulationFromUsers;

        // 150,000/24 = 6250
        if (diff > 6250 || diff < -6250)
        {
            var change = diff > 6250 ? 6250.0 : -6250.0;
            change += diff * 0.005;
            BasePopulationFromUsers += change;
        }
        else
            BasePopulationFromUsers += diff;

        double totalattractionpoints = Provinces.Sum(x => Math.Pow(x.MigrationAttraction, 1.025));

        // do migration
        // this was very "fun" to code
        //double totalmigration = Provinces.Sum(x => x.Population) * Defines.NProvince[NProvince.BASE_MIGRATION_RATE] / 30 / 24;

        double totalmigration = 0;
        foreach (var province in Provinces)
        {
            double amountleavingmuit = 1;
            if (province.RankByDevelopment <= 15)
                amountleavingmuit = 1 - (Math.Pow(17 - province.RankByDevelopment, 0.15) - 1);
            var migration = province.PopulationMultiplier * Defines.NProvince[NProvince.BASE_MIGRATION_RATE];
            totalmigration += migration*amountleavingmuit;
        }

        totalmigration = totalmigration / 30 / 24;

        double migrantsperattraction = totalmigration / totalattractionpoints;

        long totalchange = 0;
        foreach(var province in Provinces)
        {
            double amountleavingmuit = 1;
            if (province.RankByDevelopment <= 15)
                amountleavingmuit = 1 - (Math.Pow(17 - province.RankByDevelopment, 0.15) - 1);

            double leaving = -(province.PopulationMultiplier * Defines.NProvince[NProvince.BASE_MIGRATION_RATE] / 30 / 24);
            leaving *= amountleavingmuit;

            double netchange = leaving;
            netchange += Math.Pow(province.MigrationAttraction, 1.025) * migrantsperattraction;

            province.PopulationMultiplier += netchange;

            province.MonthlyEstimatedMigrants = (int)(netchange * 30 * 24 * BaseProvincePopulation);
            totalchange += province.MonthlyEstimatedMigrants;
        }
    }

    public void UpdateOrAddModifier(NationModifierType type, double value) {
        if (!Modifiers.ContainsKey(type))
            Modifiers[type] = new() { Amount = value, ModifierType = type };
        else
            Modifiers[type].Amount += value;
    }

    public void UpdateModifiers() {
        Modifiers = new();
        var value_executionstate = new ExecutionState(this, null, parentscopetype:ScriptScopeType.Nation);
        //var scaleby_executionstate = new ExecutionState(Nation, this);
        foreach (var staticmodifier in StaticModifiers) {
            foreach (var modifiernode in staticmodifier.BaseStaticModifiersObj.ModifierNodes) {
                var value = (double)modifiernode.GetValue(value_executionstate, staticmodifier.ScaleBy);
                UpdateOrAddModifier((NationModifierType)modifiernode.nationModifierType!, value);
            }
            if (staticmodifier.BaseStaticModifiersObj.EffectBody is not null)
            {
                staticmodifier.BaseStaticModifiersObj.EffectBody.Execute(new(this, null, parentscopetype: ScriptScopeType.Nation));
            }
        }
    }

    public async Task<EconomicScoreReturnModel> GetEconomicScore()
    {
        var score = 0.0;
        score += Math.Pow(TotalPopulation, Defines.NScore[NScore.ECONOMIC_SCORE_FROM_POPULATION_EXPONENT]) / Defines.NScore[NScore.ECONOMIC_SCORE_FROM_POPULATION_DIVISOR];
        int mines = 0;
        int simplefactories = 0;
        int advancedfactories = 0;
        int infrastructure = 0;
        double minesswiththroughputfromupgrades = 0;
        double simplefactoriesswiththroughputfromupgrades = 0;
        double advancedfactorieswiththroughputfromupgrades = 0;
        double infrastructurewiththroughputfromupgrades = 0;
        foreach (var province in Provinces)
        {
            foreach (var building in province.GetBuildings())
            {
                if (building.BuildingType == BuildingType.Mine)
                {
                    mines += building.Size;
                    minesswiththroughputfromupgrades += building.Size * building.GetThroughputFromUpgrades();
                }
                if (building.BuildingType == BuildingType.Factory)
                {
                    if (building.LuaBuildingObjId.Contains("advanced"))
                    {
                        advancedfactories += building.Size;
                        advancedfactorieswiththroughputfromupgrades += building.Size * building.GetThroughputFromUpgrades();
                    }
                    else
                    {
                        simplefactories += building.Size;
                        simplefactoriesswiththroughputfromupgrades += building.Size * building.GetThroughputFromUpgrades();
                    }
                }

                if (building.BuildingType == BuildingType.Infrastructure)
                {
                    infrastructure += building.Size;
                    infrastructurewiththroughputfromupgrades += building.Size * building.GetThroughputFromUpgrades();
                }
            }

            var governor = province.GetGovernor();
            var rate_for_consumergood = (double)province.Population / 10_000 * (1 + province.GetModifierValue(ProvinceModifierType.ConsumerGoodsConsumptionFactor));
            double totalgrowthbuff = 0;
            foreach (var consumergood in GameDataManager.ConsumerGoods)
            {
                var toconsume = rate_for_consumergood * consumergood.consumerGood.PopConsumptionRate;
                if (await governor.HasEnoughResource(consumergood.LowerCaseName, toconsume))
                {
                    score += consumergood.consumerGood.EconomicScoreModifier * province.Population / 10_000.0 / 10;
                }
            }
        }
        score += minesswiththroughputfromupgrades * Defines.NScore[NScore.ECONOMIC_SCORE_PER_MINE];
        score += simplefactoriesswiththroughputfromupgrades * Defines.NScore[NScore.ECONOMIC_SCORE_PER_SIMPLE_FACTORY];
        score += advancedfactorieswiththroughputfromupgrades * Defines.NScore[NScore.ECONOMIC_SCORE_PER_ADVANCED_FACTORY];
        score += infrastructurewiththroughputfromupgrades * Defines.NScore[NScore.ECONOMIC_SCORE_PER_INFRASTRUCTURE];
        return new()
        {
            Nation = this,
            Score = score,
            Mines = mines,
            SimpleFactories = simplefactories,
            AdvancedFactories = advancedfactories,
            Infrastructure = infrastructure
        };
    }
}

public class EconomicScoreReturnModel
{
    public Nation Nation { get; set; }
    public double Score { get; set; }
    public int Mines { get; set; }
    public int SimpleFactories { get; set; }
    public int AdvancedFactories { get; set; }
    public int Infrastructure { get; set; }
}