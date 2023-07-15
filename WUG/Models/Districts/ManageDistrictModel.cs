namespace WUG.Models.Nations;
public class ManageNationModel
{
    public Nation Nation { get; set; }
    public long Id { get; set; }
    public string? Description { get; set; }
    public string? NameForState { get; set; }
    public string? NameForProvince { get; set; }
    public string? NameForGovernorOfAProvince { get; set; }
    public string? NameForGovernorOfAState{ get; set; }
    public double? BasePropertyTax { get; set; }
    public double? PropertyTaxPerSize { get; set; }
}