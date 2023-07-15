using Microsoft.AspNetCore.Mvc;
using WUG.Models;
using WUG.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using WUG.Helpers;
using WUG.Extensions;
using WUG.Database.Models.Nations;
using System.Xml.Linq;
using WUG.Database.Managers;
using WUG.Scripting.Parser;
using WUG.Database.Models.Government;

namespace WUG.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class NationController : SVController
{
    private readonly ILogger<NationController> _logger;
    private readonly WashedUpDB _dbctx;

    public NationController(ILogger<NationController> logger,
        WashedUpDB dbctx)
    {
        _logger = logger;
        _dbctx = dbctx;
    }

    [HttpGet("/Nation/View/{name}")]
    public IActionResult View(string name)
    {
        Nation Nation = DBCache.GetAll<Nation>().FirstOrDefault(x => x.Name == name);

        return View(Nation);
    }

    [HttpGet("/Nation/Manage/{id}")]
    [UserRequired]
    public IActionResult Manage(long id)
    {
        User user = HttpContext.GetUser();

        Nation Nation = DBCache.Get<Nation>(id);
        if (Nation is null)
            return Redirect("/");

        if (Nation.GovernorId != user.Id)
            return RedirectBack("You must be governor of the Nation to change the details of the Nation!");

        return View(new ManageNationModel()
        {
            Nation = Nation,
            Id = id,
            Description = Nation.Description,
            NameForProvince = Nation.NameForProvince,
            NameForState = Nation.NameForState,
            BasePropertyTax = Nation.BasePropertyTax,
            PropertyTaxPerSize = Nation.PropertyTaxPerSize,
            NameForGovernorOfAProvince = Nation.NameForGovernorOfAProvince,
            NameForGovernorOfAState = Nation.NameForGovernorOfAState
        });
    }

    [HttpPost("/Nation/Manage/{id}")]
    [ValidateAntiForgeryToken]
    [UserRequired]
    public IActionResult Manage(ManageNationModel model)
    {
        Nation Nation = DBCache.Get<Nation>(model.Id);
        if (Nation is null)
            return Redirect("/");

        var user = HttpContext.GetUser();
        if (Nation.GovernorId != user.Id)
            return RedirectBack("You must be governor of the Nation to change the details of the Nation!");

        if (model.BasePropertyTax > 1000)
            return RedirectBack("Nation's Base Property Tax must be 1,000 or less!");
        if (model.PropertyTaxPerSize > 1000)
            return RedirectBack("Nation's Property Tax per size must be 1,000 or less!");

        Nation.Description = model.Description;
        Nation.TitleForProvince = model.NameForProvince?.ToTitleCase();
        Nation.TitleForState = model.NameForState?.ToTitleCase();
        Nation.TitleForGovernorOfProvince = model.NameForGovernorOfAProvince is null ? null : model.NameForGovernorOfAProvince.ToTitleCase();
        Nation.TitleForGovernorOfState = model.NameForGovernorOfAState is null ? null : model.NameForGovernorOfAState.ToTitleCase();
        Nation.BasePropertyTax = model.BasePropertyTax;
        Nation.PropertyTaxPerSize = model.PropertyTaxPerSize;

        StatusMessage = "Successfully saved your changes.";
        return Redirect($"/Nation/View/{Nation.Name}");
    }

    [HttpGet("/Nation/{Nationid}/SetAsCapital/{provinceid}")]
    [UserRequired]
    public IActionResult SetAsCapital(long Nationid, long provinceid)
    {
        Nation Nation = DBCache.Get<Nation>(Nationid);
        if (Nation is null)
            return Redirect("/");

        var user = HttpContext.GetUser();
        if (Nation.GovernorId != user.Id)
            return RedirectBack("You must be governor of the Nation to change the details of the Nation!");

        var prevcapital = Nation.CapitalProvinceId;
        Nation.CapitalProvinceId = provinceid;

        foreach (var province in Nation.Provinces)
        {
            if (province.Id == prevcapital)
            {
                province.UpdateModifiers();
                province.UpdateModifiersAfterBuildingTick();
            }
        }

        DBCache.Get<Province>(provinceid).UpdateModifiers();
        DBCache.Get<Province>(provinceid).UpdateModifiersAfterBuildingTick();

        StatusMessage = $"Successfully set {DBCache.Get<Province>(provinceid).Name} as the Capital of {Nation.Name}.";
        return Redirect($"/Province/Edit/{provinceid}");
    }

    [HttpPost("/Nation/ChangeGovernor/{id}")]
    [ValidateAntiForgeryToken]
    [UserRequired]
    public async Task<IActionResult> ChangeGovernor(long id, long GovernorId) {
        Nation? Nation = DBCache.Get<Nation>(id);
        if (Nation is null)
            return Redirect("/");

        var user = HttpContext.GetUser();
        if (!(await user.IsGovernmentAdmin()))
            return RedirectBack("You must be a government admin to change the governor of a Nation!");

        var oldgovernor = DBCache.Get<User>(Nation.GovernorId);
        var newgovernor = DBCache.Get<User>(GovernorId);
        if (newgovernor is null)
            return RedirectBack("User not found!");

        if (oldgovernor is not null) {
            var roles = Nation.Group.GetMemberRoles(oldgovernor);
            if (roles.Any(x => x.Name == "Governor")) {
                Nation.Group.RemoveEntityFromRole(DBCache.Get<Group>(100), oldgovernor, Nation.Group.Roles.First(x => x.Name == "Governor"), true);
            }
        }
        Nation.GovernorId = GovernorId;
        if (!Nation.Group.MembersIds.Contains(newgovernor.Id)) {
            Nation.Group.MembersIds.Add(newgovernor.Id);
        }
        Nation.Group.AddEntityToRole(DBCache.Get<Group>(100), newgovernor, Nation.Group.Roles.First(x => x.Name == "Governor"), true);

        return RedirectBack($"Successfully changed the governorship of this Nation to {BaseEntity.Find(GovernorId).Name}");
    }


    [HttpPost("/Nation/ChangeSenator/{id}")]
    [ValidateAntiForgeryToken]
    [UserRequired]
    public async Task<IActionResult> ChangeSenator(long id, long SenatorId)
    {
        Nation? Nation = DBCache.Get<Nation>(id);
        if (Nation is null)
            return Redirect("/");

        var user = HttpContext.GetUser();
        if (!(await user.IsGovernmentAdmin()))
            return RedirectBack("You must be a government admin to change the senator of a Nation!");

        if (DBCache.Get<User>(SenatorId) is null)
            return RedirectBack("User not found!");

        var senobj = DBCache.GetAll<CouncilMember>().FirstOrDefault(x => x.NationId == Nation.Id);
        if (senobj is null)
        {
            DBCache.AddNew(Nation.Id, new CouncilMember()
            {
                NationId = Nation.Id,
                UserId = SenatorId
            });
        }
        else
        {
            senobj.UserId = SenatorId;
        }

        return RedirectBack($"Successfully changed the senatorship of this Nation to {BaseEntity.Find(SenatorId).Name}");
    }

    [UserRequired]
    public IActionResult ManageStates(long Id) {
        Nation Nation = DBCache.Get<Nation>(Id);
        User user = HttpContext.GetUser();

        if (Nation is null)
            return Redirect("/");

        if (user.Id != Nation.GovernorId)
            return Redirect("/");

        return View(new ManageStatesModel() {
            States = Nation.States,
            Nation = Nation,
            CreateStateModel = new() {
                NationId = Nation.Id
            }
        });
    }

    [UserRequired]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateState(CreateStateModel model) {
        User user = HttpContext.GetUser();

        Nation Nation = DBCache.Get<Nation>(model.NationId);
        if (Nation is null)
            return Redirect("/");
        if (user.Id != Nation.GovernorId)
            return Redirect("/");

        if (model.MapColor is null)
            return RedirectBack("Mapcolor must be inputed.");
        if (model.MapColor.Length != 6)
            return RedirectBack("Mapcolor must be in hex format");

        var state = new State() {
            Name = model.Name,
            Description = model.Description,
            MapColor = model.MapColor,
            NationId = Nation.Id
        };
        Group stategroup = new(model.Name, Nation.GroupId) {
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
        Nation Nation = DBCache.Get<Nation>(Id);
        User user = HttpContext.GetUser();

        if (Nation is null) {
            return Redirect("/");
        }

        if (user.Id != Nation.GovernorId)
            return Redirect("/");

        return View(Nation);
    }

    [HttpPost]
    [UserRequired]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPolicies(NationPolicyModel model)
    {
        User user = HttpContext.GetUser();

        Nation Nation = DBCache.Get<Nation>(model.NationId);
        if (Nation is null) {
            return Redirect("/");
        }

        if (user.Id != Nation.GovernorId)
        {
            return Redirect("/");
        }

        // update or create ubi policies
        foreach(UBIPolicy pol in model.UBIPolicies)
        {
            UBIPolicy? oldpol = DBCache.GetAll<UBIPolicy>().FirstOrDefault(x => x.NationId == Nation.Id && x.ApplicableRank == pol.ApplicableRank);
            if (oldpol is not null) 
            {
                oldpol.Rate = pol.Rate;
            }
            else {
                pol.Id = IdManagers.GeneralIdGenerator.Generate();
                pol.NationId = model.NationId;
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
                if (oldpol.NationId != Nation.Id) {
                    continue;
                }
                oldpol.Rate = pol.Rate;
                oldpol.Minimum = pol.Minimum;
                oldpol.Maximum = pol.Maximum;
            }
            else {
                pol.Id = IdManagers.GeneralIdGenerator.Generate();
                pol.NationId = model.NationId;
                DBCache.Put(pol.Id, pol);
                DBCache.dbctx.TaxPolicies.Add(pol);
            }
        }

        //await _dbctx.SaveChangesAsync();

        StatusMessage = $"Successfully edited policies.";
        return Redirect($"/Nation/EditPolicies?Id={Nation.Id}");
    }

    [UserRequired]
    [HttpGet]
    public IActionResult MoveNation(long id)
    {
        Nation Nation = DBCache.Get<Nation>(id);

        if (Nation is null)
            return RedirectBack($"Error: Could not find {Nation.Name}!");

        User user = HttpContext.GetUser();

        if (false)
        {
            var daysWaited = (int)(DateTime.UtcNow.Subtract(user.LastMoved).TotalDays);

            if (daysWaited < 60)
                return RedirectBack($"Error: You must wait another {60 - daysWaited} days to move again!");
        }

        user.NationId = Nation.Id;

        if (user.NationId is not null)
            user.LastMoved = DateTime.UtcNow;

        return RedirectBack($"You have moved to {Nation.Name}!");
    }
}