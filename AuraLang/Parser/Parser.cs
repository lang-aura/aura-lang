using System.Globalization;
using AuraLang.AST;
using AuraLang.Exceptions.Parser;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using Char = AuraLang.Types.Char;
using String = AuraLang.Types.String;
using Tuple = AuraLang.Types.Tuple;

namespace AuraLang.Parser;

public class AuraParser
{
    private readonly List<Tok> _tokens;
    private int _index;
    private ParserExceptionContainer _exContainer = new();

    public AuraParser(List<Tok> tokens)
    {
        _tokens = tokens;
        _index = 0;
    }

    /// <summary>
    /// Parses each token in <see cref="_tokens"/> and produces an untyped AST
    /// </summary>
    /// <returns></returns>
    public List<UntypedAuraStatement> Parse()
    {
        var statements = new List<UntypedAuraStatement>();
        while (!IsAtEnd())
        {
            try
            {
                statements.Add(Declaration());
            }
            catch (ParserException ex)
            {
                _exContainer.Add(ex);
            }
        }

        if (!_exContainer.IsEmpty()) throw _exContainer;
        return statements;
    }

    /// <summary>
    /// Checks if the next token matches any of the supplied token types. If so, the parser advances past the matched
    /// token and returns true. Otherwise, false is returned.
    /// </summary>
    /// <param name="TokTypes">A list of acceptable token types for the next token</param>
    /// <returns>A boolean indicating if the next token's type matches any of the <see cref="TokTypes"/></returns>
    private bool Match(params TokType[] TokTypes)
    {
        foreach (var tokType in TokTypes)
        {
            if (Check(tokType))
            {
                Advance();
                return true;
            }
        }
        return false;
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
    /// <returnsThe next token, if it matched any of the supplied token types></returns>
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
        var params_ = new List<Param>();

        if (!Check(TokType.RightParen))
        {
            while (true)
            {
                // Max of 255 parameters
                if (params_.Count >= 255) throw new TooManyParametersException(Peek().Line);
                // Consume the paramter name
                var name = Consume(TokType.Identifier, new ExpectParameterNameException(Peek().Line));
                // Consume colon separator
                Consume(TokType.Colon, new ExpectColonAfterParameterName(Peek().Line));
                // Check for variadic signifier
                var variadic = false;
                if (Match(TokType.Dot))
                {
                    Consume(TokType.Dot, new VariadicSignifierMustHaveThreeDots(Peek().Line));
                    Consume(TokType.Dot, new VariadicSignifierMustHaveThreeDots(Peek().Line));
                    variadic = true;
                }
                // Parse the parameter type
                var pt = ParseParameterType();
                params_.Add(new Param(name, new ParamType(pt.Typ, variadic)));

                if (Check(TokType.RightParen)) return params_;
                else Consume(TokType.Comma, new ExpectEitherRightParenOrCommaAfterParam(Peek().Line));
            }
        }

        return params_;
    }

    private ParamType ParseParameterType()
    {
        var variadic = false;
        if (Match(TokType.Dot))
        {
            Consume(TokType.Dot, new VariadicSignifierMustHaveThreeDots(Peek().Line));
            Consume(TokType.Dot, new VariadicSignifierMustHaveThreeDots(Peek().Line));
            variadic = true;
        }

        if (!Match(TokType.Int, TokType.Float, TokType.String, TokType.Bool, TokType.LeftBracket, TokType.Any, TokType.Char, TokType.Fn, TokType.Identifier, TokType.Map, TokType.Tup)) throw new ExpectParameterTypeException(Peek().Line);

        var pt = TypeTokenToType(Previous());
        return new ParamType(pt, variadic);
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
                return new String();
            case TokType.Bool:
                return new Bool();
            case TokType.Identifier:
                return new Unknown(Previous().Value);
            case TokType.LeftBracket:
                if (!Match(TokType.Int, TokType.Float, TokType.String, TokType.Bool, TokType.Identifier)) throw new ExpectVariableTypeException(Peek().Line);
                var listType = TypeTokenToType(Previous());
                Consume(TokType.RightBracket, new UnterminatedListLiteralException(Peek().Line));
                return new List(listType);
            case TokType.Any:
                return new Any();
            case TokType.Char:
                return new Char();
            case TokType.Fn:
                Consume(TokType.LeftParen, new ExpectLeftParenAfterFnKeywordException(Peek().Line));
                var paramTypes = new List<ParamType>();
                while (!Match(TokType.RightParen))
                {
                    var t = ParseParameterType();
                    paramTypes.Add(t);
                }
                Consume(TokType.Arrow, new ExpectArrowInFnSignatureException(Peek().Line));
                var returnType = ParseParameterType(); // TODO don't use parseParameterType to parse return type
                return new AnonymousFunction(paramTypes, returnType.Typ);
            case TokType.Map:
                Consume(TokType.LeftBracket, new ExpectLeftBracketAfterMapKeywordException(Peek().Line));
                var keyType = ParseParameterType();
                Consume(TokType.Colon, new ExpectColonBetweenMapTypesException(Peek().Line));
                var valueType = ParseParameterType();
                Consume(TokType.RightBracket, new UnterminatedMapTypeSignatureException(Peek().Line));
                return new Map(keyType.Typ, valueType.Typ);
            case TokType.Tup:
                var paramTypes_ = new List<AuraType>();
                Consume(TokType.LeftBracket, new ExpectLeftBracketAfterTupKeywordException(Peek().Line));
                while (!Match(TokType.RightBracket))
                {
                    var t = ParseParameterType(); // TODO don't use this function because tuple types cannot be variadic
                    paramTypes_.Add(t.Typ);
                    Match(TokType.Comma);
                }
                return new Tuple(paramTypes_);
            default:
                throw new UnexpectedTypeException(Peek().Line);
        }
    }

    private UntypedAuraStatement Declaration()
    {
        if (Match(TokType.Pub))
        {
            if (Match(TokType.Class))
            {
                return ClassDeclaration(Visibility.Public);
            }
            else if (Match(TokType.Fn))
            {
                return NamedFunction(FunctionType.Function, Visibility.Public);
            }
            else
            {
                throw new InvalidTokenAfterPubKeywordException(Peek().Line);
            }
        }
        else if (Match(TokType.Class))
        {
            return ClassDeclaration(Visibility.Private);
        }
        else if (Check(TokType.Fn) && PeekNext().Typ == TokType.Identifier)
        {
            // Since we only check for the `fn` keyword, we need to advance past it here before entering the NamedFunction() call
            Advance();
            return NamedFunction(FunctionType.Function, Visibility.Private);
        }
        else if (Match(TokType.Let))
        {
            return LetDeclaration();
        }
        else if (Match(TokType.Mod))
        {
            return ModDeclaration();
        }
        else if (Match(TokType.Import))
        {
            var line = Previous().Line;
            var tok = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Line));
            // Check if the import has an alias
            if (Match(TokType.As))
            {
                var alias = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Line));
                // Parse trailing semicolon
                Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Line));
                return new UntypedImport(tok, alias, line);
            }
            // Parse trailing semicolon
            Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Line));
            return new UntypedImport(tok, null, line);
        }
        else if (Match(TokType.Mut))
        {
            // The only statement that can being with `mut` is a short let declaration (i.e. `mut s := "Hello world")
            if (Peek().Typ is TokType.Identifier && PeekNext().Typ is TokType.ColonEqual)
            {
                return ShortLetDeclaration(true);
            }
            else
            {
                throw new InvalidTokenAfterMutKeywordException(Peek().Line);
            }
        }
        else
        {
            return Statement();
        }
    }

    private UntypedAuraStatement ClassDeclaration(Visibility public_)
    {
        var line = Previous().Line;
        // Consume the class name
        var name = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Line));
        Consume(TokType.LeftParen, new ExpectLeftParenException(Peek().Line));
        // Parse parameters
        var params_ = ParseParameters();
        Consume(TokType.RightParen, new ExpectRightParenException(Peek().Line));
        Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Line));
        // Parse the class's methods
        var methods = new List<UntypedNamedFunction>();
        // Methods can be public or private, just like regular functions
        if (Match(TokType.Pub))
        {
            Consume(TokType.Fn, new InvalidTokenAfterPubKeywordException(Peek().Line));
            while (!IsAtEnd() && !Check(TokType.RightBrace))
            {
                var f = NamedFunction(FunctionType.Method, Visibility.Public);
                methods.Add(f);
            }
        }
        else if (Match(TokType.Fn))
        {
            while (!IsAtEnd() && !Check(TokType.RightBrace))
            {
                var f = NamedFunction(FunctionType.Method, Visibility.Private);
                methods.Add(f);
            }
        }

        Consume(TokType.RightBrace, new ExpectRightBraceException(Peek().Line));
        Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Line));

        return new UntypedClass(name, params_, methods, public_, line);
    }

    private UntypedAuraStatement ModDeclaration()
    {
        var line = Previous().Line;
        var val = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Line));
        Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Line));

        return new UntypedMod(val, line);
    }

    private UntypedAuraStatement Statement()
    {
        if (Match(TokType.For)) return ForStatement();
        else if (Match(TokType.ForEach)) return ForEachStatement();
        else if (Match(TokType.Return)) return ReturnStatement();
        else if (Match(TokType.While)) return WhileStatement();
        else if (Match(TokType.Defer)) return DeferStatement();
        else if (Peek().Typ is TokType.Identifier && PeekNext().Typ is TokType.ColonEqual) return ShortLetDeclaration(false);
        else if (Match(TokType.Comment)) return Comment();
        else if (Match(TokType.Continue)) return new UntypedContinue(Previous().Line);
        else if (Match(TokType.Break)) return new UntypedBreak(Previous().Line);
        else
        {
            var line = Peek().Line;
            // If the statement doesn't begin with any of the Aura statement identifiers, parse it as an expression and
            // wrap it in an Expression Statement
            var expr = Expression();
            // Consume trailing semicolon
            Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Line));

            return new UntypedExpressionStmt(expr, line);
        }
    }

    private UntypedAuraStatement ForStatement()
    {
        var line = Previous().Line;

        // Parse initializer
        UntypedAuraStatement? initializer;
        if (Match(TokType.Semicolon)) initializer = null;
        else if (Match(TokType.Let)) initializer = LetDeclaration();
        else if (Peek().Typ is TokType.Identifier && PeekNext().Typ is TokType.ColonEqual) initializer = ShortLetDeclaration(false);
        else initializer = ExpressionStatement();

        // Parse condition
        UntypedAuraExpression? condition = null;
        if (!Check(TokType.Semicolon)) condition = Expression();
        Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Line));

        // Parse increment
        UntypedAuraExpression? increment = null;
        if (!Check(TokType.LeftBrace)) increment = Expression();
        Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Line));

        // Parse body
        var body = new List<UntypedAuraStatement>();
        while (!IsAtEnd() && !Check(TokType.RightBrace)) body.Add(Declaration());
        Consume(TokType.RightBrace, new ExpectRightBraceException(Peek().Line));
        if (increment is not null) body.Add(new UntypedExpressionStmt(increment!, line));
        // Consume trailing semicolon
        Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Line));

        return new UntypedFor(initializer, condition, body, line);
    }

    private UntypedAuraStatement ForEachStatement()
    {
        var line = Previous().Line;
        // The first identifier will be attached to the current item on each iteration
        var eachName = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Line));
        Consume(TokType.In, new ExpectInKeywordException(Peek().Line));
        // Consume the iterable
        var iter = Expression();
        Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Line));
        // Parse body
        var body = new List<UntypedAuraStatement>();
        while (!IsAtEnd() && !Check(TokType.RightBrace)) body.Add(Declaration());
        Consume(TokType.RightBrace, new ExpectRightBraceException(Peek().Line));
        Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Line));

        return new UntypedForEach(eachName, iter, body, line);
    }

    private UntypedReturn ReturnStatement()
    {
        var line = Previous().Line;

        // The return keyword does not need to be followed by an expression, in which case the return statement
        // will return a value of `nil`
        UntypedAuraExpression? value = null;
        if (!Check(TokType.Semicolon)) value = Expression();
        // Parse the trailing semicolon
        Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Line));

        return new UntypedReturn(value, line);
    }

    private UntypedAuraStatement WhileStatement()
    {
        var line = Previous().Line;

        // Parse condition
        var condition = Expression();
        Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Line));
        // Parse body
        var body = new List<UntypedAuraStatement>();
        while (!IsAtEnd() && !Check(TokType.RightBrace))
        {
            var stmt = Statement();
            body.Add(stmt);
        }
        Consume(TokType.RightBrace, new ExpectRightBraceException(Peek().Line));
        // Parse trailing semicolon
        Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Line));

        return new UntypedWhile(condition, body, line);
    }

    private UntypedDefer DeferStatement()
    {
        var line = Previous().Line;

        // Parse the expression to be deferred
        var expression = Expression();
        // Make sure the deferred expression is a function call
        if (expression is not IUntypedAuraCallable callableExpr) throw new CanOnlyDeferFunctionCallException(Peek().Line);

        Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Line));
        return new UntypedDefer(callableExpr, line);
    }

    private UntypedAuraStatement LetDeclaration()
    {
        var line = Previous().Line;
        // Check if the variable is declared as mutable
        var isMutable = Match(TokType.Mut) ? true : false;
        // Parse the variable's name
        var name = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Line));
        // When declaring a new variable with the full `let` syntax, the variable's name must be followwed
        // by a colon and the variable's type
        Consume(TokType.Colon, new ExpectColonException(Peek().Line));
        var nameType = ParseParameterType();
        // Parse the variable's initializer (if there is one)
        UntypedAuraExpression? initializer = null;
        if (Match(TokType.Equal)) initializer = Expression();
        // Parse the trailing semicolon
        Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Line));

        return new UntypedLet(name, nameType.Typ, isMutable, initializer, line);
    }

    private UntypedAuraStatement ShortLetDeclaration(bool isMutable)
    {
        var line = Peek().Line;
        // Parse the variable's name
        var name = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Line));
        Consume(TokType.ColonEqual, new ExpectColonEqualException(Peek().Line));
        // Parse the variable's initializer
        var initializer = Expression();
        // Consume trailing semicolon
        Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Line));

        return new UntypedLet(name, new None(), isMutable, initializer, line);
    }

    private UntypedAuraStatement Comment()
    {
        var text = Previous();
        // Consume the trailing semicolon
        Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Line));

        return new UntypedComment(text, text.Line);
    }

    private UntypedAuraStatement ExpressionStatement()
    {
        var line = Peek().Line;
        // Parse expression
        var expr = Expression();
        // Consume the trailing semicolon
        Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Line));

        return new UntypedExpressionStmt(expr, line);
    }

    private UntypedNamedFunction NamedFunction(FunctionType kind, Visibility public_)
    {
        var line = Previous().Line;
        // Parse the function's name
        var name = Consume(TokType.Identifier, new ExpectIdentifierException(Peek().Line));
        Consume(TokType.LeftParen, new ExpectLeftParenException(Peek().Line));
        // Parse the function's parameters
        var params_ = ParseParameters();
        Consume(TokType.RightParen, new ExpectRightParenException(Peek().Line));
        // Parse the function's return type
        var returnType = Match(TokType.Arrow) ? TypeTokenToType(Advance()) : new Nil();
        Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Line));
        // Parse body
        var body = Block();
        Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Line));

        return new UntypedNamedFunction(name, params_, body, returnType, public_, line);
    }

    private UntypedAnonymousFunction AnonymousFunction()
    {
        var line = Previous().Line;

        Consume(TokType.LeftParen, new ExpectLeftParenException(Peek().Line));
        // Parse function's parameters
        var params_ = ParseParameters();
        Consume(TokType.RightParen, new ExpectRightParenException(Peek().Line));
        // Parse function's return type
        var returnType = Match(TokType.Arrow) ? TypeTokenToType(Advance()) : new Nil();
        Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Line));
        // Parse body
        var body = Block();

        return new UntypedAnonymousFunction(params_, body, returnType, line);
    }

    private UntypedAuraExpression Expression()
    {
        return Assignment();
    }

    private UntypedAuraExpression Assignment()
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
                return new UntypedAssignment(variable.Name, new UntypedBinary(variable, new Tok(TokType.Plus, "+", variable.Line), new UntypedIntLiteral(1, variable.Line), variable.Line), variable.Line);
        }
        else if (Match(TokType.MinusMinus))
        {
            var variable = expression as UntypedVariable;
            if (variable is not null)
                return new UntypedAssignment(variable.Name, new UntypedBinary(variable, new Tok(TokType.Minus, "-", variable.Line), new UntypedIntLiteral(1, variable.Line), variable.Line), variable.Line);
        }

        return expression;
    }

    private UntypedAuraExpression IfExpr()
    {
        var line = Previous().Line;
        // Parse the condition
        var condition = Expression();
        Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Line));
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
                Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Line));
                var elseBranch = Block();
                return new UntypedIf(condition, thenBranch, elseBranch, line);
            }
        }

        return new UntypedIf(condition, thenBranch, null, line);
    }

    private UntypedBlock Block()
    {
        var line = Previous().Line;
        var statements = new List<UntypedAuraStatement>();

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

        Consume(TokType.RightBrace, new ExpectRightBraceException(Peek().Line));

        return new UntypedBlock(statements, line);
    }

    private UntypedAuraExpression Or()
    {
        var expression = And();
        while (Match(TokType.Or))
        {
            var operator_ = Previous();
            var right = And();
            expression = new UntypedLogical(expression, operator_, right, expression.Line);
        }

        return expression;
    }

    private UntypedAuraExpression And()
    {
        var expression = Equality();
        while (Match(TokType.And))
        {
            var operator_ = Previous();
            var right = Equality();
            expression = new UntypedLogical(expression, operator_, right, expression.Line);
        }

        return expression;
    }

    private UntypedAuraExpression Equality()
    {
        var expression = Comparison();
        while (Match(TokType.BangEqual, TokType.EqualEqual))
        {
            var operator_ = Previous();
            var right = Comparison();
            expression = new UntypedLogical(expression, operator_, right, expression.Line);
        }

        return expression;
    }

    private UntypedAuraExpression Comparison()
    {
        var expression = Term();
        while (Match(TokType.Greater, TokType.GreaterEqual, TokType.Less, TokType.LessEqual))
        {
            var operator_ = Previous();
            var right = Term();
            expression = new UntypedLogical(expression, operator_, right, expression.Line);
        }

        return expression;
    }

    private UntypedAuraExpression Term()
    {
        var expression = Factor();
        while (Match(TokType.Minus, TokType.MinusEqual, TokType.Plus, TokType.PlusEqual))
        {
            var operator_ = Previous();
            var right = Factor();
            expression = new UntypedBinary(expression, operator_, right, expression.Line);
        }

        return expression;
    }

    private UntypedAuraExpression Factor()
    {
        var expression = Unary();
        while (Match(TokType.Slash, TokType.SlashEqual, TokType.Star, TokType.StarEqual))
        {
            var operator_ = Previous();
            var right = Unary();
            expression = new UntypedBinary(expression, operator_, right, expression.Line);
        }

        return expression;
    }

    private UntypedAuraExpression Unary()
    {
        if (Match(TokType.Bang, TokType.Minus))
        {
            var operator_ = Previous();
            var right = Unary();
            return new UntypedUnary(operator_, right, operator_.Line);
        }

        return Call();
    }

    private UntypedAuraExpression Call()
    {
        // Parse the callee
        var expression = Primary();
        while (true)
        {
            if (!IsAtEnd() && Match(TokType.LeftParen)) expression = FinishCall(expression);
            else if (!IsAtEnd() && Match(TokType.Dot))
            {
                var name = ConsumeMultiple(new ExpectPropertyNameException(Peek().Line), TokType.Identifier, TokType.IntLiteral);
                expression = new UntypedGet(expression, name, expression.Line);
            }
            else break;
        }

        return expression;
    }

    private UntypedAuraExpression FinishCall(UntypedAuraExpression callee)
    {
        var line = Previous().Line;
        var arguments = new List<UntypedAuraExpression>();
        if (!Check(TokType.RightParen))
        {
            while (true)
            {
                // Function declarations have a max of 255 arguments, so function calls have the same limit
                if (arguments.Count >= 255) throw new TooManyParametersException(Peek().Line);
                var expression = Expression();
                arguments.Add(expression);
                if (Check(TokType.RightParen)) break;
                else Match(TokType.Comma);
            }
        }

        Consume(TokType.RightParen, new ExpectRightParenException(Peek().Line));
        return new UntypedCall(callee as IUntypedAuraCallable, arguments, line);
    }

    private UntypedAuraExpression Primary()
    {
        var line = Peek().Line;

        if (Match(TokType.False)) return new UntypedBoolLiteral(false, line);
        else if (Match(TokType.True)) return new UntypedBoolLiteral(true, line);
        else if (Match(TokType.Nil)) return new UntypedNil(line);
        else if (Match(TokType.StringLiteral)) return new UntypedStringLiteral(Previous().Value, line);
        else if (Match(TokType.CharLiteral)) return new UntypedCharLiteral(Previous().Value[0], line);
        else if (Match(TokType.IntLiteral))
        {
            var i = int.Parse(Previous().Value);
            return new UntypedIntLiteral(i, line);
        }
        else if (Match(TokType.FloatLiteral))
        {
            var d = double.Parse(Previous().Value, CultureInfo.InvariantCulture);
            return new UntypedFloatLiteral(d, line);
        }
        else if (Match(TokType.This)) return new UntypedThis(Previous(), line);
        else if (Match(TokType.Identifier))
        {
            var v = Previous();
            if (Match(TokType.LeftBracket))
            {
                int i = 0;

                if (Match(TokType.IntLiteral))
                {
                    // Parse int literal's value
                    i = int.Parse(Previous().Value);
                    // Check for index range
                    if (Match(TokType.Colon))
                    {
                        if (Match(TokType.RightBracket))
                        {
                            return new UntypedGetIndexRange(new UntypedVariable(v, line), new UntypedIntLiteral(i, line), new UntypedIntLiteral(-1, line), line);
                        }
                        Consume(TokType.IntLiteral, new ExpectIntLiteralException(Peek().Line));
                        var upper = int.Parse(Previous().Value);
                        Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Line));
                        return new UntypedGetIndexRange(new UntypedVariable(v, line), new UntypedIntLiteral(i, line), new UntypedIntLiteral(upper, line), line);
                    }
                    else
                    {
                        Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Line));
                        return new UntypedGetIndex(new UntypedVariable(v, line), new UntypedIntLiteral(i, line), line);
                    }
                }
                else if (Match(TokType.Minus))
                {
                    var intLiteral = Consume(TokType.IntLiteral, new ExpectIntLiteralException(Peek().Line));
                    // Parse int literal's value
                    i = int.Parse(intLiteral.Value);
                    i = -i;

                    if (Match(TokType.Colon))
                    {
                        if (Match(TokType.RightBracket))
                        {
                            return new UntypedGetIndexRange(new UntypedVariable(v, line), new UntypedIntLiteral(i, line), new UntypedIntLiteral(-1, line), line);
                        }
                        if (Match(TokType.Minus))
                        {
                            Consume(TokType.IntLiteral, new ExpectIntLiteralException(Peek().Line));
                            var upper_ = int.Parse(Previous().Value);
                            upper_ = -upper_;
                            Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Line));
                            return new UntypedGetIndexRange(new UntypedVariable(v, line), new UntypedIntLiteral(i, line), new UntypedIntLiteral(upper_, line), line);
                        }

                        Consume(TokType.IntLiteral, new ExpectIntLiteralException(Peek().Line));
                        var upper = int.Parse(Previous().Value);
                        return new UntypedGetIndexRange(new UntypedVariable(v, line), new UntypedIntLiteral(i, line), new UntypedIntLiteral(upper, line), line);
                    }
                }
                else if (Match(TokType.Colon))
                {
                    i = 0;
                    if (Match(TokType.RightBracket))
                    {
                        var upper_ = -1;
                        return new UntypedGetIndexRange(new UntypedVariable(v, line), new UntypedIntLiteral(i, line), new UntypedIntLiteral(upper_, line), line);
                    }
                    if (Match(TokType.Minus))
                    {
                        Consume(TokType.IntLiteral, new ExpectIntLiteralException(Peek().Line));
                        var upper_ = int.Parse(Previous().Value);
                        Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Line));
                        return new UntypedGetIndexRange(new UntypedVariable(v, line), new UntypedIntLiteral(i, line), new UntypedIntLiteral(upper_, line), line);
                    }

                    // Parse upper bound
                    Consume(TokType.IntLiteral, new ExpectIntLiteralException(Peek().Line));
                    var upper = int.Parse(Previous().Value);
                    Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Line));
                    return new UntypedGetIndexRange(new UntypedVariable(v, line), new UntypedIntLiteral(i, line), new UntypedIntLiteral(i, line), line);
                }
                else if (Match(TokType.Identifier))
                {
                    // Get variable's value
                    var var_ = Previous();
                    // Check for index
                    if (Match(TokType.Colon))
                    {
                        if (Match(TokType.RightBracket))
                        {
                            return new UntypedGetIndexRange(new UntypedVariable(v, line), new UntypedIntLiteral(i, line), new UntypedIntLiteral(-1, line), line);
                        }

                        Consume(TokType.IntLiteral, new ExpectIntLiteralException(Peek().Line));
                        var upper = int.Parse(Previous().Value);
                        Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Line));
                        return new UntypedGetIndexRange(new UntypedVariable(v, line), new UntypedVariable(var_, line), new UntypedIntLiteral(upper, line), line);
                    }
                    else
                    {
                        Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Line));
                        return new UntypedGetIndex(new UntypedVariable(v, line), new UntypedVariable(var_, line), line);
                    }
                }
                else if (Match(TokType.StringLiteral))
                {
                    var lit = Previous();
                    Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Line));
                    return new UntypedGetIndex(new UntypedVariable(v, line), new UntypedStringLiteral(lit.Value, line), line);
                }
                else
                {
                    throw new Exception(); // TODO
                }
            }

            return new UntypedVariable(Previous(), line);
        }
        else if (Match(TokType.If))
        {
            return IfExpr();
        }
        else if (Match(TokType.LeftParen))
        {
            var expression = Expression();
            Consume(TokType.RightParen, new ExpectRightParenException(Peek().Line));
            return new UntypedGrouping(expression, line);
        }
        else if (Match(TokType.LeftBrace))
        {
            return Block();
        }
        else if (Match(TokType.LeftBracket))
        {
            // Parse list's type
            ParseParameterType();
            Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Line));
            Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Line));

            var items = new List<UntypedAuraExpression>();
            while (!Match(TokType.RightBrace))
            {
                var expr = Expression();
                items.Add(expr);
                if (!Check(TokType.RightBrace))
                {
                    Consume(TokType.Comma, new ExpectCommaException(Peek().Line));
                }
            }

            if (Match(TokType.LeftBracket))
            {
                var i = 0;
                if (Match(TokType.IntLiteral))
                {
                    // Parse int literal's value
                    i = int.Parse(Previous().Value);
                }
                else if (Match(TokType.Minus))
                {
                    var prev = Consume(TokType.IntLiteral, new ExpectIntLiteralException(Peek().Line));
                    // Parse int literal's value
                    i = int.Parse(prev.Value);
                    i = -i;
                }

                Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Line));
                return new UntypedGetIndex(new UntypedListLiteral<UntypedAuraExpression>(items, line), new UntypedIntLiteral(i, line), line);
            }

            return new UntypedListLiteral<UntypedAuraExpression>(items, line);
        }
        else if (Match(TokType.Fn))
        {
            return AnonymousFunction();
        }
        else if (Match(TokType.Map))
        {
            // Parse map's type signature
            Consume(TokType.LeftBracket, new ExpectLeftBracketAfterMapKeywordException(Peek().Line));
            var keyType = ParseParameterType();
            Consume(TokType.Colon, new ExpectColonException(Peek().Line));
            var valueType = ParseParameterType();
            Consume(TokType.RightBracket, new ExpectRightBracketException(Peek().Line));
            Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Line));

            var d = new Dictionary<UntypedAuraExpression, UntypedAuraExpression>();
            while (!Match(TokType.RightBrace))
            {
                var key = Expression();
                Consume(TokType.Colon, new ExpectColonException(Peek().Line));
                var value = Expression();
                Consume(TokType.Comma, new ExpectCommaException(Peek().Line));
                Consume(TokType.Semicolon, new ExpectSemicolonException(Peek().Line)); // TODO don't add implicit semicolon after map items
                d[key] = value;
            }

            return new UntypedMapLiteral(d, keyType.Typ, valueType.Typ, line);
        }
        else if (Match(TokType.Tup))
        {
            // Parse tuple's type signature
            var typ = TypeTokenToType(Previous());
            Consume(TokType.LeftBrace, new ExpectLeftBraceException(Peek().Line));
            var items = new List<UntypedAuraExpression>();
            while (!Match(TokType.RightBrace))
            {
                var item = Expression();
                items.Add(item);
                Match(TokType.Comma);
            }

            return new UntypedTupleLiteral(items, (typ as Tuple).ElementTypes, line);
        }
        else
        {
            throw new ExpectExpressionException(Peek().Line);
        }
    }
}