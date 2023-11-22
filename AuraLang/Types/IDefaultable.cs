using AuraLang.AST;

namespace AuraLang.Types;

public interface IDefaultable
{
    ITypedAuraExpression Default(int line);
}