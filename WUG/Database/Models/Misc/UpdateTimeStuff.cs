using System.ComponentModel.DataAnnotations;

namespace SV2.Database.Models.Misc;

public class UpdateTimeStuff
{
    [Key]
    public long Id { get; set; }
    public DateTime LastProvinceSlotGiven { get; set; }
}
