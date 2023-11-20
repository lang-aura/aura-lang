using AuraLang.AST;

namespace AuraLang.Types;

public interface IDefaultable
{
    TypedAuraExpression Default(int line);
}