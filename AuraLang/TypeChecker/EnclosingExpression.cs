using AuraLang.AST;

namespace AuraLang.TypeChecker;

public class EnclosingExpressionStore
{
    private readonly Stack<UntypedAuraExpression> _enclosingExpressions = new();

    public void Push(UntypedAuraExpression expr) => _enclosingExpressions.Push(expr);
    public UntypedAuraExpression Pop() => _enclosingExpressions.Pop();
    public virtual UntypedAuraExpression Peek() => _enclosingExpressions.Peek();

    public virtual T WithEnclosingExpression<T>(Func<T> f, UntypedAuraExpression expr)
    {
        Push(expr);
        var typedExpr = f();
        Pop();
        return typedExpr;
    }
}