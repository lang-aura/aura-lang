package stdlib_lists

import (
	"math"
	"reflect"
)

// Contains returns a bool indicating if `a` contains `item`
func Contains[T any](a []T, item T) bool {
	for _, t := range a {
		if reflect.DeepEqual(t, item) {
			return true
		}
	}
	return false
}

// IsEmpty returns a bool indicating if `a` is empty. `a` is considered empty if and only if it contains zero items.
func IsEmpty[T any](a []T) bool {
	return len(a) == 0
}

// Length returns the length of `a`
func Length[T any](a []T) int {
	return len(a)
}

// Map applies the supplied `b` function to every item of `a`, returning a new array containing an array of items that
// were output by `b`
func Map_[T, U any](a []T, b func(t T) U) []U {
	u := make([]U, 0)

	for _, elem := range a {
		u = append(u, b(elem))
	}

	return u
}

// Filter returns a list containing every element of `a` whose  output when passed into `f` is true
func Filter[T any](a []T, f func(t T) bool) []T {
	filtered := make([]T, 0)

	for _, elem := range a {
		if f(elem) {
			filtered = append(filtered, elem)
		}
	}

	return filtered
}

// Reduce applies the supplied function to each item in `a`, returning a single value with the same type as the type of
// the elements in `a`. The supplied function should accept two parameters -- the first parameter is the result of applying
// the function to the previous element in `a`, and the second parameter is the current item in `a`. On the first item in `a`,
// the first parameter is populated by the `t` parameter.
func Reduce[T any](a []T, f func(t1 T, t2 T) T, t T) T {
	var reduced T

	for i, elem := range a {
		if i == 0 {
			reduced = f(t, elem)
		} else {
			reduced = f(reduced, elem)
		}
	}

	return reduced
}

// Min returns the minimum value from a list of integers
func Min(a []int) int {
	return Reduce[int](a, func(t1 int, t2 int) int {
		if t2 < t1 {
			return t2
		}
		return t1
	}, math.MaxInt)
}

// Max returns the maximum value from a list of integers
func Max(a []int) int {
	return Reduce[int](a, func(t1 int, t2 int) int {
		if t2 > t1 {
			return t2
		}
		return t1
	}, math.MinInt)
}

// Push appends the supplied `t` parameter to the end of `a`
func Push[T any](a *[]T, t T) {
	*a = append(*a, t)
}

// Pop removes and returns the last element of `a`
func Pop[T any](a *[]T) T {
	t := (*a)[len(*a)-1]
	*a = (*a)[:len(*a)-1]
	return t
}

// Sum returns the sum of all elements in `a`
func Sum(a []int) int {
	return Reduce[int](a, func(t1 int, t2 int) int {
		return t1 + t2
	}, 0)
}
