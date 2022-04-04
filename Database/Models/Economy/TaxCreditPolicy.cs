using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using SV2.Database.Models.Entities;

namespace SV2.Database.Models.Economy.Taxes;

public enum TaxCreditType
{
    Employee = 1,
    Dividend = 2,
    Donation = 3
}

public class TaxCreditPolicy
{
    [Key]
    public string Id { get; set; }
    public string Name { get; set; }
    public decimal Rate { get; set; }
    // should be set to Null if this is a Imperial Tax Credit
    public string? DistrictId { get; set; }
    public TaxCreditType taxCreditType { get; set; }
    // amount this tax credit has paid in the current month
    public decimal Paid { get; set; }
}