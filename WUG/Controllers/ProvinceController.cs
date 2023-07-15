using Microsoft.AspNetCore.Mvc;
using WUG.Models;
using WUG.Managers;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using WUG.Helpers;
using WUG.Extensions;
using WUG.Database.Managers;
using WUG.Models.Provinces;
using Microsoft.EntityFrameworkCore;
using System.Net;
using WUG.Views.ProvinceViews.Models;
using WUG.Database.Models.Nations;
using WUG.Models.Building;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WUG.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class ProvinceController : SVController
{
    private readonly ILogger<ProvinceController> _logger;
    private readonly WashedUpDB _dbctx;

    public ProvinceController(ILogger<ProvinceController> logger,
        WashedUpDB dbctx)
    {
        _logger = logger;
        _dbctx = dbctx;
    }

    [HttpGet("/Province/View/{id}")]
    public IActionResult View(long id)
    {
        if (!DBCache.HCache[typeof(Province)].TryGetValue(id, out object _obj))
            return Redirect("/");
        Province province = (Province)_obj;

        return View(province);
    }

    [HttpGet("/Province/ViewBuildings")]
    [UserRequired]
    public IActionResult ViewBuildings(long id)
    {
        var user = HttpContext.GetUser();
        Province? province = DBCache.Get<Province>(id);
        if (province is null)
            return RedirectBack();

        var viewbuildingmodel = new ProvinceViewBuildingModel()
        {
            ManageModels = new(),
            Province = province
        };

        foreach (var building in province.GetBuildings())
        {

            var model = new CreateBuildingRequestModel()
            {
                Province = building.Province,
                LuaBuildingObj = building.BuildingObj,
                ProvinceId = building.ProvinceId,
                BuildingId = building.LuaBuildingObjId,
                AlreadyExistingBuildingId = building.Id,
                CanBuildAs = new(),
                IncludeScript = viewbuildingmodel.ManageModels.Count == 0,
                PrefixForIds = "",
                User = user
            };

            var managemodel = new BuildingManageModel()
            {
                Building = building,
                Name = building.Name,
                Description = building.Description,
                RecipeId = building.RecipeId,
                BuildingId = building.Id,
                createBuildingRequestModel = model
            };

            // only groups/corporations can have building employees
            if (building.Owner.EntityType == EntityType.Group || building.Owner.EntityType == EntityType.Corporation)
            {
                var group = (Group)building.Owner;
                var ownerauthority = group.GetAuthority(user);
                managemodel.GroupRolesForEmployee = new();
                managemodel.GroupRolesForEmployee.Add(new("None", "0"));
                managemodel.GroupRolesForEmployee.AddRange(group.Roles.Where(x => x.Authority < ownerauthority).Select(x => new SelectListItem(x.Name, x.Id.ToString(), x.Id == building.EmployeeGroupRoleId)));
            }

            viewbuildingmodel.ManageModels.Add(managemodel);
        }

        return View(viewbuildingmodel);
    }

    [HttpGet("/Province/BulkManage")]
    [UserRequired]
    public IActionResult BulkManage()
    {
        var user = HttpContext.GetUser();

        var model = new BulkManageModel();
        model.Provinces = DBCache.GetAll<Province>().Where(x => x.CanEdit(user)).ToList();
        return View(model);
    }

    [HttpGet("/Province/MyRequests")]
    [UserRequired]
    public async Task<IActionResult> MyRequests(bool toggleonlygranted) {
        var user = HttpContext.GetUser();

        List<BuildingRequest> requests = new();
        List<long> canbuildasids = new() { user.Id };
        canbuildasids.AddRange(DBCache.GetAll<Group>().Where(x => x.HasPermission(user, GroupPermissions.Build)).Select(x => x.Id).ToList());
        requests = await _dbctx.BuildingRequests.Where(x => x.Granted == toggleonlygranted && canbuildasids.Contains(x.RequesterId)).ToListAsync();
        return View(requests);
    }

    [HttpGet("/Province/BulkBuildingRequests")]
    [UserRequired]
    public async Task<IActionResult> BulkBuildingRequests(bool toggleonlyreviewed) {
        var user = HttpContext.GetUser();

        List<BuildingRequest> requests = new();
        if (true) {
            var idscanmanage = DBCache.GetAll<Province>().Where(x => x.CanManageBuildingRequests(user)).Select(x => x.Id).ToList();
            requests = await _dbctx.BuildingRequests.Where(x => x.Reviewed == toggleonlyreviewed && idscanmanage.Contains(x.ProvinceId)).ToListAsync();
        }
        else if (false) {
            List<long> canbuildasids = new() { user.Id };
            canbuildasids.AddRange(DBCache.GetAll<Group>().Where(x => x.HasPermission(user, GroupPermissions.Build)).Select(x => x.Id).ToList());
            requests = await _dbctx.BuildingRequests.Where(x => x.Reviewed == toggleonlyreviewed && canbuildasids.Contains(x.RequesterId)).ToListAsync();
        }
        return View(requests);
    }

    [HttpPost("Province/BuildingRequest/Approve")]
    [ValidateAntiForgeryToken]
    [UserRequired]
    public async Task<string> ApproveBuildingRequest(long id) 
    {
        var user = HttpContext.GetUser();
        var request = await _dbctx.BuildingRequests.FindAsync(id);
        if (request is null) {
            return "Request not found";
        }
        if (!request.Province.CanManageBuildingRequests(user)) {
            return "You lack permission!";
        }

        request.ActionTime = DateTime.UtcNow;
        request.ReviewerId = user.Id;
        request.Reviewed = true;
        request.Granted = true;
        await _dbctx.SaveChangesAsync();

        return $"Approved request,{request.Id}";
    }

    [HttpPost("Province/BuildingRequest/Deny")]
    [ValidateAntiForgeryToken]
    [UserRequired]
    public async Task<string> DenyBuildingRequest(long id) {
        var user = HttpContext.GetUser();
        var request = await _dbctx.BuildingRequests.FindAsync(id);
        if (request is null) {
            return "Request not found";
        }
        if (!request.Province.CanManageBuildingRequests(user)) {
            return "You lack permission!";
        }

        request.ActionTime = DateTime.UtcNow;
        request.ReviewerId = user.Id;
        request.Reviewed = true;
        request.Granted = false;
        await _dbctx.SaveChangesAsync();

        return $"Denied request,{request.Id}";
    }

    [HttpPost("/Province/BulkManage")]
    [ValidateAntiForgeryToken]
    [UserRequired]
    public IActionResult BulkManage(BulkManageModel model) {
        var user = HttpContext.GetUser();

        if (model.Provinces is null || model.Provinces.Count == 0) {
            return RedirectBack("You have no provinces you can manage!");
        }

        foreach (var newprovince in model.Provinces) {
            var oldprovince = DBCache.Get<Province>(newprovince.Id);
            if (oldprovince.CanEdit(user)) 
            {
                oldprovince.Name = newprovince.Name;
                oldprovince.BasePropertyTax = newprovince.BasePropertyTax;
                oldprovince.PropertyTaxPerSize = newprovince.PropertyTaxPerSize;
            }
        }

        return RedirectBack("Successfully saved your changes.");
    }

    [HttpGet("/Province/Edit/{id}")]
    public IActionResult Edit(long id)
    {
        if (!DBCache.HCache[typeof(Province)].TryGetValue(id, out object _obj))
            return Redirect("/");
        Province province = (Province)_obj;
        User? user = UserManager.GetUser(HttpContext);

        if (user is null)
            return Redirect("/account/login");
        if (!province.CanEdit(user))
            return RedirectBack("You lack permission to manage this province!");

        return View(province);
    }

    [UserRequired]
    [ValidateAntiForgeryToken]
    [HttpPost]
    public IActionResult Edit(Province newprovince)
    {
        Province? oldprovince = DBCache.Get<Province>(newprovince.Id);
        if (oldprovince is null)
            return Redirect("/");

        var user = HttpContext.GetUser();
        if (!oldprovince.CanEdit(user))
            return RedirectBack("You lack permission to edit this province!");

        if (newprovince.BasePropertyTax > 1000)
            return RedirectBack("Base Property Tax must be 1,000 or less!");
        if (newprovince.PropertyTaxPerSize > 1000)
            return RedirectBack("Property Tax per size must be 1,000 or less!");

        oldprovince.Name = newprovince.Name;
        oldprovince.Description = newprovince.Description;
        oldprovince.BasePropertyTax = newprovince.BasePropertyTax;
        oldprovince.PropertyTaxPerSize = newprovince.PropertyTaxPerSize;

        StatusMessage = "Successfully saved your changes.";
        return Redirect($"/Province/View/{oldprovince.Id}");
    }

    [HttpPost("/Province/ChangeGovernor/{id}")]
    [ValidateAntiForgeryToken]
    [UserRequired]
    public IActionResult ChangeGovernor(long id, long GovernorId)
    {
        Province? province = DBCache.Get<Province>(id);
        if (province is null)
            return Redirect("/");

        var user = HttpContext.GetUser();
        if (province.Nation.GovernorId != user.Id)
            return RedirectBack("You must be governor of the Nation to change the governor of a province!");

        province.GovernorId = GovernorId;

        var entity = BaseEntity.Find(GovernorId);
        var name = entity is not null ? entity.Name : "none";

        return RedirectBack($"Successfully changed the governorship of this province to {name}");
    }

    [HttpPost("/Province/ChangeState")]
    [ValidateAntiForgeryToken]
    [UserRequired]
    public IActionResult ChangeState(ChangeStateModel model) {
        Province? province = DBCache.Get<Province>(model.Id);
        if (province is null) return Redirect("/");

        var user = HttpContext.GetUser();
        if (province.Nation.GovernorId != user.Id)
            return RedirectBack("You must be governor of the Nation to change the state of a province!");
        if (model.StateId is null) {
            province.StateId = null;
            StatusMessage = $"Successfully changed the state of this province to none";
            return Redirect($"/Nation/View/{province.Nation.Name}");
        }
        else {
            State? state = DBCache.Get<State>(model.StateId);
            if (state is null) return Redirect("/");

            if (state.NationId != province.NationId)
                return RedirectBack("You can not assign a state to a province that is not in the same Nation!");

            province.StateId = model.StateId;
            StatusMessage = $"Successfully changed the state of this province to {state.Name}";
            return Redirect($"/Nation/View/{province.Nation.Name}");
        }
    }

    [HttpGet("/Province/Build/{id}")]
    public IActionResult Build(long id) {
        Province? province = DBCache.Get<Province>(id);
        if (province is null)
            return Redirect("/");

        User? user = UserManager.GetUser(HttpContext);

        if (user is null)
            return Redirect("/account/login");

        List<BaseEntity> canbuildas = new() { user };
        canbuildas.AddRange(DBCache.GetAll<Group>().Where(x => x.HasPermission(user, GroupPermissions.Build)).Select(x => (BaseEntity)x).ToList());
        return View(new SelectBuildingModel() {
            Province = province,
            CanBuildAs = canbuildas.Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = x.Id.ToString(), Text = x.Name }).ToList()
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}