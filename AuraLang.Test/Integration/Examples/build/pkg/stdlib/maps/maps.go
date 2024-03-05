package stdlib_maps

// Add adds the supplied key and value pair to the supplied map
func Add[TK comparable, TV any](m map[TK]TV, key TK, value TV) {
	m[key] = value
}

// Remove deletes the supplied key from the supplied map
func Remove[TK comparable, TV any](m map[TK]TV, key TK) {
	delete(m, key)
}

// Contains determines if the supplied key exists in the supplied map
func Contains[TK comparable, TV any](m map[TK]TV, key TK) bool {
	_, ok := m[key]
	return ok
}
