using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using WUG.Database.Models.Entities;

namespace WUG.Database.Models.Economy.Stocks;

[Index(nameof(SecurityId))]
[Index(nameof(OwnerId))]
public class SecurityOwnership
{
    [Key]
    public long Id {get; set; }

    // Owner of this stock object
    public long OwnerId { get; set; }
    
    [NotMapped]
    public BaseEntity Owner => BaseEntity.Find(OwnerId)!;

    public long SecurityId { get; set; }
    public long Amount { get; set ;}

    [ForeignKey("SecurityId")]
    public Security Security { get; set; }
}