namespace AuraLang.Exceptions.Compiler;

public class CompilerExceptionContainer : AuraExceptionContainer
{
	public void Add(CompilerException ex)
	{
		Exs.Add(ex);
	}
}

public abstract class CompilerException : AuraException
{
	protected CompilerException(string message, string filePath, int line) : base(message, filePath, line) { }
}

public class UnknownStatementException : CompilerException
{
	public UnknownStatementException(string filePath, int line) : base("Unknown statement", filePath, line) { }
}

public class UnknownExpressionException : CompilerException
{
	public UnknownExpressionException(string filePath, int line) : base("Unknown expression", filePath, line) { }
}
