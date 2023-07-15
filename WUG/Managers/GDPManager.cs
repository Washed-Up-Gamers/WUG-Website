namespace WUG.Managers;

public static class GDPManager
{
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
            if (PriceManager.GetResourcePrice(pair.Key) != 0.0m)
                total += (decimal)pair.Value * (decimal)building.GetRateForProduction() * PriceManager.GetResourcePrice(pair.Key);
        }
        return total;
    }

    public static decimal GetGDPOfANation(Nation nation)
    {
        var total = 0.0m;
        foreach (var province in nation.Provinces)
        {
            foreach (var building in DBCache.ProvincesBuildings[province.Id])
            {
                total += GetGDPOfBuilding(building);
            }
        }
        return total * 24.0m * 30.0m;
    }

    public static decimal GetGDPOfAState(State state)
    {
        var total = 0.0m;
        foreach (var province in state.Provinces)
        {
            foreach (var building in DBCache.ProvincesBuildings[province.Id])
            {
                total += GetGDPOfBuilding(building);
            }
        }
        return total * 24.0m * 30.0m;
    }
}
