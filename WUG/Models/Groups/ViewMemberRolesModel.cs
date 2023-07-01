using Valour.Api.Models;

namespace WUG.Models.Groups;

public class ViewMemberRolesModel
{
    public Group Group { get; set; }
    public BaseEntity Target { get; set; }
}