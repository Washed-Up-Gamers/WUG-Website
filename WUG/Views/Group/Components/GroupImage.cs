using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WUG.Web.Components.Groups
{
    public class GroupImage : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(Group group)
        {
            return View(group);
        }
    }
}