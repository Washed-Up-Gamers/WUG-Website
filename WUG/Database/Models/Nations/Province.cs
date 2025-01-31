using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WUG.Database.Models.Entities;
using WUG.Database.Managers;
using WUG.NonDBO;
using WUG.Managers;
using WUG.Scripting.LuaObjects;
using WUG.Database.Models.Users;
using WUG.Scripting;
using ProvinceModifier = Shared.Models.Nations.ProvinceModifier;
using Shared.Models.Nations;

namespace WUG.Database.Models.Nations;

public class ProvinceConsumerGoodsData
{
    public string ConsumerGood { get; set; }
    public double AmountNeeded { get; set; }
    public double BuffToBirthRate { get; set; }
    public double BuffToGrowth { get; set; }
}

public class Province
{
    [Key]
    public long Id { get; set; }

    [VarChar(64)]
    public string? Name { get; set; }

    public long NationId { get; set; }

    [NotMapped]
    [JsonIgnore]
    public Nation Nation { get; set; }

    public long? CityId { get; set; }

    public int BuildingSlots { get; set; }

    public long Population { get; set; }

    // brought to you by the Vooperians For Wokism (VFW)
    /// <summary>
    /// This multiples the "base population" we get from the Nation
    /// </summary>
    public double PopulationMultiplier { get; set; }

    public string? Description { get; set; }

    public long? GovernorId { get; set; }

    [NotMapped]
    [JsonIgnore]
    public BaseEntity? Governor => BaseEntity.Find(GovernorId);

    public long? StateId { get; set; }

    [NotMapped]
    [JsonIgnore]
    public State? State => DBCache.Get<State>(StateId);

    /// <summary>
    /// How "developed" this province is
    /// </summary>
    public int DevelopmentValue { get; set; }
    public int BaseDevelopmentValue { get; set; }
    public int LastTickDevelopmentValue { get; set; }
    public int MigrationAttraction { get; set; }

    /// <summary>
    /// In monthly rate
    /// </summary>
    public double? BasePropertyTax { get; set; }

    /// <summary>
    /// In monthly rate
    /// </summary>
    public double? PropertyTaxPerSize { get; set; }

    [NotMapped]
    [JsonIgnore]
    public ProvinceDevelopmentStage CurrentDevelopmentStage { get; set; }

    [NotMapped]
    [JsonIgnore]
    public Dictionary<ProvinceModifierType, ProvinceModifier> Modifiers { get; set; } = new();

    [Column("staticmodifiers", TypeName = "jsonb[]")]
    public List<StaticModifier> StaticModifiers { get; set; } = new();

    [NotMapped]
    public string MapColor => Nation?.Color ?? "ffffff";

    [NotMapped]
    public ProvinceMetadata Metadata => ProvinceManager.ProvincesMetadata[Id];

    [NotMapped]
    public int MonthlyEstimatedMigrants { get; set; }

    [NotMapped]
    public int RankByDevelopment { get; set; }

    [NotMapped]
    public int RankByMigrationAttraction { get; set; }

    [NotMapped]
    public int BuildingSlotsUsed => GetBuildings().Where(x => x.BuildingObj.UseBuildingSlots).Sum(x => x.Size);

    public Province() { }

    public Province(Random rnd)
    {
        StaticModifiers = new();
        Modifiers = new();
        int rngNum = rnd.Next(0, 101);
        if (rngNum >= 95)
            Population = rnd.NextInt64(100_000, 300_000);
        else if (rngNum >= 40)
            Population = rnd.NextInt64(25_000, 100_000);
        else
            Population = rnd.NextInt64(2_500, 30_000);
        PopulationMultiplier = 0.25;
    }

    public List<(string modifiername, double value)> GetStaticModifiersOfTypes(List<ProvinceModifierType?>? provincetypes, List<NationModifierType?> Nationtypes, bool AlsoUseNationModifiers, bool UseProvinceModifiers = true, bool IncludeDevStage = false)
    {
        if (provincetypes is null)
            provincetypes = new();
        if (Nationtypes is null)
            Nationtypes = new();
        var result = new List<(string modifiername, double value)>();
        var modifiers = new List<StaticModifier>();
        if (UseProvinceModifiers)
            modifiers.AddRange(StaticModifiers);
        if (AlsoUseNationModifiers)
            modifiers.AddRange(Nation.StaticModifiers);
        foreach (var modifier in modifiers)
        {
            foreach (var node in modifier.BaseStaticModifiersObj.ModifierNodes)
            {
                if ((provincetypes.Contains(node.provinceModifierType) && node.provinceModifierType is not null) || (Nationtypes.Contains(node.nationModifierType) && node.nationModifierType is not null))
                {
                    (string modifiername, double value) item = new()
                    {
                        modifiername = modifier.BaseStaticModifiersObj.Name,
                        value = (double)node.GetValue(new(Nation, this, null, (node.provinceModifierType is not null ? ScriptScopeType.Province : ScriptScopeType.Nation)))
                    };
                    if ((provincetypes.Contains(node.provinceModifierType) && node.provinceModifierType is not null && node.provinceModifierType.ToString().Contains("Factor"))
                        || (Nationtypes.Contains(node.nationModifierType) && node.nationModifierType is not null && node.nationModifierType.ToString().Contains("Factor")))
                    {
                        item.value += 1;
                    }
                    result.Add(item);
                }
            }
        }
        if (IncludeDevStage)
        {
            foreach (var node in CurrentDevelopmentStage.ModifierNodes)
            {
                if ((provincetypes.Contains(node.provinceModifierType) && node.provinceModifierType is not null) || (Nationtypes.Contains(node.nationModifierType) && node.nationModifierType is not null))
                {
                    (string modifiername, double value) item = new()
                    {
                        modifiername = CurrentDevelopmentStage.PrintableName,
                        value = (double)node.GetValue(new(Nation, this, null, (node.provinceModifierType is not null ? ScriptScopeType.Province : ScriptScopeType.Nation)))
                    };
                    if ((provincetypes.Contains(node.provinceModifierType) && node.provinceModifierType is not null && node.provinceModifierType.ToString().Contains("Factor"))
                        || (Nationtypes.Contains(node.nationModifierType) && node.nationModifierType is not null && node.nationModifierType.ToString().Contains("Factor")))
                    {
                        item.value += 1;
                    }
                    result.Add(item);
                }
            }
        }
        return result;
    }

    public List<(string modifiername, double value)> GetStaticModifiersOfType(ProvinceModifierType? provincetype, NationModifierType? Nationtype, bool AlsoUseNationModifiers, bool UseProvinceModifiers = true, bool IncludeDevStage = false) {
        var result = new List<(string modifiername, double value)>();
        var modifiers = new List<StaticModifier>();
        if (UseProvinceModifiers)
            modifiers.AddRange(StaticModifiers);
        if (AlsoUseNationModifiers)
            modifiers.AddRange(Nation.StaticModifiers);
        foreach (var modifier in modifiers) {
            foreach (var node in modifier.BaseStaticModifiersObj.ModifierNodes) {
                if ((node.provinceModifierType == provincetype && node.provinceModifierType is not null) || (node.nationModifierType == Nationtype && node.nationModifierType is not null)) {
                    (string modifiername, double value) item = new() {
                        modifiername = modifier.BaseStaticModifiersObj.Name,
                        value = (double)node.GetValue(new(Nation, this, null, (node.provinceModifierType is not null ? ScriptScopeType.Province : ScriptScopeType.Nation)))
                    }; 
                    if ((node.provinceModifierType == provincetype && node.provinceModifierType is not null && node.provinceModifierType.ToString().Contains("Factor") )
                        || (node.nationModifierType == Nationtype && node.nationModifierType is not null && node.nationModifierType.ToString().Contains("Factor"))) {
                        item.value += 1;
                    }
                    result.Add(item);
                }
            }
        }
        if (IncludeDevStage && CurrentDevelopmentStage is not null) {
            foreach (var node in CurrentDevelopmentStage.ModifierNodes) {
                if ((node.provinceModifierType == provincetype && node.provinceModifierType is not null) || (node.nationModifierType == Nationtype && node.nationModifierType is not null)) {
                    (string modifiername, double value) item = new() {
                        modifiername = CurrentDevelopmentStage.PrintableName,
                        value = (double)node.GetValue(new(Nation, this, null, (node.provinceModifierType is not null ? ScriptScopeType.Province : ScriptScopeType.Nation)))
                    };
                    if ((node.provinceModifierType == provincetype && node.provinceModifierType is not null && node.provinceModifierType.ToString().Contains("Factor"))
                        || (node.nationModifierType == Nationtype && node.nationModifierType is not null && node.nationModifierType.ToString().Contains("Factor"))) {
                        item.value += 1;
                    }
                    result.Add(item);
                }
            }
        }
        return result;
    }

    public double GetMiningResourceProduction(string resource)
    {
        if (!Metadata.Resources.ContainsKey(resource))
            return 0;
        var modifiervalue = GetModifierValue(ProvinceModifierType.MineThroughputFactor) + GetModifierValue(ProvinceModifierType.AllProducingBuildingThroughputFactor);
        modifiervalue += 1;
        return Metadata.Resources[resource] * modifiervalue;
    }

    public string GetMapColorForResourceDensity(double max, string resource, bool returnraw = false)
    {
        Color color = new(0, 0, 0);
        if (Metadata.Resources.ContainsKey(resource) && max > 0.01)
        {
            var amount = GetMiningResourceProduction(resource);
            var scale = amount / max;
            color.R = (int)(255 * scale);
            color.G = (int)(255 * scale);
            color.B = (int)(255 * scale);
        }
        if (!returnraw)
        {
            return $"rgb({color.R}, {color.G}, {color.B})";
        }
        else
        {
            return $"{color.R}, {color.G}, {color.B}";
        }
    }

    public string GetDevelopmentColorForMap(double scaleRequiredDevValueBy)
    {
        DevelopmentMapColor currentmapcolor = null;
        DevelopmentMapColor nextmapcolor = null;
        scaleRequiredDevValueBy = 1 / scaleRequiredDevValueBy;

        int index = 0;
        while ((nextmapcolor is null || nextmapcolor.MaxValue < DevelopmentValue*scaleRequiredDevValueBy) && index < ProvinceManager.DevelopmentMapColors.Count)
        {
            currentmapcolor = nextmapcolor;
            nextmapcolor = ProvinceManager.DevelopmentMapColors[index];
            index += 1;
        }

        Color color = new(0, 0, 0);
        if (currentmapcolor is not null)
        {
            int diff = nextmapcolor.MaxValue - currentmapcolor.MaxValue;
            float progress = ((float)((DevelopmentValue*scaleRequiredDevValueBy) - currentmapcolor.MaxValue) / (float)diff);
            color = new()
            {
                R = (int)(currentmapcolor.color.R * (1 - progress)),
                G = (int)(currentmapcolor.color.G * (1 - progress)),
                B = (int)(currentmapcolor.color.B * (1 - progress))
            };

            color.R += (int)(nextmapcolor.color.R * progress);
            color.G += (int)(nextmapcolor.color.G * progress);
            color.B += (int)(nextmapcolor.color.B * progress);
        }
        else
        {
            color = new(nextmapcolor.color.R, nextmapcolor.color.G, nextmapcolor.color.B);
        }

        if (color.R > 255) { color.R = 255; }
        if (color.G > 255) { color.G = 255; }
        if (color.B > 255) { color.B = 255; }

        return $"rgb({color.R}, {color.G}, {color.B})";
    }

    public long GetLevelsOfBuildingsOfType(string type) {
        BuildingType buildingtype = Enum.Parse<BuildingType>(type, true);
        return GetBuildings().Where(x => x.BuildingType == buildingtype).Sum(x => x.Size);
    }

    public IEnumerable<ProducingBuilding> GetBuildings()
    {
        return DBCache.ProvincesBuildings[Id];
    }

    /// <summary>
    /// Returns the governor of this province, or if governor is null, then the state
    /// If the state's is null, then the Nation
    /// </summary>
    /// <returns></returns>
    public BaseEntity GetGovernor()
    {
        if (Governor is not null)
            return Governor;
        else if (State is not null)
            return State.Group;
        else
            return Nation.Group;
    }

    public bool CanManageBuildingRequests(BaseEntity entity) {
        if (NationId == 100) return false;
        if (entity.Id == Nation.GovernorId) return true;
        if (State is not null && State.Governor is not null) {
            if (State.Governor.EntityType == EntityType.User) {
                if (State.GovernorId == entity.Id) {
                    return true;
                }
            }
            else {
                Group governorasgroup = (Group)State.Governor;
                if (governorasgroup.HasPermission(entity, GroupPermissions.ManageBuildingRequests))
                    return true;
            }
        }
        if (Governor is not null) {
            if (Governor.EntityType == EntityType.User)
                return GovernorId == entity.Id;
            else {
                Group governorasgroup = (Group)Governor;
                return governorasgroup.HasPermission(entity, GroupPermissions.ManageBuildingRequests);
            }
        }
        return false;
    }

    public bool CanEdit(BaseEntity entity)
    {
        if (entity.Id == Nation.GovernorId) return true;
        if (State is not null && State.Governor is not null) {
            if (State.Governor.EntityType == EntityType.User) {
                if (State.GovernorId == entity.Id) {
                    return true;
                }
            }
            else {
                Group governorasgroup = (Group)State.Governor;
                if (governorasgroup.HasPermission(entity, GroupPermissions.ManageBuildingRequests))
                    return true;
            }
        }
        if (Governor is not null)
        {
            if (Governor.EntityType == EntityType.User)
                return GovernorId == entity.Id;
            else
            {
                Group governorasgroup = (Group)Governor;
                return governorasgroup.HasPermission(entity, GroupPermissions.ManageProvinces);
            }
        }
        return false;
    }

    public double GetModifierValue(ProvinceModifierType modifierType) {
        if (!Modifiers.ContainsKey(modifierType))
            return 0;
        return Modifiers[modifierType].Amount;
    }

    public double GetOverpopulationModifier()
    {
        var exponent = Defines.NProvince[NProvince.OVERPOPULATION_MODIFIER_EXPONENT];
        exponent += GetModifierValue(ProvinceModifierType.OverPopulationModifierExponent);
        exponent += Nation.GetModifierValue(NationModifierType.OverPopulationModifierExponent);
        if (Id == Nation.CapitalProvinceId)
            exponent -= 0.01;
        var population = Population + GetModifierValue(ProvinceModifierType.OverPopulationModifierPopulationBase);
        if (population < 2500) population = 2500;
        var rate = Math.Pow(population, exponent) / 100.0;
        rate += Defines.NProvince[NProvince.OVERPOPULATION_MODIFIER_BASE];
        if (rate > 0)
            return rate;
        return 0.00;
    }

    public async ValueTask<double> GetMonthlyPopulationChangeFromGrowth()
    {
        // result is the monthly change in the PopulationMultiplier
        var muitgrowth = (await GetMonthlyPopulationGrowth(false)).growthrate;
        var ratio = muitgrowth / PopulationMultiplier;
        return Population * ratio;
    }

    public async ValueTask<(double growthrate, List<ProvinceConsumerGoodsData> ConsumerGoodsData)> GetMonthlyPopulationGrowth(bool UseResources = false)
    {
        double BirthRate = Defines.NProvince["BASE_BIRTH_RATE"];
        BirthRate += Nation.GetModifierValue(NationModifierType.MonthlyBirthRate);
        BirthRate *= Nation.GetModifierValue(NationModifierType.MonthlyBirthRateFactor) + 1;

        var governor = GetGovernor();
        List<ProvinceConsumerGoodsData> consumerGoodsData = new();
        var rate_for_consumergood = (double)Population / 10_000 * (1 + GetModifierValue(ProvinceModifierType.ConsumerGoodsConsumptionFactor));
        double totalgrowthbuff = 0;
        foreach (var consumergood in GameDataManager.ConsumerGoods)
        {
            var toconsume = rate_for_consumergood * consumergood.consumerGood.PopConsumptionRate;
            var data = new ProvinceConsumerGoodsData()
            {
                ConsumerGood = consumergood.Name,
                AmountNeeded = toconsume,
                BuffToGrowth = 0,
                BuffToBirthRate = 0
            };
            if (await governor.HasEnoughResource(consumergood.LowerCaseName, toconsume))
            {
                var buff = consumergood.consumerGood.PopGrowthRateModifier * (1 + GetModifierValue(ProvinceModifierType.ConsumerGoodsModifierFactor));
                totalgrowthbuff += buff;
                BirthRate += Math.Sqrt(buff*100)/140;
                data.BuffToBirthRate = Math.Sqrt(buff * 100) / 140;
                data.BuffToGrowth = buff;
                if (UseResources)
                {
                    await governor.ChangeResourceAmount(consumergood.LowerCaseName, -toconsume, $"Consumer Good Usage for Province with name: {Name}");
                }
            }
            consumerGoodsData.Add(data);
        }

        double DeathRate = Defines.NProvince["BASE_DEATH_RATE"];
        DeathRate += Nation.GetModifierValue(NationModifierType.MonthlyDeathRate);
        DeathRate *= Nation.GetModifierValue(NationModifierType.MonthlyDeathRateFactor) + 1;

        var rate = GetOverpopulationModifier();
        if (rate > 0)
            DeathRate += rate;

        double PopulationGrowth = BirthRate * PopulationMultiplier;
        PopulationGrowth -= DeathRate * PopulationMultiplier;
        PopulationGrowth *= totalgrowthbuff + 1;

        PopulationGrowth *= Nation.GetModifierValue(NationModifierType.PopulationGrowthSpeedFactor) + 1;

        var used = (double)BuildingSlotsUsed;
        double ratio = 0.0;
        if (BuildingSlots == 0)
            ratio = 0.0;
        else
            ratio = used / (double)BuildingSlots;
        PopulationGrowth *= (Math.Max(0, ratio - 0.3) * 0.75) + 1;

        if (Nation.CapitalProvinceId == Id)
            PopulationGrowth *= 3;
        else
            PopulationGrowth *= 1.2;

       return new(PopulationGrowth, consumerGoodsData);
    }

    public int GetMigrationAttraction()
    {
        double attraction = Defines.NProvince[NProvince.BASE_MIGRATION_ATTRACTION];
        attraction += Math.Max(Math.Pow(DevelopmentValue, Defines.NProvince[NProvince.MIGRATION_DEVELOPMENT_EXPONENT]) / Defines.NProvince[NProvince.MIGRATION_DEVELOPMENT_DIVISOR] + Defines.NProvince[NProvince.MIGRATION_DEVELOPMENT_BASE], 0);
        attraction += Math.Max(Math.Pow(BuildingSlots, Defines.NProvince[NProvince.MIGRATION_BUILDINGSLOTS_EXPONENT]) / Defines.NProvince[NProvince.MIGRATION_BUILDINGSLOTS_DIVISOR] + Defines.NProvince[NProvince.MIGRATION_BUILDINGSLOTS_BASE], 0);
        attraction += GetModifierValue(ProvinceModifierType.MigrationAttraction);

        // apply bonuses based on ranking by dev value
        if (Nation.ProvincesByDevelopmnet[14].DevelopmentValue <= DevelopmentValue)
        {
            int rank = Nation.ProvincesByDevelopmnet.IndexOf(this);
            attraction += 3;
            attraction *= 1.15;
        }

        attraction *= GetModifierValue(ProvinceModifierType.MigrationAttractionFactor) + 1;

        var used = (double)BuildingSlotsUsed;
        var ratio = used / (double)BuildingSlots;  
        attraction *= (Math.Max(0, ratio - 0.3)*1.2) + 1;

        if (GetOverpopulationModifier() > 0.25)
        {
            var muit = 1 - ((GetOverpopulationModifier() - 0.25) * 3);
            muit = Math.Max(muit, 0.6);
            attraction *= muit;
        }

        if (Nation.CapitalProvinceId == Id)
            attraction *= 1.5;

        return (int)attraction;
    }

    public async ValueTask HourlyTick()
    {
        if (NationId == 100)
            return;
        if (Population < 2500) Population = 2500;
        // update modifiers now
        UpdateModifiers();

        DevelopmentValue = (int)(Math.Floor(Math.Pow(Population, Defines.NProvince[NProvince.DEVELOPMENT_POPULATION_EXPONENT])) * Defines.NProvince[NProvince.DEVELOPMENT_POPULATION_FACTOR]);
        
        BaseDevelopmentValue = DevelopmentValue;
        bool hasdonecoastalbonus = false;

        foreach (var id in Metadata.Adjacencies)
        {
            var _metadata = ProvinceManager.ProvincesMetadata[id];
            //if (_metadata.TerrianType == "ocean" && hasdonecoastalbonus) continue;

            if (_metadata.TerrianType == "ocean" && !hasdonecoastalbonus)
            {
                DevelopmentValue += (int)Defines.NProvince[NProvince.DEVELOPMENT_COASTAL_BONUS];
                DevelopmentValue += (int)(Defines.NProvince[NProvince.DEVELOPMENT_COASTAL_FACTOR] * BaseDevelopmentValue);
                hasdonecoastalbonus = true;
            }
            var adj_province = DBCache.Get<Province>(id);
            if (adj_province is null || BaseDevelopmentValue > adj_province.BaseDevelopmentValue) continue;

            DevelopmentValue += (int)(adj_province.LastTickDevelopmentValue * 0.1);
        }

        LastTickDevelopmentValue = DevelopmentValue;

        RankByDevelopment = Nation.ProvincesByDevelopmnet.IndexOf(this);
        RankByMigrationAttraction = Nation.ProvincesByMigrationAttraction.IndexOf(this)+1;

        int currenthighestvalue = 0;
        int index = 0;
        var stages = GameDataManager.ProvinceDevelopmentStages.Values.ToList();
        ProvinceDevelopmentStage higheststage = stages[0];
        while (currenthighestvalue < DevelopmentValue)
        {
            if (index > stages.Count - 1) break;
            var stage = stages[index];
            if (DevelopmentValue < stage.DevelopmentLevelNeeded || index > stages.Count - 1)
                break;
            higheststage = stage;
            currenthighestvalue = stage.DevelopmentLevelNeeded;
            index++;
        }
        if (CurrentDevelopmentStage is null || CurrentDevelopmentStage.Name != higheststage.Name)
        {
            CurrentDevelopmentStage = higheststage;
            UpdateModifiers();
        }

        CurrentDevelopmentStage = higheststage;

        foreach (var building in DBCache.ProvincesBuildings[Id]) {
            await building.Tick();
            await building.TickRecipe();
        }
        
        UpdateModifiersAfterBuildingTick();

        DevelopmentValue += (int)Math.Floor(GetModifierValue(ProvinceModifierType.DevelopmentValue));

        // get hourly rate change to PopulationMultiplier
        var PopulationGrowth = (await GetMonthlyPopulationGrowth(true)).growthrate / 30 / 24;
        PopulationMultiplier += PopulationGrowth;

        Population = (int)(PopulationMultiplier * Nation.BaseProvincePopulation);

        // update building slot count
        double buildingslots_exponent = Defines.NProvince["BUILDING_SLOTS_POPULATION_EXPONENT"];
        buildingslots_exponent += GetModifierValue(ProvinceModifierType.BuildingSlotsExponent);
        buildingslots_exponent += Nation.GetModifierValue(NationModifierType.BuildingSlotsExponent);

        var slots = (Defines.NProvince["BASE_BUILDING_SLOTS"] + Math.Ceiling(Math.Pow(Population, buildingslots_exponent) * Defines.NProvince["BUILDING_SLOTS_FACTOR"]));

        if (Id == Nation.CapitalProvinceId)
            slots += 10;

        // province level
        slots += GetModifierValue(ProvinceModifierType.BuildingSlots);
        slots *= 1 + GetModifierValue(ProvinceModifierType.BuildingSlotsFactor);
        if (Id == Nation.CapitalProvinceId)
            slots *= 1.2;

        // Nation level
        slots *= 1 + Nation.GetModifierValue(NationModifierType.BuildingSlotsFactor);
        BuildingSlots = (int)slots;

        MigrationAttraction = GetMigrationAttraction();
    }

    public void UpdateOrAddModifier(ProvinceModifierType type, double value)
    {
        if (!Modifiers.ContainsKey(type))
            Modifiers[type] = new() { Amount = value, ModifierType = type};
        else
            Modifiers[type].Amount += value;
    }

    public void UpdateModifiers()
    {
        Modifiers = new();
        var value_executionstate = new ExecutionState(Nation, this);
        //var scaleby_executionstate = new ExecutionState(Nation, this);
        foreach (var staticmodifier in StaticModifiers)
        {
            foreach (var modifiernode in staticmodifier.BaseStaticModifiersObj.ModifierNodes)
            {
                var value = (double)modifiernode.GetValue(value_executionstate, staticmodifier.ScaleBy);
                UpdateOrAddModifier((ProvinceModifierType)modifiernode.provinceModifierType!, value);
            }
        }

        if (CurrentDevelopmentStage is not null)
        {
            value_executionstate = new ExecutionState(Nation, this);
            foreach (var modifiernode in CurrentDevelopmentStage.ModifierNodes)
            {
                var value = (double)modifiernode.GetValue(value_executionstate, 1);
                UpdateOrAddModifier((ProvinceModifierType)modifiernode.provinceModifierType!, value);
            }
        }
    }

    public void UpdateModifiersAfterBuildingTick() {
        var buildingtick_executionstate = new ExecutionState(Nation, this);
        foreach (var building in DBCache.ProvincesBuildings[Id]) {
            if (!building.SuccessfullyTicked) continue;
            if (building.Recipe.BaseRecipe.ModifierNodes is null) continue;
            foreach (var modifiernode in building.Recipe.BaseRecipe.ModifierNodes) {
                var value = (double)modifiernode.GetValue(buildingtick_executionstate, 1);
                value *= building.GetRateForProduction();
                UpdateOrAddModifier((ProvinceModifierType)modifiernode.provinceModifierType!, value);
            }
        }
    }

    public Shared.Models.Nations.Province ToModel()
    {
        return new()
        {
            Id = Id,
            Name = Name,
            NationId = NationId,
            BuildingSlots = BuildingSlots,
            Population = Population,
            Description = Description,
            GovernorId = GovernorId,
            StateId = StateId,
            DevelopmentValue = DevelopmentValue,
            BaseDevelopmentValue = BaseDevelopmentValue,
            LastTickDevelopmentValue = LastTickDevelopmentValue,
            MigrationAttraction = MigrationAttraction,
            BasePropertyTax = BasePropertyTax,
            PropertyTaxPerSize = PropertyTaxPerSize,
            Modifiers = Modifiers,
            StaticModifiers = StaticModifiers.Select(x => x.ToModel()).ToList(),
            Metadata = Metadata,
            MonthlyEstimatedMigrants = MonthlyEstimatedMigrants,
            RankByDevelopment = RankByDevelopment,
            RankByMigrationAttraction = RankByMigrationAttraction,
            BuildingSlotsUsed = BuildingSlotsUsed
        };
    }
}