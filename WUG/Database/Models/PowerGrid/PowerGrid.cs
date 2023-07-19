using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WUG.Database.Models.PowerGrid;

public class PowerGrid
{
    [Key]
    public long Id { get; set; }

    public long MainNationId { get; set; }
    public string Name { get; set; }
    
    [Column(TypeName = "bigint[]")]
    public List<long> NationIds { get; set; }

    /// <summary>
    /// In MW
    /// </summary>
    public double PowerSupply { get; set; }

    /// <summary>
    /// In MW
    /// </summary>
    public double PowerDemand { get; set; }

    /// <summary>
    /// $ per MWH
    /// </summary>
    [DecimalType(4)]
    public decimal AveragePrice { get; set; }

    public void UpdateStats() 
    {
        PowerDemand = 0.0;
        PowerSupply = 0.0;
        AveragePrice = 0.0m;
        if (NationIds.Count == 0)
            return;

        foreach (var nationid in NationIds)
            PowerDemand += DBCache.Get<Nation>(nationid).Provinces.Sum(x => x.GetBuildings().Where(x => x.SuccessfullyTicked).Sum(x => x.GetRateForProduction() / x.Recipe.PerHour * x.Recipe.PowerDemand * (1.0 / (1.0 - x.ThroughputLossFromPowerGrid))));
        
        // grab power producers
        List<IPowerProducingBuilding> powerPlants = new();
        foreach (var nationid in NationIds)
            powerPlants.AddRange(DBCache.Get<Nation>(nationid).Provinces.SelectMany(x => x.GetBuildings().Where(x => x.SuccessfullyTicked).Where(x => x.BuildingType is BuildingType.PowerPlant or BuildingType.Battery)).Select(x => (IPowerProducingBuilding)x));
        
        PowerSupply = powerPlants.Sum(x => x.MaxPowerProduction);

        var powerDemandLeft = PowerDemand;
        var totalCost = 0.0m;
        foreach (var powerplant in powerPlants) {
            if (powerDemandLeft <= 0)
                break;
            
            var amount = powerDemandLeft;
            if (amount > powerplant.MaxPowerProduction)
                amount = powerplant.MaxPowerProduction;
            
            totalCost += powerplant.SellingPrice * (decimal)amount;
        }

        if (PowerDemand <= 0.1)
            AveragePrice = 0.0m;
        else
            AveragePrice = totalCost / (decimal)PowerDemand;
    }

    public async ValueTask Tick()
    {
        PowerDemand = 0.0;
        PowerSupply = 0.0;
        AveragePrice = 0.0m;
        if (NationIds.Count == 0)
            return;

        foreach (var nationid in NationIds)
            PowerDemand += DBCache.Get<Nation>(nationid).Provinces.Sum(x => x.GetBuildings().Where(x => x.SuccessfullyTicked).Sum(x => x.GetRateForProduction() / x.Recipe.PerHour * x.Recipe.PowerDemand * (1.0 / (1.0 - x.ThroughputLossFromPowerGrid))));
        
        // grab power producers
        List<IPowerProducingBuilding> powerPlants = new();
        foreach (var nationid in NationIds)
            powerPlants.AddRange(DBCache.Get<Nation>(nationid).Provinces.SelectMany(x => x.GetBuildings().Where(x => x.SuccessfullyTicked).Where(x => x.BuildingType is BuildingType.PowerPlant or BuildingType.Battery)).Select(x => (IPowerProducingBuilding)x));
        
        powerPlants = powerPlants.OrderBy(x => x.SellingPrice).ToList();
        PowerSupply = powerPlants.Sum(x => x.MaxPowerProduction);

        var powerDemandLeft = PowerDemand;
        var totalCost = 0.0m;
        foreach (var powerplant in powerPlants) {
            if (powerDemandLeft <= 0)
                break;
            
            var amount = powerDemandLeft;
            if (amount > powerplant.MaxPowerProduction)
                amount = powerplant.MaxPowerProduction;
            
            totalCost += powerplant.SellingPrice * (decimal)amount;
            powerplant.PowerBrought += amount;
            var tran = new Transaction(BaseEntity.Find(98), powerplant.Owner, powerplant.SellingPrice * (decimal)amount, TransactionType.PowerPlantProducerPayment, $"For {amount:n0}MWh sold by {powerplant.Name} ({powerplant.Id}) in {Name}");
            tran.NonAsyncExecute(true);
        }

        if (PowerDemand <= 0.1)
            AveragePrice = 0.0m;
        else
            AveragePrice = totalCost / (decimal)PowerDemand;

        Dictionary<long, decimal> amountToPay = new();
        foreach(var nationid in NationIds)
        {
            foreach (var province in DBCache.Get<Nation>(nationid).Provinces)
            {
                foreach (var building in province.GetBuildings().Where(x => x.SuccessfullyTicked && x.Recipe.PowerDemand > 0.1)) {
                    if (!amountToPay.ContainsKey(building.OwnerId))
                        amountToPay[building.OwnerId] = 0.0m;
                    amountToPay[building.OwnerId] += (decimal)(building.Recipe.PowerDemand * building.GetRateForProduction());
                }
            }
        }

        if (AveragePrice > 0.0m) {
            foreach (var pair in amountToPay) 
            {
                var tran = new Transaction(BaseEntity.Find(pair.Key), BaseEntity.Find(98), pair.Value, TransactionType.PowerPlantConsumerPayment, $"For {(pair.Value / AveragePrice):n0}MWh consumed in {Name}");
                tran.NonAsyncExecute(true);
            }
        }

        if (PowerDemand > PowerSupply) {
            double loss = 0.15;
            if (PowerDemand <= 0.001)
                loss = 0.15;
            else {
                loss = Math.Min(0.75, 1.0 - (PowerSupply / PowerDemand));
            } 
            foreach(var nationid in NationIds)
            {
                foreach (var province in DBCache.Get<Nation>(nationid).Provinces)
                {
                    foreach (var building in province.GetBuildings().Where(x => x.Recipe.PowerDemand > 0.1)) {
                        building.ThroughputLossFromPowerGrid = loss;
                    }
                }
            }
        }
    }
}