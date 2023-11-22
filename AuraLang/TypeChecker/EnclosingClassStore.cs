using AuraLang.AST;

namespace AuraLang.TypeChecker;

/// <summary>
/// Stores the current enclosing class
/// </summary>
public interface IEnclosingClassStore
{
	/// <summary>
	/// Pushes a new enclosing class onto the top of the stack
	/// </summary>
	/// <param name="ptc">The new enclosing class</param>
	void Push(PartiallyTypedClass ptc);
	/// <summary>
	/// Pops the current enclosing class off the top of the stack
	/// </summary>
	void Pop();
	/// <summary>
	/// Retrieves the current enclosing class without removing it from the stack
	/// </summary>
	/// <returns>The current enclosing class</returns>
	PartiallyTypedClass Peek();
}

public class EnclosingClassStore : IEnclosingClassStore
{
	private Stack<PartiallyTypedClass> _enclosingClass = new();

	public void Push(PartiallyTypedClass ptc) => _enclosingClass.Push(ptc);
	public void Pop() => _enclosingClass.Pop();

	public PartiallyTypedClass Peek() => _enclosingClass.Peek();
}
