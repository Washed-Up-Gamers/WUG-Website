namespace WUG.Models.Nations;
public class ManageStatesModel {
    public List<State> States { get; set; }
    public Nation Nation { get; set; }

    public CreateStateModel CreateStateModel { get; set; }
}