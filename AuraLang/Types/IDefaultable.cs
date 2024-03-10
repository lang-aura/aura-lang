using AuraLang.AST;
using Range = AuraLang.Location.Range;

namespace AuraLang.Types;

/// <summary>
///     Represents a type that can provide a default value if one is not supplied when a variable of this type is defined
///     without an initializer value
/// </summary>
public interface IDefaultable
{
	/// <summary>
	///     Supplies the type's default value
	/// </summary>
	/// <param name="range"></param>
	/// <returns>The type's default value</returns>
	ITypedAuraExpression Default(Range range);
}
