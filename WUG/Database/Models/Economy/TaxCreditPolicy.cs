using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using WUG.Database.Models.Entities;

namespace WUG.Database.Models.Economy;

public enum TaxCreditType
{
    Employee = 1,
    Dividend = 2,
    Donation = 3
}

public class TaxCreditPolicy
{
    [Key]
    public long Id {get; set; }

    [VarChar(64)]
    public string Name { get; set; }
    public decimal Rate { get; set; }

    // should be set to Null if this is a Imperial Tax Credit

    public long NationId { get; set; }
    public TaxCreditType taxCreditType { get; set; }
    // amount this tax credit has paid in the current month
    public decimal Paid { get; set; }
}