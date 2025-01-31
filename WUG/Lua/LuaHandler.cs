﻿using System.Globalization;
using System.ComponentModel;
using WUG.Managers;
using Decimal = WUG.Scripting.Decimal;
using System.Xml.Linq;
using WUG.Database.Models.Buildings;
using System.Data;
using WUG.NonDBO;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using WUG.Scripting.LuaObjects;
using WUG.Scripting;
using System.Text.Json.Serialization;
using WUG.Database.Models.Nations;

namespace WUG.Scripting.Parser;

public static class StringExtensions
{
    public static string ToTitleCase(this string title)
    {
        title = title.Replace("_", " ");
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title.ToLower());
    }
}

public class LuaTable : LuaObject
{
    public List<LuaObject> Items { get; set; }
    public LuaTable()
    {
        Items = new();
        type = ObjType.LuaTable;
    }

    [JsonIgnore]
    public IEnumerable<string> Keys
    {
        get
        {
            return Items.Select(x => x.Name);
        }
    }

    [JsonIgnore]
    public IEnumerable<LuaObject> Values
    {
        get
        {
            return Items;
        }
    }
    public LuaObject this[string key]
    {
        get
        {
            return Items.FirstOrDefault(x => x.Name == key);
        }
    }

    public string GetValue(string key) {
        var obj = this[key];
        if (obj is null) return null;
        return obj.Value;
    }

    public bool ContainsKey(string key) {
        return Items.FirstOrDefault(x => x.Name == key) != null;
    }
}

public class Lua : IDisposable
{
    public LuaTable Objects { get; set; }
    public Lua()
    {
        Objects = new LuaTable();
        Objects.type = ObjType.LuaMainTable;
    }
    public void Dispose() { }
    public LuaObject this[string key]
    {
        get
        {
            return Objects[key];
        }
    }

    public void DoString(string Content, string filename = null)
    {
        LuaTable currentparent = Objects;
        int linenum = 0;
        foreach (var l in Content.Split("\n"))
        {
            linenum += 1;
            var line = l.Replace("\t", "").TrimStart();
            line = line.Replace("\r", "");
            //Console.WriteLine(line);
            //if (line.Contains("province.buildings.totaloftype[\"infrastructure\"]"))
            //    Console.WriteLine(line);
            if (line.Contains("=") && !line.StartsWith("--"))
            {
                var d = line.Split(" = ");
                var name = d[0];
                var rest = d[1];
                if (rest.Contains(" --"))
                {
                    var r = rest.Split(" --");
                    rest = r[0];
                }
                if (rest.Contains("\""))
                {
                    rest = rest.Replace("\"", "");
                    currentparent.Items.Add(new LuaObject() {
                        type = ObjType.String,
                        Value = rest,
                        Parent = currentparent,
                        Name = name,
                        FileName = filename,
                        LineNumber = linenum
                    });
                }
                else if (rest.StartsWith("{"))
                {
                    var obj = new LuaTable();
                    obj.Name = name;
                    obj.Parent = currentparent;
                    obj.FileName = filename;
                    obj.LineNumber = linenum;
                    currentparent.Items.Add(obj);
                    currentparent = obj;
                }
                else
                {
                    currentparent.Items.Add(new LuaObject()
                    {
                        type = ObjType.StringForNumber,
                        Value = rest,
                        Parent = currentparent,
                        Name = name,
                        FileName = filename,
                        LineNumber = linenum
                    });
                }
            }
            else if (line.Contains("}"))
            {
                if (currentparent.type != ObjType.LuaMainTable)
                    currentparent = currentparent.Parent;
            }
            else
            {
                if (line.StartsWith("--") || line.Length == 0)
                    continue;
                currentparent.Items.Add(new LuaObject() {
                    type = ObjType.String,
                    Value = line,
                    Parent = currentparent,
                    Name = $"{currentparent.Items.Count}",
                    FileName = filename,
                    LineNumber = linenum
                });
            }
        }
    }
}

public static class LuaHandler
{
    public static StreamWriter errorfile = new StreamWriter("Managers/ScriptErrors.txt");
    public static void HandleError(string filename, int linenumber, string error, string message)
    {
        var text = $"{filename.Split("/").Last()}:{linenumber} {error}: {message}";
        errorfile.WriteLine(text);
        errorfile.Flush();
        Console.WriteLine(text);
    }
    public static (string content, List<string> tables) PreProcessLua(string Lua)
    {
        List<string> TopLevelTables = new List<string>();
        foreach (var _line in Lua.Split("\n"))
        {
            string line = _line;
            if (!line.Contains(" = {") && !line.Contains(" = ") && line[0] == '\t' && !line.Contains("}"))
            {
                var replacewith = line.Replace("\t", "");
                line = line.Replace(replacewith, replacewith + " = 0");
                Lua = Lua.Replace(_line, line);
            }
            if (line[0] != '	' && line.Contains(" = {"))
            {
                Console.WriteLine(line);
                TopLevelTables.Add(line.Split(" = {")[0]);
            }
            if (!line.Contains("{") && !(line[0] == '}') && (line.Contains("=") || line.Contains("}")))
            {
                var splitted = line.Split(" --");
                if (splitted.Length == 1)
                {
                    Lua = Lua.Replace(line, line.Replace("\r", "") + ",\r");
                }
                else
                {
                    var first = splitted[0];
                    var second = splitted[1];
                    Lua = Lua.Replace(first, $"{first},");
                }
            }
        }
        return (Lua, TopLevelTables);
    }

    public static IEnumerable<(LuaTable, string)> HandleFile(string content, string filename)
    {
        //var data = PreProcessLua(content);
        //File.WriteAllText("../../../../Database/LuaDump.lua", data.content);
        LuaParser parser = new();
        parser.LoadTokenizer();
        parser.Parse(content, filename);
        foreach (var value in parser.Objects.Values)
        {
            var t = (LuaTable)value;
            yield return (t, value.Name);
        }
    }

    public static List<SyntaxModifierNode> HandleModifierNodes(LuaTable table)
    {
        if (table is null) return new();
        var nodes = new List<SyntaxModifierNode>();
        foreach (var item in table.Items)
        {
            string key = item.Name;
            var levels = key.Split(".").ToList();
            var node = new SyntaxModifierNode();
            if (levels[0] == "Nation")
            {
                node.nationModifierType = levels[0] switch
                {
                    "Nation" => levels[1] switch
                    {
                        "provinces" => levels[2] switch
                        {
                            "buildingslotsfactor" => NationModifierType.BuildingSlotsFactor,
                            "buildingslotsexponent" => NationModifierType.BuildingSlotsExponent,
                            "overpopulationmodifierexponent" => NationModifierType.OverPopulationModifierExponent,
                            "populationgrowthspeedfactor" => NationModifierType.PopulationGrowthSpeedFactor
                        }
                    }
                };
            }
            else if (levels[0] == "building")
            {
                node.buildingModifierType = levels[0] switch
                {
                    "building" => levels[1] switch
                    {
                        "throughputfactor" => BuildingModifierType.ThroughputFactor,
                        "efficiencyfactor" => BuildingModifierType.EfficiencyFactor
                    }
                };
            }
            else if (levels[0] == "entity")
            {
                node.entityModifierType = levels[0] switch
                {
                    "entity" => levels[1] switch
                    {
                        "factories" => levels[2] switch
                        {
                            "throughputfactor" => EntityModifierType.FactoryThroughputFactor,
                            "efficiencyfactor" => EntityModifierType.FactoryEfficiencyFactor,
                            "quantitycapfactor" => EntityModifierType.FactoryQuantityCapFactor
                        }
                    }
                };
            }
            else if (levels[0] == "item")
            {
                node.itemModifierType = levels[0] switch
                {
                    "item" => levels[1] switch
                    {
                        "attack" => ItemModifierType.Attack,
                        "attackfactor" => ItemModifierType.AttackFactor
                    }
                };
            }
            else
            {
                node.provinceModifierType = levels[0] switch
                {
                    "province" => levels[1] switch
                    {
                        "fertilelandfactor" => ProvinceModifierType.FertileLandFactor,
                        "farms" => levels[2] switch
                        {
                            "farmingthroughputfactor" => ProvinceModifierType.FarmThroughputFactor
                        },
                        "factories" => levels[2] switch
                        {
                            "throughputfactor" => ProvinceModifierType.FactoryThroughputFactor
                        },
                        "mines" => levels[2] switch
                        {
                            "throughputfactor" => ProvinceModifierType.MineThroughputFactor
                        },
                        "consumergoods" => levels[2] switch
                        {
                            "consumptionfactor" => ProvinceModifierType.ConsumerGoodsConsumptionFactor,
                            "modifierfactor" => ProvinceModifierType.ConsumerGoodsModifierFactor
                        },
                        "buildingslots" => ProvinceModifierType.BuildingSlots,
                        "buildingslotsfactor" => ProvinceModifierType.BuildingSlotsFactor,
                        "buildingslotsexponent" => ProvinceModifierType.BuildingSlotsExponent,
                        "migrationattractionfactor" => ProvinceModifierType.MigrationAttractionFactor,
                        "migrationattraction" => ProvinceModifierType.MigrationAttraction,
                        "overpopulationmodifierexponent" => ProvinceModifierType.OverPopulationModifierExponent,
                        "overpopulationmodifierpopulationbase" => ProvinceModifierType.OverPopulationModifierPopulationBase,
                        "developmentvalue" => ProvinceModifierType.DevelopmentValue
                    }
                };
            }

            item.Name = "base";
            var temptable = new LuaTable();
            temptable.Items.Add(item);

            node.Value = HandleSyntaxExpression(temptable).Body.First();

            nodes.Add(node);
        }

        //var body = HandleSyntaxExpression(table).Body;
        //int i = 0;
        // foreach (var node in nodes)
        // {
        //     node.Value = body[i];
        //    i++;
        //}

        return nodes;
    }

    public static DictNode HandleDictExpression(LuaTable table)
    {
        DictNode dict = new();
        foreach (var key in table.Keys)
        {
            var obj = table[key];
            if (obj.type == ObjType.LuaTable)
            {
                LuaTable _table = new();
                if (obj.Name == "add_locals")
                    _table.Items.Add(obj);
                else
                    _table = (LuaTable)obj;
                dict.Body[obj.Name] = HandleSyntaxExpression(_table);
            }
            else
                dict.PermanentValues[obj.Name] = Convert.ToDecimal(obj.Value);
        }
        return dict;
    }

    public static ExpressionNode HandleSyntaxExpression(LuaTable table, string parentname = null, SyntaxNode parent = null)
    {
        var expr = new ExpressionNode();
        expr.FileName = table.FileName;
        expr.LineNumber = table.LineNumber;
        foreach (var obj in table.Items)
        {
            Console.WriteLine($"Line {obj.LineNumber}: {obj.Name} ({obj.type}): {obj.Value}");
            SyntaxNode valuenode = null;
            ExpressionNode exprnode = null;
            if (obj.Name == "every_scope_building")
            {
                Console.WriteLine("hello!");
            }
            if (obj.type == ObjType.String)
                valuenode = new SystemVar() { Value = obj.Value };
            else if (obj.type == ObjType.LuaTable)
            {
                if (!(parentname == "effects" || parentname == "add_locals"))
;               {
                    var node = new ExpressionNode();
                    node.Body = HandleSyntaxExpression((LuaTable)obj).Body;
                    valuenode = node;
                    exprnode = new();
                    exprnode.Body = HandleSyntaxExpression((LuaTable)obj, obj.Name).Body;
                }
            }
            else
                valuenode = new Decimal() { Value = Convert.ToDecimal(obj.Value) };

            if (valuenode is not null)
            {
                valuenode.LineNumber = obj.LineNumber;
                valuenode.FileName = obj.FileName;
            }

            if (obj.Name == "base")
                expr.Body.Add(new Base() { Value = valuenode, LineNumber = obj.LineNumber });
            else if (obj.Name == "add")
                expr.Body.Add(new Add() { Value = valuenode, LineNumber = obj.LineNumber });
            else if (obj.Name == "factor")
                expr.Body.Add(new Factor() { Value = valuenode, LineNumber = obj.LineNumber });
            else if (obj.Name == "raiseto")
                expr.Body.Add(new RaiseTo() { Value = valuenode, LineNumber = obj.LineNumber });
            else if (obj.Name == "subtract")
                expr.Body.Add(new Subtract() { Value = valuenode, LineNumber = obj.LineNumber });
            else if (obj.Name == "divide")
                expr.Body.Add(new Divide() { Value = valuenode, LineNumber = obj.LineNumber });
            else if (obj.Name == "get_local")
                expr.Body.Add(new GetLocal() { Name = ((SystemVar)valuenode).Value, LineNumber = obj.LineNumber });
            else if (obj.Name == "effects")
                expr.Body.Add(new EffectBody() { Body = exprnode.Body.Select(x => (IEffectNode)x).ToList(), LineNumber = obj.LineNumber });
            else if (obj.Name == "limit")
                expr.Body.Add(new ConditionalStatement() { Conditionals = exprnode.Body.Select(x => (ConditionalSyntaxNode)x).ToList(), LineNumber = obj.LineNumber });
            else if (obj.Name == "AND")
                expr.Body.Add(new ConditionalLogicBlockStatement() {Type = ConditionalLogicBlockType.AND, Children = exprnode.Body.Select(x => (ConditionalSyntaxNode)x).ToList(), LineNumber = obj.LineNumber });
            else if (obj.Name == "NOT")
                expr.Body.Add(new ConditionalLogicBlockStatement() { Type = ConditionalLogicBlockType.NOT, Children = exprnode.Body.Select(x => (ConditionalSyntaxNode)x).ToList(), LineNumber = obj.LineNumber });
            else if (obj.Name == "hasstaticmodifier")
                expr.Body.Add(new HasStaticModifierStatement() { StaticModifierId = ((SystemVar)valuenode).Value, LineNumber = obj.LineNumber });
            else if (obj.Name.Contains(":"))
            {
                var spliced = obj.Name.Split(":");
                var node = new ChangeScopeNode()
                {
                    scopeType = Enum.Parse<ScriptScopeType>(spliced[0], true),
                    ChangeTo = spliced[1],
                    LineNumber = obj.LineNumber
                };

                if (node.scopeType == ScriptScopeType.Nation)
                {
                    node.ChangeTo = DBCache.GetAll<Nation>().FirstOrDefault(x => x.ScriptName == node.ChangeTo).Id.ToString();
                }

                if (parentname == "effects")
                {
                    exprnode = new();
                    exprnode.Body = HandleSyntaxExpression((LuaTable)obj, "effects").Body;
                    node.EffectBodyNode = new EffectBody() { Body = exprnode.Body.Select(x => (IEffectNode)x).ToList(), LineNumber = obj.LineNumber, FileName = obj.FileName };
                }
                else
                {
                    node.Value = valuenode;
                }
                expr.Body.Add(node);
            }
            else if (obj.Name == "add_locals")
            {
                var node = new AddLocalsNode();
                foreach (var item in exprnode.Body.Select(x => (LocalNode)x))
                    node.Body[item.Name] = item.Value;
                node.LineNumber = obj.LineNumber;
                expr.Body.Add(node);
            }
            else if (obj.Name == "if")
            {
                var iftable = (LuaTable)obj;
                var ifstatement = new IfStatement()
                {
                    Limit = (ConditionalStatement)exprnode.Body.FirstOrDefault(x => x.NodeType == NodeType.CONDITIONALSTATEMENT),
                    ValueNode = new(),
                    LineNumber = obj.LineNumber,
                    FileName = obj.FileName
                };

                if (iftable.Keys.Contains("effects"))
                    ifstatement.EffectNode = (EffectBody)exprnode.Body.FirstOrDefault(x => x.NodeType == NodeType.EFFECTBODY);

                foreach (var node in exprnode.Body)
                {
                    if (node.NodeType == NodeType.CONDITIONALSTATEMENT || node.NodeType == NodeType.EFFECTBODY)
                        continue;
                    ifstatement.ValueNode.Body.Add(node);
                }

                expr.Body.Add(ifstatement);
            }
            else if (parentname == "limit" || parentname == "AND" || parentname == "NOT")
            {
                var statement = new ConditionalStatementComparison()
                {
                    comparisonType = ComparisonType.EQUAL,
                    LeftSide = new SystemVar() { Value = obj.Name },
                    RightSide = new SystemVar() { Value = obj.Value }
                };
                expr.Body.Add(statement);
            }
            else if (parentname == "add_locals")
            {
                expr.Body.Add(new LocalNode()
                {
                    Name = obj.Name,
                    Value = HandleSyntaxExpression((LuaTable)obj),
                    LineNumber = obj.LineNumber,
                    FileName = obj.FileName
                });
            }
            else if (parentname == "effects")
            {

                if (obj.Name == "remove_static_modifier")
                {
                    var modifiernode = new RemoveStaticModifierNode()
                    {
                        ModifierName = obj.Value
                    };
                    expr.Body.Add(modifiernode);
                }

                else if (obj.Name == "every_scope_building")
                {
                    var modifiernode = new EveryScopeBuildingNode()
                    {
                        Body = HandleSyntaxExpression((LuaTable)obj, obj.Name).Body
                    };
                    expr.Body.Add(modifiernode);
                }

                else if (obj.Name == "add_static_modifier_if_not_already_added" || obj.Name == "add_static_modifier")
                {
                    var effectbody_table = (LuaTable)obj;
                    var addmodifiernode = new AddStaticModifierNode()
                    {
                        ModifierName = effectbody_table["name"].Value,
                        Decay = Convert.ToBoolean(effectbody_table.GetValue("decay") ?? "false"),
                        Duration = Convert.ToInt32(effectbody_table.GetValue("duration") ?? "0"),
                        LineNumber = obj.LineNumber,
                        FileName = obj.FileName
                    };
                    if (effectbody_table.Keys.Contains("scale_by"))
                        addmodifiernode.ScaleBy = HandleSyntaxExpression((LuaTable)effectbody_table["scale_by"]);
                    if (obj.Name == "add_static_modifier_if_not_already_added")
                    {
                        var _node = new AddStaticModifierIfNotAlreadyExistsNode()
                        {
                            AddStaticModifierNode = addmodifiernode,
                            LineNumber = obj.LineNumber,
                            FileName = obj.FileName
                        };
                        expr.Body.Add(_node);
                    }
                    else
                        expr.Body.Add(addmodifiernode);
                }
            }
        }
        foreach (var node in expr.Body)
        {
            node.FileName = table.FileName;
        }
        return expr;
    }

    public static void HandleProvinceDevelopmentStagesFile(string content, string filename)
    {
        foreach (var (table, key) in HandleFile(content, filename))
        {
            var stage = new ProvinceDevelopmentStage()
            {
                Id = key,
                Name = table["name"].Value,
                DevelopmentLevelNeeded = Convert.ToInt32(table["development_value_required"].Value),
                ModifierNodes = HandleModifierNodes((LuaTable)((LuaTable)table)["modifiers"])
            };
            GameDataManager.ProvinceDevelopmentStages[stage.Id] = stage;
        }
    }

    public static void HandleResourcesFile(string content, string filename)
    {
        foreach (var (__table, materialgroup) in HandleFile(content, filename))
        {
            GameDataManager.ResourcesByMaterialGroup[materialgroup] = new();
            var _table = (LuaTable)__table;
            foreach (var key in _table.Keys)
            {
                var table = (LuaTable)_table[key];
                var resource = new SVResource()
                {
                    Name = key.ToTitleCase(),
                    LowerCaseName = key,
                };
                if (table.GetValue("popgrowthratemodifier") is not null)
                {
                    resource.consumerGood = new()
                    {
                        PopGrowthRateModifier = Convert.ToDouble(table["popgrowthratemodifier"]),
                        EconomicScoreModifier = Convert.ToDouble(table["economicscore"]),
                        PopConsumptionRate = Convert.ToDouble(table["consumptionrate"])
                    };
                    GameDataManager.ConsumerGoods.Add(resource);
                }
                GameDataManager.ResourcesByMaterialGroup[materialgroup].Add(resource);
                GameDataManager.Resources[resource.Name] = resource;
                var itemdef = DBCache.GetAll<ItemDefinition>().FirstOrDefault(x => x.Name == resource.Name);
                if (itemdef is null)
                {
                    itemdef = new(100, resource.Name);
                    itemdef.Transferable = Convert.ToBoolean(table.GetValue("transferable") ?? "true");
                    DBCache.Put(itemdef.Id, itemdef);
                    DBCache.dbctx.Add(itemdef);
                }
                resource.ItemDefinition = itemdef;
                GameDataManager.ResourcesToItemDefinitions[key] = DBCache.GetAll<ItemDefinition>().First(x => x.Name == resource.Name);
            }
        }
    }

    public static void HandleRecipeFile(string content, string filename)
    {
        foreach (var (table, key) in HandleFile(content, filename))
        {
            var baserecipe = new BaseRecipe()
            {
                Id = key,
                Name = table["name"].Value,
                PerHour = Convert.ToDouble(table["perhour"].Value),
                Editable = Convert.ToBoolean(table.GetValue("editable") ?? "false"),
                Inputcost_Scaleperlevel = Convert.ToBoolean(table.GetValue("inputcost_scaleperlevel") ?? "true"),
                TypeOfBuilding = Enum.Parse<BuildingType>(table.GetValue("buildingtype") ?? "mine", true),
                AnyWithBaseTypes = new(),
                PowerOutput = Convert.ToDouble(table.GetValue("power_output" ?? "0.0")),
                PowerDemand = Convert.ToDouble(table.GetValue("power_demand") ?? "0.0")
            };

            var inputs = (LuaTable)table["inputs"];
            if (inputs is not null)
            {
                foreach (string input in inputs.Keys)
                {
                    if (input == "any_with_basetype")
                    {
                        var anywithTable = (LuaTable)inputs[input];
                        baserecipe.AnyWithBaseTypes.Add(new()
                        {
                            Id = anywithTable["id"].Value,
                            BaseType = anywithTable["basetype"].Value,
                            Required = Convert.ToBoolean(anywithTable["required"].Value),
                            Amount = Convert.ToDouble(anywithTable["amount"].Value)
                        });
                    }
                    else
                    {
                        var itemdef = DBCache.GetAll<ItemDefinition>().FirstOrDefault(x => x.OwnerId == 100 && x.Name.ToLower().Replace(" ", "_") == input);
                        baserecipe.Inputs[itemdef.Id] = Convert.ToDouble(inputs[input]);
                    }
                }
            }
            var outputs = (LuaTable)table["outputs"];
            foreach (string output in outputs.Keys)
            {
                if (output == "modifiers")
                {
                    baserecipe.ModifierNodes = HandleModifierNodes((LuaTable)outputs["modifiers"]);
                }
                else
                {
                    var itemdef = DBCache.GetAll<ItemDefinition>().FirstOrDefault(x => x.OwnerId == 100 && x.Name.ToLower().Replace(" ", "_") == output);
                    if (itemdef is null || baserecipe.Editable)
                    {
                        baserecipe.OutputWithCustomItem = new(output, Convert.ToDouble(outputs[output]));
                    }
                    else
                    {
                        baserecipe.Outputs[itemdef.Id] = Convert.ToDouble(outputs[output]);
                    }
                }
            }

            if (table.ContainsKey("edits"))
            {
                baserecipe.LuaRecipeEdits = new();
                var edits = (LuaTable)table["edits"];
                foreach (var id in edits.Keys)
                {
                    var edit = (LuaTable)edits[id];
                    baserecipe.LuaRecipeEdits[id] = new()
                    {
                        Id = id,
                        Name = edit["name"].Value,
                        ModifierNodes = HandleModifierNodes((LuaTable)edit["modifiers"]),
                        Costs = HandleDictExpression((LuaTable)edit["costs"])
                    };
                }
            }

            GameDataManager.BaseRecipeObjs[baserecipe.Id] = baserecipe;
            if (!baserecipe.Editable)
            {
                Recipe recipe = DBCache.Recipes.Values.FirstOrDefault(x => x.StringId == baserecipe.Id);
                if (recipe is null)
                {
                    Console.WriteLine(baserecipe.Id);
                    recipe = new Recipe()
                    {
                        Id = IdManagers.GeneralIdGenerator.Generate(),
                        StringId = baserecipe.Id,
                        Name = baserecipe.Name,
                        OwnerId = 100,
                        Outputs = new(),
                        Inputs = new(),
                        BaseRecipeId = baserecipe.Id,
                        EditsLevels = new(),
                        PerHour = baserecipe.PerHour,
                        EntityIdsThatCanUseThisRecipe = new(),
                        Obsolete = false,
                        AnyWithBaseTypesFilledIn = new(),
                        HasBeenUsed = true,
                        OutputItemName = DBCache.Get<ItemDefinition>(baserecipe.Outputs.FirstOrDefault().Key)?.Name ?? "",
                        Created = DateTime.UtcNow
                    };
                    recipe.UpdateInputs();
                    recipe.UpdateOutputs();
                    DBCache.AddNew(recipe.Id, recipe);
                    DBCache.Recipes[recipe.StringId] = recipe;
                }
                else
                {
                    recipe.PowerDemand = baserecipe.PowerDemand;
                    recipe.PowerOutput = baserecipe.PowerOutput;
                    recipe.PerHour = baserecipe.PerHour;
                }
            }
        }
    }

    public static void HandleBuildingFile(string content, string filename)
    {
        foreach (var (table, name) in HandleFile(content, filename))
        {
            var building = new LuaBuilding()
            {
                Name = name,
                Recipes = new(),
                Upgrades = new(),
                OnlyGovernorCanBuild = Convert.ToBoolean(table.GetValue("onlygovernorcanbuild") ?? "false"),
                UseBuildingSlots = Convert.ToBoolean(table.GetValue("usebuildingslots") ?? "true"),
                BuildingCosts = HandleDictExpression((LuaTable)table["buildingcosts"]),
                MustHaveResource = table.GetValue("musthaveresource"),
                ApplyStackingBonus = Convert.ToBoolean(table.GetValue("applystackingbonus") ?? "true")
            };

            var recipes = (LuaTable)table["recipes"];
            foreach (string recipe in recipes.Values.Select(x => x.Value))
                building.Recipes.Add(GameDataManager.BaseRecipeObjs[recipe]);
            var upgrades = (LuaTable)table["upgrades"];
            if (upgrades is not null)
            {
                foreach (string upgradeid in upgrades.Values.Select(x => x.Value))
                    building.Upgrades.Add(GameDataManager.BaseBuildingUpgradesObjs[upgradeid]);
            }

            if (table["base_efficiency"] is not null)
                building.BaseEfficiency = HandleSyntaxExpression((LuaTable)table["base_efficiency"]);

            building.type = Enum.Parse<BuildingType>(table["type"].Value);

            if (building.type == BuildingType.Mine)
            {
                building.Upgrades.Add(GameDataManager.BaseBuildingUpgradesObjs["mine_throughput_upgrade"]);
                building.Upgrades.Add(GameDataManager.BaseBuildingUpgradesObjs["mine_efficiency_upgrade"]);
            }

            GameDataManager.BaseBuildingObjs[building.Name] = building;
        }
    }

    public static void HandleBuildingUpgradeFile(string content, string filename)
    {
        foreach (var (table, name) in HandleFile(content, filename))
        {
            var upgrade = new LuaBuildingUpgrade()
            {
                Id = name,
                IdAsLong = Convert.ToInt32(table.GetValue("numid")),
                Name = table.GetValue("name"),
                Costs = HandleDictExpression((LuaTable)table["costs"]),
                ModifierNodes = HandleModifierNodes((LuaTable)table["modifiers"])
            };

            GameDataManager.BaseBuildingUpgradesObjs[upgrade.Id] = upgrade;
        }
    }

    public static void HandleOnActionFile(string content, string filename)
    {
        foreach (var (table, name) in HandleFile(content, filename))
        {
            var onaction = new LuaOnAction()
            {
                OnActionType = name switch
                {
                    "on_server_start" => OnActionType.OnServerStart
                },
                EffectBody = (EffectBody)(HandleSyntaxExpression(table).Body.First())
            };
            if (!GameDataManager.LuaOnActions.ContainsKey(onaction.OnActionType))
                GameDataManager.LuaOnActions[onaction.OnActionType] = new();
            GameDataManager.LuaOnActions[onaction.OnActionType].Add(onaction);
        }
    }

    public static void HandleStaticModifierFile(string content, string filename)
    {
        foreach (var (table, name) in HandleFile(content, filename))
        {
            var modifier = new LuaStaticModifier()
            {
                Name = table["name"].Value,
                Description = table.GetValue("description"),
                Stackable = Convert.ToBoolean(table["stackable"].Value),
                Icon = table.GetValue("icon") ?? "~",
                ModifierNodes = HandleModifierNodes((LuaTable)table["modifiers"] ?? null),
                IsGood = Convert.ToBoolean(table.GetValue("isgood"))
            };
            if (table.ContainsKey("effects"))
                modifier.EffectBody = (EffectBody)HandleSyntaxExpression((LuaTable)table["effects"], "effects");

            GameDataManager.BaseStaticModifiersObjs[name] = modifier;
        }
    }

    public static void HandlePolicyFile(string content, string filename)
    {
        foreach (var (table, name) in HandleFile(content, filename))
        {
            var policy = new LuaPolicy()
            {
                Id = name,
                Name = table["name"].Value,
                Type = Enum.Parse<LuaPolicyType>(table["type"].Value, true),
                DefaultOption = table["default"].Value,
                Options = new()
            };

            foreach (var item in ((LuaTable)table["options"]).Values)
            {
                var option_table = (LuaTable)item;
                var option = new LuaPolicyOption()
                {
                    Id = option_table.Name,
                    ModifierNodes = HandleModifierNodes((LuaTable)table["modifiers"])
                };
                policy.Options[option.Id] = option;
            }

            GameDataManager.LuaPolicyObjs[policy.Id] = policy;
        }
    }

    public static List<LuaResearchPrototype> GetLuaResearchPrototypes(LuaTable table)
    {
        var prototypes = new List<LuaResearchPrototype>();
        foreach(var item in table.Items)
        {
            var item_table = (LuaTable)item;
            var prototype = new LuaResearchPrototype()
            {
                Id = item.Name,
                Name = item_table["name"].Value,
                Color = item_table["color"].Value,
                WhoCanResearch = Enum.Parse<WhoCanResearch>(item_table["who_can_research"].Value, ignoreCase: true),
                ModifierNodes = HandleModifierNodes((LuaTable)item_table["modifiers"]),
                Costs = HandleDictExpression((LuaTable)item_table["costs"]),
                IsInfinite = Convert.ToBoolean(item_table.GetValue("isinfinite") ?? "true")
            };

            prototypes.Add(prototype);
            GameDataManager.BaseResearchPrototypes[prototype.Id] = prototype;

            if (item_table.Items.Any(x => x.Name == "children"))
                prototype.Children = GetLuaResearchPrototypes((LuaTable)item_table["children"]);
            else
                prototype.Children = new();
        }

        return prototypes;
    }

    public static void HandleResearchFile(string content, string filename)
    {
        foreach (var (table, name) in HandleFile(content, filename))
        {
            // first handle the category
            var category = new LuaResearchCategory()
            {
                Name = table["name"].Value,
                Id = table.Name,
                Children = new()
            };

            GameDataManager.ResearchCategories[category.Id] = category;

            // now handle the children
            category.Children = GetLuaResearchPrototypes((LuaTable)table["researches"]);
            foreach (var child in category.Children)
                child.CategoryId = category.Id;
        }
    }
}