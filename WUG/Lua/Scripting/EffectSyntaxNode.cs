﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WUG.Scripting;

namespace WUG.Scripting;

public enum EffectType
{
	None,
	AddStaticModifier,
	AddMoney,
	AddStaticModifierIfNotAlreadyAdded,
	EveryScopeBuilding,
	RemoveStaticModifier
}

public abstract class EffectSyntaxNode : SyntaxNode
{
	public NodeType NodeType => NodeType.EFFECT;
	public EffectType effectType { get; set; }
	public override decimal GetValue(ExecutionState state) => 0.00m;
	public abstract void Execute(ExecutionState state);
}

public interface IEffectNode
{
	public EffectType effectType { get; }
	public abstract void Execute(ExecutionState state);
}

[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
[JsonDerivedType(typeof(AddMoneyNode), typeDiscriminator: 0)]
[JsonDerivedType(typeof(RemoveStaticModifierNode), typeDiscriminator: 1)]
[JsonDerivedType(typeof(AddStaticModifierIfNotAlreadyExistsNode), typeDiscriminator: 2)]
[JsonDerivedType(typeof(AddStaticModifierNode), typeDiscriminator: 3)]
[JsonDerivedType(typeof(EveryScopeBuildingNode), typeDiscriminator: 4)]
[JsonDerivedType(typeof(ChangeScopeNode), typeDiscriminator: 5)]
public abstract class EffectNode : EffectSyntaxNode, IEffectNode
{
	public override void Execute(ExecutionState state)
	{
		throw new NotImplementedException();
	}
}

public class AddMoneyNode : EffectNode
{
	public EffectType effectType => EffectType.AddMoney;
	public SyntaxNode Amount { get; set; }

	public override void Execute(ExecutionState state)
	{
		var tran = new Transaction(BaseEntity.Find(100), BaseEntity.Find(state.Nation.GroupId), Amount.GetValue(state), TransactionType.FreeMoney, "From Effect Node");
		tran.NonAsyncExecute();
		Console.WriteLine("Executed AddMoneyNode!");
	}
}

public class RemoveStaticModifierNode : EffectNode
{
    public EffectType effectType => EffectType.RemoveStaticModifier;
    public string ModifierName { get; set; }

    public override void Execute(ExecutionState state)
    {
		if (state.ParentScopeType == ScriptScopeType.Nation)
		{
			var modifier = state.Nation.StaticModifiers.FirstOrDefault(x => x.LuaStaticModifierObjId == ModifierName);
			if (modifier is not null)
				state.Nation.StaticModifiers.Remove(modifier);
		}
		else if (state.ParentScopeType == ScriptScopeType.Province)
		{
            var modifier = state.Province.StaticModifiers.FirstOrDefault(x => x.LuaStaticModifierObjId == ModifierName);
            if (modifier is not null)
                state.Province.StaticModifiers.Remove(modifier);
        }
		else if (state.ParentScopeType == ScriptScopeType.Building)
		{
            var modifier = state.Building.StaticModifiers.FirstOrDefault(x => x.LuaStaticModifierObjId == ModifierName);
            if (modifier is not null)
                state.Building.StaticModifiers.Remove(modifier);
        }
    }
}

public class AddStaticModifierNode : EffectNode
{
	public EffectType effectType => EffectType.AddStaticModifier;
	public string ModifierName { get; set; }
	public bool Decay { get; set; }
	public int? Duration { get; set; }
	public ExpressionNode? ScaleBy { get; set; }

	public override void Execute(ExecutionState state)
	{
		var dbmodifier = new StaticModifier()
		{
			Decay = Decay,
			Duration = Duration,
			StartDate = DateTime.UtcNow,
			LuaStaticModifierObjId = ModifierName
		};
		if (ScaleBy is not null)
			dbmodifier.ScaleBy = ScaleBy.GetValue(state);
		else
			dbmodifier.ScaleBy = 1.0m;
		if (state.ParentScopeType == ScriptScopeType.Nation)
			state.Nation.StaticModifiers.Add(dbmodifier);
        else if (state.ParentScopeType == ScriptScopeType.Province)
            state.Province.StaticModifiers.Add(dbmodifier);
        else if (state.ParentScopeType == ScriptScopeType.Building)
            state.Building.StaticModifiers.Add(dbmodifier);
    }
}

public class AddStaticModifierIfNotAlreadyExistsNode : EffectNode {
	public AddStaticModifierNode AddStaticModifierNode { get; set; }
    public override void Execute(ExecutionState state) {
		if (state.ParentScopeType == ScriptScopeType.Nation) {
			if (state.Nation.StaticModifiers.Any(x => x.LuaStaticModifierObjId == AddStaticModifierNode.ModifierName))
				return;
		}
		else if (state.ParentScopeType == ScriptScopeType.Province) {
            if (state.Province.StaticModifiers.Any(x => x.LuaStaticModifierObjId == AddStaticModifierNode.ModifierName))
                return;
        }
        else if (state.ParentScopeType == ScriptScopeType.Building)
        {
            if (state.Building.StaticModifiers.Any(x => x.LuaStaticModifierObjId == AddStaticModifierNode.ModifierName))
                return;
        }
        AddStaticModifierNode.Execute(state);
		Console.WriteLine("hhh");
    }
}

public class EveryScopeBuildingNode : EffectNode
{
	public EffectType effectType => EffectType.EveryScopeBuilding;
	public List<SyntaxNode> Body = new();
	public override void Execute(ExecutionState state)
	{
        if (state.ParentScopeType == ScriptScopeType.Nation)
        {
            ExecutionState _state = new(state.Nation, null, null, ScriptScopeType.Building, null, null);
			_state.Locals = state.Locals;
            foreach (var province in state.Nation.Provinces)
			{
				foreach (var building in DBCache.ProvincesBuildings[province.Id]) {
					_state.Building = building;
					_state.Province = province;
					foreach (var node in Body)
					{
						if (node.NodeType == NodeType.EFFECTBODY)
						{
							var _node = (EffectBody)node;
							_node.Execute(_state);
						}
						else if (node.NodeType == NodeType.IFSTATEMENT)
						{
							var _node = (IfStatement)node;
							_node.Execute(_state);
						}
					}
				}
			}
        }
        else if (state.ParentScopeType == ScriptScopeType.Province)
        {
            foreach (var building in DBCache.ProvincesBuildings[state.Province.Id])
            {
                ExecutionState _state = new(state.Nation, state.Province, null, ScriptScopeType.Building, building, null);
                foreach (var node in Body)
                {
                    if (node.NodeType == NodeType.EFFECTBODY)
                    {
                        var _node = (EffectBody)node;
                        _node.Execute(_state);
                    }
                    else if (node.NodeType == NodeType.IFSTATEMENT)
                    {
                        var _node = (IfStatement)node;
                        _node.Execute(_state);
                    }
                }
            }
        }
    }
}

public class EffectBody : SyntaxNode, IEffectNode
{
	public List<IEffectNode> Body;
	public EffectBody()
	{
		NodeType = NodeType.EFFECTBODY;
		Body = new();
	}

	public EffectType effectType => EffectType.None;

    public void Execute(ExecutionState state)
	{
		foreach(var effectnode in Body)
			effectnode.Execute(state);
	}

	public override decimal GetValue(ExecutionState state)
	{
		throw new NotImplementedException();
	}

	public static explicit operator EffectBody(ExpressionNode v)
	{
		return new EffectBody()
		{
			Body = v.Body.Select(x => (IEffectNode)x).ToList(),
			NodeType = NodeType.EFFECTBODY
		};
	}
}