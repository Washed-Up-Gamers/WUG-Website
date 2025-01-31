building_infrastructure = {
	recipes = [
		recipe_infrastructure_roads
	]
	buildingcosts = {
		add_locals = {
			cost_increase = {
				base = province.buildings.totaloftype["infrastructure"]
				factor = 0.175
				add = 1
			}
		}
		steel = {
			base = 750 * get_local("cost_increase")
		}
		simple_components = {
			base = 175 * get_local("cost_increase")
		}
		advanced_components = {
			base = 30 * get_local("cost_increase")
		}
	}

	base_efficiency = {
		base = 1
		divide = {
			base = province.buildings.totaloftype["infrastructure"]
			factor = 0.07
			factor = province.buildings.totaloftype["infrastructure"]
			add = 1
		}
	}

	-- only the governor of this province (or Nation if governor is null) can build this building
	onlygovernorcanbuild = true
	usebuildingslots = false
	type = "Infrastructure"
	applystackingbonus = false
}