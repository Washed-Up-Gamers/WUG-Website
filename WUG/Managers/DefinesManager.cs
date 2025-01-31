﻿using System.Text;
using System.IO;
using WUG.Scripting.Parser;
using WUG.Database.Models.Nations;

namespace WUG.Database.Managers;

public enum NNation
{
    BASE_MOBILIZATION_SPEED,
    Nation_FUNDING_BASE,
    Nation_FUNDING_PER_CITIZEN
}

public enum NPops
{
    BASE_GOOD_USAGE_DIVISOR
}

public enum NProduction
{
    BASE_FACTORY_THROUGHPUT,
    BASE_MINE_THROUGHPUT,
    BASE_FARM_THROUGHPUT,
    BASE_INFRASTRUCTURE_THROUGHPUT,
    BASE_POWERPLANT_THROUGHPUT,
    BASE_FACTORY_INPUT_EFFICIENCY,
    BASE_POWERPLANT_INPUT_EFFICIENCY,
    STACKING_THROUGHPUT_BONUS,
    MAX_STACKING_THROUGHPUT_BONUS,

    BASE_MINE_QUANTITY,
    BASE_FACTORY_QUANTITY,
    BASE_FARM_QUANTITY,
    BASE_INFRASTRUCTURE_QUANTITY,
    BASE_POWERPLANT_QUANTITY,

    BASE_MINE_QUANTITY_CAP,
    BASE_FACTORY_QUANTITY_CAP,
    BASE_FARM_QUANTITY_CAP,
    BASE_INFRASTRUCTURE_QUANTITY_CAP,
    BASE_POWERPLANT_QUANTITY_CAP,

    BASE_MINE_QUANTITY_GROWTH_RATE_FACTOR,
    BASE_FACTORY_QUANTITY_GROWTH_RATE_FACTOR,
    BASE_FARM_QUANTITY_GROWTH_RATE_FACTOR,
    BASE_INFRASTRUCTURE_QUANTITY_GROWTH_RATE_FACTOR,
    BASE_POWERPLANT_QUANTITY_GROWTH_RATE_FACTOR,

    BASE_QUANTITY_GROWTH_RATE,

    FACTORY_INPUT_EFFICIENCY_LOSS_PER_SIZE
}

public enum Military
{

}

public enum NProvince
{
    BUILDING_SLOTS_FACTOR,
    BASE_BUILDING_SLOTS,
    BUILDING_SLOTS_POPULATION_EXPONENT,
    BASE_BIRTH_RATE,
    BASE_DEATH_RATE,
    OVERPOPULATION_MODIFIER_EXPONENT,
    BASE_POPULATION_MIN,
    BASE_POPULATION_MAX,
    DEVELOPMENT_POPULATION_EXPONENT,
    DEVELOPMENT_POPULATION_FACTOR,
    OVERPOPULATION_MODIFIER_BASE,
    BASE_MIGRATION_RATE,
    BASE_MIGRATION_ATTRACTION,
    MIGRATION_DEVELOPMENT_EXPONENT,
    MIGRATION_DEVELOPMENT_DIVISOR,
    MIGRATION_DEVELOPMENT_BASE,
    MIGRATION_BUILDINGSLOTS_EXPONENT,
    MIGRATION_BUILDINGSLOTS_DIVISOR,
    MIGRATION_BUILDINGSLOTS_BASE,
    DEVELOPMENT_COASTAL_BONUS,
    DEVELOPMENT_COASTAL_FACTOR
}

public enum NScore
{
    ECONOMIC_SCORE_FROM_POPULATION_EXPONENT,
    ECONOMIC_SCORE_FROM_POPULATION_DIVISOR,
    ECONOMIC_SCORE_PER_MINE,
    ECONOMIC_SCORE_PER_SIMPLE_FACTORY,
    ECONOMIC_SCORE_PER_ADVANCED_FACTORY,
    ECONOMIC_SCORE_PER_INFRASTRUCTURE
}

public class Define<T> where T : struct
{
    private Dictionary<T, double> Values = new();

    public double this[string define]
    {
        get
        {
            T result;
            if (!Enum.TryParse<T>(define, out result))
            {
                throw new Exception($"Define {define} could not be found!");
            }
            return Values[result];
        }
        set
        {
            T result;
            if (!Enum.TryParse(define, out result))
            {
                throw new Exception($"Define {define} could not be found!");
            }
            Values[result] = value;
        }
    }

    public double this[T define]
    {
        get
        {
            return Values[define];
        }
        set
        {
            Values[define] = value;
        }
    }
}

public static class Defines
{
    public static Define<NNation> NNation = new();
    public static Define<NPops> NPops = new();
    public static Define<NProduction> NProduction = new();
    public static Define<Military> NMilitary = new();
    public static Define<NProvince> NProvince = new();
    public static Define<NScore> NScore = new();

    public static bool FirstUpdate = true;

    public static void Load()
    {
        using (Lua lua = new Lua())
        {
            //lua.State.Encoding = Encoding.UTF8;
            string text = File.ReadAllText("Data/Defines.lua");
            //var data  = LuaHandler.PreProcessLua(text);
            //File.WriteAllText("../../../../Database/LuaDump.lua", text);
            lua.DoString(text);

            var table = (LuaTable)lua["NNation"];
            foreach (string key in table.Keys)
                NNation[Enum.Parse<NNation>(key)] = Convert.ToDouble(table[key]);

            table = (LuaTable)lua["NPops"];
            foreach (string key in table.Keys)
                NPops[Enum.Parse<NPops>(key)] = Convert.ToDouble(table[key]);

            table = (LuaTable)lua["NProvince"];
            foreach (string key in table.Keys)
                NProvince[Enum.Parse<NProvince>(key)] = Convert.ToDouble(table[key]);

            table = (LuaTable)lua["NProduction"];
            foreach (string key in table.Keys)
                NProduction[Enum.Parse<NProduction>(key)] = Convert.ToDouble(table[key]);

            table = (LuaTable)lua["NScore"];
            foreach (string key in table.Keys)
                NScore[Enum.Parse<NScore>(key)] = Convert.ToDouble(table[key]);

        }
    }
}
