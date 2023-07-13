namespace WUG.Managers;

public static class GDPManager
{
    /// <summary>
    /// Item Def Id: price
    /// </summary>
    public static Dictionary<long, decimal> CurrentPricesFromNVSE { get; set; }

    public static void AddPrice(string resource, decimal price)
    {
        CurrentPricesFromNVSE[GameDataManager.ResourcesToItemDefinitions[resource].Id] = price;
    }

    public static async Task UpdatePricesAsync()
    {
        CurrentPricesFromNVSE = new();
        AddPrice("iron", 0.4m);
        AddPrice("coal", 0.6m);
        AddPrice("iron_ore", 0.2m);
        AddPrice("copper", 0.8m);
        AddPrice("copper_ore", 0.3m);
    }

    public static decimal GetGDPOfBuilding(ProducingBuilding building)
    {
        decimal total = 0.0m;
        if (building.Recipe.CustomOutputItemDefinitionId is not null)
        {
            // TODO: calc outprice item price using a cached system
            // using the inputs times a profit margin
            return 0.0m;
        }
        foreach (var pair in building.Recipe.Outputs)
        {
            if (CurrentPricesFromNVSE.ContainsKey(pair.Key))
            {
                total += (decimal)pair.Value * (decimal)building.GetRateForProduction() * CurrentPricesFromNVSE[pair.Key];
            }
        }
        return total;
    }

    public static decimal GetGDPOfNation(Nation nation)
    {
        var total = 0.0m;
        foreach (var province in nation.Provinces)
        {
            foreach (var building in DBCache.ProvincesBuildings[province.Id])
            {
                total += GetGDPOfBuilding(building);
            }
        }
        return total * 24.0m * 365.24m;
    }
}
