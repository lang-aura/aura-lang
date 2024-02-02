package stdlib_results

// IsSuccess returns a boolean indicating if the supplied struct contains a `Success` value
func IsSuccess[T any](st struct {
	Success T
	Failure error
}) bool {
	return st.Failure == nil
}

// Success returns the supplied struct's `Success` value
func Success[T any](st struct {
	Success T
	Failure error
}) T {
	return st.Success
}

// IsFailure returns a boolean indicating if the supplied struct contains a `Failure` value
func IsFailure[T any](st struct {
	Success T
	Failure error
}) bool {
	return st.Failure != nil
}

// Failure returns the supplied struct's `Failure` value
func Failure[T any](st struct {
	Success T
	Failure error
}) error {
	return st.Failure
}
