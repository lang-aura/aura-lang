namespace AuraLang.Shared;

/// <summary>
///     Indicates a function's type, which is dependent on its defining context
/// </summary>
public enum FunctionType
{
	/// <summary>
	///     Represents a function defined outside of a class's body
	/// </summary>
	Function,

	/// <summary>
	///     Represents a function defined inside of a class's body
	/// </summary>
	Method
}
