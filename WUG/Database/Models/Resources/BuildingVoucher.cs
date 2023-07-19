using System.ComponentModel.DataAnnotations;

namespace WUG.Database.Models.Resources;

public enum VoucherUseType
{
    OnlyUsedByNations = 0,
    GivenOutByNations = 1,
    GivenOutByIC = 2
}

public enum VoucherBuildingType
{
    Mine = 0,
    Factory = 1
}

public class BuildingVoucher
{
    [Key]
    public long Id { get; set; }
    public long EntityId { get; set; }
    public VoucherUseType VoucherUseType { get; set; }
    public VoucherBuildingType BuildingType { get; set; }
    public int AmountGiven { get; set; }
    public int AmountUsed { get; set; }
    public DateTime TimeGiven { get; set; }
    public bool UsedAll { get; set; }
}