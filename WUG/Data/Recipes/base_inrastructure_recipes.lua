recipe_infrastructure_roads = {
	name = "Road Infrastructure"
	inputs = {
        tools = 0.02
        simple_components = 0.05
        advanced_components = 0.01
        steel = 0.1
    }
    outputs = {
		modifiers = {
			-- was 0.25
			province.buildingslots = 0.5
			province.buildingslotsfactor = 0.005
			province.overpopulationmodifierpopulationbase = -30000
			province.buildingslotsexponent = 0.0006
			province.migrationattraction = 0.25
			province.developmentvalue = 0.2
			province.factories.throughputfactor = 0.003
			province.mines.throughputfactor = 0.003
		}
	}
	inputcost_scaleperlevel = false
	perhour = 1
    editable = false
}