﻿using System.Text;
using System.IO;
using Microsoft.CodeAnalysis.Operations;
using SV2.Scripting.Parser;
using SV2.Database.Models.Districts;

namespace SV2.Database.Managers;

public enum NDistrict
{
    BASE_MOBILIZATION_SPEED
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
    BASE_FACTORY_INPUT_EFFICIENCY,
    STACKING_THROUGHPUT_BONUS,
    MAX_STACKING_THROUGHPUT_BONUS,
    BASE_MINE_QUANTITY,
    BASE_FACTORY_QUANTITY,
    BASE_FARM_QUANTITY,

    BASE_MINE_QUANTITY_CAP,
    BASE_FACTORY_QUANTITY_CAP,
    BASE_FARM_QUANTITY_CAP,

    BASE_MINE_QUANTITY_GROWTH_RATE_FACTOR,
    BASE_FACTORY_QUANTITY_GROWTH_RATE_FACTOR,
    BASE_FARM_QUANTITY_GROWTH_RATE_FACTOR,

    BASE_QUANTITY_GROWTH_RATE,

    FACTORY_INPUT_EFFICIENCY_LOSS_PER_SIZE
}

public enum Military
{

}

public enum NCity
{
    BUILDING_SLOTS_FACTOR,
    BASE_BUILDING_SLOTS,
    BUILDING_SLOTS_POPULATION_EXPONENT,
    BASE_BIRTH_RATE,
    BASE_DEATH_RATE,
    OVERPOPULATION_MODIFIER_EXPONENT
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
    public static Define<NDistrict> NDistricts = new();
    public static Define<NPops> NPops = new();
    public static Define<NProduction> NProduction = new();
    public static Define<Military> NMilitary = new();
    public static Define<NCity> NCity = new();

    public static bool FirstUpdate = true;

    public static void Load()
    {
        using (Lua lua = new Lua())
        {
            //lua.State.Encoding = Encoding.UTF8;
            string text = "";
            try
            {
                text = File.ReadAllText("../../../../Database/Managers/Data/Defines.lua");
            }
            catch
            {
                text = File.ReadAllText("../Database/Managers/Data/Defines.lua");
            }
            //var data  = LuaHandler.PreProcessLua(text);
            //File.WriteAllText("../../../../Database/LuaDump.lua", text);
            lua.DoString(text);

            var table = (LuaTable)lua["NDistrict"];
            foreach (string key in table.Keys)
                NDistricts[Enum.Parse<NDistrict>(key)] = Convert.ToDouble(table[key]);

            table = (LuaTable)lua["NPops"];
            foreach (string key in table.Keys)
                NPops[Enum.Parse<NPops>(key)] = Convert.ToDouble(table[key]);

            table = (LuaTable)lua["NCity"];
            foreach (string key in table.Keys)
                NCity[Enum.Parse<NCity>(key)] = Convert.ToDouble(table[key]);

            table = (LuaTable)lua["NProduction"];
            foreach (string key in table.Keys)
                NProduction[Enum.Parse<NProduction>(key)] = Convert.ToDouble(table[key]);

        }
    }
}