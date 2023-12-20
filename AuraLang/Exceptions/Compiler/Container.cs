namespace AuraLang.Exceptions.Compiler;

public class CompilerExceptionContainer : AuraExceptionContainer
{
	public CompilerExceptionContainer(string filePath) : base(filePath) { }

	public void Add(CompilerException ex)
	{
		Exs.Add(ex);
	}
}
