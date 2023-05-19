using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SV2.Database.Models.Users;
using SV2.Database.Models.Permissions;
using SV2.Database.Models.Economy;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace SV2.Database.Models.Entities;

public enum EntityType
{
    User,
    Group,
    Corporation,
    CreditAccount
}

public interface IHasOwner
{
    public long OwnerId { get; set; }
    public BaseEntity Owner { get;}
}

public abstract class BaseEntity
{
    [Key]
    public long Id {get; set; }

    [VarChar(64)]
    public string Name { get; set; }

    [VarChar(512)]
    public string? Description { get; set; }

    [DecimalType]
    public decimal Credits { get; set;}

    [DecimalType]
    public decimal TaxAbleBalance { get; set;}
    
    [JsonIgnore]
    [VarChar(36)]
    public string ApiKey { get; set; }

    [NotMapped]
    public string? ImageUrl
    {
        get
        {
            return _ImageUrl is null ? "https://app.valour.gg/_content/Valour.Client/icon-512.png" : _ImageUrl;
        }
        set
        {
            _ImageUrl = value;
        }
    }

    [Column("imageurl")]
    public string? _ImageUrl { get; set; }

    public long? DistrictId { get; set; }

    [NotMapped]
    [JsonIgnore]
    public District District => DBCache.Get<District>(DistrictId)!;

    [NotMapped]
    public Dictionary<long, SVItemOwnership> SVItemsOwnerships { get; set; }

    public virtual EntityType EntityType { get; }

    public static BaseEntity? Find(long Id) => DBCache.FindEntity(Id);

    public static BaseEntity? Find(long? Id) => DBCache.FindEntity(Id);

    // these methods will simply call Valour.API methods once Valour adds the Community Item System
    public async ValueTask<double> GetOwnershipOfResource(string resource) 
    {
        var itemdefid = GameDataManager.ResourcesToItemDefinitions[resource].Id;
        if (!SVItemsOwnerships.ContainsKey(itemdefid)) return 0.0;
        return SVItemsOwnerships[itemdefid].Amount;
    }

    public async ValueTask<bool> HasEnoughResource(string resource, double amount) 
    {
        return await GetOwnershipOfResource(resource) > amount;
    }

    public async ValueTask<bool> ChangeResourceAmount(string resource, double by, string details) {
        var itemdefid = GameDataManager.ResourcesToItemDefinitions[resource].Id;
        SVItemOwnership ownership = null;
        if (!SVItemsOwnerships.ContainsKey(itemdefid))
            ownership = await CreateResourceOwnership(resource);
        else
            ownership = SVItemsOwnerships[itemdefid];

        ItemTrade itemtrade = new(ItemTradeType.Server, null, Id, by, itemdefid, details);
        itemtrade.NonAsyncExecute(true);
        return true;
    }

    public async ValueTask<SVItemOwnership> CreateResourceOwnership(string resource) {
        return new();
    }

    public async Task Create() {
        Dictionary<string, double> keyValuePairs = new() {
            { "steel", 1_000_000 },
            { "tools", 1_000_000 },
            { "aluminium", 1_000_000 },
            { "simple_components", 1_000_000 },
            { "advanced_components", 1_000_000 }
        };
        foreach ((var resource, var amount) in keyValuePairs) {
            await ChangeResourceAmount(resource, amount, "");
        }
    }

    public double GetHourlyProductionOfResource(string resource) 
    {
        double total = 0;
        List<ProducingBuilding> buildings = DBCache.GetAllProducingBuildings().Where(x => x.OwnerId == Id).ToList();
        foreach (var building in buildings) {
            if (building.Recipe.Outputs.ContainsKey(resource)) {
                if (building.BuildingObj.type == BuildingType.Mine)
                    total += building.GetHourlyProduction() * building.Recipe.Outputs[resource] * building.MiningOutputFactor();
                else
                    total += building.GetHourlyProduction() * building.Recipe.Outputs[resource];
            }
        }
        return total;
    }

    public double GetHourlyUsageOfResource(string resource) {
        double total = 0;
        List<ProducingBuilding> buildings = DBCache.GetAllProducingBuildings().Where(x => x.OwnerId == Id).ToList();
        foreach (var building in buildings) {
            if (building.Recipe.Inputs.ContainsKey(resource))
                total += building.GetHourlyProduction() * building.Recipe.Inputs[resource];
        }
        return total;
    }

    public async Task<decimal> GetAvgTaxableBalance(VooperDB dbctx, int hours = 720)
    {
        DateTime timetocheck = DateTime.UtcNow.AddHours(-hours);
        return await dbctx.EntityBalanceRecords.Where(x => x.EntityId == Id && x.Time > timetocheck).AverageAsync(x => x.TaxableBalance);
    }

    public async Task DoIncomeTax(VooperDB dbctx)
    {
        // districts do not pay income tax
        if (EntityType == EntityType.Group && DBCache.Get<District>(Id) is not null)
            return;

        if (TaxAbleBalance <= 0.0m)
            return;

        DateTime timetocheck = DateTime.UtcNow.AddHours(-722);
        var recordobj = await dbctx.EntityBalanceRecords.Where(x => x.EntityId == Id && x.Time > timetocheck).OrderByDescending(x => x.Time).LastOrDefaultAsync();
        decimal taxablebalance30dago = 0.0m;
        if (recordobj is not null)
            taxablebalance30dago = recordobj.TaxableBalance;

        decimal amount = await GetAvgTaxableBalance(dbctx)-taxablebalance30dago;
        var toprocess = amount;

        if (amount <= 0.0m)
            return;

        decimal totaldue = 0.0m;

        List<TaxPolicy> policies = null;

        if (DistrictId is not null)
        {

            // do district level taxes
            policies = EntityType switch
            {
                EntityType.Group => DBCache.GetAll<TaxPolicy>().Where(x => x.DistrictId == DistrictId && x.taxType == TaxType.GroupIncome).OrderBy(x => x.Minimum).ToList(),
                EntityType.Corporation => DBCache.GetAll<TaxPolicy>().Where(x => x.DistrictId == DistrictId && x.taxType == TaxType.CorporateIncome).OrderBy(x => x.Minimum).ToList(),
                EntityType.User => DBCache.GetAll<TaxPolicy>().Where(x => x.DistrictId == DistrictId && x.taxType == TaxType.PersonalIncome).OrderBy(x => x.Minimum).ToList()
            };

            foreach (TaxPolicy policy in policies)
            {
                var _amount = policy.GetTaxAmount(amount);
                totaldue += _amount;
                policy.Collected += _amount;
                toprocess -= policy.Maximum;
                if (amount <= 0.0m)
                    break;
            }
            if (totaldue > 0.1m)
            {
                Transaction taxtrans = new Transaction(Id, (long)DistrictId, totaldue, TransactionType.TaxPayment, $"Income Tax Payment for ¢{TaxAbleBalance - taxablebalance30dago} of income.");
                taxtrans.NonAsyncExecute(true);
            }

            amount = TaxAbleBalance - taxablebalance30dago;
            toprocess = amount;
            totaldue = 0.0m;
        }

        // now do imperial level taxes
        policies = EntityType switch
        {
            EntityType.Group => DBCache.GetAll<TaxPolicy>().Where(x => x.DistrictId == 100 && x.taxType == TaxType.GroupIncome).OrderBy(x => x.Minimum).ToList(),
            EntityType.Corporation => DBCache.GetAll<TaxPolicy>().Where(x => x.DistrictId == 100 && x.taxType == TaxType.CorporateIncome).OrderBy(x => x.Minimum).ToList(),
            EntityType.User => DBCache.GetAll<TaxPolicy>().Where(x => x.DistrictId == 100 && x.taxType == TaxType.PersonalIncome).OrderBy(x => x.Minimum).ToList()
        };

        foreach(TaxPolicy policy in policies)
        {
            var _amount = policy.GetTaxAmount(amount);
            totaldue += _amount;
            policy.Collected += _amount;
            toprocess -= policy.Maximum;
            if (amount <= 0.0m)
                break;
        }
        if (totaldue > 0.1m) {
            Transaction taxtrans = new Transaction(Id, 100, totaldue, TransactionType.TaxPayment, $"Income Tax Payment for ¢{TaxAbleBalance - taxablebalance30dago} of income.");
            taxtrans.NonAsyncExecute(true);
        }

        // do district level balance tx
        TaxPolicy? _policy = DBCache.GetAll<TaxPolicy>().FirstOrDefault(x => x.DistrictId == DistrictId && x.taxType == TaxType.UserBalance);
        if (_policy is not null) {
            totaldue = _policy.GetTaxAmount(Credits);
            if (totaldue > 0.1m) {
                Transaction taxtrans = new(Id, (long)DistrictId, totaldue, TransactionType.TaxPayment, $"Balance Tax Payment tax id: {_policy.Id}");
                taxtrans.NonAsyncExecute(true);
                _policy.Collected += totaldue;
            }
        }
    }

    public bool HasPermission(BaseEntity entity, GroupPermission permission)
    {
        return false;
    }

    public static BaseEntity? FindByApiKey(string apikey)
    {
        BaseEntity? entity = DBCache.GetAll<Group>().FirstOrDefault(x => x.ApiKey == apikey);
        if (entity is null) {
            entity = DBCache.GetAll<SVUser>().FirstOrDefault(x => x.ApiKey == apikey);
        }
        return entity;
    }

    public string GetPfpUrl()
    {
        if (ImageUrl is null || ImageUrl.Length == 0 || ImageUrl == " ")
        {
            return "/media/unity-128.png";
        }
        return ImageUrl;
    }
}

public enum EntityModifierType
{
    FactoryThroughputFactor,
    FactoryEfficiencyFactor,
    FactoryQuantityCapFactor
}