namespace Shared.Models.Nations.Modifiers;

/// <summary>
/// Enum of all modifiers in the Nation scope
/// "Factor" means a % effect, if something does not have "Factor" in its name then it's just adding the modifier
/// </summary>
public enum NationModifierType 
{
    MiningThroughputFactor,
    SmeltingEfficiency,
    MonthlyBirthRate,
    MonthlyDeathRate,
    MonthlyBirthRateFactor,
    MonthlyDeathRateFactor,
    MineQuantityCap,
    MineQuantityGrowthRateFactor,
    MineThroughputFactor,
    FarmQuantityCap,
    FarmQuantityGrowthRateFactor,
    FarmThroughputFactor,
    FactoryQuantityCap,
    FactoryQuantityGrowthRateFactor,
    FactoryThroughputFactor,
    FactoryEfficiencyFactor,
    FactoryEfficiency,
    ArmyAttackFactory,
    ArmyEntrenchmentFactor,
    ArmyEntrenchment,
    ArmyEntrenchmentSpeed,
    ArmyEntrenchmentSpeedFactor,
    ArmySpeedFactor,
    ArmyMorale,
    ArmyMoraleFactor,
    DivisionXpGainFactor,
    RecruitmentCenterManpowerFactor,
    AllProducingBuildingThroughputFactor,
    BuildingSlotsFactor,
    BuildingSlotsExponent,
    OverPopulationModifierExponent,
    InfrastructureThroughputFactor,
    PopulationGrowthSpeedFactor,
    PowerPlantThroughputFactor,
    PowerPlantQuantityCap
}