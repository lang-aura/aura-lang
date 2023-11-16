package stdlib_strings

import (
	"strconv"
	"strings"
)

// ToLower converts each character in a string to lower-case
func ToLower(s string) string {
	return strings.ToLower(s)
}

// ToUpper converts each character in a string to upper-case
func ToUpper(s string) string {
	return strings.ToUpper(s)
}

// Contains returns a bool indicating if `s` contains the `sub` string
func Contains(s, sub string) bool {
	return strings.Contains(s, sub)
}

// Length returns the number of characters contained in `s`
func Length(s string) int {
	return len(s)
}

// Split separates `s` into all of the substrings separated by (and not including) `sep`
func Split(s, sep string) []string {
	return strings.Split(s, sep)
}

// ToInt converts the provided string to an integer
func ToInt(s string) int {
	i, _ := strconv.ParseInt(s, 10, 32)
	return int(i)
}
