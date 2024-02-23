namespace AuraLang.Exceptions.Compiler;

/// <summary>
///     A container for exceptions thrown by the compiler
/// </summary>
public class CompilerExceptionContainer : AuraExceptionContainer
{
	public CompilerExceptionContainer(string filePath) : base(filePath) { }

	public void Add(CompilerException ex)
	{
		Exs.Add(ex);
	}
}
