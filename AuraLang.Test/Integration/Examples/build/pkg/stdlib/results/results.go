package stdlib_results

func IsSuccess[T any](st struct {
	Success T
	Failure error
}) bool {
	return st.Failure == nil
}

func Success[T any](st struct {
	Success T
	Failure error
}) T {
	return st.Success
}

func IsFailure[T any](st struct {
	Success T
	Failure error
}) bool {
	return st.Failure != nil
}

func Failure[T any](st struct {
	Success T
	Failure error
}) error {
	return st.Failure
}
