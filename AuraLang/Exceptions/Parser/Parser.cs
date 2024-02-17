using Range = AuraLang.Location.Range;

namespace AuraLang.Exceptions.Parser;

public abstract class ParserException : AuraException
{
	protected ParserException(string message, Range range) : base(message, range) { }
}

public class TooManyParametersException : ParserException
{
	public TooManyParametersException(int limit, Range range) : base($"Cannot have more than {limit} parameters", range) { }
}

public class ExpectParameterNameException : ParserException
{
	public ExpectParameterNameException(Range range) : base("Expected parameter name", range) { }
}

public class ExpectColonAfterParameterName : ParserException
{
	public ExpectColonAfterParameterName(string found, Range range) : base($"Expected `:` after parameter name, but found `{found}` instead", range) { }
}

public class VariadicSignifierMustHaveThreeDots : ParserException
{
	public VariadicSignifierMustHaveThreeDots(Range range) : base("Variadic signifier must have three dots", range) { }
}

public class ExpectEitherRightParenOrCommaAfterParam : ParserException
{
	public ExpectEitherRightParenOrCommaAfterParam(string found, Range range)
		: base($"Expected either right paren or comma after param, but found `{found}` instead", range) { }
}

public class ExpectParameterTypeException : ParserException
{
	public ExpectParameterTypeException(string found, Range range) : base($"Expected parameter type, but found `{found}` instead", range) { }
}

public class UnterminatedListLiteralException : ParserException
{
	public UnterminatedListLiteralException(Range range) : base("Unterminated list literal", range) { }
}

public class ExpectLeftBracketException : ParserException
{
	public ExpectLeftBracketException(string found, Range range)
		: base($"Expected `[` after map keyword, but found `{found}` instead", range) { }
}

public class ExpectColonBetweenMapTypesException : ParserException
{
	public ExpectColonBetweenMapTypesException(string found, Range range)
		: base($"Expected colon between map types, but found `{found}` instead", range) { }
}

public class UnterminatedMapTypeSignatureException : ParserException
{
	public UnterminatedMapTypeSignatureException(Range range) : base("Unterminated map type signature", range) { }
}

public class UnexpectedTypeException : ParserException
{
	public UnexpectedTypeException(string found, Range range) : base($"Type `{found}` was unexpected", range) { }
}

public class InvalidTokenAfterPubKeywordException : ParserException
{
	public InvalidTokenAfterPubKeywordException(string found, Range range)
		: base($"Token `{found}` may not come directly after `pub` keyword", range) { }
}

public class ExpectIdentifierException : ParserException
{
	public ExpectIdentifierException(string found, Range range) : base($"Expected identifier, but found `{found}` instead", range) { }
}

public class ExpectSemicolonException : ParserException
{
	public ExpectSemicolonException(string found, Range range) : base($"Expected semicolon, but found `{found}` instead", range) { }
}

public class InvalidTokenAfterMutKeywordException : ParserException
{
	public InvalidTokenAfterMutKeywordException(string found, Range range)
		: base($"Token `{found}` may not come directly after mut keyword", range) { }
}

public class ExpectLeftParenException : ParserException
{
	public ExpectLeftParenException(string found, Range range) : base($"Expected left paren, but found `{found}` instead", range) { }
}

public class ExpectRightParenException : ParserException
{
	public ExpectRightParenException(string found, Range range) : base($"Expected right paren, but found `{found}` instead", range) { }
}

public class ExpectLeftBraceException : ParserException
{
	public ExpectLeftBraceException(string found, Range range) : base($"Expected left brace, but found `{found}` instead", range) { }
}

public class ExpectRightBraceException : ParserException
{
	public ExpectRightBraceException(string found, Range range) : base($"Expected right brace, but found `{found}`", range) { }
}

public class ExpectInKeywordException : ParserException
{
	public ExpectInKeywordException(string found, Range range) : base($"Expected `in` keyword, but found `{found}` instead", range) { }
}

public class CanOnlyDeferFunctionCallException : ParserException
{
	public CanOnlyDeferFunctionCallException(Range range) : base("Can only defer function call", range) { }
}

public class ExpectColonException : ParserException
{
	public ExpectColonException(string found, Range range) : base($"Expected `:`, but found `{found}` instead", range) { }
}

public class ExpectColonEqualException : ParserException
{
	public ExpectColonEqualException(string found, Range range) : base($"Expected `:=`, but found `{found}` instead", range) { }
}

public class InvalidAssignmentTargetException : ParserException
{
	public InvalidAssignmentTargetException(Range range) : base("Invalid assignment target", range) { }
}

public class UnreachableCodeException : ParserException
{
	public UnreachableCodeException(Range range) : base("Unreachable code", range) { }
}

public class ExpectPropertyNameException : ParserException
{
	public ExpectPropertyNameException(string found, Range range) : base($"Expected property name, but found `{found}` instead", range) { }
}

public class ExpectIntLiteralException : ParserException
{
	public ExpectIntLiteralException(string found, Range range) : base($"Expected int literal, but found `{found}` instead", range) { }
}

public class ExpectRightBracketException : ParserException
{
	public ExpectRightBracketException(string found, Range range) : base($"Expected `]`, but found `{found}` instead", range) { }
}

public class ExpectCommaException : ParserException
{
	public ExpectCommaException(string found, Range range) : base($"Expected `,`, but found `{found}` instead", range) { }
}

public class ExpectExpressionException : ParserException
{
	public ExpectExpressionException(Range range) : base("Expected expression", range) { }
}

public class ParameterDefaultValueMustBeALiteralException : ParserException
{
	public ParameterDefaultValueMustBeALiteralException(Range range) : base("Parameter default value must be a literal", range) { }
}

public class InvalidIndexTypeException : ParserException
{
	public InvalidIndexTypeException(Range range) : base("Invalid index type", range) { }
}

public class PostfixIndexCannotBeEmptyException : ParserException
{
	public PostfixIndexCannotBeEmptyException(Range range) : base("Postfix index cannot be empty", range) { }
}

public class ExpectFunctionSignatureException : ParserException
{
	public ExpectFunctionSignatureException(Range range) : base("Expected function signature", range) { }
}

public class ExpectNewLineException : ParserException
{
	public ExpectNewLineException(string found, Range range) : base($"Expected '\n', but found '{found}'", range) { }
}

public class FileMustBeginWithModStmtException : ParserException
{
	public FileMustBeginWithModStmtException(Range range) : base("Cannot omit `mod` statement", range) { }
}

public class CanOnlyCheckFunctionCallException : ParserException
{
	public CanOnlyCheckFunctionCallException(Range range) : base("Can only check function call", range) { }
}
