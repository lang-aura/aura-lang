﻿using AuraLang.AST;
using AuraLang.Exceptions.Parser;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;

namespace AuraLang.Parser;

public class AuraParser
{
	private readonly List<Tok> _tokens;
	private int _index;
	private readonly ParserExceptionContainer _exContainer;
	private const int MAX_PARAMS = 255;

	public AuraParser(List<Tok> tokens, string filePath)
	{
		_tokens = tokens;
		_index = 0;
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
		var nonComments = statements.Where(stmt => stmt is not UntypedComment);
		if (nonComments.First() is not UntypedMod)
		{
			_exContainer.Add(new FileMustBeginWithModStmtException(nonComments.First().Range));
			throw _exContainer;
		}
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
	/// Returns the token two before the current index
	/// </summary>
	/// <returns>The token located two indices before the current index</returns>
	private string? IsPrecededByComment()
	{
		var i = _index - 1;
		while (i >= 0)
		{
			if (_tokens[i].Typ == TokType.Pub || _tokens[i].Typ == TokType.Semicolon || _tokens[i].Typ == TokType.Fn || _tokens[i].Typ == TokType.Interface || _tokens[i].Typ == TokType.Class || _tokens[i].Typ == TokType.Struct)
			{
				i--;
				continue;
			}

			if (_tokens[i].Typ == TokType.Comment) return _tokens[i].Value;
			return null;
		}

		return null;
	}

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
				case TokType.Struct:
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
		var @params = new List<Param>();

		if (!Check(TokType.RightParen))
		{
			while (true)
			{
				// Max of 255 parameters
				if (@params.Count >= MAX_PARAMS) throw new TooManyParametersException(MAX_PARAMS, Peek().Range);
				// Parse parameter
				var param = ParseParameter();
				@params.Add(param);

				if (Check(TokType.RightParen)) return @params;
				Consume(TokType.Comma, new ExpectEitherRightParenOrCommaAfterParam(Peek().Value, Peek().Range));
			}
		}

		return @params;
	}

	private AuraType TypeTokenToType(Tok tok)
	{
		switch (tok.Typ)
		{
			case TokType.Int:
				return new AuraInt();
			case TokType.Float:
				return new AuraFloat();
			case TokType.String:
				return new AuraString();
			case TokType.Bool:
				return new AuraBool();
			case TokType.LeftBracket:
				var kind = TypeTokenToType(Advance());
				Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Value, tok.Range));
				return new AuraList(kind);
			case TokType.Any:
				return new AuraAny();
			case TokType.Char:
				return new AuraChar();
			case TokType.Fn:
				// Check if there is a name -- if not, we are parsing an anonymous function
				Tok? name = Match(TokType.Identifier) ? Previous() : null;
				// Parse parameters
				Consume(TokType.LeftParen, new ExpectLeftParenException(Peek().Value, tok.Range));
				var paramz = ParseParameters();
				Consume(TokType.RightParen, new ExpectRightParenException(Peek().Value, tok.Range));
				// Parse return type (if there is one)
				var returnType = Match(TokType.Arrow)
					? TypeTokenToType(Advance())
					: new AuraNil();

				var f = new AuraFunction(paramz, returnType);
				return name is null ? f : new AuraNamedFunction(name.Value.Value, Visibility.Private, f);
			case TokType.Identifier:
				return new AuraUnknown(Previous().Value);
			case TokType.Map:
				Consume(TokType.LeftBracket, new ExpectLeftBracketException(Peek().Value, tok.Range));
				var key = TypeTokenToType(Advance());
				Consume(TokType.Colon, new ExpectColonException(Peek().Value, tok.Range));
				var value = TypeTokenToType(Advance());
				Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Value, tok.Range));
				return new AuraMap(key, value);
			case TokType.Error:
				return new AuraError();
			case TokType.Result:
				// Parse result's success type
				Consume(TokType.LeftBracket, new ExpectLeftBracketException(Peek().Value, tok.Range));
				var success = TypeTokenToType(Advance());
				Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Value, tok.Range));
				return new AuraResult(success, new AuraError());
			default:
				throw new UnexpectedTypeException(tok.Value, tok.Range);
		}
	}

	private ParamType ParseParameterType()
	{
		// Parse variadic
		var variadic = false;
		if (Match(TokType.Dot))
		{
			Consume(TokType.Dot, new VariadicSignifierMustHaveThreeDots(Peek().Range));
			Consume(TokType.Dot, new VariadicSignifierMustHaveThreeDots(Peek().Range));
			variadic = true;
		}

		// Parse type
		if (!Match(TokType.Int, TokType.Float, TokType.String, TokType.Bool, TokType.LeftBracket, TokType.Any,
				TokType.Char, TokType.Fn, TokType.Identifier, TokType.Map))
			throw new ExpectParameterTypeException(Peek().Value, Peek().Range);
		var pt = TypeTokenToType(Previous());
		// Parse default value
		if (!Match(TokType.Equal)) return new ParamType(pt, variadic, null);
		var defaultValue = Expression();
		if (defaultValue is not ILiteral lit)
			throw new ParameterDefaultValueMustBeALiteralException(Peek().Range);
		return new ParamType(pt, variadic, lit);
	}

	private Param ParseParameter()
	{
		// Consume the parameter name
		var name = Consume(TokType.Identifier, new ExpectParameterNameException(Peek().Range));
		// Consume colon separator
		Consume(TokType.Colon, new ExpectColonAfterParameterName(Peek().Value, Peek().Range));
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

			throw new InvalidTokenAfterPubKeywordException(Peek().Value, Peek().Range);
		}

		if (Match(TokType.Interface)) return InterfaceDeclaration(Visibility.Private);
		if (Match(TokType.Class)) return ClassDeclaration(Visibility.Private);
		if (Match(TokType.Struct)) return StructDeclaration();
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
			var import = Previous();
			if (Match(TokType.LeftParen))
			{
				Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));
				Consume(TokType.Newline, new ExpectNewLineException(Peek().Value, Peek().Range));
				var packages = new List<UntypedImport>();
				while (!Match(TokType.RightParen))
				{
					var tok_ = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Range));
					if (Match(TokType.As))
					{
						var alias = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Range));
						// Parse trailing semicolon
						Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));
						packages.Add(new UntypedImport(import, tok_, alias));
					}
					else
					{
						Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));
						Consume(TokType.Newline, new ExpectNewLineException(Peek().Value, Peek().Range));
						packages.Add(new UntypedImport(import, tok_, null));
					}
				}
				var rightParen = Previous(); // Store the closing right parenthesis
				Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));
				Consume(TokType.Newline, new ExpectNewLineException(Peek().Value, Peek().Range));
				return new UntypedMultipleImport(import, packages, rightParen);
			}

			var tok = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Range));
			// Check if the import has an alias
			if (Match(TokType.As))
			{
				var alias = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Range));
				// Parse trailing semicolon
				Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));
				return new UntypedImport(import, tok, alias);
			}

			// Parse trailing semicolon
			Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));
			return new UntypedImport(import, tok, null);
		}

		if (Match(TokType.Mut))
		{
			// The only statement that can being with `mut` is a short let declaration (i.e. `mut s := "Hello world")
			if (Peek().Typ is TokType.Identifier && PeekNext().Typ is TokType.ColonEqual)
			{
				return ShortLetDeclaration(true);
			}

			throw new InvalidTokenAfterMutKeywordException(Peek().Value, Peek().Range);
		}

		return Statement();
	}

	private IUntypedAuraStatement InterfaceDeclaration(Visibility pub)
	{
		var doc = IsPrecededByComment();
		var @interface = Previous();
		// Consume the interface name
		var name = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Range));
		Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Range));
		// Parse the interface's methods
		var methods = new List<AuraNamedFunction>();
		while (!IsAtEnd() && !Check(TokType.RightBrace))
		{
			if (Match(TokType.Comment)) Advance(); // Check for comment and advance past semicolon, if necessary
			var typ = TypeTokenToType(Advance());
			if (typ is not AuraNamedFunction f) throw new ExpectFunctionSignatureException(Peek().Range);
			methods.Add(f);
			Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));
		}

		var closingBrace = Consume(TokType.RightBrace, new ExpectRightBraceException(Peek().Value, Peek().Range));
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));

		return new UntypedInterface(@interface, name, methods, pub, closingBrace, doc);
	}

	private IUntypedAuraStatement ClassDeclaration(Visibility pub)
	{
		var doc = IsPrecededByComment();
		var @class = Previous();
		// Consume the class name
		var name = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Range));
		Consume(TokType.LeftParen, new ExpectLeftParenException(Peek().Value, Peek().Range));
		// Parse parameters
		var paramz = ParseParameters();
		Consume(TokType.RightParen, new ExpectRightParenException(Peek().Value, Peek().Range));
		// Check if class implements an interface
		var interfaceNames = Match(TokType.Colon) ? ParseImplementingInterfaces() : new List<Tok>();
		// Parse the class's methods
		Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Range));
		var body = ParseClassBody();

		var closingBrace = Consume(TokType.RightBrace, new ExpectRightBraceException(Peek().Value, Peek().Range));
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));

		return new UntypedClass(@class, name, paramz, body, pub, interfaceNames, closingBrace, doc);
	}

	private IUntypedAuraStatement StructDeclaration()
	{
		var doc = IsPrecededByComment();

		var @struct = Previous();
		// Consume the struct name
		var name = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Range));
		Consume(TokType.LeftParen, new ExpectLeftParenException(Peek().Value, Peek().Range));
		// Parse parameters
		var @params = ParseParameters();
		var closingBrace = Consume(TokType.RightParen, new ExpectRightParenException(Peek().Value, Peek().Range));
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));

		return new UntypedStruct(@struct, name, @params, closingBrace, doc);
	}

	private List<Tok> ParseImplementingInterfaces()
	{
		var interfaces = new List<Tok>();
		while (!Check(TokType.LeftBrace))
		{
			var interfaceName = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Range));
			if (!Check(TokType.LeftBrace)) Consume(TokType.Comma, new ExpectCommaException(Peek().Value, Peek().Range));
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
		Consume(TokType.Fn, new InvalidTokenAfterPubKeywordException(Peek().Value, Peek().Range));
		return NamedFunction(FunctionType.Method, pub);
	}

	private IUntypedAuraStatement ModDeclaration()
	{
		var mod = Previous();
		var val = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Range));
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));

		return new UntypedMod(mod, val);
	}

	private IUntypedAuraStatement Statement()
	{
		if (Match(TokType.For)) return ForStatement();
		if (Match(TokType.ForEach)) return ForEachStatement();
		if (Match(TokType.Return)) return ReturnStatement();
		if (Match(TokType.While)) return WhileStatement();
		if (Match(TokType.Defer)) return DeferStatement();
		if (Peek().Typ is TokType.Identifier && (PeekNext().Typ is TokType.ColonEqual || PeekNext().Typ is TokType.Comma)) return ShortLetDeclaration(false);
		if (Match(TokType.Comment)) return Comment();
		if (Match(TokType.Continue)) return new UntypedContinue(Previous());
		if (Match(TokType.Break)) return new UntypedBreak(Previous());
		if (Match(TokType.Yield)) return Yield();
		if (Match(TokType.Newline)) return new UntypedNewLine(Previous());
		if (Match(TokType.Check)) return CheckStatement();

		// If the statement doesn't begin with any of the Aura statement identifiers, parse it as an expression and
		// wrap it in an Expression Statement
		var expr = Expression();
		// Consume trailing semicolon
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));

		return new UntypedExpressionStmt(expr);
	}

	private IUntypedAuraStatement ForStatement()
	{
		var @for = Previous();

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
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));

		// Parse increment
		IUntypedAuraExpression? increment = null;
		if (!Check(TokType.LeftBrace)) increment = Expression();
		Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Range));

		// Parse body
		var body = new List<IUntypedAuraStatement>();
		while (!IsAtEnd() && !Check(TokType.RightBrace)) body.Add(Declaration());
		var closingBrace = Consume(TokType.RightBrace, new ExpectRightBraceException(Peek().Value, Peek().Range));
		// Consume trailing semicolon
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));

		return new UntypedFor(@for, initializer, condition, increment, body, closingBrace);
	}

	private IUntypedAuraStatement ForEachStatement()
	{
		var @foreach = Previous();
		// The first identifier will be attached to the current item on each iteration
		var eachName = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Range));
		Consume(TokType.In, new ExpectInKeywordException(Peek().Value, Peek().Range));
		// Consume the iterable
		var iter = Expression();
		Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Range));
		// Parse body
		var body = new List<IUntypedAuraStatement>();
		while (!IsAtEnd() && !Check(TokType.RightBrace)) body.Add(Declaration());
		var closingBrace = Consume(TokType.RightBrace, new ExpectRightBraceException(Peek().Value, Peek().Range));
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));

		return new UntypedForEach(@foreach, eachName, iter, body, closingBrace);
	}

	private UntypedReturn ReturnStatement()
	{
		var @return = Previous();

		// The return keyword does not need to be followed by an expression, in which case the return statement
		// will return a value of `nil`
		if (Match(TokType.Semicolon)) return new UntypedReturn(@return, null);

		var returnTypes = new List<IUntypedAuraExpression>();
		while (!IsAtEnd())
		{
			returnTypes.Add(Expression());
			if (!Match(TokType.Comma)) break;
		}
		// Parse the trailing semicolon
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));

		return new UntypedReturn(@return, returnTypes);
	}

	private IUntypedAuraStatement WhileStatement()
	{
		// Store while keyword
		var @while = Previous();
		// Parse condition
		var condition = Expression();
		Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Range));
		// Parse body
		var body = new List<IUntypedAuraStatement>();
		while (!IsAtEnd() && !Check(TokType.RightBrace))
		{
			var stmt = Statement();
			body.Add(stmt);
		}

		var closingBrace = Consume(TokType.RightBrace, new ExpectRightBraceException(Peek().Value, Peek().Range));
		// Parse trailing semicolon
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));
		return new UntypedWhile(@while, condition, body, closingBrace);
	}

	private UntypedDefer DeferStatement()
	{
		var defer = Previous();

		// Parse the expression to be deferred
		var expression = Expression();
		// Make sure the deferred expression is a function call
		if (expression is not IUntypedAuraCallable callableExpr)
			throw new CanOnlyDeferFunctionCallException(Peek().Range);

		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));
		return new UntypedDefer(defer, callableExpr);
	}

	private IUntypedAuraStatement LetDeclaration()
	{
		var let = Previous();
		// Check if the variable is declared as mutable
		var isMutable = Match(TokType.Mut);
		// Parse the variable's name(s)
		var names = ParseLongVariableNames();
		// Parse the variable's initializer (if there is one)
		IUntypedAuraExpression? initializer = null;
		if (Match(TokType.Equal)) initializer = Expression();
		// Parse the trailing semicolon
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));

		return new UntypedLet(let, names.Select(n => n.Item1).ToList(), names.Select(n => n.Item2).ToList(), isMutable, initializer);
	}

	private IUntypedAuraStatement ShortLetDeclaration(bool isMutable)
	{
		// Parse the variable's name
		var names = ParseShortVariableNames();
		Consume(TokType.ColonEqual, new ExpectColonEqualException(Peek().Value, Peek().Range));
		// Parse the variable's initializer
		var initializer = Expression();
		// Consume trailing semicolon
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));

		return new UntypedLet(null, names, new List<AuraType>(), isMutable, initializer);
	}

	private List<(Tok, AuraType)> ParseLongVariableNames()
	{
		var names = new List<(Tok, AuraType)>();
		while (!IsAtEnd())
		{
			var name = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Range));
			Consume(TokType.Colon, new ExpectColonAfterParameterName(Peek().Value, Peek().Range));
			var nameType = TypeTokenToType(Advance());
			names.Add((name, nameType));

			if (Peek().Typ != TokType.Equal && Peek().Typ != TokType.Semicolon) Consume(TokType.Comma, new ExpectCommaException(Peek().Value, Peek().Range));
			else break;
		}

		return names;
	}

	private List<Tok> ParseShortVariableNames()
	{
		var names = new List<Tok>();
		while (true)
		{
			names.Add(Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Range)));

			if (Peek().Typ != TokType.ColonEqual) Consume(TokType.Comma, new ExpectCommaException(Peek().Value, Peek().Range));
			else break;
		}

		return names;
	}

	private IUntypedAuraStatement Comment()
	{
		var text = Previous();
		// Consume the trailing semicolon
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));

		return new UntypedComment(text);
	}

	private IUntypedAuraStatement Yield()
	{
		// Store the yield keyword
		var yield = Previous();
		// Parse the expression to be yielded
		var value = Expression();
		// Consume the trailing semicolon
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));

		return new UntypedYield(yield, value);
	}

	private IUntypedAuraStatement CheckStatement()
	{
		// Store the check keyword
		var check = Previous();
		// Parse the expression to be checked
		var expression = Expression();
		// Make sure the checked expression is a function call
		if (expression is not UntypedCall callableExpr)
			throw new CanOnlyCheckFunctionCallException(Peek().Range);

		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));
		return new UntypedCheck(check, callableExpr);
	}

	private IUntypedAuraStatement ExpressionStatement()
	{
		// Parse expression
		var expr = Expression();
		// Consume the trailing semicolon
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));

		return new UntypedExpressionStmt(expr);
	}

	private UntypedNamedFunction NamedFunction(FunctionType kind, Visibility pub)
	{
		var doc = IsPrecededByComment();
		var fn = Previous();
		// Parse the function's name
		var name = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Value, Peek().Range));
		Consume(TokType.LeftParen, new ExpectLeftParenException(Peek().Value, Peek().Range));
		// Parse the function's parameters
		var @params = ParseParameters();
		Consume(TokType.RightParen, new ExpectRightParenException(Peek().Value, Peek().Range));
		// Parse the function's return type
		var returnTypes = ParseFunctionReturnTypes();
		Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Range));
		// Parse body
		var body = Block();
		Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Value, Peek().Range));

		return new UntypedNamedFunction(fn, name, @params, body, returnTypes, pub, doc);
	}

	private UntypedAnonymousFunction AnonymousFunction()
	{
		var fn = Previous();
		Consume(TokType.LeftParen, new ExpectLeftParenException(Peek().Value, Peek().Range));
		// Parse function's parameters
		var @params = ParseParameters();
		Consume(TokType.RightParen, new ExpectRightParenException(Peek().Value, Peek().Range));
		// Parse function's return type
		var returnTypes = ParseFunctionReturnTypes();
		Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Range));
		// Parse body
		var body = Block();

		return new UntypedAnonymousFunction(fn, @params, body, returnTypes);
	}

	private List<AuraType>? ParseFunctionReturnTypes()
	{
		if (!Match(TokType.Arrow)) return null;
		return Match(TokType.LeftParen)
			? ParseMultipleFunctionReturnTypes()
			: new List<AuraType> { TypeTokenToType(Advance()) };
	}

	private List<AuraType> ParseMultipleFunctionReturnTypes()
	{
		var returnTypes = new List<AuraType>();
		while (!IsAtEnd())
		{
			returnTypes.Add(TypeTokenToType(Advance()));
			if (Peek().Typ != TokType.RightParen) Consume(TokType.Comma, new ExpectCommaException(Peek().Value, Peek().Range));
			else break;
		}

		Advance(); // Advance past closing right parenthesis
		return returnTypes;
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
			if (expression is UntypedVariable v) return new UntypedAssignment(v.Name, value);
			if (expression is UntypedGet g) return new UntypedSet(g.Obj, g.Name, value);
			throw new InvalidAssignmentTargetException(Peek().Range);
		}
		else if (Match(TokType.PlusPlus))
		{
			var variable = expression as UntypedVariable;
			if (variable is not null) return new UntypedPlusPlusIncrement(new UntypedVariable(variable.Name), Previous());
		}
		else if (Match(TokType.MinusMinus))
		{
			var variable = expression as UntypedVariable;
			if (variable is not null) return new UntypedMinusMinusDecrement(new UntypedVariable(variable.Name), Previous());
		}

		return expression;
	}

	private IUntypedAuraExpression IfExpr()
	{
		var @if = Previous();
		// Parse the condition
		var condition = Expression();
		Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Range));
		// Parse `then` branch
		var thenBranch = Block();
		// Parse the `else` branch
		if (Match(TokType.Else))
		{
			if (Match(TokType.If))
			{
				var elseBranch = IfExpr();
				return new UntypedIf(@if, condition, thenBranch, elseBranch);
			}
			else
			{
				Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Range));
				var elseBranch = Block();
				return new UntypedIf(@if, condition, thenBranch, elseBranch);
			}
		}

		return new UntypedIf(@if, condition, thenBranch, null);
	}

	private UntypedBlock Block()
	{
		var openingBrace = Previous();
		var statements = new List<IUntypedAuraStatement>();

		while (!IsAtEnd() && !Check(TokType.RightBrace))
		{
			var decl = Declaration();
			// If the statement is a return statement, it should be the last line of the block.
			// Otherwise, any lines after it will be unreachable.
			if (decl is UntypedReturn)
			{
				if (!Check(TokType.RightBrace)) throw new UnreachableCodeException(Peek().Range);
			}

			statements.Add(decl);
		}

		var closingBrace = Consume(TokType.RightBrace, new ExpectRightBraceException(Peek().Value, Peek().Range));

		return new UntypedBlock(openingBrace, statements, closingBrace);
	}

	private IUntypedAuraExpression Or()
	{
		var expression = And();
		while (Match(TokType.Or))
		{
			var op = Previous();
			var right = And();
			expression = new UntypedLogical(expression, op, right);
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
			expression = new UntypedLogical(expression, op, right);
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
			expression = new UntypedLogical(expression, op, right);
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
			expression = new UntypedLogical(expression, op, right);
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
			expression = new UntypedBinary(expression, op, right);
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
			expression = new UntypedBinary(expression, op, right);
		}

		return expression;
	}

	private IUntypedAuraExpression Unary()
	{
		if (Match(TokType.Bang, TokType.Minus))
		{
			var op = Previous();
			var right = Unary();
			return new UntypedUnary(op, right);
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
				var name = ConsumeMultiple(new ExpectPropertyNameException(Peek().Value, Peek().Range), TokType.Identifier,
					TokType.IntLiteral);
				expression = new UntypedGet(expression, name);
			}
			else break;
		}

		return expression;
	}

	private IUntypedAuraExpression FinishCall(IUntypedAuraExpression callee)
	{
		var arguments = new List<(Tok?, IUntypedAuraExpression)>();
		if (!Check(TokType.RightParen))
		{
			while (true)
			{
				// Function declarations have a max of 255 arguments, so function calls have the same limit
				if (arguments.Count >= MAX_PARAMS) throw new TooManyParametersException(MAX_PARAMS, Peek().Range);

				Tok? tag = null;
				if (PeekNext().Typ is TokType.Colon)
				{
					tag = Advance();
					Consume(TokType.Colon, new ExpectColonException(Peek().Value, Peek().Range));
				}

				var expression = Expression();
				arguments.Add((tag, expression));
				if (Check(TokType.RightParen)) break;
				Match(TokType.Comma);
			}
		}

		var closingParen = Consume(TokType.RightParen, new ExpectRightParenException(Peek().Value, Peek().Range));
		return new UntypedCall((IUntypedAuraCallable)callee, arguments, closingParen);
	}

	private UntypedIs Is(IUntypedAuraExpression expr)
	{
		// Parse the expected type's token
		var expected = Advance();
		return new UntypedIs(expr, expected);
	}

	private IUntypedAuraExpression Primary()
	{
		if (Match(TokType.False)) return new BoolLiteral(new Tok(TokType.False, "false"));
		if (Match(TokType.True)) return new BoolLiteral(new Tok(TokType.True, "true"));
		if (Match(TokType.Nil)) return new UntypedNil(Previous());
		if (Match(TokType.StringLiteral)) return new StringLiteral(Previous());
		if (Match(TokType.CharLiteral)) return new CharLiteral(Previous());
		if (Match(TokType.IntLiteral))
		{
			return new IntLiteral(Int: Previous());
		}

		if (Match(TokType.FloatLiteral))
		{
			return new FloatLiteral(Float: Previous());
		}

		if (Match(TokType.This)) return new UntypedThis(Previous());
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
			var openingParen = Previous();
			var expression = Expression();
			var closingParen = Consume(TokType.RightParen, new ExpectRightParenException(Peek().Value, Peek().Range));
			return new UntypedGrouping(openingParen, expression, closingParen);
		}

		if (Match(TokType.LeftBrace))
		{
			return Block();
		}

		if (Match(TokType.LeftBracket))
		{
			var leftBracket = Previous();
			// Parse list's type
			var typ = TypeTokenToType(Advance());
			Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Value, Peek().Range));
			Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Range));

			var items = new List<IUntypedAuraExpression>();
			while (!Match(TokType.RightBrace))
			{
				var expr = Expression();
				items.Add(expr);
				if (!Check(TokType.RightBrace))
				{
					Consume(TokType.Comma, new ExpectCommaException(Peek().Value, Peek().Range));
				}
			}

			var closingBrace = Previous();
			var listExpr = new ListLiteral<IUntypedAuraExpression>(leftBracket, items, typ, closingBrace);
			return Match(TokType.LeftBracket) ? ParseGetAccess(listExpr) : listExpr;
		}

		if (Match(TokType.Fn))
		{
			return AnonymousFunction();
		}

		if (Match(TokType.Map))
		{
			var map = Previous();
			// Parse map's type signature
			Consume(TokType.LeftBracket, new ExpectLeftBracketException(Peek().Value, Peek().Range));
			var keyType = TypeTokenToType(Advance());
			Consume(TokType.Colon, new ExpectColonException(Peek().Value, Peek().Range));
			var valueType = TypeTokenToType(Advance());
			Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Value, Peek().Range));
			Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Value, Peek().Range));

			var d = new Dictionary<IUntypedAuraExpression, IUntypedAuraExpression>();
			while (!Match(TokType.RightBrace))
			{
				var key = Expression();
				Consume(TokType.Colon, new ExpectColonException(Peek().Value, Peek().Range));
				var value = Expression();
				Consume(TokType.Comma, new ExpectCommaException(Peek().Value, Peek().Range));
				Consume(TokType.Semicolon,
					new ExpectSemicolonException(Peek().Value, Peek().Range)); // TODO don't add implicit semicolon after map items
				d[key] = value;
			}

			var closingBrace = Previous();
			var mapExpr = new MapLiteral<IUntypedAuraExpression, IUntypedAuraExpression>(map, d, keyType, valueType, closingBrace);
			return Match(TokType.LeftBracket) ? ParseSingleGetAccess(mapExpr) : mapExpr;
		}

		throw new ExpectExpressionException(Peek().Range);
	}

	private IUntypedAuraExpression ParseIdentifier(Tok iden)
	{
		if (!Match(TokType.LeftBracket)) return new UntypedVariable(iden);
		return ParseGetAccess(new UntypedVariable(iden));
	}

	private IUntypedAuraExpression ParseSingleGetAccess(IUntypedAuraExpression obj)
	{
		var index = ParseIndex();
		var closingBracket = Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Value, Peek().Range));
		return new UntypedGetIndex(obj, index, closingBracket);
	}

	private IUntypedAuraExpression ParseGetAccess(IUntypedAuraExpression obj)
	{
		if (Match(TokType.Colon))
		{
			var upper = Match(TokType.RightBracket) ? new IntLiteral(new Tok(TokType.IntLiteral, "-1")) : ParseIndex();
			var closingBracket = Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Value, obj.Range));
			return new UntypedGetIndexRange(obj, new IntLiteral(new Tok(TokType.IntLiteral, "0")), upper, closingBracket);
		}

		if (!Match(TokType.RightBracket))
		{
			var lower = ParseIndex();
			if (Match(TokType.RightBracket)) return new UntypedGetIndex(obj, lower, Previous());
			Consume(TokType.Colon, new ExpectColonException(Peek().Value, Peek().Range));

			IUntypedAuraExpression upper;
			if (Match(TokType.RightBracket)) upper = new IntLiteral(new Tok(TokType.IntLiteral, "-1"));
			else
			{
				upper = ParseIndex();
				Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Value, Peek().Range));
			}

			return new UntypedGetIndexRange(obj, lower, upper, Previous());
		}

		throw new PostfixIndexCannotBeEmptyException(Peek().Range);
	}

	private IUntypedAuraExpression ParseIndex()
	{
		if (Match(TokType.IntLiteral))
		{
			return new IntLiteral(Previous());
		}

		if (Match(TokType.Minus))
		{
			var intLiteral = Consume(TokType.IntLiteral, new ExpectIntLiteralException(Peek().Value, Peek().Range));
			var i = int.Parse(intLiteral.Value);
			return new IntLiteral(new Tok(TokType.IntLiteral, $"-{i}"));
		}

		if (Match(TokType.Identifier))
		{
			return new UntypedVariable(Previous());
		}

		if (Match(TokType.StringLiteral))
		{
			return new StringLiteral(Previous());
		}

		throw new InvalidIndexTypeException(Peek().Range);
	}
}
