using System.Globalization;
using AuraLang.AST;
using AuraLang.Exceptions.Parser;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using AuraChar = AuraLang.Types.Char;
using AuraString = AuraLang.Types.String;

namespace AuraLang.Parser;

public class AuraParser
{
	private readonly List<Tok> _tokens;
	private int _index;
	private readonly ParserExceptionContainer _exContainer;
	private string FilePath { get; }
	private const int MAX_PARAMS = 255;

	public AuraParser(List<Tok> tokens, string filePath)
	{
		_tokens = tokens;
		_index = 0;
		FilePath = filePath;
		_exContainer = new ParserExceptionContainer(filePath);
	}

	/// <summary>
	/// Parses each token in <see cref="_tokens"/> and produces an untyped AST
	/// </summary>
	/// <returns></returns>
	public List<IUntypedAuraStatement> Parse()
	{
		var statements = new List<IUntypedAuraStatement>();
		while (!IsAtEnd())
		{
			try
			{
				statements.Add(Declaration());
			}
			catch (ParserException ex)
			{
				_exContainer.Add(ex);
				Synchronize();
			}
		}

		if (!_exContainer.IsEmpty()) throw _exContainer;
		return statements;
	}

	/// <summary>
	/// Checks if the next token matches any of the supplied token types. If so, the parser advances past the matched
	/// token and returns true. Otherwise, false is returned.
	/// </summary>
	/// <param name="tokTypes">A list of acceptable token types for the next token</param>
	/// <returns>A boolean indicating if the next token's type matches any of the <see cref="tokTypes"/></returns>
	private bool Match(params TokType[] tokTypes)
	{
		return tokTypes.Any(tt =>
		{
			if (!Check(tt)) return false;
			Advance();
			return true;
		});
	}

	/// <summary>
	/// Checks if the next token matches the supplied token type. If so, it advances past the matched token
	/// and returns it. Otherwise, the supplied exception is thrown.
	/// </summary>
	/// <param name="tokType">The type that the next token should match</param>
	/// <param name="ex">The exception to throw if the next token does not match <c>tokType</c></param>
	/// <returns>The next token, if it matched the supplied token type</returns>
	private Tok Consume(TokType tokType, ParserException ex)
	{
		if (Check(tokType))
		{
			return Advance();
		}

		throw ex;
	}

	/// <summary>
	/// Checks if the next token matches any of the supplied token types. If so, it advances past the matched
	/// token and returns it. Otherwise, the supplied exception is thrown.
	/// </summary>
	/// <param name="ex">The exception to throw if the next token does not match <c>tokType</c></param>
	/// <param name="tokTypes">The types that the next token should match</param>
	/// <returns>The next token, if it matched any of the supplied token types></returns>
	private Tok ConsumeMultiple(ParserException ex, params TokType[] tokTypes)
	{
		foreach (var tokType in tokTypes)
		{
			if (Check(tokType))
			{
				return Advance();
			}
		}

		throw ex;
	}

	/// <summary>
	/// Returns a boolean indicating if the next token matches the supplied type
	/// </summary>
	/// <param name="tokType">The type that the next token will be compared against</param>
	/// <returns>A boolean indicating if the next token matches <c>tokType</c></returns>
	private bool Check(TokType tokType)
	{
		if (IsAtEnd()) return false;
		return Peek().Typ == tokType;
	}

	/// <summary>
	/// Increments the parser's index counter
	/// </summary>
	/// <returns>The token just advanced past</returns>
	private Tok Advance()
	{
		if (!IsAtEnd()) _index++;
		return Previous();
	}

	/// <summary>
	/// Checks if we've reached the end of the parser's tokens
	/// </summary>
	/// <returns>A boolean indicating if the end of the source has been reached</returns>
	private bool IsAtEnd() => Peek().Typ == TokType.Eof;

	/// <summary>
	/// Returns the current token without advancing the counter
	/// </summary>
	/// <returns>The current token</returns>
	private Tok Peek() => _tokens[_index];

	/// <summary>
	/// Returns the next token without advancing the counter
	/// </summary>
	/// <returns>The next token</returns>
	private Tok PeekNext() => _tokens[_index + 1];

	/// <summary>
	/// Returns the previous token without advancing the counter
	/// </summary>
	/// <returns>The previous token</returns>
	private Tok Previous() => _tokens[_index - 1];

	/// <summary>
	/// Attempts to reset the <c>_index</c> at the start of the next statement
	/// </summary>
	private void Synchronize()
	{
		Advance();
		while (!IsAtEnd())
		{
			if (Previous().Typ == TokType.Semicolon) return;
			switch (Peek().Typ)
			{
				case TokType.Class:
				case TokType.Fn:
				case TokType.Let:
				case TokType.For:
				case TokType.ForEach:
				case TokType.If:
				case TokType.While:
				case TokType.Return:
					return;
			}

			Advance();
		}
	}

	private List<Param> ParseParameters()
	{
		var paramz = new List<Param>();

		if (!Check(TokType.RightParen))
		{
			while (true)
			{
				// Max of 255 parameters
				if (paramz.Count >= MAX_PARAMS) throw new TooManyParametersException(MAX_PARAMS, Peek().Line);
				// Parse parameter
				var param = ParseParameter();
				paramz.Add(param);

				if (Check(TokType.RightParen)) return paramz;
				Consume(TokType.Comma, new ExpectEitherRightParenOrCommaAfterParam(Peek().Value, Peek().Line));
			}
		}

		return paramz;
	}

	private AuraType TypeTokenToType(Tok tok)
	{
		switch (tok.Typ)
		{
			case TokType.Int:
				return new Int();
			case TokType.Float:
				return new Float();
			case TokType.String:
				return new AuraString();
			case TokType.Bool:
				return new Bool();
			case TokType.LeftBracket:
				var kind = TypeTokenToType(Advance());
				Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Value, tok.Line));
				return new List(kind);
			case TokType.Any:
				return new Any();
			case TokType.Char:
				return new AuraChar();
			case TokType.Fn:
				// Check if there is a name -- if not, we are parsing an anonymous function
				Tok? name = Match(TokType.Identifier) ? Previous() : null;
				// Parse parameters
				Consume(TokType.LeftParen, new ExpectLeftParenException(Peek().Value, tok.Line));
				var paramz = ParseParameters();
				Consume(TokType.RightParen, new ExpectRightParenException(Peek().Value, tok.Line));
				// Parse return type (if there is one)
				var returnType = Match(TokType.Arrow)
					? TypeTokenToType(Advance())
					: new Nil();

				var f = new Function(paramz, returnType);
				return name is null ? f : new NamedFunction(name.Value.Value, Visibility.Private, f);
			case TokType.Identifier:
				return new Unknown(Previous().Value);
			case TokType.Map:
				Consume(TokType.LeftBracket, new ExpectLeftBracketAfterMapKeywordException(Peek().Value, tok.Line));
				var key = TypeTokenToType(Advance());
				Consume(TokType.Colon, new ExpectColonException(Peek().Value, tok.Line));
				var value = TypeTokenToType(Advance());
				Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Value, tok.Line));
				return new Map(key, value);
			default:
				throw new UnexpectedTypeException(tok.Value, tok.Line);
		}
	}

	private ParamType ParseParameterType()
	{
		// Parse variadic
		var variadic = false;
		if (Match(TokType.Dot))
		{
			Consume(TokType.Dot, new VariadicSignifierMustHaveThreeDots(Peek().Line));
			Consume(TokType.Dot, new VariadicSignifierMustHaveThreeDots(Peek().Line));
			variadic = true;
		}

		// Parse type
		if (!Match(TokType.Int, TokType.Float, TokType.String, TokType.Bool, TokType.LeftBracket, TokType.Any,
				TokType.Char, TokType.Fn, TokType.Identifier, TokType.Map))
			throw new ExpectParameterTypeException(Peek().Value, Peek().Line);
		var pt = TypeTokenToType(Previous());
		// Parse default value
		if (!Match(TokType.Equal)) return new ParamType(pt, variadic, null);
		var defaultValue = Expression();
		if (defaultValue is not ILiteral lit)
			throw new ParameterDefaultValueMustBeALiteralException(Peek().Line);
		return new ParamType(pt, variadic, lit);
	}

	private Param ParseParameter()
	{
		// Consume the parameter name
		var name = Consume(TokType.Identifier, new ExpectParameterNameException(Peek().Line));
		// Consume colon separator
		Consume(TokType.Colon, new ExpectColonAfterParameterName(Peek().Value, Peek().Line));
		var pt = ParseParameterType();
		return new Param(name, pt);
	}

	private IUntypedAuraStatement Declaration()
	{
		if (Match(TokType.Pub))
		{
			if (Match(TokType.Class)) return ClassDeclaration(Visibility.Public);
			if (Match(TokType.Fn)) return NamedFunction(FunctionType.Function, Visibility.Public);
			if (Match(TokType.Interface)) return InterfaceDeclaration(Visibility.Public);

			throw new InvalidTokenAfterPubKeywordException(Peek().Value, Peek().Line);
		}

		if (Match(TokType.Interface)) return InterfaceDeclaration(Visibility.Private);
		if (Match(TokType.Class)) return ClassDeclaration(Visibility.Private);
		if (Check(TokType.Fn) && PeekNext().Typ == TokType.Identifier)
		{
			// Since we only check for the `fn` keyword, we need to advance past it here before entering the NamedFunction() call
			Advance();
			return NamedFunction(FunctionType.Function, Visibility.Private);
		}

		if (Match(TokType.Let)) return LetDeclaration();
		if (Match(TokType.Mod)) return ModDeclaration();
		if (Match(TokType.Import))
		{
			var line = Previous().Line;
			var tok = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Line));
			// Check if the import has an alias
			if (Match(TokType.As))
			{
				var alias = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Line));
				// Parse trailing semicolon
				Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));
				return new UntypedImport(tok, alias, line);
			}

			// Parse trailing semicolon
			Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));
			return new UntypedImport(tok, null, line);
		}

		if (Match(TokType.Mut))
		{
			// The only statement that can being with `mut` is a short let declaration (i.e. `mut s := "Hello world")
			if (Peek().Typ is TokType.Identifier && PeekNext().Typ is TokType.ColonEqual)
			{
				return ShortLetDeclaration(true);
			}

			throw new InvalidTokenAfterMutKeywordException(Peek().Value, Peek().Line);
		}

		return Statement();
	}

	private IUntypedAuraStatement InterfaceDeclaration(Visibility pub)
	{
		var line = Previous().Line;
		// Consume the interface name
		var name = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Line));
		Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Line));
		// Parse the interface's methods
		var methods = new List<NamedFunction>();
		while (!IsAtEnd() && !Check(TokType.RightBrace))
		{
			if (Match(TokType.Comment)) Advance(); // Check for comment and advance past semicolon, if necessary
			var typ = TypeTokenToType(Advance());
			if (typ is not NamedFunction f) throw new ExpectFunctionSignatureException(Peek().Line);
			methods.Add(f);
			Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));
		}

		Consume(TokType.RightBrace, new ExpectRightBraceException(Peek().Value, Peek().Line));
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));

		return new UntypedInterface(name, methods, pub, line);
	}

	private IUntypedAuraStatement ClassDeclaration(Visibility pub)
	{
		var line = Previous().Line;
		// Consume the class name
		var name = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Line));
		Consume(TokType.LeftParen, new ExpectLeftParenException(Peek().Value, Peek().Line));
		// Parse parameters
		var paramz = ParseParameters();
		Consume(TokType.RightParen, new ExpectRightParenException(Peek().Value, Peek().Line));
		// Check if class implements an interface
		var interfaceNames = Match(TokType.Colon) ? ParseImplementingInterfaces() : new List<Tok>();
		// Parse the class's methods
		Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Line));
		var body = ParseClassBody();

		Consume(TokType.RightBrace, new ExpectRightBraceException(Peek().Value, Peek().Line));
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));

		return new UntypedClass(name, paramz, body, pub, interfaceNames, line);
	}

	private List<Tok> ParseImplementingInterfaces()
	{
		var interfaces = new List<Tok>();
		while (!Check(TokType.LeftBrace))
		{
			var interfaceName = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Line));
			if (!Check(TokType.LeftBrace)) Consume(TokType.Comma, new ExpectCommaException(Peek().Value, Peek().Line));
			interfaces.Add(interfaceName);
		}

		return interfaces;
	}

	private List<IUntypedAuraStatement> ParseClassBody()
	{
		var body = new List<IUntypedAuraStatement>();
		while (!IsAtEnd() && !Check(TokType.RightBrace))
		{
			// Parse comments, if necessary
			while (Match(TokType.Comment))
			{
				body.Add(Comment());
			}

			// Methods can be public or private, just like regular functions
			var pub = Match(TokType.Pub) ? Visibility.Public : Visibility.Private;

			var f = ParseClassMethod(pub);
			body.Add(f);
		}

		return body;
	}

	private UntypedNamedFunction ParseClassMethod(Visibility pub)
	{
		Consume(TokType.Fn, new InvalidTokenAfterPubKeywordException(Peek().Value, Peek().Line));
		return NamedFunction(FunctionType.Method, pub);
	}

	private IUntypedAuraStatement ModDeclaration()
	{
		var line = Previous().Line;
		var val = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Line));
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));

		return new UntypedMod(val, line);
	}

	private IUntypedAuraStatement Statement()
	{
		if (Match(TokType.For)) return ForStatement();
		if (Match(TokType.ForEach)) return ForEachStatement();
		if (Match(TokType.Return)) return ReturnStatement();
		if (Match(TokType.While)) return WhileStatement();
		if (Match(TokType.Defer)) return DeferStatement();
		if (Peek().Typ is TokType.Identifier && PeekNext().Typ is TokType.ColonEqual) return ShortLetDeclaration(false);
		if (Match(TokType.Comment)) return Comment();
		if (Match(TokType.Continue)) return new UntypedContinue(Previous().Line);
		if (Match(TokType.Break)) return new UntypedBreak(Previous().Line);
		if (Match(TokType.Yield)) return Yield();
		if (Match(TokType.Newline)) return new UntypedNewLine(Peek().Line);

		var line = Peek().Line;
		// If the statement doesn't begin with any of the Aura statement identifiers, parse it as an expression and
		// wrap it in an Expression Statement
		var expr = Expression();
		// Consume trailing semicolon
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));

		return new UntypedExpressionStmt(expr, line);
	}

	private IUntypedAuraStatement ForStatement()
	{
		var line = Previous().Line;

		// Parse initializer
		IUntypedAuraStatement? initializer;
		if (Match(TokType.Semicolon)) initializer = null;
		else if (Match(TokType.Let)) initializer = LetDeclaration();
		else if (Peek().Typ is TokType.Identifier && PeekNext().Typ is TokType.ColonEqual)
			initializer = ShortLetDeclaration(false);
		else initializer = ExpressionStatement();

		// Parse condition
		IUntypedAuraExpression? condition = null;
		if (!Check(TokType.Semicolon)) condition = Expression();
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));

		// Parse increment
		IUntypedAuraExpression? increment = null;
		if (!Check(TokType.LeftBrace)) increment = Expression();
		Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Line));

		// Parse body
		var body = new List<IUntypedAuraStatement>();
		while (!IsAtEnd() && !Check(TokType.RightBrace)) body.Add(Declaration());
		Consume(TokType.RightBrace, new ExpectRightBraceException(Peek().Value, Peek().Line));
		// Consume trailing semicolon
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));

		return new UntypedFor(initializer, condition, increment, body, line);
	}

	private IUntypedAuraStatement ForEachStatement()
	{
		var line = Previous().Line;
		// The first identifier will be attached to the current item on each iteration
		var eachName = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Line));
		Consume(TokType.In, new ExpectInKeywordException(Peek().Value, Peek().Line));
		// Consume the iterable
		var iter = Expression();
		Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Line));
		// Parse body
		var body = new List<IUntypedAuraStatement>();
		while (!IsAtEnd() && !Check(TokType.RightBrace)) body.Add(Declaration());
		Consume(TokType.RightBrace, new ExpectRightBraceException(Peek().Value, Peek().Line));
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));

		return new UntypedForEach(eachName, iter, body, line);
	}

	private UntypedReturn ReturnStatement()
	{
		var line = Previous().Line;

		// The return keyword does not need to be followed by an expression, in which case the return statement
		// will return a value of `nil`
		IUntypedAuraExpression? value = null;
		if (!Check(TokType.Semicolon)) value = Expression();
		// Parse the trailing semicolon
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));

		return new UntypedReturn(value, line);
	}

	private IUntypedAuraStatement WhileStatement()
	{
		var line = Previous().Line;

		// Parse condition
		var condition = Expression();
		Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Line));
		// Parse body
		var body = new List<IUntypedAuraStatement>();
		while (!IsAtEnd() && !Check(TokType.RightBrace))
		{
			var stmt = Statement();
			body.Add(stmt);
		}

		Consume(TokType.RightBrace, new ExpectRightBraceException(Peek().Value, Peek().Line));
		// Parse trailing semicolon
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));
		return new UntypedWhile(condition, body, line);
	}

	private UntypedDefer DeferStatement()
	{
		var line = Previous().Line;

		// Parse the expression to be deferred
		var expression = Expression();
		// Make sure the deferred expression is a function call
		if (expression is not IUntypedAuraCallable callableExpr)
			throw new CanOnlyDeferFunctionCallException(Peek().Line);

		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));
		return new UntypedDefer(callableExpr, line);
	}

	private IUntypedAuraStatement LetDeclaration()
	{
		var line = Previous().Line;
		// Check if the variable is declared as mutable
		var isMutable = Match(TokType.Mut);
		// Parse the variable's name
		var name = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Line));
		// When declaring a new variable with the full `let` syntax, the variable's name must be followed
		// by a colon and the variable's type
		Consume(TokType.Colon, new ExpectColonException(Peek().Value, Peek().Line));
		var nameType = TypeTokenToType(Advance());
		// Parse the variable's initializer (if there is one)
		IUntypedAuraExpression? initializer = null;
		if (Match(TokType.Equal)) initializer = Expression();
		// Parse the trailing semicolon
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));

		return new UntypedLet(name, nameType, isMutable, initializer, line);
	}

	private IUntypedAuraStatement ShortLetDeclaration(bool isMutable)
	{
		var line = Peek().Line;
		// Parse the variable's name
		var name = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Line));
		Consume(TokType.ColonEqual, new ExpectColonEqualException(Peek().Value, Peek().Line));
		// Parse the variable's initializer
		var initializer = Expression();
		// Consume trailing semicolon
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));

		return new UntypedLet(name, null, isMutable, initializer, line);
	}

	private IUntypedAuraStatement Comment()
	{
		var text = Previous();
		// Consume the trailing semicolon
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));

		return new UntypedComment(text, text.Line);
	}

	private IUntypedAuraStatement Yield()
	{
		var value = Expression();
		// Consume the trailing semicolon
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));

		return new UntypedYield(value, Previous().Line);
	}

	private IUntypedAuraStatement ExpressionStatement()
	{
		var line = Peek().Line;
		// Parse expression
		var expr = Expression();
		// Consume the trailing semicolon
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));

		return new UntypedExpressionStmt(expr, line);
	}

	private UntypedNamedFunction NamedFunction(FunctionType kind, Visibility pub)
	{
		var line = Previous().Line;
		// Parse the function's name
		var name = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Line));
		Consume(TokType.LeftParen, new ExpectLeftParenException(Peek().Value, Peek().Line));
		// Parse the function's parameters
		var paramz = ParseParameters();
		Consume(TokType.RightParen, new ExpectRightParenException(Peek().Value, Peek().Line));
		// Parse the function's return type
		Tok? returnType = Match(TokType.Arrow) ? Advance() : null;
		Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Line));
		// Parse body
		var body = Block();
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Line));

		return new UntypedNamedFunction(name, paramz, body, returnType, pub, line);
	}

	private UntypedAnonymousFunction AnonymousFunction()
	{
		var line = Previous().Line;

		Consume(TokType.LeftParen, new ExpectLeftParenException(Peek().Value, Peek().Line));
		// Parse function's parameters
		var paramz = ParseParameters();
		Consume(TokType.RightParen, new ExpectRightParenException(Peek().Value, Peek().Line));
		// Parse function's return type
		Tok? returnType = Match(TokType.Arrow) ? Advance() : null;
		Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Line));
		// Parse body
		var body = Block();

		return new UntypedAnonymousFunction(paramz, body, returnType, line);
	}

	private IUntypedAuraExpression Expression()
	{
		var expr = Assignment();
		return Match(TokType.Is) ? Is(expr) : expr;
	}

	private IUntypedAuraExpression Assignment()
	{
		var expression = Or();
		if (Match(TokType.Equal))
		{
			var value = Assignment();
			if (expression is UntypedVariable v) return new UntypedAssignment(v.Name, value, value.Line);
			if (expression is UntypedGet g) return new UntypedSet(g.Obj, g.Name, value, value.Line);
			throw new InvalidAssignmentTargetException(Peek().Line);
		}
		else if (Match(TokType.PlusPlus))
		{
			var variable = expression as UntypedVariable;
			if (variable is not null)
				return new UntypedAssignment(variable.Name,
					new UntypedBinary(variable, new Tok(TokType.Plus, "+", variable.Line),
						new IntLiteral(1, variable.Line), variable.Line), variable.Line);
		}
		else if (Match(TokType.MinusMinus))
		{
			var variable = expression as UntypedVariable;
			if (variable is not null)
				return new UntypedAssignment(variable.Name,
					new UntypedBinary(variable, new Tok(TokType.Minus, "-", variable.Line),
						new IntLiteral(1, variable.Line), variable.Line), variable.Line);
		}

		return expression;
	}

	private IUntypedAuraExpression IfExpr()
	{
		var line = Previous().Line;
		// Parse the condition
		var condition = Expression();
		Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Line));
		// Parse `then` branch
		var thenBranch = Block();
		// Parse the `else` branch
		if (Match(TokType.Else))
		{
			if (Match(TokType.If))
			{
				var elseBranch = IfExpr();
				return new UntypedIf(condition, thenBranch, elseBranch, line);
			}
			else
			{
				Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Line));
				var elseBranch = Block();
				return new UntypedIf(condition, thenBranch, elseBranch, line);
			}
		}

		return new UntypedIf(condition, thenBranch, null, line);
	}

	private UntypedBlock Block()
	{
		var line = Previous().Line;
		var statements = new List<IUntypedAuraStatement>();

		while (!IsAtEnd() && !Check(TokType.RightBrace))
		{
			var decl = Declaration();
			// If the statement is a return statement, it should be the last line of the block.
			// Otherwise, any lines after it will be unreachable.
			if (decl is UntypedReturn)
			{
				if (!Check(TokType.RightBrace)) throw new UnreachableCodeException(Peek().Line);
			}

			statements.Add(decl);
		}

		Consume(TokType.RightBrace, new ExpectRightBraceException(Peek().Value, Peek().Line));

		return new UntypedBlock(statements, line);
	}

	private IUntypedAuraExpression Or()
	{
		var expression = And();
		while (Match(TokType.Or))
		{
			var op = Previous();
			var right = And();
			expression = new UntypedLogical(expression, op, right, expression.Line);
		}

		return expression;
	}

	private IUntypedAuraExpression And()
	{
		var expression = Equality();
		while (Match(TokType.And))
		{
			var op = Previous();
			var right = Equality();
			expression = new UntypedLogical(expression, op, right, expression.Line);
		}

		return expression;
	}

	private IUntypedAuraExpression Equality()
	{
		var expression = Comparison();
		while (Match(TokType.BangEqual, TokType.EqualEqual))
		{
			var op = Previous();
			var right = Comparison();
			expression = new UntypedLogical(expression, op, right, expression.Line);
		}

		return expression;
	}

	private IUntypedAuraExpression Comparison()
	{
		var expression = Term();
		while (Match(TokType.Greater, TokType.GreaterEqual, TokType.Less, TokType.LessEqual))
		{
			var op = Previous();
			var right = Term();
			expression = new UntypedLogical(expression, op, right, expression.Line);
		}

		return expression;
	}

	private IUntypedAuraExpression Term()
	{
		var expression = Factor();
		while (Match(TokType.Minus, TokType.MinusEqual, TokType.Plus, TokType.PlusEqual))
		{
			var op = Previous();
			var right = Factor();
			expression = new UntypedBinary(expression, op, right, expression.Line);
		}

		return expression;
	}

	private IUntypedAuraExpression Factor()
	{
		var expression = Unary();
		while (Match(TokType.Slash, TokType.SlashEqual, TokType.Star, TokType.StarEqual))
		{
			var op = Previous();
			var right = Unary();
			expression = new UntypedBinary(expression, op, right, expression.Line);
		}

		return expression;
	}

	private IUntypedAuraExpression Unary()
	{
		if (Match(TokType.Bang, TokType.Minus))
		{
			var op = Previous();
			var right = Unary();
			return new UntypedUnary(op, right, op.Line);
		}

		return Call();
	}

	private IUntypedAuraExpression Call()
	{
		// Parse the callee
		var expression = Primary();
		while (true)
		{
			if (!IsAtEnd() && Match(TokType.LeftParen)) expression = FinishCall(expression);
			else if (!IsAtEnd() && Match(TokType.Dot))
			{
				var name = ConsumeMultiple(new ExpectPropertyNameException(Peek().Value, Peek().Line), TokType.Identifier,
					TokType.IntLiteral);
				expression = new UntypedGet(expression, name, expression.Line);
			}
			else break;
		}

		return expression;
	}

	private IUntypedAuraExpression FinishCall(IUntypedAuraExpression callee)
	{
		var line = Previous().Line;
		var arguments = new List<(Tok?, IUntypedAuraExpression)>();
		if (!Check(TokType.RightParen))
		{
			while (true)
			{
				// Function declarations have a max of 255 arguments, so function calls have the same limit
				if (arguments.Count >= MAX_PARAMS) throw new TooManyParametersException(MAX_PARAMS, Peek().Line);

				Tok? tag = null;
				if (PeekNext().Typ is TokType.Colon)
				{
					tag = Advance();
					Consume(TokType.Colon, new ExpectColonException(Peek().Value, Peek().Line));
				}

				var expression = Expression();
				arguments.Add((tag, expression));
				if (Check(TokType.RightParen)) break;
				Match(TokType.Comma);
			}
		}

		Consume(TokType.RightParen, new ExpectRightParenException(Peek().Value, Peek().Line));
		return new UntypedCall((IUntypedAuraCallable)callee, arguments, line);
	}

	private UntypedIs Is(IUntypedAuraExpression expr)
	{
		// Parse the expected type's token
		var expected = Advance();
		return new UntypedIs(expr, expected, expr.Line);
	}

	private IUntypedAuraExpression Primary()
	{
		var line = Peek().Line;

		if (Match(TokType.False)) return new BoolLiteral(false, line);
		if (Match(TokType.True)) return new BoolLiteral(true, line);
		if (Match(TokType.Nil)) return new UntypedNil(line);
		if (Match(TokType.StringLiteral)) return new StringLiteral(Previous().Value, line);
		if (Match(TokType.CharLiteral)) return new CharLiteral(Previous().Value[0], line);
		if (Match(TokType.IntLiteral))
		{
			return new IntLiteral(
				int.Parse(Previous().Value),
				line);
		}

		if (Match(TokType.FloatLiteral))
		{
			return new FloatLiteral(
				double.Parse(Previous().Value, CultureInfo.InvariantCulture),
				line);
		}

		if (Match(TokType.This)) return new UntypedThis(Previous(), line);
		if (Match(TokType.Identifier))
		{
			return ParseIdentifier(Previous());
		}

		if (Match(TokType.If))
		{
			return IfExpr();
		}

		if (Match(TokType.LeftParen))
		{
			var expression = Expression();
			Consume(TokType.RightParen, new ExpectRightParenException(Peek().Value, Peek().Line));
			return new UntypedGrouping(expression, line);
		}

		if (Match(TokType.LeftBrace))
		{
			return Block();
		}

		if (Match(TokType.LeftBracket))
		{
			// Parse list's type
			var typ = TypeTokenToType(Advance());
			Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Value, Peek().Line));
			Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Line));

			var items = new List<IUntypedAuraExpression>();
			while (!Match(TokType.RightBrace))
			{
				var expr = Expression();
				items.Add(expr);
				if (!Check(TokType.RightBrace))
				{
					Consume(TokType.Comma, new ExpectCommaException(Peek().Value, Peek().Line));
				}
			}

			var listExpr = new ListLiteral<IUntypedAuraExpression>(items, typ, line);
			return Match(TokType.LeftBracket) ? ParseGetAccess(listExpr) : listExpr;
		}

		if (Match(TokType.Fn))
		{
			return AnonymousFunction();
		}

		if (Match(TokType.Map))
		{
			// Parse map's type signature
			Consume(TokType.LeftBracket, new ExpectLeftBracketAfterMapKeywordException(Peek().Value, Peek().Line));
			var keyType = TypeTokenToType(Advance());
			Consume(TokType.Colon, new ExpectColonException(Peek().Value, Peek().Line));
			var valueType = TypeTokenToType(Advance());
			Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Value, Peek().Line));
			Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Line));

			var d = new Dictionary<IUntypedAuraExpression, IUntypedAuraExpression>();
			while (!Match(TokType.RightBrace))
			{
				var key = Expression();
				Consume(TokType.Colon, new ExpectColonException(Peek().Value, Peek().Line));
				var value = Expression();
				Consume(TokType.Comma, new ExpectCommaException(Peek().Value, Peek().Line));
				Consume(TokType.Semicolon,
					new ExpectSemicolonException(Peek().Value, Peek().Line)); // TODO don't add implicit semicolon after map items
				d[key] = value;
			}

			var mapExpr = new MapLiteral<IUntypedAuraExpression, IUntypedAuraExpression>(d, keyType, valueType, line);
			return Match(TokType.LeftBracket) ? ParseSingleGetAccess(mapExpr) : mapExpr;
		}

		throw new ExpectExpressionException(Peek().Line);
	}

	private IUntypedAuraExpression ParseIdentifier(Tok iden)
	{
		if (!Match(TokType.LeftBracket)) return new UntypedVariable(iden, iden.Line);
		return ParseGetAccess(new UntypedVariable(iden, iden.Line));
	}

	private IUntypedAuraExpression ParseSingleGetAccess(IUntypedAuraExpression obj)
	{
		var line = obj.Line;
		var index = ParseIndex();
		Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Value, line));
		return new UntypedGetIndex(obj, index, line);
	}

	private IUntypedAuraExpression ParseGetAccess(IUntypedAuraExpression obj)
	{
		var line = obj.Line;

		if (Match(TokType.Colon))
		{
			var upper = Match(TokType.RightBracket) ? new IntLiteral(-1, line) : ParseIndex();
			Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Value, obj.Line));
			return new UntypedGetIndexRange(obj, new IntLiteral(0, line), upper,
				line);
		}

		if (!Match(TokType.RightBracket))
		{
			var lower = ParseIndex();
			if (Match(TokType.RightBracket)) return new UntypedGetIndex(obj, lower, line);
			Consume(TokType.Colon, new ExpectColonException(Peek().Value, line));

			IUntypedAuraExpression upper;
			if (Match(TokType.RightBracket)) upper = new IntLiteral(-1, line);
			else
			{
				upper = ParseIndex();
				Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Value, line));
			}

			return new UntypedGetIndexRange(obj, lower, upper, line);
		}

		throw new PostfixIndexCannotBeEmptyException(line);
	}

	private IUntypedAuraExpression ParseIndex()
	{
		var line = Previous().Line;

		if (Match(TokType.IntLiteral))
		{
			return new IntLiteral(int.Parse(Previous().Value), line);
		}

		if (Match(TokType.Minus))
		{
			var intLiteral = Consume(TokType.IntLiteral, new ExpectIntLiteralException(Peek().Value, line));
			var i = int.Parse(intLiteral.Value);
			return new IntLiteral(-i, line);
		}

		if (Match(TokType.Identifier))
		{
			return new UntypedVariable(Previous(), line);
		}

		if (Match(TokType.StringLiteral))
		{
			return new StringLiteral(Previous().Value, line);
		}

		throw new InvalidIndexTypeException(line);
	}
}
