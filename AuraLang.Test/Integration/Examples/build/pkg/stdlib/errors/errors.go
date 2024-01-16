package stdlib_errors

// Message returns the error's message
func Message(err error) string {
	return err.Error()
}
