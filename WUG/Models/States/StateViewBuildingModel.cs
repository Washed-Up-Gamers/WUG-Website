using WUG.Models.Building;

namespace WUG.Models.States;

public class StateViewBuildingModel
{
    public State State { get; set; }
    public List<BuildingManageModel> ManageModels { get; set; }
}
