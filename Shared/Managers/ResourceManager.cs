using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Models.Nations;

namespace Shared.Managers;

public class LuaAnyWithBaseType
{
    public string Id { get; set; }
    public string BaseType { get; set; }
    public bool Required { get; set; }
    public SyntaxNode Amount { get; set; }
}

public class BaseRecipe : Item
{
    public Dictionary<long, double> Inputs { get; set; }
    public Dictionary<long, double> Outputs { get; set; }
    public string Id { get; set; }
    public long IdAsLong { get; set; }
    public double PerHour { get; set; }
    public bool Editable { get; set; }
    public bool Inputcost_Scaleperlevel { get; set; }
    public string Name { get; set; }
    public BuildingType? TypeOfBuilding { get; set; }
    public List<SyntaxModifierNode>? ModifierNodes { get; set; }
    public Dictionary<string, LuaRecipeEdit> LuaRecipeEdits { get; set; }
    public List<LuaAnyWithBaseType> AnyWithBaseTypes { get; set; }
    public KeyValuePair<string, double>? OutputWithCustomItem { get; set; }

    public static async ValueTask<BaseRecipe> FindAsync(string id, bool refresh = false)
    {
        if (!refresh)
        {
            var cached = SVCache.Get<BaseRecipe>(id);
            if (cached is not null)
                return cached;
        }

        var item = (await SVClient.GetJsonAsync<BaseRecipe>($"api/baserecipes/{id}")).Data;

        if (item is not null)
            await item.AddToCache();

        return item;
    }

    public override async Task AddToCache()
    {
        await SVCache.Put(Id, this);
    }
}

public class ConsumerGood
{
    // in x per 1,000 citizens per month
    public double PopGrowthRateModifier { get; set; }

    // the score per 10k citizens that have this good filled
    public double EconomicScoreModifier { get; set; }
    
    // 10k citizens will consume this many units per hour
    public double PopConsumptionRate { get; set; }
}

public class SVResource 
{
    public string Name { get; set; }
    public string LowerCaseName { get; set; }
    public ConsumerGood? consumerGood { get; set; }

    public ItemDefinition ItemDefinition { get; set; }
}

public static class ResourceManager 
{
    public static List<string> GetFilePaths(string path)
    {
        if (path.Contains("/"))
        {
            return Directory.GetFiles(path).ToList();
        }
        return Directory.GetFiles($"Data/{path}").ToList();
    }
}