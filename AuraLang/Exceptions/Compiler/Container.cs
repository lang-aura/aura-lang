namespace AuraLang.Exceptions.Compiler;

/// <summary>
///     A container for exceptions thrown by the compiler
/// </summary>
public class CompilerExceptionContainer : AuraExceptionContainer<string>
{
	public override string? Valid { get; set; }

	public CompilerExceptionContainer(string filePath) : base(filePath) { }

	public void Add(CompilerException ex)
	{
		Exs.Add(ex);
	}
}
