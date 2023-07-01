using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WUG.Database.Models.Corporations;

public enum CorporationVotingProposalType
{
    ChangingCorporateCharter,
    Text,
    DividendRateChange
}

[Index(nameof(CorporationId))]
public class CorporationVotingProposal
{
    [Key]
    public long Id { get; set; }

    public long CorporationId { get; set; }

    [NotMapped]
    public Corporation Corporation => DBCache.Get<Corporation>(CorporationId);

    public CorporationVotingProposalType Type { get; set; }
}
