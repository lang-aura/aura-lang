namespace AuraLang.TypeChecker;

public class EnclosingNodeStore<T>
{
    private readonly Stack<T> _enclosingNodes = new();

    private void Push(T node) => _enclosingNodes.Push(node);
    private T Pop() => _enclosingNodes.Pop();
    public virtual T Peek() => _enclosingNodes.Peek();

    public virtual TU WithEnclosing<TU>(Func<TU> f, T node)
    {
        Push(node);
        var typedNode = f();
        Pop();
        return typedNode;
    }
}