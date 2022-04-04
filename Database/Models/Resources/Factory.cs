using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SV2.Database.Models.Entities;
using SV2.Database.Models.Items;

namespace SV2.Database.Models.Factories;

public class Recipe
{
    [Key]
    public string Id { get; set; }
    public List<string> Inputs_Names { get; set; }
    public List<decimal> Inputs_Amounts { get; set; }
    public List<string> Output_Names { get; set; }
    public List<decimal> Output_Amounts { get; set; }
}

public class Factory : IHasOwner
{
    [Key]
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string OwnerId { get; set; }

    [NotMapped]
    public IEntity Owner { get; set; }

    public string CountyId { get; set; }
    public string RecipeId { get; set; }

    [ForeignKey("RecipeId")]
    public Recipe recipe { get; set; }
    public int Level { get; set; }
    public bool HasAEmployee { get; set; }

    public async Task Tick(List<TradeItem> tradeItems)
    {
        decimal ProductionBonus = 1.0m;
        if (HasAEmployee) {
            ProductionBonus += 0.5m;
        };

        for (int i = 0; i < recipe.Inputs_Amounts.Count; i++)
        {
            // find the tradeitem
            TradeItem? item = tradeItems.FirstOrDefault(x => x.Definition.Name == recipe.Inputs_Names[i] && x.Definition.OwnerId == "g-vooperia" && x.OwnerId == OwnerId);
            if (item is null) {
                break;
            }
            decimal amountNeeded = recipe.Inputs_Amounts[i]*ProductionBonus;
            if (item.Amount < amountNeeded) {
                break;
            }
            item.Amount -= amountNeeded;
        }

        for (int i = 0; i < recipe.Output_Amounts.Count; i++)
        {
            // find the tradeitem
            TradeItem? item = tradeItems.FirstOrDefault(x => x.Definition.Name == recipe.Output_Names[i] && x.Definition.OwnerId == "g-vooperia" && x.OwnerId == OwnerId);
            if (item is null) {
                item = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    OwnerId = OwnerId,
                    Definition_Id = DBCache.GetAll<TradeItemDefinition>().FirstOrDefault(x => x.Name == recipe.Output_Names[i] && x.OwnerId == "g-vooperia").Id,
                    Amount = 0
                };
                await DBCache.Put<TradeItem>(item.Id, item);
                await VooperDB.Instance.TradeItems.AddAsync(item);
                await VooperDB.Instance.SaveChangesAsync();
            }
            item.Amount += recipe.Output_Amounts[i]*ProductionBonus;
        }
    }

}