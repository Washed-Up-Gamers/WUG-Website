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
            if (PriceManager.ContainsKey(pair.Key))
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
