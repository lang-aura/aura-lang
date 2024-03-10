using AuraLang.Shared;

namespace AuraLang.Types;

/// <summary>
///     Represents a type that can be used as the callee in a call expression
/// </summary>
public interface ICallable : IDocumentable
{
	/// <summary>
	///     Fetches the call's parameters
	/// </summary>
	/// <returns>A list of parameters</returns>
	List<Param> GetParams();

	/// <summary>
	///     Fetches the call's parameter types, where each index in the returned list will correspond to parameter located at
	///     the same index in the list returned by <see cref="GetParams" />
	/// </summary>
	/// <returns>A list of parameter types</returns>
	List<ParamType> GetParamTypes();

	/// <summary>
	///     Fetches the call's return type
	/// </summary>
	/// <returns>The call's return type</returns>
	AuraType GetReturnType();

	/// <summary>
	///     Returns the index of the parameter identified by the supplied name
	/// </summary>
	/// <param name="name">The name of the parameter</param>
	/// <returns>The index of the parameter with the supplied name</returns>
	int GetParamIndex(string name);

	/// <summary>
	///     Determines if any of the parameters are variadic. If a callable does have a variadic parameter, it may only have
	///     one and it must be the final parameter
	/// </summary>
	/// <returns></returns>
	bool HasVariadicParam();

	/// <summary>
	///     Converts the callable to its Aura representation
	/// </summary>
	/// <returns>The callable's Aura representation</returns>
	string ToAuraString();
}
