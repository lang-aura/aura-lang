using AuraLang.AST;
using Range = AuraLang.Location.Range;

namespace AuraLang.Exceptions.Compiler;

public abstract class CompilerException : AuraException
{
	protected CompilerException(string message, Range range) : base(message, range) { }
}

public class UnknownStatementException : CompilerException
{
	public UnknownStatementException(ITypedAuraStatement stmt, Range range)
		: base($"Unknown statement: {stmt}", range) { }
}

public class UnknownExpressionException : CompilerException
{
	public UnknownExpressionException(ITypedAuraExpression expr, Range range)
		: base($"Unknown expression: {expr}", range) { }
}

public class DirectoryCannotContainMultipleModulesException : CompilerException
{
	public DirectoryCannotContainMultipleModulesException(Range range) : base("Directory cannot contain multiple modules", range) { }
}
