﻿using Microsoft.AspNetCore.Mvc;
using WUG.Models;
using WUG.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using WUG.Helpers;
using WUG.Extensions;
using WUG.Database.Models.Districts;
using System.Xml.Linq;
using WUG.Database.Managers;
using WUG.Scripting.Parser;
using WUG.Database.Models.Government;

namespace WUG.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class DistrictController : SVController
{
    private readonly ILogger<DistrictController> _logger;
    private readonly WashedUpDB _dbctx;

    public DistrictController(ILogger<DistrictController> logger,
        WashedUpDB dbctx)
    {
        _logger = logger;
        _dbctx = dbctx;
    }

    [HttpGet("/District/View/{name}")]
    public IActionResult View(string name)
    {
        Nation district = DBCache.GetAll<Nation>().FirstOrDefault(x => x.Name == name);

        return View(district);
    }

    [HttpGet("/District/Manage/{id}")]
    [UserRequired]
    public IActionResult Manage(long id)
    {
        User user = HttpContext.GetUser();

        Nation district = DBCache.Get<Nation>(id);
        if (district is null)
            return Redirect("/");

        if (district.GovernorId != user.Id)
            return RedirectBack("You must be governor of the district to change the details of the district!");

        return View(new ManageDistrictModel()
        {
            District = district,
            Id = id,
            Description = district.Description,
            NameForProvince = district.NameForProvince,
            NameForState = district.NameForState,
            BasePropertyTax = district.BasePropertyTax,
            PropertyTaxPerSize = district.PropertyTaxPerSize,
            NameForGovernorOfAProvince = district.NameForGovernorOfAProvince,
            NameForGovernorOfAState = district.NameForGovernorOfAState
        });
    }

    [HttpPost("/District/Manage/{id}")]
    [ValidateAntiForgeryToken]
    [UserRequired]
    public IActionResult Manage(ManageDistrictModel model)
    {
        Nation district = DBCache.Get<Nation>(model.Id);
        if (district is null)
            return Redirect("/");

        var user = HttpContext.GetUser();
        if (district.GovernorId != user.Id)
            return RedirectBack("You must be governor of the district to change the details of the district!");

        if (model.BasePropertyTax > 1000)
            return RedirectBack("District's Base Property Tax must be 1,000 or less!");
        if (model.PropertyTaxPerSize > 1000)
            return RedirectBack("District's Property Tax per size must be 1,000 or less!");

        district.Description = model.Description;
        district.TitleForProvince = model.NameForProvince?.ToTitleCase();
        district.TitleForState = model.NameForState?.ToTitleCase();
        district.TitleForGovernorOfProvince = model.NameForGovernorOfAProvince is null ? null : model.NameForGovernorOfAProvince.ToTitleCase();
        district.TitleForGovernorOfState = model.NameForGovernorOfAState is null ? null : model.NameForGovernorOfAState.ToTitleCase();
        district.BasePropertyTax = model.BasePropertyTax;
        district.PropertyTaxPerSize = model.PropertyTaxPerSize;

        StatusMessage = "Successfully saved your changes.";
        return Redirect($"/District/View/{district.Name}");
    }

    [HttpGet("/District/{districtid}/SetAsCapital/{provinceid}")]
    [UserRequired]
    public IActionResult SetAsCapital(long districtid, long provinceid)
    {
        Nation district = DBCache.Get<Nation>(districtid);
        if (district is null)
            return Redirect("/");

        var user = HttpContext.GetUser();
        if (district.GovernorId != user.Id)
            return RedirectBack("You must be governor of the district to change the details of the district!");

        var prevcapital = district.CapitalProvinceId;
        district.CapitalProvinceId = provinceid;

        foreach (var province in district.Provinces)
        {
            if (province.Id == prevcapital)
            {
                province.UpdateModifiers();
                province.UpdateModifiersAfterBuildingTick();
            }
        }

        DBCache.Get<Province>(provinceid).UpdateModifiers();
        DBCache.Get<Province>(provinceid).UpdateModifiersAfterBuildingTick();

        StatusMessage = $"Successfully set {DBCache.Get<Province>(provinceid).Name} as the Capital of {district.Name}.";
        return Redirect($"/Province/Edit/{provinceid}");
    }

    [HttpPost("/District/ChangeGovernor/{id}")]
    [ValidateAntiForgeryToken]
    [UserRequired]
    public async Task<IActionResult> ChangeGovernor(long id, long GovernorId) {
        Nation? district = DBCache.Get<Nation>(id);
        if (district is null)
            return Redirect("/");

        var user = HttpContext.GetUser();
        if (!(await user.IsGovernmentAdmin()))
            return RedirectBack("You must be a government admin to change the governor of a district!");

        var oldgovernor = DBCache.Get<User>(district.GovernorId);
        var newgovernor = DBCache.Get<User>(GovernorId);
        if (newgovernor is null)
            return RedirectBack("User not found!");

        if (oldgovernor is not null) {
            var roles = district.Group.GetMemberRoles(oldgovernor);
            if (roles.Any(x => x.Name == "Governor")) {
                district.Group.RemoveEntityFromRole(DBCache.Get<Group>(100), oldgovernor, district.Group.Roles.First(x => x.Name == "Governor"), true);
            }
        }
        district.GovernorId = GovernorId;
        if (!district.Group.MembersIds.Contains(newgovernor.Id)) {
            district.Group.MembersIds.Add(newgovernor.Id);
        }
        district.Group.AddEntityToRole(DBCache.Get<Group>(100), newgovernor, district.Group.Roles.First(x => x.Name == "Governor"), true);

        return RedirectBack($"Successfully changed the governorship of this district to {BaseEntity.Find(GovernorId).Name}");
    }


    [HttpPost("/District/ChangeSenator/{id}")]
    [ValidateAntiForgeryToken]
    [UserRequired]
    public async Task<IActionResult> ChangeSenator(long id, long SenatorId)
    {
        Nation? district = DBCache.Get<Nation>(id);
        if (district is null)
            return Redirect("/");

        var user = HttpContext.GetUser();
        if (!(await user.IsGovernmentAdmin()))
            return RedirectBack("You must be a government admin to change the senator of a district!");

        if (DBCache.Get<User>(SenatorId) is null)
            return RedirectBack("User not found!");

        var senobj = DBCache.GetAll<CouncilMember>().FirstOrDefault(x => x.DistrictId == district.Id);
        if (senobj is null)
        {
            DBCache.AddNew(district.Id, new CouncilMember()
            {
                DistrictId = district.Id,
                UserId = SenatorId
            });
        }
        else
        {
            senobj.UserId = SenatorId;
        }

        return RedirectBack($"Successfully changed the senatorship of this district to {BaseEntity.Find(SenatorId).Name}");
    }

    [UserRequired]
    public IActionResult ManageStates(long Id) {
        Nation district = DBCache.Get<Nation>(Id);
        User user = HttpContext.GetUser();

        if (district is null)
            return Redirect("/");

        if (user.Id != district.GovernorId)
            return Redirect("/");

        return View(new ManageStatesModel() {
            States = district.States,
            District = district,
            CreateStateModel = new() {
                DistrictId = district.Id
            }
        });
    }

    [UserRequired]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateState(CreateStateModel model) {
        User user = HttpContext.GetUser();

        Nation district = DBCache.Get<Nation>(model.DistrictId);
        if (district is null)
            return Redirect("/");
        if (user.Id != district.GovernorId)
            return Redirect("/");

        if (model.MapColor is null)
            return RedirectBack("Mapcolor must be inputed.");
        if (model.MapColor.Length != 6)
            return RedirectBack("Mapcolor must be in hex format");

        var state = new State() {
            Name = model.Name,
            Description = model.Description,
            MapColor = model.MapColor,
            DistrictId = district.Id
        };
        Group stategroup = new(model.Name, district.GroupId) {
            Id = IdManagers.GroupIdGenerator.Generate(),
            GroupType = GroupTypes.State
        };

        DBCache.AddNew(stategroup.Id, stategroup);
        state.GroupId = stategroup.Id;
        state.Id = stategroup.Id;

        var role = new GroupRole() {
            Name = "Governor",
            Color = "ffffff",
            GroupId = stategroup.Id,
            PermissionValue = GroupPermissions.FullControl.Value,
            Id = IdManagers.GeneralIdGenerator.Generate(),
            Authority = 99999999,
            Salary = 0.0m,
            MembersIds = new()
        };
        DBCache.AddNew(role.Id, role);

        DBCache.AddNew(state.Id, state);

        return RedirectBack("Successfully create state.");
    }

    [UserRequired]
    public IActionResult TaxPolicies(long Id)
    {
        Nation district = DBCache.Get<Nation>(Id);
        User user = HttpContext.GetUser();

        if (district is null) {
            return Redirect("/");
        }

        if (user.Id != district.GovernorId)
            return Redirect("/");

        return View(district);
    }

    [HttpPost]
    [UserRequired]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPolicies(DistrictPolicyModel model)
    {
        User user = HttpContext.GetUser();

        Nation district = DBCache.Get<Nation>(model.DistrictId);
        if (district is null) {
            return Redirect("/");
        }

        if (user.Id != district.GovernorId)
        {
            return Redirect("/");
        }

        // update or create ubi policies
        foreach(UBIPolicy pol in model.UBIPolicies)
        {
            UBIPolicy? oldpol = DBCache.GetAll<UBIPolicy>().FirstOrDefault(x => x.NationId == district.Id && x.ApplicableRank == pol.ApplicableRank);
            if (oldpol is not null) 
            {
                oldpol.Rate = pol.Rate;
            }
            else {
                pol.Id = IdManagers.GeneralIdGenerator.Generate();
                pol.NationId = model.DistrictId;
                DBCache.Put(pol.Id, pol);
                DBCache.dbctx.UBIPolicies.Add(pol);
            }
        }
        
        // update or create tax policies
        foreach(TaxPolicy pol in model.TaxPolicies)
        {
            TaxPolicy? oldpol = DBCache.Get<TaxPolicy>(pol.Id);
            if (oldpol is not null) 
            {
                if (oldpol.NationId != district.Id) {
                    continue;
                }
                oldpol.Rate = pol.Rate;
                oldpol.Minimum = pol.Minimum;
                oldpol.Maximum = pol.Maximum;
            }
            else {
                pol.Id = IdManagers.GeneralIdGenerator.Generate();
                pol.NationId = model.DistrictId;
                DBCache.Put(pol.Id, pol);
                DBCache.dbctx.TaxPolicies.Add(pol);
            }
        }

        //await _dbctx.SaveChangesAsync();

        StatusMessage = $"Successfully edited policies.";
        return Redirect($"/District/EditPolicies?Id={district.Id}");
    }

    [UserRequired]
    [HttpGet]
    public IActionResult MoveDistrict(long id)
    {
        Nation district = DBCache.Get<Nation>(id);

        if (district is null)
            return RedirectBack($"Error: Could not find {district.Name}!");

        User user = HttpContext.GetUser();

        if (false)
        {
            var daysWaited = (int)(DateTime.UtcNow.Subtract(user.LastMoved).TotalDays);

            if (daysWaited < 60)
                return RedirectBack($"Error: You must wait another {60 - daysWaited} days to move again!");
        }

        user.NationId = district.Id;

        if (user.NationId is not null)
            user.LastMoved = DateTime.UtcNow;

        return RedirectBack($"You have moved to {district.Name}!");
    }
}