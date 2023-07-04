using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;
namespace WUG.Models.Groups;

public class IssueStockModal
{
    [Display(Name = "Amount", Description = "The amount of stock you will issue.")]
    public long Amount { get; set; }

    [Display(Name = "Deposit", Description = "The amount of money you wish to deposit doing the issuing.")]
    public decimal DepositAmount { get; set; }

    [Display(Name = "Purchase", Description = "An amount of stock to immediately purchase. This will ONLY work if you can afford the amount, " +
        "so please factor for the change in price and taxes involved.")]
    public long Purchase { get; set; }
    public long GroupId { get; set; }
    public Group Group { get; set; }
}
