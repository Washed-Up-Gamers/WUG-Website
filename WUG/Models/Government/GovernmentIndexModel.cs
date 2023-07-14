namespace WUG.Models.Government;

public class GovernmentIndexModel
{
    public User? Emperor;
    public User? PrimeMinister;
    public User? CFV;
    public List<CouncilMember> Senators;
    public List<User> Justices;
    public List<User> PanelMembers;
}