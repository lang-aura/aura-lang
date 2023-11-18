package main

import io "Examples/stdlib/io"

func main() {
	// `if` expressions (and blocks) in Aura are capable of returning a value with the `yield` keyword. `yield` works similarly to `return`, except `yield`
	// returns a value from `if` expressions and blocks instead of returning from the enclosing function. In this case, the variable `i` will contain the
	// value 0.
	var i int
	if true {
		i = 0
	} else {
		i = 1
	}
	// You can return from the enclosing function inside an `if` expression or block as you normally would, by using the `return` keyword.
	if false {
		return
	}
	io.Printf("%d\n", i) // 0
}
