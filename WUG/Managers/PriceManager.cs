using System.Collections.Concurrent;
using WUG.Database;

namespace WUG.Managers;

public static class PriceManager 
{
    /// <summary>
    /// Item Def Id: price
    /// </summary>
    public static ConcurrentDictionary<long, decimal> ResourcePrices = new();

    public static long? GetItemDefIdFromName(string name) 
    {
        if (!GameDataManager.ResourcesToItemDefinitions.ContainsKey(name))
            return null;
        return GameDataManager.ResourcesToItemDefinitions[name].Id;
    }

    public static async Task UpdatePricesAsync()
    {
        // temporary for testing
        Dictionary<string, decimal> _resourceprices = new();
        string data = """
        iron_ore:0.4
        iron:1
        copper_ore:0.6
        copper:1.4
        coal:1.75
        steel:2.75
        """;
        foreach (var stringpair in data.Split("\n")) {
            var pair = stringpair.Split(":");
            _resourceprices.Add(pair[0], decimal.Parse(pair[1]));
        }
        // end of temporary

        ResourcePrices.Clear();
        foreach (var pair in _resourceprices) {
            long? id = GetItemDefIdFromName(pair.Key);
            if (id is null)
                continue;
            ResourcePrices.TryAdd((long)id, pair.Value);
        }
    }

    public static decimal GetResourcePrice(long defid) {
        if (ResourcePrices.ContainsKey(defid))
            return ResourcePrices[defid];
        return 0.0m;
    }
}

// we need PPI so that the International Reserve can know what monetary policy to take
public static class PPIManager {

    /// <summary>
    /// Producer Price Index aka index of the cost of resources that producers (factories and mines) need
    /// in order to produce stuff
    /// </summary>
    public static decimal PPI = 0.0m;

    public static PriceStat UpdatePPI()
    {
        var resourceUsage = new Dictionary<long, double>();

        foreach (ProducingBuilding building in DBCache.GetAllProducingBuildings()) {
            foreach (var pair in building.Recipe.Inputs) {
                if (!resourceUsage.ContainsKey(pair.Key))
                    resourceUsage[pair.Key] = 0.0;
                resourceUsage[pair.Key] += pair.Value * building.GetRateForProduction();
            }
        }

        double totalUsage = resourceUsage.Sum(x => x.Value);
        decimal newPPI = 0.0m;

        // I am sure that this is a sorta bad way to measure PPI
        // example of my way, if iron ore is 20% of total resource usage
        // and it has increased by 5% over the timeframe (1 hour)
        // then PPI will increase by 1%
        foreach (var pair in resourceUsage) {
            newPPI += (decimal)(pair.Value / totalUsage) * PriceManager.GetResourcePrice(pair.Key);
        }
        PPI = newPPI;

        Dictionary<long, decimal> prices = new();
        foreach (var pair in PriceManager.ResourcePrices)
            prices.Add(pair.Key, Math.Round(pair.Value, 4));

        PriceStat stat = new() {
            Id = IdManagers.GeneralIdGenerator.Generate(),
            ResourcePrices = prices,
            PPI = PPI,
            Time = DateTime.UtcNow
        };
    }
}