using AuraLang.AST;
using Range = AuraLang.Location.Range;

namespace AuraLang.Exceptions.Compiler;

/// <summary>
///     Represents an error encountered by the compiler. Concrete errors should extend this abstract class
/// </summary>
public abstract class CompilerException : AuraException
{
	protected CompilerException(string message, Range range) : base(message, range) { }
}

/// <summary>
///     Thrown when the compiler encounters an unknown statement
/// </summary>
public class UnknownStatementException : CompilerException
{
	public UnknownStatementException(ITypedAuraStatement stmt, Range range)
		: base($"Unknown statement: {stmt}", range) { }
}

/// <summary>
///     Thrown when the compiler encounters an unknown expression
/// </summary>
public class UnknownExpressionException : CompilerException
{
	public UnknownExpressionException(ITypedAuraExpression expr, Range range)
		: base($"Unknown expression: {expr}", range) { }
}

/// <summary>
///     Thrown when a directory contains more than one Aura module
/// </summary>
public class DirectoryCannotContainMultipleModulesException : CompilerException
{
	public DirectoryCannotContainMultipleModulesException(Range range) : base("Directory cannot contain multiple modules", range) { }
}
