﻿simple_factory_throughput_upgrade = {
    name = "Increase Throughput"
    numid = 0
    costs = {
        add_locals = {
			cost_increase = 1.25^upgrade.level
		}
		steel = 1000 * get_local("cost_increase")
        concrete = 2000 * get_local("cost_increase")
		simple_components = 1000 * get_local("cost_increase")
		advanced_components = 150 * get_local("cost_increase")
    }
    modifiers = {
        building.throughputfactor = 0.2
		building.efficiencyfactor = -0.03
    }
}

simple_factory_efficiency_upgrade = {
    name = "Increase Efficiency"
    numid = 1
    costs = {
        add_locals = {
			cost_increase = 1.3^upgrade.level
		}
		simple_components = 2000 * get_local("cost_increase")
		advanced_components = 300 * get_local("cost_increase")
    }
    modifiers = {
        building.efficiencyfactor = 0.075
    }
}

advanced_factory_throughput_upgrade = {
    name = "Increase Throughput"
    numid = 2
    costs = {
        add_locals = {
			cost_increase = 1.25^upgrade.level
		}
		steel = 5000 * get_local("cost_increase")
        concrete = 10000 * get_local("cost_increase")
		simple_components = 5000 * get_local("cost_increase")
		advanced_components = 800 * get_local("cost_increase")
    }
    modifiers = {
        building.throughputfactor = 0.25
		building.efficiencyfactor = -0.04
    }
}

advanced_factory_efficiency_upgrade = {
    name = "Increase Efficiency"
    numid = 3
    costs = {
        add_locals = {
			cost_increase = 1.35^upgrade.level
		}
		simple_components = 8000 * get_local("cost_increase")
		advanced_components = 1500 * get_local("cost_increase")
    }
    modifiers = {
        building.efficiencyfactor = 0.08
    }
}

pothium_factory_throughput_upgrade = {
    name = "Increase Throughput"
    costs = {
        add_locals = {
            cost_increase = 1.25^upgrade.level
        }
        steel = 9000 * get_local("cost_increase")
        simple_components = 5000 * get_local("cost_increase")
        advanced_components = 800 * get_local("cost_increase")
    }
    modifiers = {
        building.throughputfactor = 0.25
        building.efficiencyfactor = -0.04
    }
}

pothium_factory_efficiency_upgrade = {
    name = "Increase Efficiency"
    costs = {
        add_locals = {
            cost_increase = 1.3^upgrade.level
        }
        simple_components = 8000 * get_local("cost_increase")
        advanced_components = 1500 * get_local("cost_increase")
    }
    modifiers = {
        building.efficiencyfactor = 0.08
    }
}