recipe_oil_power_plant_base = {
	name = "Oil Power"
	inputs = {
		oil = 1.25
	}
	outputs = {
	}
	-- in MW then is multiplied by per hour 
	power_output = 5
	perhour = 8
    editable = false
}

recipe_coal_power_plant_base = {
	name = "Coal Power"
	inputs = {
		coal = 1.75
	}
	outputs = {
	}
	power_output = 1
	perhour = 24
    editable = false
}

recipe_natural_gas_power_plant_base = {
	name = "Natural Gas Power"
	inputs = {
		natural_gas = 1.25
	}
	outputs = {
	}
	power_output = 5.5
	perhour = 10
    editable = false
}