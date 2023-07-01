using WUG.Scripting;
using WUG.Scripting.Parser;

namespace WUG.NonDBO;

public class ProvinceDevelopmentStage
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int DevelopmentLevelNeeded { get; set; }
    public List<SyntaxModifierNode> ModifierNodes { get; set; }

    public string PrintableName => Name.Replace("_", " ").ToTitleCase();
}
