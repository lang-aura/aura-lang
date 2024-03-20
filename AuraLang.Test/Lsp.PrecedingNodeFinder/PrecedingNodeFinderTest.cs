using AuraLang.AST;
using AuraLang.Location;
using AuraLang.Lsp.PrecedingNodeFinder;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using Range = AuraLang.Location.Range;

namespace AuraLang.Test.Lsp.PrecedingNodeFinder;

public class PrecedingNodeFinderTest
{
	[Test]
	public void TestPrecedingNodeFinder_Assignment()
	{
		// Arrange
		var intLiteral = new IntLiteral(
			new Tok(
				TokType.IntLiteral,
				"5",
				new Range(new Position(4, 0), new Position(5, 0))
			)
		);
		var assignment = new TypedAssignment(
			new Tok(
				TokType.Identifier,
				"a",
				new Range(new Position(), new Position(1, 0))
			),
			intLiteral,
			new AuraInt()
		);
		var after = new Position(6, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			assignment,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { intLiteral, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_AnonymousFunction()
	{
		// Arrange
		var anonymousFunction = new TypedAnonymousFunction(
			new Tok(
				TokType.Fn,
				"fn",
				new Range()
			),
			new List<Param>(),
			new TypedBlock(
				new Tok(
					TokType.LeftBrace,
					"{",
					new Range(new Position(4, 0), new Position(5, 0))
				),
				new List<ITypedAuraStatement>(),
				new Tok(
					TokType.RightBrace,
					"}",
					new Range(new Position(6, 0), new Position(7, 0))
				),
				new AuraNil()
			),
			new AuraNil()
		);
		var after = new Position(8, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			anonymousFunction,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { anonymousFunction, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_PlusPlusIncrement()
	{
		var plusPlusIncrement = new TypedPlusPlusIncrement(
			new TypedVariable(
				new Tok(
					TokType.Identifier,
					"x",
					new Range()
				),
				new AuraInt()
			),
			new Tok(
				TokType.PlusPlus,
				"++",
				new Range(new Position(1, 0), new Position(3, 0))
			),
			new AuraInt()
		);
		var after = new Position(4, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			plusPlusIncrement,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { plusPlusIncrement, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_MinusMinusDecrement()
	{
		var minusMinusDecrement = new TypedMinusMinusDecrement(
			new TypedVariable(
				new Tok(
					TokType.Identifier,
					"x",
					new Range()
				),
				new AuraInt()
			),
			new Tok(
				TokType.MinusMinus,
				"--",
				new Range(new Position(1, 0), new Position(3, 0))
			),
			new AuraInt()
		);
		var after = new Position(4, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			minusMinusDecrement,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { minusMinusDecrement, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Binary()
	{
		// Arrange
		var binary = new TypedBinary(
			new IntLiteral(
				new Tok(
					TokType.IntLiteral,
					"5",
					new Range()
				)
			),
			new Tok(TokType.Plus, "+"),
			new IntLiteral(
				new Tok(
					TokType.IntLiteral,
					"6",
					new Range(new Position(4, 0), new Position(5, 0))
				)
			),
			new AuraInt()
		);
		var after = new Position(6, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			binary,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { binary, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Block()
	{
		var block = new TypedBlock(
			new Tok(
				TokType.LeftBrace,
				"{",
				new Range()
			),
			new List<ITypedAuraStatement>(),
			new Tok(
				TokType.RightBrace,
				"}",
				new Range(new Position(1, 0), new Position(2, 0))
			),
			new AuraNil()
		);
		var after = new Position(3, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			block,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { block, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Call()
	{
		var call = new TypedCall(
			new TypedVariable(
				new Tok(
					TokType.Identifier,
					"f",
					new Range()
				),
				new AuraNamedFunction(
					"f",
					Visibility.Public,
					new AuraFunction(new List<Param>(), new AuraNil())
				)
			),
			new List<ITypedAuraExpression>(),
			new Tok(
				TokType.RightParen,
				")",
				new Range(new Position(2, 0), new Position(3, 0))
			),
			new AuraNamedFunction(
				"f",
				Visibility.Public,
				new AuraFunction(new List<Param>(), new AuraNil())
			)
		);
		var after = new Position(4, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			call,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { call, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Get()
	{
		var get = new TypedGet(
			new TypedVariable(
				new Tok(
					TokType.Identifier,
					"c",
					new Range()
				),
				new AuraClass(
					"c",
					new List<Param>(),
					new List<AuraNamedFunction>(),
					new List<AuraInterface>(),
					Visibility.Public
				)
			),
			new Tok(
				TokType.Identifier,
				"tmp",
				new Range(new Position(2, 0), new Position(5, 0))
			),
			new AuraInt()
		);
		var after = new Position(6, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			get,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { get, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_GetIndex()
	{
		var getIndex = new TypedGetIndex(
			new ListLiteral<IntLiteral>(
				new Tok(
					TokType.LeftBracket,
					"[",
					new Range()
				),
				new List<IntLiteral> { new(new Tok(TokType.IntLiteral, "1")) },
				new AuraInt(),
				new Tok(
					TokType.RightBrace,
					"}",
					new Range(new Position(7, 0), new Position(8, 0))
				)
			),
			new IntLiteral(new Tok(TokType.IntLiteral, "0")),
			new Tok(
				TokType.RightBracket,
				"]",
				new Range(new Position(10, 0), new Position(11, 0))
			),
			new AuraInt()
		);
		var after = new Position(12, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			getIndex,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { getIndex, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_GetIndexRange()
	{
		var getIndexRange = new TypedGetIndexRange(
			new ListLiteral<IntLiteral>(
				new Tok(
					TokType.LeftBracket,
					"[",
					new Range()
				),
				new List<IntLiteral> { new(new Tok(TokType.IntLiteral, "1")) },
				new AuraInt(),
				new Tok(
					TokType.RightBrace,
					"}",
					new Range(new Position(7, 0), new Position(8, 0))
				)
			),
			new IntLiteral(new Tok(TokType.IntLiteral, "0")),
			new IntLiteral(new Tok(TokType.IntLiteral, "2")),
			new Tok(
				TokType.RightBracket,
				"]",
				new Range(new Position(10, 0), new Position(13, 0))
			),
			new AuraInt()
		);
		var after = new Position(14, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			getIndexRange,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { getIndexRange, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Grouping()
	{
		var grouping = new TypedGrouping(
			new Tok(
				TokType.LeftParen,
				"(",
				new Range()
			),
			new IntLiteral(new Tok(TokType.IntLiteral, "5")),
			new Tok(
				TokType.RightParen,
				")",
				new Range(new Position(2, 0), new Position(3, 0))
			),
			new AuraInt()
		);
		var after = new Position(4, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			grouping,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { grouping, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_If()
	{
		var @if = new TypedIf(
			new Tok(
				TokType.If,
				"if",
				new Range()
			),
			new BoolLiteral(new Tok(TokType.True, "true")),
			new TypedBlock(
				new Tok(TokType.LeftBrace, "{"),
				new List<ITypedAuraStatement>(),
				new Tok(
					TokType.RightBrace,
					"}",
					new Range(new Position(9, 0), new Position(10, 0))
				),
				new AuraNil()
			),
			null,
			new AuraNil()
		);
		var after = new Position(11, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			@if,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { @if, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Nil()
	{
		var nil = new TypedNil(
			new Tok(
				TokType.Nil,
				"nil",
				new Range(new Position(), new Position(3, 0))
			)
		);
		var after = new Position(4, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			nil,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { nil, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Logical()
	{
		var logical = new TypedLogical(
			new BoolLiteral(
				new Tok(
					TokType.True,
					"true",
					new Range()
				)
			),
			new Tok(TokType.And, "and"),
			new BoolLiteral(
				new Tok(
					TokType.False,
					"false",
					new Range(new Position(9, 0), new Position(14, 0))
				)
			),
			new AuraBool()
		);
		var after = new Position(15, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			logical,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { logical, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Set()
	{
		var set = new TypedSet(
			new TypedVariable(
				new Tok(
					TokType.Identifier,
					"c",
					new Range()
				),
				new AuraClass(
					"c",
					new List<Param>(),
					new List<AuraNamedFunction>(),
					new List<AuraInterface>(),
					Visibility.Public
				)
			),
			new Tok(TokType.Identifier, "tmp"),
			new IntLiteral(
				new Tok(
					TokType.IntLiteral,
					"5",
					new Range(new Position(10, 0), new Position(15, 0))
				)
			),
			new AuraInt()
		);
		var after = new Position(16, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			set,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { set, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_This()
	{
		var @this = new TypedThis(
			new Tok(
				TokType.This,
				"this",
				new Range(new Position(), new Position(4, 0))
			),
			new AuraClass(
				"tmp",
				new List<Param>(),
				new List<AuraNamedFunction>(),
				new List<AuraInterface>(),
				Visibility.Public
			)
		);
		var after = new Position(5, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			@this,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { @this, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Unary()
	{
		var unary = new TypedUnary(
			new Tok(
				TokType.Bang,
				"!",
				new Range()
			),
			new BoolLiteral(
				new Tok(
					TokType.True,
					"true",
					new Range(new Position(1, 0), new Position(5, 0))
				)
			),
			new AuraBool()
		);
		var after = new Position(6, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			unary,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { unary, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Variable()
	{
		var variable = new TypedVariable(
			new Tok(
				TokType.Identifier,
				"x",
				new Range(new Position(), new Position(1, 0))
			),
			new AuraInt()
		);
		var after = new Position(2, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			variable,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { variable, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Is()
	{
		var @is = new TypedIs(
			new TypedVariable(
				new Tok(
					TokType.Identifier,
					"x",
					new Range(new Position(), new Position(1, 0))
				),
				new AuraInt()
			),
			new TypedInterfacePlaceholder(
				new Tok(
					TokType.Identifier,
					"IGreeter",
					new Range(new Position(5, 0), new Position(12, 0))
				),
				new AuraInterface(
					"IGreeter",
					new List<AuraNamedFunction>(),
					Visibility.Public
				)
			)
		);
		var after = new Position(13, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			@is,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { @is, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_InterfacePlaceholder()
	{
		var ip = new TypedInterfacePlaceholder(
			new Tok(
				TokType.Identifier,
				"IGreeter",
				new Range(new Position(), new Position(7, 0))
			),
			new AuraInterface(
				"IGreeter",
				new List<AuraNamedFunction>(),
				Visibility.Public
			)
		);
		var after = new Position(8, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			ip,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { ip, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_IntLiteral()
	{
		var intLiteral = new IntLiteral(
			new Tok(
				TokType.IntLiteral,
				"5",
				new Range(new Position(), new Position(1, 0))
			)
		);
		var after = new Position(2, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			intLiteral,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { intLiteral, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_FloatLiteral()
	{
		var floatLiteral = new FloatLiteral(
			new Tok(
				TokType.FloatLiteral,
				"5.0",
				new Range(new Position(), new Position(3, 0))
			)
		);
		var after = new Position(4, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			floatLiteral,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { floatLiteral, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_StringLiteral()
	{
		var stringLiteral = new StringLiteral(
			new Tok(
				TokType.StringLiteral,
				"Hello",
				new Range(new Position(), new Position(5, 0))
			)
		);
		var after = new Position(6, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			stringLiteral,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { stringLiteral, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_BoolLiteral()
	{
		var boolLiteral = new BoolLiteral(
			new Tok(
				TokType.True,
				"true",
				new Range(new Position(), new Position(4, 0))
			)
		);
		var after = new Position(5, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			boolLiteral,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { boolLiteral, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_CharLiteral()
	{
		var charLiteral = new CharLiteral(
			new Tok(
				TokType.CharLiteral,
				"a",
				new Range(new Position(), new Position(1, 0))
			)
		);
		var after = new Position(2, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			charLiteral,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { charLiteral, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_AnonymousStruct()
	{
		var anonymousStruct = new TypedAnonymousStruct(
			new Tok(
				TokType.Identifier,
				"s",
				new Range(new Position(), new Position(1, 0))
			),
			new List<Param>(),
			new List<ITypedAuraExpression>(),
			new Tok(
				TokType.RightParen,
				")",
				new Range(new Position(8, 0), new Position(9, 0))
			)
		);
		var after = new Position(10, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			anonymousStruct,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { anonymousStruct, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_MapLiteral()
	{
		var mapLiteral = new MapLiteral<ITypedAuraExpression, ITypedAuraExpression>(
			new Tok(
				TokType.Map,
				"map",
				new Range()
			),
			new Dictionary<ITypedAuraExpression, ITypedAuraExpression>(),
			new AuraString(),
			new AuraInt(),
			new Tok(
				TokType.RightBrace,
				"}",
				new Range(new Position(15, 0), new Position(16, 0))
			)
		);
		var after = new Position(17, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			mapLiteral,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { mapLiteral, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_ListLiteral()
	{
		var listLiteral = new ListLiteral<ITypedAuraExpression>(
			new Tok(
				TokType.LeftBracket,
				"[",
				new Range()
			),
			new List<ITypedAuraExpression>(),
			new AuraString(),
			new Tok(
				TokType.RightBrace,
				"}",
				new Range(new Position(10, 0), new Position(11, 0))
			)
		);
		var after = new Position(12, 0);
		var notAfter = new Position();

		ActAndAssert_Expression(
			listLiteral,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { listLiteral, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Defer()
	{
		var typedCall = new TypedCall(
			new TypedVariable(
				new Tok(TokType.Identifier, "f"),
				new AuraNamedFunction(
					"f",
					Visibility.Private,
					new AuraFunction(new List<Param>(), new AuraNil())
				)
			),
			new List<ITypedAuraExpression>(),
			new Tok(
				TokType.RightParen,
				")",
				new Range(new Position(8, 0), new Position(9, 0))
			),
			new AuraFunction(new List<Param>(), new AuraNil())
		);
		var defer = new TypedDefer(
			new Tok(
				TokType.Defer,
				"defer",
				new Range()
			),
			typedCall
		);
		var after = new Position(10, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			defer,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { typedCall, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_For()
	{
		var forLoop = new TypedFor(
			new Tok(
				TokType.For,
				"for",
				new Range()
			),
			null,
			null,
			null,
			new List<ITypedAuraStatement>(),
			new Tok(
				TokType.RightBrace,
				"}",
				new Range(new Position(10, 0), new Position(11, 0))
			)
		);
		var after = new Position(12, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			forLoop,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { forLoop, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_ForEach()
	{
		var foreachLoop = new TypedForEach(
			new Tok(
				TokType.ForEach,
				"foreach",
				new Range()
			),
			new Tok(TokType.Identifier, "each"),
			new ListLiteral<ITypedAuraExpression>(
				new Tok(TokType.LeftBracket, "["),
				new List<ITypedAuraExpression>(),
				new AuraString(),
				new Tok(TokType.RightBrace, "}")
			),
			new List<ITypedAuraStatement>(),
			new Tok(
				TokType.RightBrace,
				"}",
				new Range(new Position(10, 0), new Position(11, 0))
			)
		);
		var after = new Position(12, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			foreachLoop,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { foreachLoop, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_NamedFunction()
	{
		var namedFunction = new TypedNamedFunction(
			new Tok(
				TokType.Fn,
				"fn",
				new Range(new Position(), new Position(2, 0))
			),
			new Tok(TokType.Identifier, "f"),
			new List<Param>(),
			new TypedBlock(
				new Tok(TokType.LeftBrace, "{"),
				new List<ITypedAuraStatement>(),
				new Tok(
					TokType.RightBrace,
					"}",
					new Range(new Position(8, 0), new Position(9, 0))
				),
				new AuraNil()
			),
			new AuraNil(),
			Visibility.Private,
			null
		);
		var after = new Position(10, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			namedFunction,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { namedFunction, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Let()
	{
		var init = new IntLiteral(
			new Tok(
				TokType.IntLiteral,
				"5",
				new Range(new Position(10, 0), new Position(11, 0))
			)
		);
		var let = new TypedLet(
			new Tok(
				TokType.Let,
				"let",
				new Range(new Position(), new Position(3, 0))
			),
			new List<(bool, Tok)> { (false, new Tok(TokType.Identifier, "i")) },
			true,
			init
		);
		var after = new Position(12, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			let,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { init, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Mod()
	{
		var mod = new TypedMod(
			new Tok(
				TokType.Mod,
				"mod",
				new Range(new Position(), new Position(3, 0))
			),
			new Tok(
				TokType.Identifier,
				"main",
				new Range(new Position(4, 0), new Position(8, 0))
			)
		);
		var after = new Position(9, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			mod,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { mod, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Return_NoValue()
	{
		var @return = new TypedReturn(
			new Tok(
				TokType.Return,
				"return",
				new Range(new Position(), new Position(6, 0))
			),
			null
		);
		var after = new Position(7, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			@return,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { @return, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Return_Value()
	{
		var intLiteral = new IntLiteral(
			new Tok(
				TokType.IntLiteral,
				"5",
				new Range(new Position(7, 0), new Position(8, 0))
			)
		);
		var @return = new TypedReturn(
			new Tok(
				TokType.Return,
				"return",
				new Range(new Position(), new Position(6, 0))
			),
			intLiteral
		);
		var after = new Position(9, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			@return,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { intLiteral, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Class()
	{
		var @class = new FullyTypedClass(
			new Tok(
				TokType.Class,
				"class",
				new Range(new Position(), new Position(5, 0))
			),
			new Tok(TokType.Identifier, "c"),
			new List<Param>(),
			new List<TypedNamedFunction>(),
			Visibility.Private,
			new List<AuraInterface>(),
			new Tok(
				TokType.RightBrace,
				"}",
				new Range(new Position(8, 0), new Position(9, 0))
			),
			null
		);
		var after = new Position(10, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			@class,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { @class, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Interface()
	{
		var @interface = new TypedInterface(
			new Tok(
				TokType.Interface,
				"interface",
				new Range(new Position(), new Position(9, 0))
			),
			new Tok(TokType.Identifier, "i"),
			new List<TypedFunctionSignature>(),
			Visibility.Private,
			new Tok(
				TokType.RightBrace,
				"}",
				new Range(new Position(10, 0), new Position(11, 0))
			),
			null
		);
		var after = new Position(12, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			@interface,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { @interface, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_FnSignature()
	{
		var fnSignature = new TypedFunctionSignature(
			null,
			new Tok(
				TokType.Fn,
				"fn",
				new Range(new Position(), new Position(2, 0))
			),
			new Tok(TokType.Identifier, "f"),
			new List<Param>(),
			new Tok(
				TokType.RightParen,
				")",
				new Range(new Position(5, 0), new Position(6, 0))
			),
			new AuraNil(),
			null
		);
		var after = new Position(7, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			fnSignature,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { fnSignature, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_While()
	{
		var @while = new TypedWhile(
			new Tok(
				TokType.While,
				"while",
				new Range(new Position(), new Position(5, 0))
			),
			new BoolLiteral(new Tok(TokType.True, "true")),
			new List<ITypedAuraStatement>(),
			new Tok(
				TokType.RightBrace,
				"}",
				new Range(new Position(7, 0), new Position(8, 0))
			)
		);
		var after = new Position(9, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			@while,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { @while, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Import()
	{
		var import = new TypedImport(
			new Tok(
				TokType.Import,
				"import",
				new Range(new Position(), new Position(6, 0))
			),
			new Tok(
				TokType.Identifier,
				"tmp",
				new Range(new Position(8, 0), new Position(9, 0))
			),
			null
		);
		var after = new Position(10, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			import,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { import, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_MultipleImport()
	{
		var multipleImport = new TypedMultipleImport(
			new Tok(
				TokType.Import,
				"import",
				new Range(new Position(), new Position(6, 0))
			),
			new List<TypedImport>(),
			new Tok(
				TokType.RightParen,
				")",
				new Range(new Position(8, 0), new Position(9, 0))
			)
		);
		var after = new Position(10, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			multipleImport,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { multipleImport, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Comment()
	{
		var comment = new TypedComment(
			new Tok(
				TokType.Comment,
				"// comment",
				new Range(new Position(), new Position(10, 0))
			)
		);
		var after = new Position(11, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			comment,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { comment, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Continue()
	{
		var @continue = new TypedContinue(
			new Tok(
				TokType.Continue,
				"continue",
				new Range(new Position(), new Position(8, 0))
			)
		);
		var after = new Position(9, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			@continue,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { @continue, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Break()
	{
		var @break = new TypedBreak(
			new Tok(
				TokType.Break,
				"break",
				new Range(new Position(), new Position(5, 0))
			)
		);
		var after = new Position(6, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			@break,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { @break, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Yield()
	{
		var yield = new TypedYield(
			new Tok(
				TokType.Yield,
				"yield",
				new Range(new Position(), new Position(5, 0))
			),
			new IntLiteral(
				new Tok(
					TokType.IntLiteral,
					"5",
					new Range(new Position(6, 0), new Position(7, 0))
				)
			)
		);
		var after = new Position(8, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			yield,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { yield, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Check()
	{
		var check = new TypedCheck(
			new Tok(
				TokType.Check,
				"check",
				new Range(new Position(), new Position(5, 0))
			),
			new TypedCall(
				new TypedVariable(
					new Tok(TokType.Identifier, "f"),
					new AuraNamedFunction(
						"f",
						Visibility.Private,
						new AuraFunction(new List<Param>(), new AuraNil())
					)
				),
				new List<ITypedAuraExpression>(),
				new Tok(
					TokType.RightParen,
					")",
					new Range(new Position(6, 0), new Position(7, 0))
				),
				new AuraNamedFunction(
					"f",
					Visibility.Private,
					new AuraFunction(new List<Param>(), new AuraNil())
				)
			)
		);
		var after = new Position(8, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			check,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { check, null }
		);
	}

	[Test]
	public void TestPrecedingNodeFinder_Struct()
	{
		var @struct = new TypedStruct(
			new Tok(
				TokType.Struct,
				"struct",
				new Range(new Position(), new Position(5, 0))
			),
			new Tok(TokType.Identifier, "s"),
			new List<Param>(),
			new Tok(
				TokType.RightParen,
				")",
				new Range(new Position(7, 0), new Position(8, 0))
			),
			null
		);
		var after = new Position(9, 0);
		var notAfter = new Position();

		ActAndAssert_Statement(
			@struct,
			new List<Position> { after, notAfter },
			new List<ITypedAuraAstNode?> { @struct, null }
		);
	}

	private static void ActAndAssert_Expression(
		ITypedAuraExpression node,
		IEnumerable<Position> positions,
		IEnumerable<ITypedAuraAstNode?> expected
	)
	{
		var nodes = new List<ITypedAuraStatement> { new TypedExpressionStmt(node) };
		var positionAndExpected = positions.Zip(expected);
		foreach (var (pos, ex) in positionAndExpected)
		{
			var precedingNodeFinder = new AuraPrecedingNodeFinder(pos, nodes);
			Assert.That(precedingNodeFinder.FindImmediatelyPrecedingNode(), Is.EqualTo(ex));
		}
	}

	private static void ActAndAssert_Statement(
		ITypedAuraStatement node,
		IEnumerable<Position> positions,
		IEnumerable<ITypedAuraAstNode?> expected
	)
	{
		var nodes = new List<ITypedAuraStatement> { node };
		var positionAndExpected = positions.Zip(expected);
		foreach (var (pos, ex) in positionAndExpected)
		{
			var precedingNodeFinder = new AuraPrecedingNodeFinder(pos, nodes);
			Assert.That(precedingNodeFinder.FindImmediatelyPrecedingNode(), Is.EqualTo(ex));
		}
	}
}
