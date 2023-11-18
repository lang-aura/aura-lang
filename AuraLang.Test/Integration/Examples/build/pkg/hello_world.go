package main

import io "Examples/stdlib/io"

// A classic "Hello World" program written in Aura
// Modules can be imported with the `import` statement followed by the module's import path. In this case, the `io` package
// is being imported from Aura's standard library, as indicated by the `aura/` prefix.
// Every Aura program must contain a `main` function, which is where the execution will begin
func main() {
	// Print `Hello world!` to the console followed by a newline
	io.Println("Hello world!")
}
