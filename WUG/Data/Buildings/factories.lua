building_simple_factory = {
	recipes = [
		recipe_iron_smeltery_base
		recipe_copper_smeltery_base
		recipe_bauxite_smeltery_base
		recipe_steel_factory_base
		recipe_tool_factory_base
		recipe_simple_components_factory_base
		recipe_plastic_factory_base
		recipe_concrete_base
	]
	buildingcosts = {
		steel = 5000
		concrete = 10000
		simple_components = 7500
		advanced_components = 1000
	}
	upgrades = [
		simple_factory_throughput_upgrade
		simple_factory_efficiency_upgrade
	]
	type = "Factory"
}

building_advanced_factory = {
	recipes = [
		recipe_advanced_components_factory_base
		recipe_computer_chips_factory_base
		recipe_cars_factory_base
		recipe_televisions_factory_base
	]
	buildingcosts = {
		steel = 20000
		concrete = 30000
		simple_components = 20000
		advanced_components = 5000
	}
	upgrades = [
		advanced_factory_throughput_upgrade
		advanced_factory_efficiency_upgrade
	]
	type = "Factory"
}

building_pothium_factory = {
	recipes = [
		recipe_pothium_components_factory_base
	]
	buildingcosts = {
		pothium_components = 5000
		advanced_components = 2500
		simple_components = 15000
		concrete = 50000
		steel = 30000
	}
	upgrades = [
		pothium_factory_throughput_upgrade
		pothium_factory_efficiency_upgrade
	]
	type = "Factory"
}