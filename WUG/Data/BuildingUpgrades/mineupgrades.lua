mine_throughput_upgrade = {
    name = "Increase Throughput"
    costs = {
        add_locals = {
			cost_increase = 1.25^upgrade.level
		}
		steel = 500 * get_local("cost_increase")
		simple_components = 200 * get_local("cost_increase")
		advanced_components = 50 * get_local("cost_increase")
    }
    modifiers = {
        building.throughputfactor = 0.175
		building.efficiencyfactor = -0.05
    }
}

mine_efficiency_upgrade = {
    name = "Increase Efficiency"
    costs = {
        add_locals = {
			cost_increase = 1.35^upgrade.level
		}
		simple_components = 500 * get_local("cost_increase")
		advanced_components = 50 * get_local("cost_increase")
    }
    modifiers = {
        building.efficiencyfactor = 0.075
    }
}