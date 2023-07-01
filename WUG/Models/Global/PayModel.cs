using Microsoft.AspNetCore.Mvc.Rendering;

namespace WUG.Models.Global;

public class PayModel
{
    public List<SelectListItem> CanSendAs { get; set; }
    public long FromEntityId { get; set; }
    public long ToEntityId { get; set; }
    public decimal Amount { get; set; }
}
