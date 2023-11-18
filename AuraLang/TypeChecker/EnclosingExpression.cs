using AuraLang.AST;

namespace AuraLang.TypeChecker;

public interface IEnclosingExpressionStore
{
    void Push(UntypedAuraExpression expr);
    UntypedAuraExpression Pop();
    UntypedAuraExpression Peek();
    T WithEnclosingExpression<T>(Func<T> f, UntypedAuraExpression expr);
}

public class EnclosingExpressionStore : IEnclosingExpressionStore
{
    private readonly Stack<UntypedAuraExpression> _enclosingExpressions = new();

    public void Push(UntypedAuraExpression expr) => _enclosingExpressions.Push(expr);
    public UntypedAuraExpression Pop() => _enclosingExpressions.Pop();
    public UntypedAuraExpression Peek() => _enclosingExpressions.Peek();

    public T WithEnclosingExpression<T>(Func<T> f, UntypedAuraExpression expr)
    {
        Push(expr);
        var typedExpr = f();
        Pop();
        return typedExpr;
    }
}