using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace WUG.Models.Groups;

public class IssueIPOModel
{
    [MaxLength(4, ErrorMessage = "Ticker should be 4 or less characters.")]
    [MinLength(2, ErrorMessage = "Ticker should be over 2 characters.")]
    [RegularExpression("^[A-Z]*$", ErrorMessage = "Please use only capital letters.")]
    [Display(Name = "Ticker", Description = "A ticker is a identification for a stock. For example, $TSLA is Tesla stock.")]
    public string Ticker { get; set; }

    [Display(Name = "Amount", Description = "The amount of shares you will issue.")]
    public long Amount { get; set; }

    [Display(Name = "Starting Balance", Description = "The amount of money that your stock will start with.")]
    public decimal StartingBalance { get; set; }

    [Display(Name = "Keep", Description = "The amount of stock you will keep yourself, taken from the total issued. An amount under half will put you at risk for corporate takeovers!")]
    public long Keep { get; set; }
    public long GroupId { get; set; }

    [JsonIgnore]
    public Group Group { get; set; }
}
