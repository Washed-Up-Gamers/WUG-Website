recipe_iron_smeltery_base = {
	name = "Iron Smelting"
	inputs = {
		iron_ore = 1
		tools = 0.005
	}
	outputs = {
		iron = 1
	}
	perhour = 100

	-- this will consume 2 MW at size 1 with 1.00x throughput
	power_demand = 2
    editable = false
}

recipe_copper_smeltery_base = {
	name = "Copper Smelting"
	inputs = {
		copper_ore = 1
		tools = 0.005
	}
	outputs = {
		copper = 1
	}
	perhour = 100
	power_demand = 2
    editable = false
}

recipe_lead_smeltery_base = {
	name = "Lead Smelting"
	inputs = {
		lead_ore = 1
		tools = 0.005
	}
	outputs = {
		lead = 1
	}
	perhour = 50
	power_demand = 2
    editable = false
}

recipe_zinc_smeltery_base = {
	name = "Zinc Smelting"
	inputs = {
		zinc_ore = 1
		tools = 0.005
	}
	outputs = {
		zinc = 1
	}
	perhour = 50
	power_demand = 2
    editable = false
}

recipe_bauxite_smeltery_base = {
	name = "Bauxite Smelting"
	inputs = {
		bauxite = 1
		tools = 0.0075
	}
	outputs = {
		aluminium = 0.75
	}
	perhour = 50
	power_demand = 3
    editable = false
}

recipe_brass_factory_base = {
	name = "Brass Production"
	inputs = {
		copper = 2
		zinc = 1
	}
	outputs = {
		brass = 3
	}
	perhour = 7.5
	power_demand = 2
    editable = false
}

recipe_steel_factory_base = {
	name = "Steel Production"
	inputs = {
		coal = 2
		iron = 4
	}
	outputs = {
		steel = 2.5
	}

	-- was 16
	perhour = 28
	power_demand = 3
    editable = false
}

recipe_tool_factory_base = {
	name = "Tool Production"
	inputs = {
		computer_chips = 0.2
		simple_components = 0.2
		steel = 2
	}
	outputs = {
		tools = 3
	}
	perhour = 8
	power_demand = 1
    editable = false
}

recipe_silicon_factory_base = {
	name = "Silicon Production"
	inputs = {
		sand = 1
	}
	outputs = {
		silicon = 1
	}
	perhour = 50
	power_demand = 0.75
    editable = false
}


recipe_plastic_factory_base = {
	name = "Plastic Production"
	inputs = {
		oil = 1
	}
	outputs = {
		plastic = 7.5
	}
	perhour = 50
	power_demand = 0.75
    editable = false
}

recipe_simple_components_factory_base = {
	name = "Simple Component Production"
	inputs = {
		iron = 1
		silicon = 1
		copper = 1
		plastic = 1
	}
	outputs = {
		simple_components = 1.75
	}

	-- was 16
	perhour = 34
	power_demand = 2
    editable = false
}

recipe_advanced_components_factory_base = {
	name = "Advanced Component Production"
	inputs = {
		simple_components = 3
		steel = 4
		computer_chips = 1
	}
	outputs = {
		-- was 1
		advanced_components = 2
	}
	-- was 4 then 7
	perhour = 14
	power_demand = 7.5
    editable = false
}

recipe_solar_pv_cell_factory_base = {
	name = "Solar PV Cell Production"
	inputs = {
		cadmium = 1
		silicon = 10
		computer_chips = 1
	}
	outputs = {
		solar_pv_cells = 20
	}
	perhour = 7.5
	power_demand = 7.5
    editable = false
}

recipe_battery_factory_base = {
	name = "Battery Production"
	inputs = {
		cadmium = 1
		lithium = 5
	}
	outputs = {
		batteries = 3
	}
	perhour = 20
	power_demand = 7.5
    editable = false
}

recipe_concrete_base = {
	name = "Concrete Production"
	inputs = {
		sand = 1
	}
	outputs = {
		concrete = 1
	}
	perhour = 500
	power_demand = 7.5
	editable = false
}

recipe_computer_chips_factory_base = {
	name = "Computer Chip Production"
	inputs = {
		silicon = 2
		copper = 2.5
		gold = 0.25
	}
	outputs = {
		computer_chips = 1.5
	}
	-- was 1.5
	perhour = 15
	power_demand = 7.5
    editable = false
}

recipe_televisions_factory_base = {
	name = "Television Production"
	inputs = {
		computer_chips = 8
		steel = 2
		plastic = 20
	}
	outputs = {
		televisions = 1
	}
	perhour = 12
	power_demand = 15
    editable = false
}

recipe_cars_factory_base = {
	name = "Car Production"
	inputs = {
		computer_chips = 12
		steel = 8
		plastic = 40
		aluminium = 60
	}
	outputs = {
		cars = 1
	}
	perhour = 3
	power_demand = 15
    editable = false
}

recipe_normal_ammo_factory_base = {
	name = "Normal Ammo Production"
	inputs = {
		lead = 1
		steel = 0.25
		copper = 0.25
	}
	outputs = {
		normal_ammo = 1
	}
	perhour = 50
	editable = false
}

recipe_pothium_infused_ammo_factory_base = {
	name = "Pothium Infused Ammo Production"
	inputs = {
		lead = 0.75
		steel = 0.25
		copper = 0.25
		pothium = 0.25
	}
	outputs = {
		pothium_infused_ammo = 1
	}
	perhour = 50
	editable = false
}

recipe_105mm_artillery_shell_factory_base = {
	name = "105mm Artillery Shell Production"
	inputs = {
		brass = 10
		lead = 5
		steel = 5
	}
	outputs = {
		artillery_shell_105mm = 1
	}
	perhour = 15
	editable = false
}

recipe_155mm_artillery_shell_factory_base = {
	name = "155mm Artillery Shell Production"
	inputs = {
		brass = 20
		lead = 10
		steel = 10
	}
	outputs = {
		artillery_shell_155mm = 1
	}
	perhour = 10
	editable = false
}

recipe_105mm_artillery_factory_base = {
	name = "105mm Artillery Production"
	inputs = {
		steel = 1
	}
	outputs = {
		artillery_105mm = 1
	}
	perhour = 1
	editable = true
	buildingtype = "factory"
	edits = {
		attack = {
			name = "Attack"
			modifiers = {
				item.attack = {
					base = 1
					add = {
						base = 0.25
						factor = edit.level
					}
				}
			}
			costs = {
				steel = {
					base = 1
					factor = edit.level
					factor = {
						base = edit.level
						factor = 0.25
						add = 1
					}
				}
			}
		}
	}
}

recipe_rifle_factory_base = {
	name = "Rifle Production"
	inputs = {
		steel = 0
	}
	outputs = {
		rifle = 1
	}
	perhour = 1
	editable = true
	buildingtype = "factory"
	edits = {
		attack = {
			name = "Attack"

			-- these are NOT scaled to the edit's level
			modifiers = {
				item.attack = {
					base = 1
					add = {
						base = 0.25
						factor = edit.level
					}
				}
			}

			-- these are NOT scaled to the edit's level
			costs = {
				steel = {
					base = 2
					factor = 1.175 ^ edit.level
				}
			}
		}
	}
}