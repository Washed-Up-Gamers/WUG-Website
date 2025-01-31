using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace WUG.Database.Models.Economy;

public class UBIPolicy
{
    [Key]
    public long Id {get; set; }
    public decimal Rate { get; set;}

    // if true, then pay Rate to everyone, and ApplicableRank should be set to Unranked
    public bool Anyone { get; set;}

    // users with this rank will get paid Rate daily
    public Rank? ApplicableRank { get; set;}

    // should be 100 if this is the UN UBI
    public long NationId { get; set;}

    public UBIPolicy()
    {

    }
}