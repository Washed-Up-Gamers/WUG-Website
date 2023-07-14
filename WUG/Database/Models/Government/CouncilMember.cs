using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WUG.Database.Models.Government;

public class CouncilMember
{
    [Key]
    public long DistrictId { get; set; }

    [JsonIgnore]
    public Nation District => DBCache.Get<Nation>(DistrictId)!;

    public long UserId { get; set; }

    public User User => DBCache.Get<User>(UserId)!;
}
