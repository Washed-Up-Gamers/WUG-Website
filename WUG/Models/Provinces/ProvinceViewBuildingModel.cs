using WUG.Models.Building;

namespace WUG.Models.Provinces;

public class ProvinceViewBuildingModel
{
    public Province Province { get; set; }
    public List<BuildingManageModel> ManageModels { get; set; }
}
