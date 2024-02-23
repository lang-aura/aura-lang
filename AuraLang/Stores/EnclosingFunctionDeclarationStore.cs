using AuraLang.AST;

namespace AuraLang.Stores;

/// <summary>
///     Responsible for storing the current enclosing function declaration, if there is one. The presence or absence of an
///     enclosing function declaration may impact the behavior of the compilation process in certain specific circumstances
/// </summary>
public interface IEnclosingFunctionDeclarationStore
{
	/// <summary>
	///     Pushes a new enclosing function declaration onto the top of the stack
	/// </summary>
	/// <param name="ptf">The new enclosing function declaration</param>
	void Push(UntypedNamedFunction ptf);

	/// <summary>
	///     Pops the current enclosing function declaration off the top of the stack
	/// </summary>
	void Pop();

	/// <summary>
	///     Retrieves the current enclosing function declaration without removing it from the stack
	/// </summary>
	/// <returns>The current enclosing function declaration</returns>
	UntypedNamedFunction? Peek();
}

public class EnclosingFunctionDeclarationStore : IEnclosingFunctionDeclarationStore
{
	private readonly Stack<UntypedNamedFunction> _enclosingFunctionDeclaration = new();

	public void Push(UntypedNamedFunction ptf) { _enclosingFunctionDeclaration.Push(ptf); }

	public void Pop() { _enclosingFunctionDeclaration.Pop(); }

	public UntypedNamedFunction? Peek() { return _enclosingFunctionDeclaration.Peek(); }
}