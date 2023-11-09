using AuraLang.AST;
using AuraLang.Parser;
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
			new Tok(TokType.Identifier, "i", 1),
			new Tok(TokType.Equal, "=", 1),
			new Tok(TokType.IntLiteral, "5", 1),
			new Tok(TokType.Semicolon, ";", 1),
			new Tok(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst,
			new UntypedExpressionStmt(
				new UntypedAssignment(
					new Tok(TokType.Identifier, "i", 1),
					new UntypedIntLiteral(5, 1),
					1
				), 1));
	}

	[Test]
	public void TestParse_Binary()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new Tok(TokType.IntLiteral, "1", 1),
			new Tok(TokType.Plus, "+", 1),
			new Tok(TokType.IntLiteral, "2", 1),
			new Tok(TokType.Semicolon, ";", 1),
			new Tok(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst,
			new UntypedExpressionStmt(
				new UntypedBinary(
					new UntypedIntLiteral(1, 1),
					new Tok(TokType.Plus, "+", 1),
					new UntypedIntLiteral(2, 1),
					1
				), 1));
	}

	[Test]
	public void TestParse_Block()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new Tok(TokType.LeftBrace, "{", 1),
			new Tok(TokType.IntLiteral, "1", 2),
			new Tok(TokType.Semicolon, ";", 2),
			new Tok(TokType.RightBrace, "}", 3),
			new Tok(TokType.Semicolon, ";", 3),
			new Tok(TokType.Eof, "eof", 3)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedBlock(
				new List<UntypedAuraStatement>
				{
					new UntypedReturn(
						new UntypedIntLiteral((long)1, 2),
						false,
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
			new Tok(TokType.Identifier, "f", 1),
			new Tok(TokType.LeftParen, "(", 1),
			new Tok(TokType.RightParen, ")", 1),
			new Tok(TokType.Semicolon, ";", 1),
			new Tok(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedCall(new UntypedVariable(new Tok(TokType.Identifier, "f", 1), 1),
			new List<UntypedAuraExpression>(), 1),
			1));
	}

	[Test]
	public void TestParse_Get()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new Tok(TokType.Identifier, "greeter", 1),
			new Tok(TokType.Dot, ".", 1),
			new Tok(TokType.Identifier, "name", 1),
			new Tok(TokType.Semicolon, ";", 1),
			new Tok(TokType.Eof, "eof", 1),
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
			new Tok(TokType.Identifier, "collection", 1),
			new Tok(TokType.LeftBracket, "[", 1),
			new Tok(TokType.IntLiteral, "0", 1),
			new Tok(TokType.RightBracket, "]", 1),
			new Tok(TokType.Semicolon, ";", 1),
			new Tok(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedGetIndex(
				new UntypedVariable(new Tok(TokType.Identifier, "collection", 1), 1),
				new UntypedIntLiteral(0, 1),
				1),
			1));
	}

	[Test]
	public void TestParse_GetIndexRange()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new Tok(TokType.Identifier, "collection", 1),
			new Tok(TokType.LeftBracket, "[", 1),
			new Tok(TokType.IntLiteral, "0", 1),
			new Tok(TokType.Colon, ":", 1),
			new Tok(TokType.IntLiteral, "1", 1),
			new Tok(TokType.RightBracket, "]", 1),
			new Tok(TokType.Semicolon, ";", 1),
			new Tok(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedGetIndexRange(
				new UntypedVariable(new Tok(TokType.Identifier, "collection", 1), 1),
				new UntypedIntLiteral(0, 1),
				new UntypedIntLiteral(1, 1),
				1),
			1));
	}

	[Test]
	public void TestParse_Grouping()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new Tok(TokType.LeftParen, "(", 1),
			new Tok(TokType.IntLiteral, "1", 1),
			new Tok(TokType.RightParen, ")", 1),
			new Tok(TokType.Semicolon, ";", 1),
			new Tok(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedGrouping(
				new UntypedIntLiteral(1, 1),
				1),
			1));
	}

	[Test]
	public void TestParse_If()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new Tok(TokType.If, "if", 1),
			new Tok(TokType.True, "true", 1),
			new Tok(TokType.LeftBrace, "{", 1),
			new Tok(TokType.Return, "return", 2),
			new Tok(TokType.IntLiteral, "1", 2),
			new Tok(TokType.Semicolon, ";", 2),
			new Tok(TokType.RightBrace, "}", 3),
			new Tok(TokType.Semicolon, ";", 3),
			new Tok(TokType.Eof, "eof", 3)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedIf(
				new UntypedBoolLiteral(true, 1),
				new UntypedBlock(
					new List<UntypedAuraStatement>
					{
						new UntypedReturn(
							new UntypedIntLiteral(1, 2),
							true,
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
			new Tok(TokType.IntLiteral, "5", 1),
			new Tok(TokType.Semicolon, ";", 1),
			new Tok(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedIntLiteral(5, 1),
			1));
	}

	[Test]
	public void TestParse_FloatLiteral()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new Tok(TokType.FloatLiteral, "5.0", 1),
			new Tok(TokType.Semicolon, ";", 1),
			new Tok(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedFloatLiteral(5.0, 1),
			1));
	}

	[Test]
	public void TestParse_StringLiteral()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new Tok(TokType.StringLiteral, "Hello", 1),
			new Tok(TokType.Semicolon, ";", 1),
			new Tok(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedStringLiteral("Hello", 1),
			1));
	}

	[Test]
	public void TestParse_ListLiteral()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new Tok(TokType.LeftBracket, "[", 1),
			new Tok(TokType.Int, "int", 1),
			new Tok(TokType.RightBracket, "]", 1),
			new Tok(TokType.LeftBrace, "{", 1),
			new Tok(TokType.IntLiteral, "5", 1),
			new Tok(TokType.Comma, ",", 1),
			new Tok(TokType.IntLiteral, "6", 1),
			new Tok(TokType.RightBrace, "}", 1),
			new Tok(TokType.Semicolon, ";", 1),
			new Tok(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedListLiteral<Object>(
				new List<Object>
				{
					new UntypedIntLiteral(5, 1),
					new UntypedIntLiteral(6, 1)
				},
				1),
			1));
	}

	[Test]
	public void TestParse_MapLiteral()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new Tok(TokType.Map, "map", 1),
			new Tok(TokType.LeftBracket, "[", 1),
			new Tok(TokType.String, "string", 1),
			new Tok(TokType.Colon, ":", 1),
			new Tok(TokType.Int, "int", 1),
			new Tok(TokType.RightBracket, "]", 1),
			new Tok(TokType.LeftBrace, "{", 1),
			new Tok(TokType.StringLiteral, "Hello", 2),
			new Tok(TokType.Colon, ":", 2),
			new Tok(TokType.IntLiteral, "1", 2),
			new Tok(TokType.Comma, ",", 2),
			new Tok(TokType.Semicolon, ";", 2),
			new Tok(TokType.StringLiteral, "World", 3),
			new Tok(TokType.Colon, ":", 3),
			new Tok(TokType.IntLiteral, "2", 3),
			new Tok(TokType.Comma, ",", 3),
			new Tok(TokType.Semicolon, ";", 3),
			new Tok(TokType.RightBrace, "}", 4),
			new Tok(TokType.Semicolon, ";", 4),
			new Tok(TokType.Eof, "eof", 4)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedMapLiteral(
				new Dictionary<Object, Object>
				{
					{
						new UntypedStringLiteral("Hello", 2),
						new UntypedIntLiteral(1, 2)
					},
					{
						new UntypedStringLiteral("World", 3),
						new UntypedIntLiteral(2, 3)
					}
				},
                new String(),
				new Int(),
				1),
			1));
	}

	[Test]
	public void TestParse_TupleLiteral()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new Tok(TokType.Tup, "tup", 1),
			new Tok(TokType.LeftBracket, "[", 1),
			new Tok(TokType.Int, "int", 1),
			new Tok(TokType.Comma, ",", 1),
			new Tok(TokType.String, "string", 1),
			new Tok(TokType.RightBracket, "]", 1),
			new Tok(TokType.LeftBrace, "{", 1),
			new Tok(TokType.IntLiteral, "1", 1),
			new Tok(TokType.Comma, ",", 1),
			new Tok(TokType.StringLiteral, "Hello world", 1),
			new Tok(TokType.RightBrace, "}", 1),
			new Tok(TokType.Semicolon, ";", 1),
			new Tok(TokType.Eof, "eof", 1),
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedTupleLiteral(
				new List<Object>
				{
					new UntypedIntLiteral(1, 1),
					new UntypedStringLiteral("Hello world", 1)
				},
				new List<AuraType>
				{
					new Int(),
					new String()
				},
				1),
			1));
	}

	[Test]
	public void TestParse_BoolLiteral()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new Tok(TokType.True, "true", 1),
			new Tok(TokType.Semicolon, ";", 1),
			new Tok(TokType.Eof, "eof", 1),
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedBoolLiteral(true, 1),
			1));
	}

	[Test]
	public void TestParse_Nil()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new Tok(TokType.Nil, "nil", 1),
			new Tok(TokType.Semicolon, ";", 1),
			new Tok(TokType.Eof, "eof", 1)
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
			new Tok(TokType.CharLiteral, "c", 1),
			new Tok(TokType.Semicolon, ";", 1),
			new Tok(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedCharLiteral('c', 1),
			1));
	}

	[Test]
	public void TestParse_Logical()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new Tok(TokType.True, "true", 1),
			new Tok(TokType.Or, "or", 1),
			new Tok(TokType.False, "false", 1),
			new Tok(TokType.Semicolon, ";", 1),
			new Tok(TokType.Eof, "eof", 1)
		});
		MakeAssertions(untypedAst, new UntypedExpressionStmt(
			new UntypedLogical(
				new UntypedBoolLiteral(true, 1),
				new Tok(TokType.Or, "or", 1),
				new UntypedBoolLiteral(false, 1),
				1),
			1));
	}

	private List<UntypedAuraStatement> ArrangeAndAct(List<Tok> tokens)
	{
		// Arrange
		var parser = new AuraParser(tokens);
		// Act
		return parser.Parse();
	}

	private void MakeAssertions(List<UntypedAuraStatement> untypedAst, UntypedAuraStatement expected)
	{
		Assert.Multiple(() =>
		{
			Assert.That(untypedAst, Is.Not.Null);
			Assert.That(untypedAst!, Has.Count.EqualTo(1));

			var expectedJson = JsonConvert.SerializeObject(expected);
			var actualJson = JsonConvert.SerializeObject(untypedAst[0]);
			Assert.That(actualJson, Is.EqualTo(expectedJson));
		});
	}
}

