NPops = {
    -- Good Usage per day per x population
    BASE_GOOD_USAGE_DIVISOR = 1000
}

NMilitary = {

}

NNation = {

}

NProduction = {
    BASE_FACTORY_THROUGHPUT = 1
    BASE_MINE_THROUGHPUT = 1
    BASE_FARM_THROUGHPUT = 1
    BASE_INFRASTRUCTURE_THROUGHPUT = 1
    BASE_POWERPLANT_THROUGHPUT = 1

    BASE_FACTORY_INPUT_EFFICIENCY = 0 -- the base % reduction in input usage
    FACTORY_INPUT_EFFICIENCY_LOSS_PER_SIZE = 0.0075

    STACKING_THROUGHPUT_BONUS = 0.01 -- % bonus to throughput per level of building built
    MAX_STACKING_THROUGHPUT_BONUS = 0.75

    BASE_MINE_QUANTITY = 0.1
    BASE_FACTORY_QUANTITY = 0.1
    BASE_FARM_QUANTITY = 0.1
    BASE_INFRASTRUCTURE_QUANTITY = 1
    BASE_POWERPLANT_QUANTITY = 1

    BASE_MINE_QUANTITY_CAP = 1
    BASE_FACTORY_QUANTITY_CAP = 1
    BASE_FARM_QUANTITY_CAP = 1
    BASE_INFRASTRUCTURE_QUANTITY_CAP = 1
    BASE_POWERPLANT_QUANTITY_CAP = 1

    BASE_MINE_QUANTITY_GROWTH_RATE_FACTOR = 1.25
    BASE_FACTORY_QUANTITY_GROWTH_RATE_FACTOR = 6
    BASE_FARM_QUANTITY_GROWTH_RATE_FACTOR = 1.5
    BASE_INFRASTRUCTURE_QUANTITY_GROWTH_RATE_FACTOR = 1
    -- was 6
    BASE_POWERPLANT_QUANTITY_GROWTH_RATE_FACTOR = 60

    --0.005
    BASE_QUANTITY_GROWTH_RATE = 0.015
}

NProvince = {
    BUILDING_SLOTS_FACTOR = 0.005
    BASE_BUILDING_SLOTS = 1
    BUILDING_SLOTS_POPULATION_EXPONENT = 0.65
    BASE_BIRTH_RATE = 0.65
    BASE_DEATH_RATE = 0.3
    -- added to BASE_DEATH_RATE
    OVERPOPULATION_MODIFIER_EXPONENT = 0.26 -- modifier value: (province.Population^0.26)/100 - 0.15
    OVERPOPULATION_MODIFIER_BASE = -0.15

    -- used for randomizing province populations
    BASE_POPULATION_MIN = 1500
    BASE_POPULATION_MAX = 90000

    DEVELOPMENT_POPULATION_EXPONENT = 0.54
    DEVELOPMENT_POPULATION_FACTOR = 0.04
    DEVELOPMENT_COASTAL_BONUS = 5
    DEVELOPMENT_COASTAL_FACTOR = 0.05

    -- migration stuff
    BASE_MIGRATION_RATE = 0.275 -- monthly
    BASE_MIGRATION_ATTRACTION = 2

    -- the final formula for this is ((province.development^2)/250) - 5
    MIGRATION_DEVELOPMENT_EXPONENT = 2
    MIGRATION_DEVELOPMENT_DIVISOR = 250
    MIGRATION_DEVELOPMENT_BASE = -5

    -- the final formula for this is ((province.buildingslots^2)/2000) + 1.5
    MIGRATION_BUILDINGSLOTS_EXPONENT = 2
    MIGRATION_BUILDINGSLOTS_DIVISOR = 2000
    MIGRATION_BUILDINGSLOTS_BASE = 1.5
}

NScore = {
    -- Nation.population ^ 0.5 / 10
    ECONOMIC_SCORE_FROM_POPULATION_EXPONENT = 0.5
    ECONOMIC_SCORE_FROM_POPULATION_DIVISOR = 10
    ECONOMIC_SCORE_PER_MINE = 5
    ECONOMIC_SCORE_PER_SIMPLE_FACTORY = 10
    ECONOMIC_SCORE_PER_ADVANCED_FACTORY = 30
    ECONOMIC_SCORE_PER_INFRASTRUCTURE = 3.5
}