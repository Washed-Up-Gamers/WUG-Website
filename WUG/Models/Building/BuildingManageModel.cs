using Microsoft.AspNetCore.Mvc.Rendering;
using WUG.Scripting.LuaObjects;

namespace WUG.Models.Building;

public class BuildingManageModel {
    public ProducingBuilding Building { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public long BuildingId { get; set; }
    public string RecipeId { get; set; }
    public CreateBuildingRequestModel createBuildingRequestModel { get; set; }
    public List<SelectListItem> GroupRolesForEmployee { get; set; }
    public long? GroupRoleIdForEmployee { get; set; }

    public bool IncludeScript { get; set; } = false;
}