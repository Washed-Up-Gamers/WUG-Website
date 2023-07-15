namespace WUG.Models.Groups;

public class CreateGroupModel
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public long NationId { get; set; }
    public string? ImageUrl { get; set; }
    public GroupTypes GroupType { get; set; }
    public long OwnerId { get; set; }
}