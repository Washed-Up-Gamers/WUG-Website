using WUG.Scripting;

namespace WUG.Scripting.LuaObjects;

public class LuaRecipeEdit
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<SyntaxModifierNode> ModifierNodes { get; set; }
    public DictNode Costs { get; set; }
}
