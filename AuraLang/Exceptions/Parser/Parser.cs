namespace AuraLang.Exceptions.Parser;

public abstract class ParserException : AuraException
{
	protected ParserException(string message, int line) : base(message, line) { }
}

public class TooManyParametersException : ParserException
{
	public TooManyParametersException(int limit, int line) : base($"Cannot have more than {limit} parameters", line) { }
}

public class ExpectParameterNameException : ParserException
{
	public ExpectParameterNameException(int line) : base("Expected parameter name", line) { }
}

public class ExpectColonAfterParameterName : ParserException
{
	public ExpectColonAfterParameterName(string found, int line) : base($"Expected `:` after parameter name, but found `{found}` instead", line) { }
}

public class VariadicSignifierMustHaveThreeDots : ParserException
{
	public VariadicSignifierMustHaveThreeDots(int line) : base("Variadic signifier must have three dots", line) { }
}

public class ExpectEitherRightParenOrCommaAfterParam : ParserException
{
	public ExpectEitherRightParenOrCommaAfterParam(string found, int line)
		: base($"Expected either right paren or comma after param, but found `{found}` instead", line) { }
}

public class ExpectParameterTypeException : ParserException
{
	public ExpectParameterTypeException(string found, int line) : base($"Expected parameter type, but found `{found}` instead", line) { }
}

public class UnterminatedListLiteralException : ParserException
{
	public UnterminatedListLiteralException(int line) : base("Unterminated list literal", line) { }
}

public class ExpectLeftBracketAfterMapKeywordException : ParserException
{
	public ExpectLeftBracketAfterMapKeywordException(string found, int line)
		: base($"Expected `[` after map keyword, but found `{found}` instead", line) { }
}

public class ExpectColonBetweenMapTypesException : ParserException
{
	public ExpectColonBetweenMapTypesException(string found, int line)
		: base($"Expected colon between map types, but found `{found}` instead", line) { }
}

public class UnterminatedMapTypeSignatureException : ParserException
{
	public UnterminatedMapTypeSignatureException(int line) : base("Unterminated map type signature", line) { }
}

public class UnexpectedTypeException : ParserException
{
	public UnexpectedTypeException(string found, int line) : base($"Type `{found}` was unexpected", line) { }
}

public class InvalidTokenAfterPubKeywordException : ParserException
{
	public InvalidTokenAfterPubKeywordException(string found, int line)
		: base($"Token `{found}` may not come directly after `pub` keyword", line) { }
}

public class ExpectIdentifierException : ParserException
{
	public ExpectIdentifierException(string found, int line) : base($"Expected identifier, but found `{found}` instead", line) { }
}

public class ExpectSemicolonException : ParserException
{
	public ExpectSemicolonException(string found, int line) : base($"Expected semicolon, but found `{found}` instead", line) { }
}

public class InvalidTokenAfterMutKeywordException : ParserException
{
	public InvalidTokenAfterMutKeywordException(string found, int line)
		: base($"Token `{found}` may not come directly after mut keyword", line) { }
}

public class ExpectLeftParenException : ParserException
{
	public ExpectLeftParenException(string found, int line) : base($"Expected left paren, but found `{found}` instead", line) { }
}

public class ExpectRightParenException : ParserException
{
	public ExpectRightParenException(string found, int line) : base($"Expected right paren, but found `{found}` instead", line) { }
}

public class ExpectLeftBraceException : ParserException
{
	public ExpectLeftBraceException(string found, int line) : base($"Expected left brace, but found `{found}` instead", line) { }
}

public class ExpectRightBraceException : ParserException
{
	public ExpectRightBraceException(string found, int line) : base($"Expected right brace, but found `{found}`", line) { }
}

public class ExpectInKeywordException : ParserException
{
	public ExpectInKeywordException(string found, int line) : base($"Expected `in` keyword, but found `{found}` instead", line) { }
}

public class CanOnlyDeferFunctionCallException : ParserException
{
	public CanOnlyDeferFunctionCallException(int line) : base("Can only defer function call", line) { }
}

public class ExpectColonException : ParserException
{
	public ExpectColonException(string found, int line) : base($"Expected `:`, but found `{found}` instead", line) { }
}

public class ExpectColonEqualException : ParserException
{
	public ExpectColonEqualException(string found, int line) : base($"Expected `:=`, but found `{found}` instead", line) { }
}

public class InvalidAssignmentTargetException : ParserException
{
	public InvalidAssignmentTargetException(int line) : base("Invalid assignment target", line) { }
}

public class UnreachableCodeException : ParserException
{
	public UnreachableCodeException(int line) : base("Unreachable code", line) { }
}

public class ExpectPropertyNameException : ParserException
{
	public ExpectPropertyNameException(string found, int line) : base($"Expected property name, but found `{found}` instead", line) { }
}

public class ExpectIntLiteralException : ParserException
{
	public ExpectIntLiteralException(string found, int line) : base($"Expected int literal, but found `{found}` instead", line) { }
}

public class ExpectRightBracketException : ParserException
{
	public ExpectRightBracketException(string found, int line) : base($"Expected `]`, but found `{found}` instead", line) { }
}

public class ExpectCommaException : ParserException
{
	public ExpectCommaException(string found, int line) : base($"Expected `,`, but found `{found}` instead", line) { }
}

public class ExpectExpressionException : ParserException
{
	public ExpectExpressionException(int line) : base("Expected expression", line) { }
}

public class ParameterDefaultValueMustBeALiteralException : ParserException
{
	public ParameterDefaultValueMustBeALiteralException(int line) : base("Parameter default value must be a literal", line) { }
}

public class InvalidIndexTypeException : ParserException
{
	public InvalidIndexTypeException(int line) : base("Invalid index type", line) { }
}

public class PostfixIndexCannotBeEmptyException : ParserException
{
	public PostfixIndexCannotBeEmptyException(int line) : base("Postfix index cannot be empty", line) { }
}

public class ExpectFunctionSignatureException : ParserException
{
	public ExpectFunctionSignatureException(int line) : base("Expected function signature", line) { }
}
