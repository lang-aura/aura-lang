package stdlib_io

import (
	"bufio"
	"fmt"
	"os"
	"strings"
)

// Printf formats the supplied `format` string, replacing a placeholder value with `a`, then prints the resulting string to stdout
func Printf(format string, a ...any) {
	fmt.Printf(format, a...)
}

// Println prints the supplied string to stdout followed by a newline
func Println(s string) {
	fmt.Println(s)
}

// Print prints the supplied string to stdout
func Print(s string) {
	fmt.Print(s)
}

// Eprintln prints the supplied string to stderr followed by a newline
func Eprintln(s string) {
	fmt.Fprintln(os.Stderr, s)
}

// Eprint prints the supplied string to stderr
func Eprint(s string) {
	fmt.Fprint(os.Stderr, s)
}

// Readln reads a line from stdin and returns it without the ending newline
func Readln() string {
	s := bufio.NewScanner(os.Stdin)
	if s.Scan() {
		return s.Text()
	}
	return ""
}

// ReadFile reads the entire contents of the file located at `path`
func ReadFile(path string) string {
	b, _ := os.ReadFile(path)
	return string(b)
}

// ReadLines reads the entire contents of the file located at `path` and returns a list of strings,
// with each string representing one line of the file
func ReadLines(path string) []string {
	contents := ReadFile(path)
	return strings.Split(contents, "\n")
}

// WriteFile writes `content` to the file located at `path`. If the file already exists, it is truncated
// before the write occurs.
func WriteFile(path, content string) {
	os.WriteFile(path, []byte(content), 0755)
}

// isAbsolutePath returns a bool indicating if the supplied `path` is an absolute path (i.e. from the root
// of the file system)
func isAbsolutePath(path string) bool {
	return path[0] == '/'
}
