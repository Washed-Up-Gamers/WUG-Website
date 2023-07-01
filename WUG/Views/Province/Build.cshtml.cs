using Microsoft.AspNetCore.Mvc.Rendering;

namespace WUG.Views.ProvinceViews.Models;

public class SelectBuildingModel {
    public Province Province { get; set; }
    public List<SelectListItem> CanBuildAs { get; set; }
}