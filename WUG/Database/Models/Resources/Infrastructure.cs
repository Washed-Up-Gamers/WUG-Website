using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WUG.Database.Models.Entities;
using WUG.Database.Models.Items;
using WUG.Managers;
using WUG.Database.Managers;

namespace WUG.Database.Models.Factories;

public class Infrastructure : ProducingBuilding
{
    public override BuildingType BuildingType { get => BuildingType.Infrastructure; }

    /// <summary>
    /// This function is called every IRL hour
    /// </summary>
    public async ValueTask Tick()
    {
    }
}