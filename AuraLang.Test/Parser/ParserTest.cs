using AuraLang.AST;
using AuraLang.Exceptions.Parser;
using AuraLang.Parser;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using Newtonsoft.Json;
using String = AuraLang.Types.String;

namespace AuraLang.Test.Parser;

public class ParserTest
{
	[Test]
	public void TestParse_Assignment()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Identifier, "i", 1),
			new(TokType.Equal, "=", 1),
			new(TokType.IntLiteral, "5", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst,
			new UntypedExpressionStmt(
				new UntypedAssignment(
					new Tok(TokType.Identifier, "i", 1),
					new IntLiteral(5, 1),
					1
				), 1));
	}

	[Test]
	public void TestParse_Binary()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.IntLiteral, "1", 1),
			new(TokType.Plus, "+", 1),
			new(TokType.IntLiteral, "2", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst,
			new UntypedExpressionStmt(
				new UntypedBinary(
					new IntLiteral(1, 1),
					new Tok(TokType.Plus, "+", 1),
					new IntLiteral(2, 1),
					1
				), 1));
	}

	[Test]
	public void TestParse_Block()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.LeftBrace, "{", 1),
			new(TokType.IntLiteral, "1", 2),
			new(TokType.Semicolon, ";", 2),
			new(TokType.RightBrace, "}", 3),
			new(TokType.Semicolon, ";", 3),
			new(TokType.Eof, "eof", 3)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedBlock(
				new List<IUntypedAuraStatement>
				{
					new UntypedExpressionStmt(
						new IntLiteral(1, 2),
						2)
				},
				1),
			1));
	}

	[Test]
	public void TestParse_Call()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Identifier, "f", 1),
			new(TokType.LeftParen, "(", 1),
			new(TokType.RightParen, ")", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedCall(new UntypedVariable(new Tok(TokType.Identifier, "f", 1), 1),
				new List<(Tok?, IUntypedAuraExpression)>(), 1),
			1));
	}

	[Test]
	public void TestParse_Get()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Identifier, "greeter", 1),
			new(TokType.Dot, ".", 1),
			new(TokType.Identifier, "name", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1),
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedGet(
				new UntypedVariable(new Tok(TokType.Identifier, "greeter", 1), 1),
				new Tok(TokType.Identifier, "name", 1),
				1),
			1));
	}

	[Test]
	public void TestParse_GetIndex()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Identifier, "collection", 1),
			new(TokType.LeftBracket, "[", 1),
			new(TokType.IntLiteral, "0", 1),
			new(TokType.RightBracket, "]", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedGetIndex(
				new UntypedVariable(new Tok(TokType.Identifier, "collection", 1), 1),
				new IntLiteral(0, 1),
				1),
			1));
	}

	[Test]
	public void TestParse_GetIndex_Empty()
	{
		ArrangeAndAct_Invalid(
			new List<Tok>
			{
				new(TokType.Identifier, "collection", 1),
				new(TokType.LeftBracket, "[", 1),
				new(TokType.RightBracket, "]", 1),
				new(TokType.Semicolon, ";", 1),
				new(TokType.Eof, "eof", 1)
			}, typeof(PostfixIndexCannotBeEmptyException));
	}

	[Test]
	public void TestParse_GetIndexRange()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Identifier, "collection", 1),
			new(TokType.LeftBracket, "[", 1),
			new(TokType.IntLiteral, "0", 1),
			new(TokType.Colon, ":", 1),
			new(TokType.IntLiteral, "1", 1),
			new(TokType.RightBracket, "]", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedGetIndexRange(
				new UntypedVariable(new Tok(TokType.Identifier, "collection", 1), 1),
				new IntLiteral(0, 1),
				new IntLiteral(1, 1),
				1),
			1));
	}

	[Test]
	public void TestParse_Grouping()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.LeftParen, "(", 1),
			new(TokType.IntLiteral, "1", 1),
			new(TokType.RightParen, ")", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedGrouping(
				new IntLiteral(1, 1),
				1),
			1));
	}

	[Test]
	public void TestParse_If()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.If, "if", 1),
			new(TokType.True, "true", 1),
			new(TokType.LeftBrace, "{", 1),
			new(TokType.Return, "return", 2),
			new(TokType.IntLiteral, "1", 2),
			new(TokType.Semicolon, ";", 2),
			new(TokType.RightBrace, "}", 3),
			new(TokType.Semicolon, ";", 3),
			new(TokType.Eof, "eof", 3)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedIf(
				new BoolLiteral(true, 1),
				new UntypedBlock(
					new List<IUntypedAuraStatement>
					{
						new UntypedReturn(
							new IntLiteral(1, 2),
							2),
					},
					1),
				null,
				1),
			1));
	}

	[Test]
	public void TestParse_IntLiteral()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.IntLiteral, "5", 1), new(TokType.Semicolon, ";", 1), new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new IntLiteral(5, 1),
			1));
	}

	[Test]
	public void TestParse_FloatLiteral()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.FloatLiteral, "5.0", 1), new(TokType.Semicolon, ";", 1), new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new FloatLiteral(5.0, 1),
			1));
	}

	[Test]
	public void TestParse_StringLiteral()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.StringLiteral, "Hello", 1), new(TokType.Semicolon, ";", 1), new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new StringLiteral("Hello", 1),
			1));
	}

	[Test]
	public void TestParse_ListLiteral()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.LeftBracket, "[", 1),
			new(TokType.Int, "int", 1),
			new(TokType.RightBracket, "]", 1),
			new(TokType.LeftBrace, "{", 1),
			new(TokType.IntLiteral, "5", 1),
			new(TokType.Comma, ",", 1),
			new(TokType.IntLiteral, "6", 1),
			new(TokType.RightBrace, "}", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new ListLiteral<ITypedAuraExpression>(
				new List<ITypedAuraExpression> { new IntLiteral(5, 1), new IntLiteral(6, 1) },
				new Int(),
				1),
			1));
	}

	[Test]
	public void TestParse_MapLiteral_Get()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Map, "map", 1),
			new(TokType.LeftBracket, "[", 1),
			new(TokType.String, "string", 1),
			new(TokType.Colon, ":", 1),
			new(TokType.Int, "int", 1),
			new(TokType.RightBracket, "]", 1),
			new(TokType.LeftBrace, "{", 1),
			new(TokType.StringLiteral, "Hello", 2),
			new(TokType.Colon, ":", 2),
			new(TokType.IntLiteral, "1", 2),
			new(TokType.Comma, ",", 2),
			new(TokType.Semicolon, ";", 2),
			new(TokType.StringLiteral, "World", 3),
			new(TokType.Colon, ":", 3),
			new(TokType.IntLiteral, "2", 3),
			new(TokType.Comma, ",", 3),
			new(TokType.Semicolon, ";", 3),
			new(TokType.RightBrace, "}", 4),
			new(TokType.LeftBracket, "[", 4),
			new(TokType.StringLiteral, "Hello", 4),
			new(TokType.RightBracket, "]", 4),
			new(TokType.Semicolon, ";", 4),
			new(TokType.Eof, "eof", 4)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedGetIndex(new MapLiteral<ITypedAuraExpression, ITypedAuraExpression>(
				new Dictionary<ITypedAuraExpression, ITypedAuraExpression>
				{
					{ new StringLiteral("Hello", 2), new IntLiteral(1, 2) },
					{ new StringLiteral("World", 3), new IntLiteral(2, 3) }
				},
				new String(),
				new Int(),
				1), new StringLiteral("Hello", 4), 1),
			1));
	}

	[Test]
	public void TestParse_MapLiteral()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Map, "map", 1),
			new(TokType.LeftBracket, "[", 1),
			new(TokType.String, "string", 1),
			new(TokType.Colon, ":", 1),
			new(TokType.Int, "int", 1),
			new(TokType.RightBracket, "]", 1),
			new(TokType.LeftBrace, "{", 1),
			new(TokType.StringLiteral, "Hello", 2),
			new(TokType.Colon, ":", 2),
			new(TokType.IntLiteral, "1", 2),
			new(TokType.Comma, ",", 2),
			new(TokType.Semicolon, ";", 2),
			new(TokType.StringLiteral, "World", 3),
			new(TokType.Colon, ":", 3),
			new(TokType.IntLiteral, "2", 3),
			new(TokType.Comma, ",", 3),
			new(TokType.Semicolon, ";", 3),
			new(TokType.RightBrace, "}", 4),
			new(TokType.Semicolon, ";", 4),
			new(TokType.Eof, "eof", 4)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new MapLiteral<ITypedAuraExpression, ITypedAuraExpression>(
				new Dictionary<ITypedAuraExpression, ITypedAuraExpression>
				{
					{ new StringLiteral("Hello", 2), new IntLiteral(1, 2) },
					{ new StringLiteral("World", 3), new IntLiteral(2, 3) }
				},
				new String(),
				new Int(),
				1),
			1));
	}

	[Test]
	public void TestParse_BoolLiteral()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.True, "true", 1), new(TokType.Semicolon, ";", 1), new(TokType.Eof, "eof", 1),
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new BoolLiteral(true, 1),
			1));
	}

	[Test]
	public void TestParse_Nil()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Nil, "nil", 1), new(TokType.Semicolon, ";", 1), new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedNil(1),
			1));
	}

	[Test]
	public void TestParse_CharLiteral()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.CharLiteral, "c", 1), new(TokType.Semicolon, ";", 1), new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new CharLiteral('c', 1),
			1));
	}

	[Test]
	public void TestParse_Logical()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.True, "true", 1),
			new(TokType.Or, "or", 1),
			new(TokType.False, "false", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedLogical(
				new BoolLiteral(true, 1),
				new Tok(TokType.Or, "or", 1),
				new BoolLiteral(false, 1),
				1),
			1));
	}

	[Test]
	public void TestParse_Set()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Identifier, "greeter", 1),
			new(TokType.Dot, ".", 1),
			new(TokType.Identifier, "name", 1),
			new(TokType.Equal, "=", 1),
			new(TokType.StringLiteral, "Bob", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedSet(
				new UntypedVariable(new Tok(TokType.Identifier, "greeter", 1), 1),
				new Tok(TokType.Identifier, "name", 1),
				new StringLiteral("Bob", 1),
				1),
			1));
	}

	[Test]
	public void TestParse_This()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.This, "this", 1), new(TokType.Semicolon, ";", 1), new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedThis(new Tok(TokType.This, "this", 1), 1),
			1));
	}

	[Test]
	public void TestParse_Unary_Bang()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Bang, "!", 1),
			new(TokType.True, "true", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedUnary(
				new Tok(TokType.Bang, "!", 1),
				new BoolLiteral(true, 1),
				1),
			1));
	}

	[Test]
	public void TestParse_Unary_Minus()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Minus, "-", 1),
			new(TokType.IntLiteral, "5", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedUnary(
				new Tok(TokType.Minus, "-", 1),
				new IntLiteral(5, 1),
				1),
			1));
	}

	[Test]
	public void TestParse_Variable()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Identifier, "variable", 1), new(TokType.Semicolon, ";", 1), new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedVariable(new Tok(TokType.Identifier, "variable", 1), 1),
			1));
	}

	[Test]
	public void TestParse_Defer()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Defer, "defer", 1),
			new(TokType.Identifier, "f", 1),
			new(TokType.LeftParen, "(", 1),
			new(TokType.RightParen, ")", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedDefer(
			new UntypedCall(
				new UntypedVariable(new Tok(TokType.Identifier, "f", 1), 1),
				new List<(Tok?, IUntypedAuraExpression)>(),
				1),
			1));
	}

	[Test]
	public void TestParse_For()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.For, "for", 1),
			new(TokType.Identifier, "i", 1),
			new(TokType.ColonEqual, ":=", 1),
			new(TokType.IntLiteral, "0", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Identifier, "i", 1),
			new(TokType.Less, "<", 1),
			new(TokType.IntLiteral, "10", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Identifier, "i", 1),
			new(TokType.PlusPlus, "++", 1),
			new(TokType.LeftBrace, "{", 1),
			new(TokType.RightBrace, "}", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedFor(
			new UntypedLet(
				new Tok(TokType.Identifier, "i", 1),
				null,
				false,
				new IntLiteral(0, 1),
				1),
			new UntypedBinary(
				new UntypedVariable(new Tok(TokType.Identifier, "i", 1), 1),
				new Tok(TokType.Less, "<", 1),
				new IntLiteral(10, 1),
				1),
			new UntypedPlusPlusIncrement(new UntypedVariable(new Tok(TokType.Identifier, "i", 1), 1), 1),
			new List<IUntypedAuraStatement> { },
			1));
	}

	[Test]
	public void TestParse_ForEach()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.ForEach, "foreach", 1),
			new(TokType.Identifier, "i", 1),
			new(TokType.In, "in", 1),
			new(TokType.Identifier, "iter", 1),
			new(TokType.LeftBrace, "{", 1),
			new(TokType.RightBrace, "}", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedForEach(
			new Tok(TokType.Identifier, "i", 1),
			new UntypedVariable(new Tok(TokType.Identifier, "iter", 1), 1),
			new List<IUntypedAuraStatement>(),
			1));
	}

	[Test]
	public void TestParse_NamedFunction_ParamDefaultValue_Invalid()
	{
		ArrangeAndAct_Invalid(
			new List<Tok>
			{
				new(TokType.Fn, "fn", 1),
				new(TokType.Identifier, "f", 1),
				new(TokType.LeftParen, "(", 1),
				new(TokType.Identifier, "i", 1),
				new(TokType.Colon, ":", 1),
				new(TokType.Int, "int", 1),
				new(TokType.Equal, "=", 1),
				new(TokType.Identifier, "var", 1),
				new(TokType.RightParen, ")", 1),
				new(TokType.LeftBrace, "{", 1),
				new(TokType.RightBrace, "}", 1),
				new(TokType.Semicolon, ";", 1),
				new(TokType.Eof, "eof", 1)
			}, typeof(ParameterDefaultValueMustBeALiteralException));
	}

	[Test]
	public void TestParse_NamedFunction_NoParams_NoReturnType_NoBody()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Fn, "fn", 1),
			new(TokType.Identifier, "f", 1),
			new(TokType.LeftParen, "(", 1),
			new(TokType.RightParen, ")", 1),
			new(TokType.LeftBrace, "{", 1),
			new(TokType.RightBrace, "}", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedNamedFunction(
			new Tok(TokType.Identifier, "f", 1),
			new List<Param>(),
			new UntypedBlock(new List<IUntypedAuraStatement>(), 1),
			null,
			Visibility.Private,
			1));
	}

	[Test]
	public void TestParse_AnonymousFunction_NoParams_NoReturnType_NoBody()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Fn, "fn", 1),
			new(TokType.LeftParen, "(", 1),
			new(TokType.RightParen, ")", 1),
			new(TokType.LeftBrace, "{", 1),
			new(TokType.RightBrace, "}", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedAnonymousFunction(
				new List<Param>(),
				new UntypedBlock(new List<IUntypedAuraStatement>(), 1),
				null,
				1),
			1));
	}

	[Test]
	public void TestParse_Let_Long()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Let, "let", 1),
			new(TokType.Identifier, "i", 1),
			new(TokType.Colon, ":", 1),
			new(TokType.Int, "int", 1),
			new(TokType.Equal, "=", 1),
			new(TokType.IntLiteral, "5", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedLet(
			new Tok(TokType.Identifier, "i", 1),
			new Int(),
			false,
			new IntLiteral(5, 1),
			1));
	}

	[Test]
	public void TestParse_Let_Short()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Identifier, "i", 1),
			new(TokType.ColonEqual, ":=", 1),
			new(TokType.IntLiteral, "5", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedLet(
			new Tok(TokType.Identifier, "i", 1),
			null,
			false,
			new IntLiteral(5, 1),
			1));
	}

	[Test]
	public void TestParse_Mod()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Mod, "mod", 1),
			new(TokType.Identifier, "main", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedMod(new Tok(TokType.Identifier, "main", 1), 1));
	}

	[Test]
	public void TestParse_Return()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Return, "return", 1),
			new(TokType.IntLiteral, "5", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedReturn(
			new IntLiteral(5, 1),
			1));
	}

	[Test]
	public void TestParse_Interface_NoMethods()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Interface, "interface", 1),
			new(TokType.Identifier, "Greeter", 1),
			new(TokType.LeftBrace, "{", 1),
			new(TokType.RightBrace, "}", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedInterface(
			new Tok(TokType.Identifier, "Greeter", 1),
			new List<NamedFunction>(),
			Visibility.Private,
			1));
	}

	[Test]
	public void TestParse_Interface_OneMethod_NoParams_NoReturnType()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Interface, "interface", 1),
			new(TokType.Identifier, "Greeter", 1),
			new(TokType.LeftBrace, "{", 1),
			new(TokType.Fn, "fn", 2),
			new(TokType.Identifier, "say_hi", 2),
			new(TokType.LeftParen, "(", 2),
			new(TokType.RightParen, ")", 2),
			new(TokType.Semicolon, ";", 2),
			new(TokType.RightBrace, "}", 3),
			new(TokType.Semicolon, ";", 3),
			new(TokType.Eof, "eof", 3)
		});
		MakeAssertions(untypedAst, new UntypedInterface(
			new Tok(TokType.Identifier, "Greeter", 1),
			new List<NamedFunction> { new("say_hi", Visibility.Private, new Function(new List<Param>(), new Nil())) },
			Visibility.Private,
			1));
	}

	[Test]
	public void TestParse_Interface_OneMethod()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Interface, "interface", 1),
			new(TokType.Identifier, "Greeter", 1),
			new(TokType.LeftBrace, "{", 1),
			new(TokType.Fn, "fn", 2),
			new(TokType.Identifier, "say_hi", 2),
			new(TokType.LeftParen, "(", 2),
			new(TokType.Identifier, "i", 2),
			new(TokType.Colon, ":", 1),
			new(TokType.Int, "int", 1),
			new(TokType.RightParen, ")", 2),
			new(TokType.Arrow, "->", 2),
			new(TokType.String, "string", 2),
			new(TokType.Semicolon, ";", 2),
			new(TokType.RightBrace, "}", 3),
			new(TokType.Semicolon, ";", 3),
			new(TokType.Eof, "eof", 3)
		});
		MakeAssertions(untypedAst, new UntypedInterface(
			new Tok(TokType.Identifier, "Greeter", 1),
			new List<NamedFunction>
			{
				new(
					"say_hi",
					Visibility.Private,
					new Function(
						new List<Param>
						{
							new(
								new Tok(TokType.Identifier, "i", 2),
								new ParamType(
									new Int(),
									false,
									null))
						},
						new String())
				)
			},
			Visibility.Private,
			1));
	}

	[Test]
	public void TestParse_Class_ImplementingTwoInterfaces()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Class, "class", 1),
			new(TokType.Identifier, "c", 1),
			new(TokType.LeftParen, "(", 1),
			new(TokType.RightParen, ")", 1),
			new(TokType.Colon, ":", 1),
			new(TokType.Identifier, "IClass", 1),
			new(TokType.Comma, ",", 1),
			new(TokType.Identifier, "IClass2", 1),
			new(TokType.LeftBrace, "{", 1),
			new(TokType.RightBrace, "}", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedClass(
			new Tok(TokType.Identifier, "c", 1),
			new List<Param>(),
			new List<IUntypedAuraStatement>(),
			Visibility.Private,
			new List<Tok> { new(TokType.Identifier, "IClass", 1), new(TokType.Identifier, "IClass2", 1) },
			1));
	}

	[Test]
	public void TestParse_Class_ImplementingOneInterface()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Class, "class", 1),
			new(TokType.Identifier, "c", 1),
			new(TokType.LeftParen, "(", 1),
			new(TokType.RightParen, ")", 1),
			new(TokType.Colon, ":", 1),
			new(TokType.Identifier, "IClass", 1),
			new(TokType.LeftBrace, "{", 1),
			new(TokType.RightBrace, "}", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedClass(
			new Tok(TokType.Identifier, "c", 1),
			new List<Param>(),
			new List<IUntypedAuraStatement>(),
			Visibility.Private,
			new List<Tok> { new(TokType.Identifier, "IClass", 1) },
			1));
	}

	[Test]
	public void TestParse_Class_NoParams_NoMethods()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Class, "class", 1),
			new(TokType.Identifier, "c", 1),
			new(TokType.LeftParen, "(", 1),
			new(TokType.RightParen, ")", 1),
			new(TokType.LeftBrace, "{", 1),
			new(TokType.RightBrace, "}", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedClass(
			new Tok(TokType.Identifier, "c", 1),
			new List<Param>(),
			new List<IUntypedAuraStatement>(),
			Visibility.Private,
			new List<Tok>(),
			1));
	}

	[Test]
	public void TestParse_While_EmptyBody()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.While, "while", 1),
			new(TokType.True, "true", 1),
			new(TokType.LeftBrace, "{", 1),
			new(TokType.RightBrace, "}", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedWhile(
			new BoolLiteral(true, 1),
			new List<IUntypedAuraStatement>(),
			1));
	}

	[Test]
	public void TestParse_Import_NoAlias()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Import, "import", 1),
			new(TokType.Identifier, "external_pkg", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedImport(
			new Tok(TokType.Identifier, "external_pkg", 1),
			null,
			1));
	}

	[Test]
	public void TestParse_Import_Alias()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Import, "import", 1),
			new(TokType.Identifier, "external_pkg", 1),
			new(TokType.As, "as", 1),
			new(TokType.Identifier, "ep", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedImport(
			new Tok(TokType.Identifier, "external_pkg", 1),
			new Tok(TokType.Identifier, "ep", 1),
			1));
	}

	[Test]
	public void TestParse_Comment()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Comment, "// this is a comment", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedComment(
			new Tok(TokType.Comment, "// this is a comment", 1),
			1));
	}

	[Test]
	public void TestParse_Yield()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Yield, "yield", 1),
			new(TokType.IntLiteral, "5", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedYield(new IntLiteral(5, 1), 1));
	}

	[Test]
	public void TestParse_ClassImplementingInterface()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(TokType.Class, "class", 1),
			new(TokType.Identifier, "Greeter", 1),
			new(TokType.LeftParen, "(", 1),
			new(TokType.RightParen, ")", 1),
			new(TokType.Colon, ":", 1),
			new(TokType.Identifier, "IGreeter", 1),
			new(TokType.LeftBrace, "{", 1),
			new(TokType.RightBrace, "}", 1),
			new(TokType.Semicolon, ";", 1),
			new(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedClass(
			new Tok(TokType.Identifier, "Greeter", 1),
			new List<Param>(),
			new List<IUntypedAuraStatement>(),
			Visibility.Private,
			new List<Tok> { new(TokType.Identifier, "IGreeter", 1) },
			1));
	}

	[Test]
	public void TestParse_Is()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new Tok(TokType.Identifier, "v", 1),
			new Tok(TokType.Is, "is", 1),
			new Tok(TokType.Identifier, "IGreeter", 1),
			new Tok(TokType.Semicolon, ";", 1),
			new Tok(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedIs(
				new UntypedVariable(
					new Tok(
						TokType.Identifier,
						"v",
						1),
					1),
				new Tok(TokType.Identifier, "IGreeter", 1),
				1),
			1));
	}

	private List<IUntypedAuraStatement> ArrangeAndAct(List<Tok> tokens)
	{
		// Arrange
		var parser = new AuraParser(tokens, "Test");
		// Act
		return parser.Parse();
	}

	private void ArrangeAndAct_Invalid(List<Tok> tokens, Type expected)
	{
		// Arrange
		var parser = new AuraParser(tokens, "Test");
		try
		{
			parser.Parse();
			Assert.Fail();
		}
		catch (ParserExceptionContainer e)
		{
			Assert.That(e.Exs.First(), Is.TypeOf(expected));
		}
	}

	private void MakeAssertions(List<IUntypedAuraStatement> untypedAst, IUntypedAuraStatement expected)
	{
		Assert.Multiple(() =>
		{
			Assert.That(untypedAst, Is.Not.Null);
			Assert.That(untypedAst, Has.Count.EqualTo(1));

			var expectedJson = JsonConvert.SerializeObject(expected);
			var actualJson = JsonConvert.SerializeObject(untypedAst[0]);
			Assert.That(actualJson, Is.EqualTo(expectedJson));
		});
	}
}

