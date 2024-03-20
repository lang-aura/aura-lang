using AuraLang.Types;

namespace AuraLang.Symbol;

/// <summary>
///     The Type Checker's representation of a local variable in the Aura source code
/// </summary>
/// <param name="Name">The variable's name</param>
/// <param name="Kind">The variable's type</param>
public record struct AuraSymbol(string Name, AuraType Kind)
{
	/// <summary>
	///     Indicates whether the symbol represents a mutable variable. This field is not applicable for all symbols (i.e. a
	///     named function declaration)
	/// </summary>
	public bool Mutable { get; }

	public AuraSymbol(string name, AuraType kind, bool mutable) : this(name, kind) { Mutable = mutable; }
}
