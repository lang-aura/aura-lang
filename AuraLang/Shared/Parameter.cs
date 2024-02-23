using AuraLang.AST;
using AuraLang.Token;
using AuraLang.Types;
using Range = AuraLang.Location.Range;

namespace AuraLang.Shared;

/// <summary>
///     Represents a parameter
/// </summary>
/// <param name="Name">The parameter's name</param>
/// <param name="ParamType">The parameter's type</param>
public readonly record struct Param(Tok Name, ParamType ParamType) : IHoverable
{
	/// <summary>
	///     The parameter's range in the Aura source file
	/// </summary>
	public Range Range => Name.Range;

	/// <summary>
	///     The parameter's hoverable range in the Aura source file. Although a parameter consists of a name and a type
	///     separated by a colon, typically the user must hover over the parameter's name to see additional information
	/// </summary>
	public Range HoverableRange => Name.Range;

	/// <summary>
	///     The text to show when the user hovers over the parameter's <see cref="HoverableRange" />
	/// </summary>
	public string HoverText => $"(parameter) {Name.Value}: {ParamType.Typ.ToAuraString()}";
}

/// <summary>
///     Represents a parameter's type information
/// </summary>
/// <param name="Typ">The parameter's type</param>
/// <param name="Variadic">Whether the parameter is variadic, meaning it may accept more than one argument</param>
/// <param name="DefaultValue">Whether the parameter was defined with a default value</param>
public record struct ParamType(AuraType Typ, bool Variadic, ILiteral? DefaultValue);
