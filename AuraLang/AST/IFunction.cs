using AuraLang.Shared;

namespace AuraLang.AST;

/// <summary>
///     Represents an untyped function
/// </summary>
public interface IUntypedFunction
{
	/// <summary>
	///     Gets the function's parameters
	/// </summary>
	/// <returns>The function's parameters</returns>
	public List<Param> GetParams();

	/// <summary>
	///     Gets the function's parameter types
	/// </summary>
	/// <returns>
	///     The function's parameter types, where each parameter type in the returned list corresponds to the parameter
	///     at the same index in the list returned by <see cref="GetParams" />
	/// </returns>
	public List<ParamType> GetParamTypes();
}

/// <summary>
///     Represents a typed function
/// </summary>
public interface ITypedFunction
{
	/// <summary>
	///     Gets the function's parameters
	/// </summary>
	/// <returns>The function's parameters</returns>
	public List<Param> GetParams();

	/// <summary>
	///     Gets the function's parameter types
	/// </summary>
	/// <returns>
	///     The function's parameter types, where each parameter type in the returned list corresponds to the parameter
	///     at the same index in the list returned by <see cref="GetParams" />
	/// </returns>
	public List<ParamType> GetParamTypes();
}
