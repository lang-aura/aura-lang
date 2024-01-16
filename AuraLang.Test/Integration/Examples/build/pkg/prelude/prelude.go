package prelude

import "errors"

// Error returns an Aura error containing the supplied message
func Err(message string) error {
	return errors.New(message)
}
