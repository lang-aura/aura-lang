using AuraLang.AST;

namespace AuraLang.Exceptions.Compiler;

public abstract class CompilerException : AuraException
{
	protected CompilerException(string message, int line) : base(message, line) { }
}

public class UnknownStatementException : CompilerException
{
	public UnknownStatementException(ITypedAuraStatement stmt, int line)
		: base($"Unknown statement: {stmt}", line) { }
}

public class UnknownExpressionException : CompilerException
{
	public UnknownExpressionException(ITypedAuraExpression expr, int line)
		: base($"Unknown expression: {expr}", line) { }
}
