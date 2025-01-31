﻿using System;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Shared.Lua.Scripting;
using WUG.Scripting;
using WUG.Scripting.LuaObjects;
using WUG.Scripting.Parser;

namespace WUG.Scripting;

public enum NodeType
{
    BASE,
    ADD,
    SUBTRACT,
    FACTOR,
    RAISETO,
    DECIMAL,
    SYSTEMVAR,
    EXPRESSION,
    IFSTATEMENT,
    COMPARISON,
    CONDITIONALSTATEMENT,
    CONDITIONALLOGICBLOCK,
    EFFECT,
    EFFECTBODY,
    MODIFIER,
    DICTNODE,
    ADDLOCALSNODE,
    GETLOCAL,
    DIVIDE,
    HASSTATICMODIFIER
}

public class ExecutionState
{
    public Dictionary<string, decimal> Locals { get; set; }
    public Nation Nation { get; set; }
    public Province? Province { get; set; }
    public ProducingBuilding Building { get; set; }
    public LuaResearch Research { get; set; }
    public BuildingUpgrade BuildingUpgrade { get; set; }
    public Recipe Recipe { get; set; }
    public LuaRecipeEdit RecipeEdit { get; set; }
    public Dictionary<string, decimal> ChangeSystemVarsBy { get; set; }
    public ScriptScopeType? ParentScopeType { get; set; }
    public ExecutionState(Nation nation, Province? province, Dictionary<string, decimal>? changesystemvarsby = null, ScriptScopeType? parentscopetype = null, ProducingBuilding? building = null, LuaResearch? research = null, BuildingUpgrade? buildingUpgrade = null, Recipe? recipe = null, LuaRecipeEdit? recipeedit = null)
    {
        Locals = new();
        Nation = nation;
        Province = province;
        ChangeSystemVarsBy = changesystemvarsby ?? new();
        ParentScopeType = parentscopetype;
        Building = building;
        Research = research;
        BuildingUpgrade = buildingUpgrade;
        Recipe = recipe;
        RecipeEdit = recipeedit;
    }
}

[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
[JsonDerivedType(typeof(Base), typeDiscriminator: 0)]
[JsonDerivedType(typeof(Add), typeDiscriminator: 1)]
[JsonDerivedType(typeof(Subtract), typeDiscriminator: 2)]
[JsonDerivedType(typeof(Factor), typeDiscriminator: 3)]
[JsonDerivedType(typeof(RaiseTo), typeDiscriminator: 4)]
[JsonDerivedType(typeof(Decimal), typeDiscriminator: 5)]
[JsonDerivedType(typeof(SystemVar), typeDiscriminator: 6)]
[JsonDerivedType(typeof(ExpressionNode), typeDiscriminator: 7)]
[JsonDerivedType(typeof(ConditionalSyntaxNode), typeDiscriminator: 8)]
[JsonDerivedType(typeof(EffectNode), typeDiscriminator: 9)]
[JsonDerivedType(typeof(EffectBody), typeDiscriminator: 10)]
[JsonDerivedType(typeof(SyntaxModifierNode), typeDiscriminator: 11)]
[JsonDerivedType(typeof(DictNode), typeDiscriminator: 12)]
[JsonDerivedType(typeof(AddLocalsNode), typeDiscriminator: 13)]
[JsonDerivedType(typeof(GetLocal), typeDiscriminator: 14)]
[JsonDerivedType(typeof(Divide), typeDiscriminator: 15)]
public abstract class SyntaxNode
{
    public NodeType NodeType;
    public SyntaxNode Parent;

    [JsonIgnore]
    public int LineNumber { get; set; }

    [JsonIgnore]
    public string FileName { get; set; }

    public abstract decimal GetValue(ExecutionState state);
    public void HandleError(string error, string message)
    {
        LuaHandler.HandleError(FileName, LineNumber, error, message);
    }
}

[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
[JsonDerivedType(typeof(ConditionalStatementComparison), typeDiscriminator: 0)]
[JsonDerivedType(typeof(ConditionalLogicBlockStatement), typeDiscriminator: 1)]
[JsonDerivedType(typeof(ConditionalStatement), typeDiscriminator: 2)]
[JsonDerivedType(typeof(HasStaticModifierStatement), typeDiscriminator: 3)]
[JsonDerivedType(typeof(IfStatement), typeDiscriminator: 4)]
public abstract class ConditionalSyntaxNode : SyntaxNode
{
    public override decimal GetValue(ExecutionState state) => 0.00m;
    public abstract bool IsTrue(ExecutionState state);
}

public class Add : SyntaxNode
{
    public SyntaxNode Value { get; set; }
    public Add()
    {
        NodeType = NodeType.ADD;
    }

    public override decimal GetValue(ExecutionState state)
    {
        return Value.GetValue(state);
    }
}

public class Subtract : SyntaxNode
{
    public SyntaxNode Value { get; set; }
    public Subtract()
    {
        NodeType = NodeType.SUBTRACT;
    }

    public override decimal GetValue(ExecutionState state)
    {
        return Value.GetValue(state);
    }
}

public class Decimal : SyntaxNode
{
    public decimal Value { get; set; }
    public Decimal()
    {
        NodeType = NodeType.DECIMAL;
    }

    public override decimal GetValue(ExecutionState state)
    {
        return Value;
    }
}

public class Base : SyntaxNode
{
    public SyntaxNode Value { get; set; }
    public Base()
    {
        NodeType = NodeType.BASE;
    }

    public override decimal GetValue(ExecutionState state)
    {
        return Value.GetValue(state);
    }
}

public class RaiseTo : SyntaxNode
{
    public SyntaxNode Value { get; set; }
    public RaiseTo()
    {
        NodeType = NodeType.RAISETO;
    }
    public override decimal GetValue(ExecutionState state)
    {
        return Value.GetValue(state);
    }
}


public class Factor : SyntaxNode
{
    public SyntaxNode Value { get; set; }
    public Factor()
    {
        NodeType = NodeType.FACTOR;
    }
    public override decimal GetValue(ExecutionState state)
    {
        return Value.GetValue(state);
    }
}

public class Divide : SyntaxNode
{
    public SyntaxNode Value { get; set; }
    public Divide()
    {
        NodeType = NodeType.DIVIDE;
    }
    public override decimal GetValue(ExecutionState state)
    {
        return Value.GetValue(state);
    }
}

public enum ComparisonType
{
    GREATER_THAN,
    GREATER_THAN_OR_EQUAL,
    LESS_THAN,
    LESS_THAN_OR_EQUAL,
    EQUAL
}

public class ConditionalStatementComparison : ConditionalSyntaxNode
{
    public ComparisonType comparisonType { get; set; }
    public SyntaxNode LeftSide { get; set; }
    public SyntaxNode RightSide { get; set; }
    public ConditionalStatementComparison()
    {
        NodeType = NodeType.COMPARISON;
    }

    public override bool IsTrue(ExecutionState state)
    {
        return comparisonType switch
        {
            ComparisonType.EQUAL => LeftSide.GetValue(state) == RightSide.GetValue(state),
            ComparisonType.GREATER_THAN => LeftSide.GetValue(state) > RightSide.GetValue(state),
            ComparisonType.GREATER_THAN_OR_EQUAL => LeftSide.GetValue(state) >= RightSide.GetValue(state),
            ComparisonType.LESS_THAN => LeftSide.GetValue(state) < RightSide.GetValue(state),
            ComparisonType.LESS_THAN_OR_EQUAL => LeftSide.GetValue(state) <= RightSide.GetValue(state),
        };
    }
}

public enum ConditionalLogicBlockType
{
    AND,
    OR,
    NOT,
    NOR
}

public class ConditionalLogicBlockStatement : ConditionalSyntaxNode
{
    public List<ConditionalSyntaxNode> Children { get; set; }
    public ConditionalLogicBlockType Type { get; set; }
    public ConditionalLogicBlockStatement()
    {
        NodeType = NodeType.CONDITIONALLOGICBLOCK;
        Children = new();
    }

    public override bool IsTrue(ExecutionState state)
    {
        return Type switch
        {
            ConditionalLogicBlockType.AND => Children.All(x => x.IsTrue(state)),
            ConditionalLogicBlockType.OR => Children.Any(x => x.IsTrue(state)),
            ConditionalLogicBlockType.NOR => !Children.Any(x => x.IsTrue(state)),
            ConditionalLogicBlockType.NOT => !Children.All(x => x.IsTrue(state))
        };
    }
}

public class HasStaticModifierStatement : ConditionalSyntaxNode
{
    public string StaticModifierId { get; set; }
    public HasStaticModifierStatement()
    {
        NodeType = NodeType.HASSTATICMODIFIER;
    }

    public override bool IsTrue(ExecutionState state)
    {
        return state.ParentScopeType switch
        {
            ScriptScopeType.Nation => state.Nation.StaticModifiers.Any(x => x.LuaStaticModifierObjId == StaticModifierId),
            ScriptScopeType.Province => state.Province.StaticModifiers.Any(x => x.LuaStaticModifierObjId == StaticModifierId),
            ScriptScopeType.Building => state.Building.StaticModifiers.Any(x => x.LuaStaticModifierObjId == StaticModifierId),
            _ => false
        };
    }
}

public class ConditionalStatement : ConditionalSyntaxNode
{
    public List<ConditionalSyntaxNode> Conditionals { get; set; }
    public ConditionalStatement()
    {
        NodeType = NodeType.CONDITIONALSTATEMENT;
    }

    public override bool IsTrue(ExecutionState state)
    {
        return Conditionals.All(x => x.IsTrue(state));
    }
}

public class IfStatement : ConditionalSyntaxNode, IEffectNode
{
    public ConditionalStatement Limit { get; set; }
    public ExpressionNode ValueNode { get; set; }
    public EffectBody EffectNode { get; set; }
    public EffectType effectType => EffectType.None;

    public IfStatement()
    {
        NodeType = NodeType.IFSTATEMENT;
    }

    public void Execute(ExecutionState state)
    {
        if (IsTrue(state))
            EffectNode.Execute(state);
    }

    public override decimal GetValue(ExecutionState state)
    {
        if (Limit.IsTrue(state))
        {
            var value = ValueNode.GetValue(state);
            if (value != 99999999999999999999999.99999m)
                return value;
        }
        return Parent.NodeType switch
        {
            NodeType.ADD => 0.00m,
            NodeType.FACTOR => 1.00m,
            NodeType.RAISETO => 1.00m,
            NodeType.BASE => 0.00m,
            NodeType.IFSTATEMENT => 99999999999999999999999.99999m,
            _ => 99999999999999999999999.99999m
        };
    }

    public override bool IsTrue(ExecutionState state)
    {
        return Limit.IsTrue(state);
    }
}

public class SystemVar : SyntaxNode
{
    public string Value { get; set; }
    public SystemVar()
    {
        NodeType = NodeType.SYSTEMVAR;
    }

    public static string CleanUp(string value)
    {
        return value
            .Replace("[", ".").Replace("]", "");//.Replace("\"", "");
    }

    public override decimal GetValue(ExecutionState state)
    {
        var levels = CleanUp(Value).Split(".").ToList();
        decimal value = levels[0].ToLower() switch
        {
            "Nation" => levels[1].ToLower() switch
            {
                "population" => state.Nation.TotalPopulation
            },
            "province" => levels[1].ToLower() switch
            {
                "population" => state.Province.Population,
                "owner" => state.Province.Nation.Id,
                "buildings" => levels[2].ToLower() switch
                {
                    "totaloftype" => (decimal)state.Province.GetLevelsOfBuildingsOfType(levels[3])
                }
            },
            "research" => levels[1].ToLower() switch
            {
                "level" => state.Research.Level
            },
            "building" => levels[1].ToLower() switch
            {
                "recipe" => state.Building.Recipe.Id
            },
            "upgrade" => levels[1].ToLower() switch
            {
                "level" => state.BuildingUpgrade.Level
            },
            "edit" => levels[1].ToLower() switch
            {
                "level" => state.Recipe.EditsLevels[state.RecipeEdit.Id]
            },
            "recipes" => DBCache.Recipes.ContainsKey(levels[1]) ? DBCache.Recipes[levels[1]].Id : 0,
            "get_local" => state.Locals[levels[1]],
            _ => 0.00m
        };
        if (levels[0] == "recipes")
        {
            if (!DBCache.Recipes.ContainsKey(levels[1]))
                HandleError("Could not find recipe with id", levels[1]);
        }
        if (state.ChangeSystemVarsBy.Count > 0)
        {
            if (state.ChangeSystemVarsBy.ContainsKey(Value))
            {
                value += state.ChangeSystemVarsBy[Value];
            }
            return value;
        }
        else
        {
            return value;
        }
    }
}

public class ExpressionNode : SyntaxNode
{
    public List<SyntaxNode> Body { get; set; }

    public ExpressionNode()
    {
        Body = new();
        NodeType = NodeType.EXPRESSION;
    }

    public override decimal GetValue(ExecutionState state)
    {
        decimal result = 0.00m;

        foreach (var node in Body)
        {
            switch (node.NodeType)
            {
                case NodeType.BASE:
                    result = node.GetValue(state);
                    break;
                case NodeType.ADD:
                    result += node.GetValue(state);
                    break;
                case NodeType.SUBTRACT:
                    result -= node.GetValue(state);
                    break;
                case NodeType.FACTOR:
                    result *= node.GetValue(state);
                    break;
                case NodeType.RAISETO:
                    result = (decimal)Math.Pow((double)result, (double)node.GetValue(state));
                    break;
                case NodeType.DIVIDE:
                    result /= node.GetValue(state);
                    break;
                case NodeType.GETLOCAL:
                    result = node.GetValue(state);
                    break;
                case NodeType.SYSTEMVAR:
                    result = node.GetValue(state);
                    break;
                default:
                    break;
            }
        }

        return result;
    }
}

public class DictNode : SyntaxNode
{
    public Dictionary<string, SyntaxNode> Body { get; set; }

    public Dictionary<string, decimal> PermanentValues { get; set; }

    public DictNode()
    {
        Body = new();
        PermanentValues = new();
        NodeType = NodeType.DICTNODE;
    }

    public override decimal GetValue(ExecutionState state) { return 0.00m; }

    public Dictionary<string, decimal> Evaluate(ExecutionState state)
    {
        var data = new Dictionary<string, decimal>();
        foreach ((var key, var value) in PermanentValues)
            data[key] = value;
        foreach ((var key, var valuenode) in Body)
        {
            if (key == "add_locals")
            {
                foreach (var _node in ((ExpressionNode)valuenode).Body)
                {
                    var node = (AddLocalsNode)_node;
                    node.Execute(state);
                }
            }

            else
                data[key] = valuenode.GetValue(state);
        }
        return data;
    }
}

public class GetLocal : SyntaxNode
{
    public string Name { get; set; }
    public GetLocal()
    {
        NodeType = NodeType.GETLOCAL;
    }

    public override decimal GetValue(ExecutionState state)
    {
        return state.Locals[Name];
    }
}

public class AddLocalsNode : SyntaxNode
{
    public Dictionary<string, SyntaxNode> Body { get; set; }

    public AddLocalsNode()
    {
        Body = new();
        NodeType = NodeType.ADDLOCALSNODE;
    }

    public override decimal GetValue(ExecutionState state) { return 0.00m; }

    public void Execute(ExecutionState state)
    {
        foreach ((var key, var valuenode) in Body)
        {
            state.Locals[key] = valuenode.GetValue(state);
        }
    }
}

public class LocalNode : SyntaxNode
{
    public string Name { get; set; }
    public SyntaxNode Value { get; set; }
    public override decimal GetValue(ExecutionState state) { return 0.00m; }
}

public enum ScriptScopeType
{
    Nation,
    Province,
    Building,
    Research,
    Recipe
}

public class ChangeScopeNode : EffectNode
{
    public ScriptScopeType scopeType { get; set; }
    public string ChangeTo { get; set; }
    public SyntaxNode Value { get; set; }
    public EffectBody EffectBodyNode { get; set; }

    public ExecutionState GetExecutionState(ExecutionState state)
    {
        var newstate = new ExecutionState(state.Nation, state.Province, state.ChangeSystemVarsBy, null, state.Building, state.Research);
        newstate.Locals = state.Locals;

        if (scopeType == ScriptScopeType.Nation)
        {
            var _changeto = long.Parse(ChangeTo);
            var Nation = DBCache.GetAll<Nation>().FirstOrDefault(x => x.Id == _changeto);
            if (Nation is null)
                HandleError("Could not find Nation", $"key: {ChangeTo}");
            newstate.Nation = Nation;
            newstate.ParentScopeType = scopeType;
        }

        else if (scopeType == ScriptScopeType.Province)
        {
            var _changeto = long.Parse(ChangeTo);
            var province = DBCache.GetAll<Province>().FirstOrDefault(x => x.Id == _changeto);
            if (province is null)
                HandleError("Could not find province", $"key: {ChangeTo}");
            newstate.Province = province;
            newstate.ParentScopeType = scopeType;
        }

        else if (scopeType == ScriptScopeType.Building)
        {

        }
        return newstate;
    }

    public override decimal GetValue(ExecutionState state)
    {
        return Value.GetValue(GetExecutionState(state));
    }

    public override void Execute(ExecutionState state)
    {
        EffectBodyNode.Execute(GetExecutionState(state));
    }
}