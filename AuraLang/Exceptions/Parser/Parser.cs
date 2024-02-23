using Range = AuraLang.Location.Range;

namespace AuraLang.Exceptions.Parser;

/// <summary>
///     Represents an error encountered by the parser
/// </summary>
public abstract class ParserException : AuraException
{
	protected ParserException(string message, Range range) : base(message, range) { }
}

/// <summary>
///     Thrown when a callable node (such as a function call, function declaration, class declaration, etc.) contains more
///     than the maximum number of parameters
/// </summary>
public class TooManyParametersException : ParserException
{
	public TooManyParametersException(int limit, Range range) : base($"Cannot have more than {limit} parameters", range) { }
}

/// <summary>
///     Thrown when a parameter name is expected, but another token type is encountered
/// </summary>
public class ExpectParameterNameException : ParserException
{
	public ExpectParameterNameException(Range range) : base("Expected parameter name", range) { }
}

/// <summary>
///     Thrown when a parameter name is not immediately followed by the required colon token
/// </summary>
public class ExpectColonAfterParameterName : ParserException
{
	public ExpectColonAfterParameterName(string found, Range range) : base($"Expected `:` after parameter name, but found `{found}` instead", range) { }
}

/// <summary>
///     Thrown when a variadic parameter is not defined with the required <c>...</c> preceding the parameter's type
/// </summary>
public class VariadicSignifierMustHaveThreeDots : ParserException
{
	public VariadicSignifierMustHaveThreeDots(Range range) : base("Variadic signifier must have three dots", range) { }
}

/// <summary>
///     Thrown when a parameter is not followed by either a comma (indicating that another parameter is to follow) or a
///     right parenthesis (indicating this is the last parameter)
/// </summary>
public class ExpectEitherRightParenOrCommaAfterParam : ParserException
{
	public ExpectEitherRightParenOrCommaAfterParam(string found, Range range)
		: base($"Expected either right paren or comma after param, but found `{found}` instead", range) { }
}

/// <summary>
///     Thrown when a parameter type is expected, but another token type is encountered
/// </summary>
public class ExpectParameterTypeException : ParserException
{
	public ExpectParameterTypeException(string found, Range range) : base($"Expected parameter type, but found `{found}` instead", range) { }
}

/// <summary>
///     Thrown when a left bracket ( <c>[</c> ) is expected, but another token type is encountered
/// </summary>
public class ExpectLeftBracketException : ParserException
{
	public ExpectLeftBracketException(string found, Range range)
		: base($"Expected `[` after map keyword, but found `{found}` instead", range) { }
}

/// <summary>
///     Thrown when an unexpected type is encountered
/// </summary>
public class UnexpectedTypeException : ParserException
{
	public UnexpectedTypeException(string found, Range range) : base($"Type `{found}` was unexpected", range) { }
}

/// <summary>
///     Thrown when the <c>pub</c> keyword is followed by an invalid token. The <c>pub</c> keyword may precede the
///     following keywords:
///     <code>
/// fn
/// class
/// interface
/// struct
/// </code>
/// </summary>
public class InvalidTokenAfterPubKeywordException : ParserException
{
	public InvalidTokenAfterPubKeywordException(string found, Range range)
		: base($"Token `{found}` may not come directly after `pub` keyword", range) { }
}

/// <summary>
///     Thrown when an identifier is expected, but another token type is encountered
/// </summary>
public class ExpectIdentifierException : ParserException
{
	public ExpectIdentifierException(string found, Range range) : base($"Expected identifier, but found `{found}` instead", range) { }
}

/// <summary>
///     Thrown when a semicolon is expected, but another token type is encountered
/// </summary>
public class ExpectSemicolonException : ParserException
{
	public ExpectSemicolonException(string found, Range range) : base($"Expected semicolon, but found `{found}` instead", range) { }
}

/// <summary>
///     Thrown when the <c>mut</c> keyword is followed by an invalid token. The <c>mut</c> keyword may only be followed by
///     an identifier in a <c>let</c> statement
/// </summary>
public class InvalidTokenAfterMutKeywordException : ParserException
{
	public InvalidTokenAfterMutKeywordException(string found, Range range)
		: base($"Token `{found}` may not come directly after mut keyword", range) { }
}

/// <summary>
///     Thrown when a left parenthesis is expected, but another token type is encountered
/// </summary>
public class ExpectLeftParenException : ParserException
{
	public ExpectLeftParenException(string found, Range range) : base($"Expected left paren, but found `{found}` instead", range) { }
}

/// <summary>
///     Thrown when a right parenthesis is expected, but another token type is encountered
/// </summary>
public class ExpectRightParenException : ParserException
{
	public ExpectRightParenException(string found, Range range) : base($"Expected right paren, but found `{found}` instead", range) { }
}

/// <summary>
///     Thrown when a left brace ( <c>{</c> ) is expected, but another token type is encountered
/// </summary>
public class ExpectLeftBraceException : ParserException
{
	public ExpectLeftBraceException(string found, Range range) : base($"Expected left brace, but found `{found}` instead", range) { }
}

/// <summary>
///     Thrown when a right brace ( <c>}</c> ) is expected, but another token type is encountered
/// </summary>
public class ExpectRightBraceException : ParserException
{
	public ExpectRightBraceException(string found, Range range) : base($"Expected right brace, but found `{found}`", range) { }
}

/// <summary>
///     Thrown when the <c>in</c> keyword is expected, but another token type is encountered
/// </summary>
public class ExpectInKeywordException : ParserException
{
	public ExpectInKeywordException(string found, Range range) : base($"Expected `in` keyword, but found `{found}` instead", range) { }
}

/// <summary>
///     Thrown when the <c>defer</c> keyword is used before a token type other than a function call
/// </summary>
public class CanOnlyDeferFunctionCallException : ParserException
{
	public CanOnlyDeferFunctionCallException(Range range) : base("Can only defer function call", range) { }
}

/// <summary>
///     Thrown when a colon is expected, but another token type is encountered
/// </summary>
public class ExpectColonException : ParserException
{
	public ExpectColonException(string found, Range range) : base($"Expected `:`, but found `{found}` instead", range) { }
}

/// <summary>
///     Thrown when a colon-equal ( <c>:=</c> ) is expected, but another token type is encountered
/// </summary>
public class ExpectColonEqualException : ParserException
{
	public ExpectColonEqualException(string found, Range range) : base($"Expected `:=`, but found `{found}` instead", range) { }
}

/// <summary>
///     Thrown when the target in an assignment expression is unable to be assigned a new value
/// </summary>
public class InvalidAssignmentTargetException : ParserException
{
	public InvalidAssignmentTargetException(Range range) : base("Invalid assignment target", range) { }
}

/// <summary>
///     Thrown when a scope contains one or more lines of code after a <c>return</c> statement
/// </summary>
public class UnreachableCodeException : ParserException
{
	public UnreachableCodeException(Range range) : base("Unreachable code", range) { }
}

/// <summary>
///     Thrown when a property name is expected, but another token type is encountered
/// </summary>
public class ExpectPropertyNameException : ParserException
{
	public ExpectPropertyNameException(string found, Range range) : base($"Expected property name, but found `{found}` instead", range) { }
}

/// <summary>
///     Thrown when an int literal is expected, but another token type is encountered
/// </summary>
public class ExpectIntLiteralException : ParserException
{
	public ExpectIntLiteralException(string found, Range range) : base($"Expected int literal, but found `{found}` instead", range) { }
}

/// <summary>
///     Thrown when a right bracket ( <c>]</c> ) is expected, but another token type is encountered
/// </summary>
public class ExpectRightBracketException : ParserException
{
	public ExpectRightBracketException(string found, Range range) : base($"Expected `]`, but found `{found}` instead", range) { }
}

/// <summary>
///     Thrown when a comma is expected, but another token type is encountered
/// </summary>
public class ExpectCommaException : ParserException
{
	public ExpectCommaException(string found, Range range) : base($"Expected `,`, but found `{found}` instead", range) { }
}

/// <summary>
///     Thrown when an expression is expected, but a statement is encountered
/// </summary>
public class ExpectExpressionException : ParserException
{
	public ExpectExpressionException(Range range) : base("Expected expression", range) { }
}

/// <summary>
///     Thrown when a parameter's default value is defined with a non-literal value (i.e. a variable expression, etc.)
/// </summary>
public class ParameterDefaultValueMustBeALiteralException : ParserException
{
	public ParameterDefaultValueMustBeALiteralException(Range range) : base("Parameter default value must be a literal", range) { }
}

/// <summary>
///     Thrown when an index is of an invalid type
/// </summary>
public class InvalidIndexTypeException : ParserException
{
	public InvalidIndexTypeException(Range range) : base("Invalid index type", range) { }
}

/// <summary>
///     Thrown when a postfix index accessor is empty (i.e. <c>list[]</c> is not valid. In order to to access all items in
///     a list, you would use <c>list[:]</c> instead)
/// </summary>
public class PostfixIndexCannotBeEmptyException : ParserException
{
	public PostfixIndexCannotBeEmptyException(Range range) : base("Postfix index cannot be empty", range) { }
}

/// <summary>
///     Thrown when a function signature is expected, but another token type is encountered
/// </summary>
public class ExpectFunctionSignatureException : ParserException
{
	public ExpectFunctionSignatureException(Range range) : base("Expected function signature", range) { }
}

/// <summary>
///     Thrown when a newline is expected, but another token type is encountered
/// </summary>
public class ExpectNewLineException : ParserException
{
	public ExpectNewLineException(string found, Range range) : base($"Expected '\n', but found '{found}'", range) { }
}

/// <summary>
///     Thrown when an Aura source file does not begin with a <c>mod</c> declaration. The <c>mod</c> declaration should be
///     the first non-comment token in the source file. As an example, both this:
///     <code>mod main</code>
///     and this:
///     <code>
/// 		// This is a comment
/// 		mod main
/// 		</code>
///     are valid, but this is not:
///     <code>
/// 		let x = 5
/// 		mod main
/// 		</code>
/// </summary>
public class FileMustBeginWithModStmtException : ParserException
{
	public FileMustBeginWithModStmtException(Range range) : base("Cannot omit `mod` statement", range) { }
}

/// <summary>
///     Thrown if the <c>check</c> keyword is used in front of an AST node that is not a call expression
/// </summary>
public class CanOnlyCheckFunctionCallException : ParserException
{
	public CanOnlyCheckFunctionCallException(Range range) : base("Can only check function call", range) { }
}
