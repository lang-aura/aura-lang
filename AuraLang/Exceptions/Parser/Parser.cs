namespace AuraLang.Exceptions.Parser;

public class ParserExceptionContainer : AuraExceptionContainer
{
	public void Add(ParserException ex)
	{
		Exs.Add(ex);
	}
}

public abstract class ParserException : AuraException
{
	protected ParserException(string message, int line) : base(message, line) { }
}

public class TooManyParametersException : ParserException
{
	public TooManyParametersException(int line) : base("Too many parameters", line) { }
}

public class ExpectParameterNameException : ParserException
{
	public ExpectParameterNameException(int line) : base("Expect parameter name", line) { }
}

public class ExpectColonAfterParameterName : ParserException
{
	public ExpectColonAfterParameterName(int line) : base("Expect colon after parameter name", line) { }
}

public class VariadicSignifierMustHaveThreeDots : ParserException
{
	public VariadicSignifierMustHaveThreeDots(int line) : base("Variadic signifier must have three dots", line) { }
}

public class ExpectEitherRightParenOrCommaAfterParam : ParserException
{
	public ExpectEitherRightParenOrCommaAfterParam(int line) : base("Expect either right paren or comma after param", line) { }
}

public class ExpectParameterTypeException : ParserException
{
	public ExpectParameterTypeException(int line) : base("Expect parameter type", line) { }
}

public class ExpectVariableTypeException : ParserException
{
	public ExpectVariableTypeException(int line) : base("Expect variable type", line) { }
}

public class UnterminatedListLiteralException : ParserException
{
	public UnterminatedListLiteralException(int line) : base("Unterminated list literal", line) { }
}

public class ExpectLeftParenAfterFnKeywordException : ParserException
{
	public ExpectLeftParenAfterFnKeywordException(int line) : base("Expect left paren after fn keyword", line) { }
}

public class ExpectArrowInFnSignatureException : ParserException
{
	public ExpectArrowInFnSignatureException(int line) : base("Expect arrow in fn signature", line) { }
}

public class ExpectLeftBracketAfterMapKeywordException : ParserException
{
	public ExpectLeftBracketAfterMapKeywordException(int line) : base("Expeect left bracket after map keyword", line) { }
}

public class ExpectColonBetweenMapTypesException : ParserException
{
	public ExpectColonBetweenMapTypesException(int line) : base("Expect colon between map types", line) { }
}

public class UnterminatedMapTypeSignatureException : ParserException
{
	public UnterminatedMapTypeSignatureException(int line) : base("Unterminated map type signature", line) { }
}

public class ExpectLeftBracketAfterTupKeywordException : ParserException
{
	public ExpectLeftBracketAfterTupKeywordException(int line) : base("Expect left bracket after tup keyword", line) { }
}

public class UnexpectedTypeException : ParserException
{
	public UnexpectedTypeException(int line) : base("Unexpected type", line) { }
}

public class InvalidTokenAfterPubKeywordException : ParserException
{
	public InvalidTokenAfterPubKeywordException(int line) : base("Invalid token after pub keyword", line) { }
}

public class ExpectIdentifierException : ParserException
{
	public ExpectIdentifierException(int line) : base("Expect identifier", line) { }
}

public class ExpectSemicolonException : ParserException
{
	public ExpectSemicolonException(int line) : base("Expect semicolon", line) { }
}

public class InvalidTokenAfterMutKeywordException : ParserException
{
	public InvalidTokenAfterMutKeywordException(int line) : base("Invalid token after mut keyword", line) { }
}

public class ExpectLeftParenException : ParserException
{
	public ExpectLeftParenException(int line) : base("Expect left paren", line) { }
}

public class ExpectRightParenException : ParserException
{
	public ExpectRightParenException(int line) : base("Expect right paren", line) { }
}

public class ExpectLeftBraceException : ParserException
{
	public ExpectLeftBraceException(int line) : base("Expect left brace", line) { }
}

public class ExpectRightBraceException : ParserException
{
	public ExpectRightBraceException(int line) : base("Expect right brace", line) { }
}

public class ExpectInKeywordException : ParserException
{
	public ExpectInKeywordException(int line) : base("Expect in keyword", line) { }
}

public class CanOnlyDeferFunctionCallException : ParserException
{
	public CanOnlyDeferFunctionCallException(int line) : base("Can only defer function call", line) { }
}

public class ExpectColonException : ParserException
{
	public ExpectColonException(int line) : base("Expect colon", line) { }
}

public class ExpectColonEqualException : ParserException
{
	public ExpectColonEqualException(int line) : base("Expect `:=`", line) { }
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
	public ExpectPropertyNameException(int line) : base("Expect property name", line) { }
}

public class ExpectIntLiteralException : ParserException
{
	public ExpectIntLiteralException(int line) : base("Expect int literal", line) { }
}

public class ExpectRightBracketException : ParserException
{
	public ExpectRightBracketException(int line) : base("Expect right bracket", line) { }
}

public class ExpectCommaException : ParserException
{
	public ExpectCommaException(int line) : base("Expect comma", line) { }
}

public class ExpectExpressionException : ParserException
{
	public ExpectExpressionException(int line) : base("Expect expression", line) { }
}

public class ParameterDefaultValueMustBeALiteralException : ParserException
{
	public ParameterDefaultValueMustBeALiteralException(int line) : base("Parameter default value must be a literal", line) { }
}
