using Range = AuraLang.Location.Range;

namespace AuraLang.AST;

/// <summary>
///     Represents all Abstract Syntax Tree nodes
/// </summary>
public interface IAuraAstNode
{
	/// <summary>
	///     The range in the Aura source file where the AST node appears
	/// </summary>
	public Range Range { get; }
}


