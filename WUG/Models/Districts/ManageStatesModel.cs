namespace WUG.Models.Districts;
public class ManageStatesModel {
    public List<State> States { get; set; }
    public Nation District { get; set; }

    public CreateStateModel CreateStateModel { get; set; }
}