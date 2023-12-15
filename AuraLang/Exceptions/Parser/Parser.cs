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
	protected ParserException(string message, string filePath, int line) : base(message, filePath, line) { }
}

public class TooManyParametersException : ParserException
{
	public TooManyParametersException(string filePath, int line) : base("Too many parameters", filePath, line) { }
}

public class ExpectParameterNameException : ParserException
{
	public ExpectParameterNameException(string filePath, int line) : base("Expect parameter name", filePath, line) { }
}

public class ExpectColonAfterParameterName : ParserException
{
	public ExpectColonAfterParameterName(string filePath, int line) : base("Expect colon after parameter name",
		filePath, line)
	{
	}
}

public class VariadicSignifierMustHaveThreeDots : ParserException
{
	public VariadicSignifierMustHaveThreeDots(string filePath, int line) : base(
		"Variadic signifier must have three dots", filePath, line)
	{
	}
}

public class ExpectEitherRightParenOrCommaAfterParam : ParserException
{
	public ExpectEitherRightParenOrCommaAfterParam(string filePath, int line) : base(
		"Expect either right paren or comma after param", filePath,
		line)
	{
	}
}

public class ExpectParameterTypeException : ParserException
{
	public ExpectParameterTypeException(string filePath, int line) : base("Expect parameter type", filePath, line) { }
}

public class ExpectVariableTypeException : ParserException
{
	public ExpectVariableTypeException(string filePath, int line) : base("Expect variable type", filePath, line) { }
}

public class UnterminatedListLiteralException : ParserException
{
	public UnterminatedListLiteralException(string filePath, int line) : base("Unterminated list literal", filePath,
		line)
	{
	}
}

public class ExpectLeftBracketAfterMapKeywordException : ParserException
{
	public ExpectLeftBracketAfterMapKeywordException(string filePath, int line) : base(
		"Expeect left bracket after map keyword", filePath, line)
	{
	}
}

public class ExpectColonBetweenMapTypesException : ParserException
{
	public ExpectColonBetweenMapTypesException(string filePath, int line) : base("Expect colon between map types",
		filePath, line)
	{
	}
}

public class UnterminatedMapTypeSignatureException : ParserException
{
	public UnterminatedMapTypeSignatureException(string filePath, int line) : base("Unterminated map type signature",
		filePath, line)
	{
	}
}

public class ExpectLeftBracketAfterTupKeywordException : ParserException
{
	public ExpectLeftBracketAfterTupKeywordException(string filePath, int line) : base(
		"Expect left bracket after tup keyword", filePath, line)
	{
	}
}

public class UnexpectedTypeException : ParserException
{
	public UnexpectedTypeException(string filePath, int line) : base("Unexpected type", filePath, line) { }
}

public class InvalidTokenAfterPubKeywordException : ParserException
{
	public InvalidTokenAfterPubKeywordException(string filePath, int line) : base("Invalid token after pub keyword",
		filePath, line)
	{
	}
}

public class ExpectIdentifierException : ParserException
{
	public ExpectIdentifierException(string filePath, int line) : base("Expect identifier", filePath, line) { }
}

public class ExpectSemicolonException : ParserException
{
	public ExpectSemicolonException(string filePath, int line) : base("Expect semicolon", filePath, line) { }
}

public class InvalidTokenAfterMutKeywordException : ParserException
{
	public InvalidTokenAfterMutKeywordException(string filePath, int line) : base("Invalid token after mut keyword",
		filePath, line)
	{
	}
}

public class ExpectLeftParenException : ParserException
{
	public ExpectLeftParenException(string filePath, int line) : base("Expect left paren", filePath, line) { }
}

public class ExpectRightParenException : ParserException
{
	public ExpectRightParenException(string filePath, int line) : base("Expect right paren", filePath, line) { }
}

public class ExpectLeftBraceException : ParserException
{
	public ExpectLeftBraceException(string filePath, int line) : base("Expect left brace", filePath, line) { }
}

public class ExpectRightBraceException : ParserException
{
	public ExpectRightBraceException(string filePath, int line) : base("Expect right brace", filePath, line) { }
}

public class ExpectInKeywordException : ParserException
{
	public ExpectInKeywordException(string filePath, int line) : base("Expect in keyword", filePath, line) { }
}

public class CanOnlyDeferFunctionCallException : ParserException
{
	public CanOnlyDeferFunctionCallException(string filePath, int line) : base("Can only defer function call", filePath,
		line)
	{
	}
}

public class ExpectColonException : ParserException
{
	public ExpectColonException(string filePath, int line) : base("Expect colon", filePath, line) { }
}

public class ExpectColonEqualException : ParserException
{
	public ExpectColonEqualException(string filePath, int line) : base("Expect `:=`", filePath, line) { }
}

public class InvalidAssignmentTargetException : ParserException
{
	public InvalidAssignmentTargetException(string filePath, int line) : base("Invalid assignment target", filePath,
		line)
	{
	}
}

public class UnreachableCodeException : ParserException
{
	public UnreachableCodeException(string filePath, int line) : base("Unreachable code", filePath, line) { }
}

public class ExpectPropertyNameException : ParserException
{
	public ExpectPropertyNameException(string filePath, int line) : base("Expect property name", filePath, line) { }
}

public class ExpectIntLiteralException : ParserException
{
	public ExpectIntLiteralException(string filePath, int line) : base("Expect int literal", filePath, line) { }
}

public class ExpectRightBracketException : ParserException
{
	public ExpectRightBracketException(string filePath, int line) : base("Expect right bracket", filePath, line) { }
}

public class ExpectCommaException : ParserException
{
	public ExpectCommaException(string filePath, int line) : base("Expect comma", filePath, line) { }
}

public class ExpectExpressionException : ParserException
{
	public ExpectExpressionException(string filePath, int line) : base("Expect expression", filePath, line) { }
}

public class ParameterDefaultValueMustBeALiteralException : ParserException
{
	public ParameterDefaultValueMustBeALiteralException(string filePath, int line) : base(
		"Parameter default value must be a literal", filePath,
		line)
	{
	}
}

public class InvalidIndexTypeException : ParserException
{
	public InvalidIndexTypeException(string filePath, int line) : base("Invalid index type", filePath, line) { }
}

public class PostfixIndexCannotBeEmptyException : ParserException
{
	public PostfixIndexCannotBeEmptyException(string filePath, int line) : base("Postfix index cannot be empty",
		filePath, line)
	{
	}
}

public class ExpectFunctionSignatureException : ParserException
{
	public ExpectFunctionSignatureException(string filePath, int line) : base("Expect function signature", filePath,
		line)
	{
	}
}
