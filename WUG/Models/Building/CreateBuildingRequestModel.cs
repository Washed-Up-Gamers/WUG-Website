﻿using Microsoft.AspNetCore.Mvc.Rendering;
using WUG.Scripting.LuaObjects;

namespace WUG.Models.Building;

public class CreateBuildingRequestModel 
{
    public Province Province { get; set; }
    public LuaBuilding LuaBuildingObj { get; set; }
    public long RequesterId { get; set; }
    public string BuildingId { get; set; }
    public long? AlreadyExistingBuildingId { get; set; }
    public long ProvinceId { get; set; }
    public int levelsToBuild { get; set; }
    public List<SelectListItem> CanBuildAs { get; set; }
    public long? BuildAsId { get; set; }
    public string Name { get; set; }
    public bool IncludeScript { get; set; }
    public string PrefixForIds { get; set; }
    public User User { get; set; }

    public List<BuildingRequest> CurrentRequestsFromThisUser { get; set; }
}