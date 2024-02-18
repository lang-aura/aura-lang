using AuraLang.AST;
using Range = AuraLang.Location.Range;

namespace AuraLang.Types;

public interface IDefaultable
{
	ITypedAuraExpression Default(Range range);
}
