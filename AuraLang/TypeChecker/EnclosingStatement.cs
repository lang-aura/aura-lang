using AuraLang.AST;

namespace AuraLang.TypeChecker;

public class EnclosingStatementStore
{
    private readonly Stack<UntypedAuraStatement> _enclosingStatements = new();

    private void Push(UntypedAuraStatement stmt) => _enclosingStatements.Push(stmt);
    private UntypedAuraStatement Pop() => _enclosingStatements.Pop();
    public virtual UntypedAuraStatement Peek() => _enclosingStatements.Peek();

    public virtual T WithEnclosingStatement<T>(Func<T> f, UntypedAuraStatement stmt)
    {
        Push(stmt);
        var typedStmt = f();
        Pop();
        return typedStmt;
    }
}