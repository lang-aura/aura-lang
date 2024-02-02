using AuraLang.AST;
using AuraLang.Exceptions.TypeChecker;
using AuraLang.Shared;
using AuraLang.Stores;
using AuraLang.Symbol;
using AuraLang.Token;
using AuraLang.TypeChecker;
using AuraLang.Types;
using Moq;
using Newtonsoft.Json;

namespace AuraLang.Test.TypeChecker;

public class TypeCheckerTest
{
	private readonly Mock<IGlobalSymbolsTable> _symbolsTable = new();
	private readonly Mock<IEnclosingClassStore> _enclosingClassStore = new();
	private readonly Mock<IEnclosingFunctionDeclarationStore> _enclosingFunctionDeclarationStore = new();
	private readonly Mock<EnclosingNodeStore<IUntypedAuraExpression>> _enclosingExprStore = new();
	private readonly Mock<EnclosingNodeStore<IUntypedAuraStatement>> _enclosingStmtStore = new();

	[SetUp]
	public void Setup()
	{
		_enclosingExprStore.CallBase = true;
		_enclosingStmtStore.CallBase = true;
	}

	[Test]
	public void TestTypeCheck_Assignment()
	{
		_symbolsTable.Setup(v => v.GetSymbol("i", It.IsAny<string>()))
			.Returns(new AuraSymbol("i", new AuraInt()));

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedAssignment(
						new Tok(TokType.Identifier, "i", 1),
						new IntLiteral(6, 1),
						1),
					1)
			});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedAssignment(
				new Tok(TokType.Identifier, "i", 1),
				new IntLiteral(6, 1),
				new AuraInt(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_Binary()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedBinary(
					new BoolLiteral(true, 1),
					new Tok(TokType.And, "and", 1),
					new BoolLiteral(false, 1),
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedBinary(
				new BoolLiteral(true, 1),
				new Tok(TokType.And, "and", 1),
				new BoolLiteral(false, 1),
				new AuraInt(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_Block_EmptyBody()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedBlock(
					new List<IUntypedAuraStatement>(),
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedBlock(
				new List<ITypedAuraStatement>(),
				new AuraNil(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_Block()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedBlock(
					new List<IUntypedAuraStatement>
					{
						new UntypedLet(
							new List<Tok>{ new(TokType.Identifier, "i", 2) },
							new List<AuraType?>{ new AuraInt() },
							false,
							new IntLiteral(5, 2),
							2)
					},
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedBlock(
				new List<ITypedAuraStatement>
				{
					new TypedLet(
						new List<Tok>{ new(TokType.Identifier, "i", 2) },
						true,
						false,
						new IntLiteral(5, 2),
						2)
				},
				new AuraNil(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_Call_NoArgs()
	{
		_symbolsTable.Setup(v => v.GetSymbol("f", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"f",
				new AuraNamedFunction(
					"f",
					Visibility.Private,
					new AuraFunction(
						new List<Param>(),
						new AuraNil()
					)
				)
			)
		);

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedCall(
						new UntypedVariable(new Tok(TokType.Identifier, "f", 1), 1),
						new List<(Tok?, IUntypedAuraExpression)>(),
						1),
					1)
			});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedCall(
				new TypedVariable(
					new Tok(TokType.Identifier, "f", 1),
					new AuraNamedFunction("f", Visibility.Private, new AuraFunction(new List<Param>(), new AuraNil())),
					1),
				new List<ITypedAuraExpression>(),
				new AuraNil(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_TwoArgs_WithTags()
	{
		_symbolsTable.Setup(v => v.GetSymbol("f", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"f",
				new AuraNamedFunction(
					"f",
					Visibility.Private,
					new AuraFunction(
						new List<Param>
						{
							new(
								new Tok(TokType.Identifier, "i", 1),
								new ParamType(new AuraInt(), false, null)),
							new(
								new Tok(TokType.Identifier, "s", 1),
								new ParamType(new AuraString(), false, null))
						},
						new AuraNil()
					)
				)
			)
		);

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedCall(
						new UntypedVariable(new Tok(TokType.Identifier, "f", 1), 1),
						new List<(Tok?, IUntypedAuraExpression)>
						{
							(
								new Tok(TokType.Identifier, "s", 1),
								new StringLiteral("Hello world", 1)),
							(
								new Tok(TokType.Identifier, "i", 1),
								new IntLiteral(5, 1))
						},
						1),
					1)
			});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedCall(
				new TypedVariable(
					new Tok(TokType.Identifier, "f", 1),
					new AuraNamedFunction("f", Visibility.Private, new AuraFunction(new List<Param>(), new AuraNil())),
					1),
				new List<ITypedAuraExpression> { new IntLiteral(5, 1), new StringLiteral("Hello world", 1) },
				new AuraNil(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_Call_DefaultValues()
	{
		_symbolsTable.Setup(v => v.GetSymbol("f", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"f",
				new AuraNamedFunction(
					"f",
					Visibility.Private,
					new AuraFunction(
						new List<Param>
						{
							new(
								new Tok(TokType.Identifier, "i", 1),
								new ParamType(new AuraInt(), false, new IntLiteral(10, 1))),
							new(
								new Tok(TokType.Identifier, "s", 1),
								new ParamType(new AuraString(), false, null))
						},
						new AuraNil()
					)
				)
			)
		);

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedCall(
						new UntypedVariable(new Tok(TokType.Identifier, "f", 1), 1),
						new List<(Tok?, IUntypedAuraExpression)>
						{
							(
								new Tok(TokType.Identifier, "s", 1),
								new StringLiteral("Hello world", 1))
						},
						1),
					1)
			});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedCall(
				new TypedVariable(
					new Tok(TokType.Identifier, "f", 1),
					new AuraNamedFunction("f", Visibility.Private, new AuraFunction(new List<Param>(), new AuraNil())),
					1),
				new List<ITypedAuraExpression> { new IntLiteral(10, 1), new StringLiteral("Hello world", 1) },
				new AuraNil(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_Call_NoValueForParameterWithoutDefaultValue()
	{
		_symbolsTable.Setup(v => v.GetSymbol("f", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"f",
				new AuraNamedFunction(
					"f",
					Visibility.Private,
					new AuraFunction(
						new List<Param>
						{
							new(
								new Tok(TokType.Identifier, "i", 1),
								new ParamType(new AuraInt(), false, null)),
							new(
								new Tok(TokType.Identifier, "s", 1),
								new ParamType(new AuraString(), false, new StringLiteral("Hello world", 1)))
						},
						new AuraNil()
					)
				)
			));

		ArrangeAndAct_Invalid(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedCall(
					new UntypedVariable(new Tok(TokType.Identifier, "f", 1), 1),
					new List<(Tok?, IUntypedAuraExpression)>
					{
						(
							new Tok(TokType.Identifier, "s", 1),
							new StringLiteral("Hello world", 1))
					},
					1),
				1)
		}, typeof(MustSpecifyValueForArgumentWithoutDefaultValueException));
	}

	[Test]
	public void TestTypeCheck_Call_MixNamedAndUnnamedArguments()
	{
		_symbolsTable.Setup(v => v.GetSymbol("f", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"f",
				new AuraNamedFunction(
					"f",
					Visibility.Private,
					new AuraFunction(
						new List<Param>
						{
							new(
								new Tok(TokType.Identifier, "i", 1),
								new ParamType(new AuraInt(), false, null)),
							new(
								new Tok(TokType.Identifier, "s", 1),
								new ParamType(new AuraString(), false, null))
						},
						new AuraNil()
					)
				)
			)
		);

		ArrangeAndAct_Invalid(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedCall(
					new UntypedVariable(new Tok(TokType.Identifier, "f", 1), 1),
					new List<(Tok?, IUntypedAuraExpression)>
					{
						(
							new Tok(TokType.Identifier, "s", 1),
							new StringLiteral("Hello world", 1)),
						(
							null,
							new IntLiteral(5, 1))
					},
					1),
				1)
		}, typeof(CannotMixNamedAndUnnamedArgumentsException));
	}

	[Test]
	public void TestTypeCheck_Get()
	{
		_symbolsTable.Setup(v => v.GetSymbol("greeter", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"greeter",
				new AuraClass(
					"Greeter",
					new List<Param>
					{
						new(
							new Tok(TokType.Identifier, "name", 1),
							new ParamType(new AuraString(), false, null)
						)
					},
					new List<AuraNamedFunction>(),
					new List<AuraInterface>(),
					Visibility.Private
				)
			)
		);

		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedGet(
					new UntypedVariable(
						new Tok(TokType.Identifier, "greeter", 1),
						1),
					new Tok(TokType.Identifier, "name", 1),
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedGet(
				new TypedVariable(
					new Tok(TokType.Identifier, "greeter", 1),
					new AuraClass(
						"Greeter",
						new List<Param>
						{
							new Param(
								new Tok(TokType.Identifier, "name", 1),
								new ParamType(new AuraString(), false, null))
						},
						new List<AuraNamedFunction>(),
						new List<AuraInterface>(),
						Visibility.Private),
					1),
				new Tok(TokType.Identifier, "name", 1),
				new AuraString(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_GetIndex()
	{
		_symbolsTable.Setup(v => v.GetSymbol("names", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"names",
				new AuraList(new AuraString())
			)
		);

		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedGetIndex(
					new UntypedVariable(
						new Tok(TokType.Identifier, "names", 1),
						1),
					new IntLiteral(0, 1),
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedGetIndex(
				new TypedVariable(
					new Tok(TokType.Identifier, "names", 1),
					new AuraList(new AuraString()),
					1),
				new IntLiteral(0, 1),
				new AuraString(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_GetIndexRange()
	{
		_symbolsTable.Setup(v => v.GetSymbol("names", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"names",
				new AuraList(new AuraString())
			)
		);

		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedGetIndexRange(
					new UntypedVariable(
						new Tok(TokType.Identifier, "names", 1),
						1),
					new IntLiteral(0, 1),
					new IntLiteral(2, 1),
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedGetIndexRange(
				new TypedVariable(
					new Tok(TokType.Identifier, "names", 1),
					new AuraList(new AuraString()),
					1),
				new IntLiteral(0, 1),
				new IntLiteral(2, 1),
				new AuraList(new AuraString()),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_Grouping()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedGrouping(
					new StringLiteral("Hello world", 1),
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedGrouping(
				new StringLiteral("Hello world", 1),
				new AuraString(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_If_EmptyThenBranch()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedIf(
					new BoolLiteral(true, 1),
					new UntypedBlock(
						new List<IUntypedAuraStatement>(),
						1),
					null,
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedIf(
				new BoolLiteral(true, 1),
				new TypedBlock(
					new List<ITypedAuraStatement>(),
					new AuraNil(),
					1),
				null,
				new AuraNil(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_IntLiteral()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new IntLiteral(5, 1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new IntLiteral(5, 1),
			1));
	}

	[Test]
	public void TestTypeCheck_FloatLiteral()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new FloatLiteral(5.1, 1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new FloatLiteral(5.1, 1),
			1));
	}

	[Test]
	public void TestTypeCheck_StringLiteral()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new StringLiteral("Hello world", 1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new StringLiteral("Hello world", 1),
			1));
	}

	[Test]
	public void TestTypeCheck_ListLiteral()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new ListLiteral<IUntypedAuraExpression>(
					new List<IUntypedAuraExpression> { new IntLiteral(1, 1) },
					new AuraInt(),
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new ListLiteral<ITypedAuraExpression>(
				new List<ITypedAuraExpression> { new IntLiteral(1, 1) },
				new AuraInt(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_MapLiteral()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new MapLiteral<IUntypedAuraExpression, IUntypedAuraExpression>(
					new Dictionary<IUntypedAuraExpression, IUntypedAuraExpression>
					{
						{ new StringLiteral("Hello", 1), new IntLiteral(1, 1) }
					},
					new AuraString(),
					new AuraInt(),
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new MapLiteral<ITypedAuraExpression, ITypedAuraExpression>(
				new Dictionary<ITypedAuraExpression, ITypedAuraExpression>
				{
					{ new StringLiteral("Hello", 1), new IntLiteral(1, 1) }
				},
				new AuraString(),
				new AuraInt(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_BoolLiteral()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new BoolLiteral(true, 1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new BoolLiteral(true, 1),
			1));
	}

	[Test]
	public void TestTypeCheck_NilLiteral()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedNil(1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedNil(1),
			1));
	}

	[Test]
	public void TestTypeCheck_CharLiteral()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new CharLiteral('a', 1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new CharLiteral('a', 1),
			1));
	}

	[Test]
	public void TestTypeCheck_Logical()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedLogical(
					new IntLiteral(5, 1),
					new Tok(TokType.Less, "<", 1),
					new IntLiteral(10, 1),
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedLogical(
				new IntLiteral(5, 1),
				new Tok(TokType.Less, "<", 1),
				new IntLiteral(10, 1),
				new AuraBool(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_Set()
	{
		_symbolsTable.Setup(v => v.GetSymbol("greeter", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"greeter",
				new AuraClass(
					"Greeter",
					new List<Param>
					{
						new Param(
							new Tok(TokType.Identifier, "name", 1),
							new ParamType(new AuraString(), false, null))
					},
					new List<AuraNamedFunction>(),
					new List<AuraInterface>(),
					Visibility.Private
				)
			)
		);

		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedSet(
					new UntypedVariable(
						new Tok(TokType.Identifier, "greeter", 1),
						1),
					new Tok(TokType.Identifier, "name", 1),
					new StringLiteral("Bob", 1),
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedSet(
				new TypedVariable(
					new Tok(TokType.Identifier, "greeter", 1),
					new AuraClass(
						"Greeter",
						new List<Param>
						{
							new Param(
								new Tok(TokType.Identifier, "name", 1),
								new ParamType(new AuraString(), false, null))
						},
						new List<AuraNamedFunction>(),
						new List<AuraInterface>(),
						Visibility.Private),
					1),
				new Tok(TokType.Identifier, "name", 1),
				new StringLiteral("Bob", 1),
				new AuraString(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_This()
	{
		_enclosingClassStore.Setup(ecs => ecs.Peek())
			.Returns(new PartiallyTypedClass(
				new Tok(TokType.Identifier, "Greeter", 1),
				new List<Param>(),
				new List<AuraNamedFunction>(),
				Visibility.Public,
				new AuraClass(
					"Greeter",
					new List<Param>(),
					new List<AuraNamedFunction>(),
					new List<AuraInterface>(),
					Visibility.Private),
				1));

		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedThis(
					new Tok(TokType.This, "this", 1),
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedThis(
				new Tok(TokType.This, "this", 1),
				new AuraClass(
					"Greeter",
					new List<Param>(),
					new List<AuraNamedFunction>(),
					new List<AuraInterface>(),
					Visibility.Private),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_Unary_Bang()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedUnary(
					new Tok(TokType.Bang, "!", 1),
					new BoolLiteral(true, 1),
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedUnary(
				new Tok(TokType.Bang, "!", 1),
				new BoolLiteral(true, 1),
				new AuraBool(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_Unary_Minus()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedUnary(
					new Tok(TokType.Minus, "-", 1),
					new IntLiteral(5, 1),
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedUnary(
				new Tok(TokType.Minus, "-", 1),
				new IntLiteral(5, 1),
				new AuraInt(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_Variable()
	{
		_symbolsTable.Setup(v => v.GetSymbol("name", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"name",
				new AuraString()
			)
		);

		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedVariable(
					new Tok(TokType.Identifier, "name", 1),
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedVariable(
				new Tok(TokType.Identifier, "name", 1),
				new AuraString(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_Defer()
	{
		_symbolsTable.Setup(v => v.GetSymbol("f", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"f",
				new AuraNamedFunction(
					"f",
					Visibility.Private,
					new AuraFunction(
						new List<Param>(),
						new AuraNil()
					)
				)
			)
		);

		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedDefer(
				new UntypedCall(
					new UntypedVariable(
						new Tok(TokType.Identifier, "f", 1),
						1),
					new List<(Tok?, IUntypedAuraExpression)>(),
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedDefer(
			new TypedCall(
				new TypedVariable(
					new Tok(TokType.Identifier, "f", 1),
					new AuraNamedFunction(
						"f",
						Visibility.Private,
						new AuraFunction(
							new List<Param>(),
							new AuraNil()
						)
					),
					1
				),
				new List<ITypedAuraExpression>(),
				new AuraNil(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_For_EmptyBody()
	{
		_symbolsTable.Setup(v => v.GetSymbol("i", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"i",
				new AuraInt()
			)
		);

		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedFor(
				new UntypedLet(
					new List<Tok>{ new(TokType.Identifier, "i", 1) },
					new List<AuraType?>(),
					false,
					new IntLiteral(0, 1),
					1),
				new UntypedLogical(
					new UntypedVariable(
						new Tok(TokType.Identifier, "i", 1),
						1),
					new Tok(TokType.Less, "<", 1),
					new IntLiteral(10, 1),
					1),
				null,
				new List<IUntypedAuraStatement>(),
				1)
		});
		MakeAssertions(typedAst, new TypedFor(
			new TypedLet(
				new List<Tok> { new(TokType.Identifier, "i", 1) },
				false,
				false,
				new IntLiteral(0, 1),
				1),
			new TypedLogical(
				new TypedVariable(
					new Tok(TokType.Identifier, "i", 1),
					new AuraInt(),
					1),
				new Tok(TokType.Less, "<", 1),
				new IntLiteral(10, 1),
				new AuraBool(),
				1),
			null,
			new List<ITypedAuraStatement>(),
			1));
	}

	[Test]
	public void TestTypeCheck_ForEach_EmptyBody()
	{
		_symbolsTable.Setup(v => v.GetSymbol("names", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"names",
				new AuraList(new AuraString())
			)
		);

		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedForEach(
				new Tok(TokType.Identifier, "name", 1),
				new UntypedVariable(
					new Tok(TokType.Identifier, "names", 1),
					1),
				new List<IUntypedAuraStatement>(),
				1)
		});
		MakeAssertions(typedAst, new TypedForEach(
			new Tok(TokType.Identifier, "name", 1),
			new TypedVariable(
				new Tok(TokType.Identifier, "names", 1),
				new AuraList(new AuraString()),
				1),
			new List<ITypedAuraStatement>(),
			1));
	}

	[Test]
	public void TestTypeCheck_NamedFunction_NoParams_ReturnError()
	{
		_symbolsTable.Setup(v => v.GetSymbol("error", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"error",
				new AuraNamedFunction(
					name: "error",
					pub: Visibility.Public,
					f: new AuraFunction(
						fParams: new List<Param>
						{
							new(
								Name: new Tok(
									Typ: TokType.Identifier,
									Value: "message",
									Line: 1
								),
								ParamType: new ParamType(
									Typ: new AuraString(),
									Variadic: false,
									DefaultValue: null
								)
							)
						},
						returnType: new AuraError()
					)
				)
			)
		);

		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedNamedFunction(
				Name: new Tok(TokType.Identifier, "f", 1),
				Params: new List<Param>(),
				Body: new UntypedBlock(new List<IUntypedAuraStatement>
				{
					new UntypedReturn(
						Value: new List<IUntypedAuraExpression>
						{
							new UntypedCall(
								Callee: new UntypedVariable(
									Name: new Tok(
										Typ: TokType.Identifier,
										Value: "error",
										Line: 1
									),
									Line: 1
								),
								Arguments: new List<(Tok?, IUntypedAuraExpression)>
								{
									(null, new StringLiteral("Helpful error message", 1))
								},
								Line: 1
							)
						},
						Line: 1
					)
				}, 1),
				ReturnType: new List<AuraType>{ new AuraError() },
				Public: Visibility.Public,
				Line: 1)
		});
		MakeAssertions(typedAst, new TypedNamedFunction(
			Name: new Tok(TokType.Identifier, "f", 1),
			Params: new List<Param>(),
			Body: new TypedBlock(
				Statements: new List<ITypedAuraStatement>
				{
					new TypedReturn(
							Value: new TypedCall(
								Callee: new TypedVariable(
									Name: new Tok(
										Typ: TokType.Identifier,
										Value: "error",
										Line: 1
									),
									Typ: new AuraNamedFunction(
										name: "error",
										pub: Visibility.Public,
										f: new AuraFunction(
											fParams: new List<Param>
											{
												new(
													Name: new Tok(
														Typ: TokType.Identifier,
														Value: "message",
														Line: 1
													),
													ParamType: new ParamType(
														Typ: new AuraString(),
														Variadic: false,
														DefaultValue: null
													)
												)
											},
											returnType: new AuraError()
										)
									),
									Line: 1
								),
								Arguments: new List<ITypedAuraExpression>
								{
									new StringLiteral("Helpful error message", 1)
								},
								Typ: new AuraError(),
								Line: 1
							),
							Line: 1
						)
				},
				Typ: new AuraError(),
				Line: 1),
			ReturnType: new AuraError(),
			Public: Visibility.Public,
			Line: 1));
	}

	[Test]
	public void TestTypeCheck_NamedFunction_NoParams_NoReturnType_NoBody()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedNamedFunction(
				new Tok(TokType.Identifier, "f", 1),
				new List<Param>(),
				new UntypedBlock(new List<IUntypedAuraStatement>(), 1),
				null,
				Visibility.Public,
				1)
		});
		MakeAssertions(typedAst, new TypedNamedFunction(
			new Tok(TokType.Identifier, "f", 1),
			new List<Param>(),
			new TypedBlock(new List<ITypedAuraStatement>(), new AuraNil(), 1),
			new AuraNil(),
			Visibility.Public,
			1));
	}

	[Test]
	public void TestTypeCheck_AnonymousFunction_NoParams_NoReturnType_NoBody()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedAnonymousFunction(
					new List<Param>(),
					new UntypedBlock(new List<IUntypedAuraStatement>(), 1),
					null,
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedAnonymousFunction(
				new List<Param>(),
				new TypedBlock(
					new List<ITypedAuraStatement>(),
					new AuraNil(),
					1),
				new AuraNil(),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_Let_Long()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedLet(
				new List<Tok>{ new(TokType.Identifier, "i", 1) },
				new List<AuraType?>{ new AuraInt() },
				false,
				new IntLiteral(1, 1),
				1)
		});
		MakeAssertions(typedAst, new TypedLet(
			new List<Tok> { new(TokType.Identifier, "i", 1) },
			true,
			false,
			new IntLiteral(1, 1),
			1));
	}

	[Test]
	public void TestTypeCheck_Let_Uninitialized()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedLet(
				new List<Tok>{ new(TokType.Identifier, "i", 1) },
				new List<AuraType?>{ new AuraInt() },
				false,
				null,
				1)
		});
		MakeAssertions(typedAst, new TypedLet(
			new List<Tok> { new(TokType.Identifier, "i", 1) },
			true,
			false,
			new IntLiteral(0, 1),
			1));
	}

	[Test]
	public void TestTypeCheck_Let_Uninitialized_NonDefaultable()
	{
		ArrangeAndAct_Invalid(new List<IUntypedAuraStatement>
		{
			new UntypedLet(
				new List<Tok>{ new(TokType.Identifier, "c", 1) },
				new List<AuraType?>{ new AuraChar() },
				false,
				null,
				1)
		}, typeof(MustSpecifyInitialValueForNonDefaultableTypeException));
	}

	[Test]
	public void TestTypeCheck_Long_Short()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedLet(
				new List<Tok>{ new(TokType.Identifier, "i", 1) },
				new List<AuraType?>(),
				false,
				new IntLiteral(1, 1),
				1)
		});
		MakeAssertions(typedAst, new TypedLet(
			new List<Tok> { new(TokType.Identifier, "i", 1) },
			false,
			false,
			new IntLiteral(1, 1),
			1));
	}

	[Test]
	public void TestTypeCheck_Return_NoValue()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedReturn(
				null,
				1)
		});
		MakeAssertions(typedAst, new TypedReturn(
			null,
			1));
	}

	[Test]
	public void TestTypeCheck_Return()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedReturn(
				new List<IUntypedAuraExpression>{ new IntLiteral(5, 1) },
				1)
		});
		MakeAssertions(typedAst, new TypedReturn(
			new IntLiteral(5, 1),
			1));
	}

	[Test]
	public void TestTypeCheck_Class_NoParams_NoMethods()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedClass(
				new Tok(TokType.Identifier, "Greeter", 1),
				new List<Param>(),
				new List<IUntypedAuraStatement>(),
				Visibility.Private,
				new List<Tok>(),
				1)
		});
		MakeAssertions(typedAst, new FullyTypedClass(
			new Tok(TokType.Identifier, "Greeter", 1),
			new List<Param>(),
			new List<TypedNamedFunction>(),
			Visibility.Private,
			new List<AuraInterface>(),
			1));
	}

	[Test]
	public void TestTypeCheck_While_EmptyBody()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedWhile(
				new BoolLiteral(true, 1),
				new List<IUntypedAuraStatement>(),
				1)
		});
		MakeAssertions(typedAst, new TypedWhile(
			new BoolLiteral(true, 1),
			new List<ITypedAuraStatement>(),
			1));
	}

	// [Test]
	// public void TestTypeCheck_Import_NoAlias()
	// {
	// 	_localModuleReader.Setup(m => m.GetModuleSourcePaths(It.IsAny<string>())).Returns(new string[] { "test_pkg" });
	// 	_localModuleReader.Setup(m => m.Read(It.IsAny<string>())).Returns(string.Empty);

	// 	var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
	// 	{
	// 		new UntypedImport(
	// 			new Tok(TokType.Identifier, "test_pkg", 1),
	// 			null,
	// 			1)
	// 	});
	// 	MakeAssertions(typedAst, new TypedImport(
	// 		new Tok(TokType.Identifier, "test_pkg", 1),
	// 		null,
	// 		1));
	// }

	[Test]
	public void TestTypeCheck_Comment()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedComment(
				new Tok(TokType.Comment, "// this is a comment", 1),
				1)
		});
		MakeAssertions(typedAst, new TypedComment(
			new Tok(TokType.Comment, "// this is a comment", 1),
			1));
	}

	[Test]
	public void TestTypeCheck_Yield()
	{
		_enclosingExprStore.Setup(expr => expr.Peek()).Returns(new UntypedBlock(new List<IUntypedAuraStatement>(), 1));

		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedYield(
				new IntLiteral(5, 1),
				1)
		});
		MakeAssertions(typedAst, new TypedYield(
			new IntLiteral(5, 1),
			1));
	}

	[Test]
	public void TestTypeCheck_Yield_Invalid()
	{
		_enclosingExprStore.Setup(expr => expr.Peek()).Returns(new UntypedNil(1));

		ArrangeAndAct_Invalid(new List<IUntypedAuraStatement> { new UntypedYield(new IntLiteral(5, 1), 1) },
			typeof(InvalidUseOfYieldKeywordException));
	}

	[Test]
	public void TestTypeCheck_Break()
	{
		_enclosingStmtStore.Setup(stmt => stmt.Peek()).Returns(new UntypedWhile(
			new BoolLiteral(true, 1),
			new List<IUntypedAuraStatement>(),
			1));

		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement> { new UntypedBreak(1) });
		MakeAssertions(typedAst, new TypedBreak(1));
	}

	[Test]
	public void TestTypeCheck_Break_Invalid()
	{
		_enclosingStmtStore.Setup(stmt => stmt.Peek()).Returns(new UntypedExpressionStmt(new UntypedNil(1), 1));

		ArrangeAndAct_Invalid(new List<IUntypedAuraStatement> { new UntypedBreak(1) },
			typeof(InvalidUseOfBreakKeywordException));
	}

	[Test]
	public void TestTypeCheck_Continue()
	{
		_enclosingStmtStore.Setup(stmt => stmt.Peek()).Returns(new UntypedWhile(
			new BoolLiteral(true, 1),
			new List<IUntypedAuraStatement>(),
			1));

		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement> { new UntypedContinue(1) });
		MakeAssertions(typedAst, new TypedContinue(1));
	}

	[Test]
	public void TestTypeCheck_Continue_Invalid()
	{
		_enclosingStmtStore.Setup(stmt => stmt.Peek()).Returns(new UntypedExpressionStmt(new UntypedNil(1), 1));

		ArrangeAndAct_Invalid(new List<IUntypedAuraStatement> { new UntypedContinue(1) },
			typeof(InvalidUseOfContinueKeywordException));
	}

	[Test]
	public void TestTypeCheck_Interface_NoMethods()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedInterface(
				new Tok(TokType.Identifier, "IGreeter", 1),
				new List<AuraNamedFunction>(),
				Visibility.Public,
				1)
		});
		MakeAssertions(typedAst, new TypedInterface(
			new Tok(TokType.Identifier, "IGreeter", 1),
			new List<AuraNamedFunction>(),
			Visibility.Public,
			1));
	}

	[Test]
	public void TestTypeCheck_Interface_OneMethod()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedInterface(
				new Tok(TokType.Identifier, "IGreeter", 1),
				new List<AuraNamedFunction>
				{
					new AuraNamedFunction(
						"say_hi",
						Visibility.Private,
						new AuraFunction(
							new List<Param>
							{
								new Param(
									new Tok(TokType.Identifier, "i", 1),
									new ParamType(
										new AuraInt(),
										false,
										null))
							},
							new AuraString()))
				},
				Visibility.Public,
				1)
		});
		MakeAssertions(typedAst, new TypedInterface(
			new Tok(TokType.Identifier, "IGreeter", 1),
			new List<AuraNamedFunction>
			{
				new AuraNamedFunction(
					"say_hi",
					Visibility.Private,
					new AuraFunction(
						new List<Param>
						{
							new Param(
								new Tok(TokType.Identifier, "i", 1),
								new ParamType(
									new AuraInt(),
									false,
									null))
						},
						new AuraString()))
			},
			Visibility.Public,
			1));
	}

	[Test]
	public void TestTypeCheck_ClassImplementingTwoInterfaces_NoMethods()
	{
		_symbolsTable.Setup(v => v.GetSymbol("IGreeter", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"IGreeter",
				new AuraInterface("IGreeter", new List<AuraNamedFunction>(), Visibility.Private)
			)
		);
		_symbolsTable.Setup(v => v.GetSymbol("IGreeter2", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"IGreeter2",
				new AuraInterface("IGreeter2", new List<AuraNamedFunction>(), Visibility.Private)
			)
		);

		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedClass(
				new Tok(TokType.Identifier, "Greeter", 1),
				new List<Param>(),
				new List<IUntypedAuraStatement>(),
				Visibility.Private,
				new List<Tok> { new(TokType.Identifier, "IGreeter", 1), new(TokType.Identifier, "IGreeter2", 1) },
				1)
		});
		MakeAssertions(typedAst, new FullyTypedClass(
			new Tok(TokType.Identifier, "Greeter", 1),
			new List<Param>(),
			new List<TypedNamedFunction>(),
			Visibility.Private,
			new List<AuraInterface>
			{
				new("IGreeter", new List<AuraNamedFunction>(), Visibility.Private),
				new("IGreeter2", new List<AuraNamedFunction>(), Visibility.Private)
			},
			1));
	}

	[Test]
	public void TestTypeCheck_ClassImplementingInterface_OneMethod_MissingImplementation()
	{
		_symbolsTable.Setup(v => v.GetSymbol("IGreeter", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"IGreeter",
				new AuraInterface("IGreeter",
				new List<AuraNamedFunction>
				{
					new(
						"f",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "i", 1),
									new ParamType(
										new AuraInt(),
										false,
										null
									)
								)
							},
							new AuraInt()
						)
					)
				},
				Visibility.Private)
			)
		);

		ArrangeAndAct_Invalid(new List<IUntypedAuraStatement>
		{
			new UntypedClass(
				Name: new Tok(TokType.Identifier, "Greeter", 1),
				Params: new List<Param>(),
				Body: new List<IUntypedAuraStatement>{},
				Public: Visibility.Private,
				Implementing: new List<Tok> { new(TokType.Identifier, "IGreeter", 1) },
				Line: 1)
		},
		typeof(MissingInterfaceMethodException));
	}

	[Test]
	public void TestTypeCheck_ClassImplementingInterface_OneMethod_ImplementationNotPublic()
	{
		_symbolsTable.Setup(v => v.GetSymbol("IGreeter", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"IGreeter",
				new AuraInterface("IGreeter",
				new List<AuraNamedFunction>
				{
					new(
						"f",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "i", 1),
									new ParamType(
										new AuraInt(),
										false,
										null
									)
								)
							},
							new AuraInt()
						)
					)
				},
				Visibility.Private)
			)
		);

		ArrangeAndAct_Invalid(new List<IUntypedAuraStatement>
		{
			new UntypedClass(
				Name: new Tok(TokType.Identifier, "Greeter", 1),
				Params: new List<Param>(),
				Body: new List<IUntypedAuraStatement>
				{
					new UntypedNamedFunction(
						Name: new Tok(TokType.Identifier, "f", 1),
						Params: new List<Param>
						{
							new(
								Name: new Tok(TokType.Identifier, "i", 1),
								ParamType: new ParamType(
									new AuraInt(),
									false,
									null
								)
							)
						},
						Body: new UntypedBlock(
							Statements: new List<IUntypedAuraStatement>
							{
								new UntypedReturn(
									Value: new List<IUntypedAuraExpression>{ new IntLiteral(I: 5, Line: 1) },
									Line: 1
								)
							},
							Line: 1
						),
						ReturnType: new List<AuraType>{ new AuraInt() },
						Public: Visibility.Private,
						Line: 1
					)
				},
				Public: Visibility.Private,
				Implementing: new List<Tok> { new(TokType.Identifier, "IGreeter", 1) },
				Line: 1)
		},
		typeof(MissingInterfaceMethodException));
	}

	[Test]
	public void TestTypeCheck_ClassImplementingInterface_OneMethod()
	{
		_symbolsTable.Setup(v => v.GetSymbol("IGreeter", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"IGreeter",
				new AuraInterface("IGreeter",
				new List<AuraNamedFunction>
				{
					new(
						"f",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "i", 1),
									new ParamType(
										new AuraInt(),
										false,
										null
									)
								)
							},
							new AuraInt()
						)
					)
				},
				Visibility.Private)
			)
		);

		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedClass(
				Name: new Tok(TokType.Identifier, "Greeter", 1),
				Params: new List<Param>(),
				Body: new List<IUntypedAuraStatement>
				{
					new UntypedNamedFunction(
						Name: new Tok(TokType.Identifier, "f", 1),
						Params: new List<Param>
						{
							new(
								Name: new Tok(TokType.Identifier, "i", 1),
								ParamType: new ParamType(
									new AuraInt(),
									false,
									null
								)
							)
						},
						Body: new UntypedBlock(
							Statements: new List<IUntypedAuraStatement>
							{
								new UntypedReturn(
									Value: new List<IUntypedAuraExpression>{ new IntLiteral(I: 5, Line: 1) },
									Line: 1
								)
							},
							Line: 1
						),
						ReturnType: new List<AuraType>{ new AuraInt() },
						Public: Visibility.Public,
						Line: 1
					)
				},
				Public: Visibility.Private,
				Implementing: new List<Tok> { new(TokType.Identifier, "IGreeter", 1) },
				Line: 1)
		});
		MakeAssertions(typedAst, new FullyTypedClass(
			Name: new Tok(TokType.Identifier, "Greeter", 1),
			Params: new List<Param>(),
			Methods: new List<TypedNamedFunction>
			{
				new TypedNamedFunction(
						Name: new Tok(TokType.Identifier, "f", 1),
						Params: new List<Param>
						{
							new(
								Name: new Tok(TokType.Identifier, "i", 1),
								ParamType: new ParamType(
									new AuraInt(),
									false,
									null
								)
							)
						},
						Body: new TypedBlock(
							Statements: new List<ITypedAuraStatement>
							{
								new TypedReturn(
									Value: new IntLiteral(I: 5, Line: 1),
									Line: 1
								)
							},
							Typ: new AuraInt(),
							Line: 1
						),
						ReturnType: new AuraInt(),
						Public: Visibility.Public,
						Line: 1
					)
			},
			Public: Visibility.Private,
			Implementing: new List<AuraInterface>
			{
				new(
					name: "IGreeter",
					functions: new List<AuraNamedFunction>
					{
						new(
							"f",
							Visibility.Public,
							new AuraFunction(
								new List<Param>
								{
									new(
										new Tok(TokType.Identifier, "i", 1),
										new ParamType(
											new AuraInt(),
											false,
											null
										)
									)
								},
								new AuraInt()
							)
						)
					},
					pub: Visibility.Private) },
			Line: 1));
	}

	[Test]
	public void TestTypeCheck_ClassImplementingInterface_NoMethods()
	{
		_symbolsTable.Setup(v => v.GetSymbol("IGreeter", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"IGreeter",
				new AuraInterface("IGreeter", new List<AuraNamedFunction>(), Visibility.Private)
			)
		);

		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedClass(
				new Tok(TokType.Identifier, "Greeter", 1),
				new List<Param>(),
				new List<IUntypedAuraStatement>(),
				Visibility.Private,
				new List<Tok> { new(TokType.Identifier, "IGreeter", 1) },
				1)
		});
		MakeAssertions(typedAst, new FullyTypedClass(
			new Tok(TokType.Identifier, "Greeter", 1),
			new List<Param>(),
			new List<TypedNamedFunction>(),
			Visibility.Private,
			new List<AuraInterface> { new("IGreeter", new List<AuraNamedFunction>(), Visibility.Private) },
			1));
	}

	[Test]
	public void TestTypeCheck_Set_Invalid()
	{
		_symbolsTable.Setup(v => v.GetSymbol("v", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"v",
				new AuraInt()
			)
		);

		ArrangeAndAct_Invalid(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedSet(
						new UntypedVariable(
							new Tok(TokType.Identifier, "v", 1),
							1),
						new Tok(TokType.Identifier, "name", 1),
						new StringLiteral("Bob", 1),
						1),
					1)
			},
			typeof(CannotSetOnNonClassException));
	}

	[Test]
	public void TestTypeCheck_Is()
	{
		_symbolsTable.Setup(v => v.GetSymbol("v", It.IsAny<string>())).Returns(
			new AuraSymbol(
				"v",
				new AuraInt()
			)
		);

		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedIs(
					new UntypedVariable(
						new Tok(TokType.Identifier, "v", 1),
						1),
					new Tok(TokType.Identifier, "IGreeter", 1),
					1),
				1)
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedIs(
				new TypedVariable(
					new Tok(TokType.Identifier, "v", 1),
					new AuraInt(),
					1),
				new AuraInterface("IGreeter", new List<AuraNamedFunction>(), Visibility.Private),
				1),
			1));
	}

	[Test]
	public void TestTypeCheck_Check()
	{
		_enclosingFunctionDeclarationStore.Setup(f => f.Peek()).Returns(
			new UntypedNamedFunction(
				Name: new Tok(
					Typ: TokType.Identifier,
					Value: "f",
					Line: 1
				),
				Params: new List<Param>(),
				Body: new UntypedBlock(
					Statements: new List<IUntypedAuraStatement>(),
					Line: 1
				),
				ReturnType: new List<AuraType> { new AuraResult(new AuraString(), new AuraError()) },
				Public: Visibility.Public,
				Line: 1
			)
		);
		_symbolsTable.Setup(st => st.GetSymbol("c", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "c",
				Kind: new AuraNamedFunction(
					name: "c",
					pub: Visibility.Public,
					f: new AuraFunction(
						fParams: new List<Param>(),
						returnType: new AuraResult(new AuraString(), new AuraError())
					)
				)
			)
		);

		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedCheck(
				Call: new UntypedCall(
					Callee: new UntypedVariable(
						Name: new Tok(
							Typ: TokType.Identifier,
							Value: "c",
							Line: 1
						),
						Line: 1
					),
					Arguments: new List<(Tok?, IUntypedAuraExpression)>(),
					Line: 1
				),
				Line: 1
			)
		});
		MakeAssertions(typedAst, new TypedCheck(
			Call: new TypedCall(
				Callee: new TypedVariable(
					Name: new Tok(
						Typ: TokType.Identifier,
						Value: "c",
						Line: 1
					),
					Typ: new AuraNamedFunction(
						name: "c",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>(),
							returnType: new AuraResult(new AuraString(), new AuraError())
						)
					),
					Line: 1
				),
				Arguments: new List<ITypedAuraExpression>(),
				Typ: new AuraResult(new AuraString(), new AuraError()),
				Line: 1
			),
			Line: 1
		));
	}

	[Test]
	public void TestTypeCheck_Struct()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedStruct(
				Name: new Tok(
					Typ: TokType.Identifier,
					Value: "s",
					Line: 1
				),
				Params: new List<Param>(),
				Line: 1
			)
		});
		MakeAssertions(typedAst, new TypedStruct(
			Name: new Tok(
				Typ: TokType.Identifier,
				Value: "s",
				Line: 1
			),
			Params: new List<Param>(),
			Line: 1
		));
	}

	private List<ITypedAuraStatement> ArrangeAndAct(List<IUntypedAuraStatement> untypedAst)
		=> new AuraTypeChecker(_symbolsTable.Object, _enclosingClassStore.Object, _enclosingFunctionDeclarationStore.Object, _enclosingExprStore.Object,
				_enclosingStmtStore.Object, "Test", "Test")
			.CheckTypes(AddModStmtIfNecessary(untypedAst));

	private void ArrangeAndAct_Invalid(List<IUntypedAuraStatement> untypedAst, Type expected)
	{
		try
		{
			new AuraTypeChecker(_symbolsTable.Object, _enclosingClassStore.Object, _enclosingFunctionDeclarationStore.Object, _enclosingExprStore.Object,
					_enclosingStmtStore.Object, "Test", "Test")
				.CheckTypes(AddModStmtIfNecessary(untypedAst));
			Assert.Fail();
		}
		catch (TypeCheckerExceptionContainer e)
		{
			Assert.That(e.Exs.First(), Is.TypeOf(expected));
		}
	}

	private List<IUntypedAuraStatement> AddModStmtIfNecessary(List<IUntypedAuraStatement> untypedAst)
	{
		if (untypedAst.Count > 0 && untypedAst[0] is not UntypedMod)
		{
			var untypedAstWithMod = new List<IUntypedAuraStatement>
			{
				new UntypedMod(
					new Tok(TokType.Identifier, "main", 1),
					1)
			};
			untypedAstWithMod.AddRange(untypedAst);
			return untypedAstWithMod;
		}

		return untypedAst;
	}

	private void MakeAssertions(List<ITypedAuraStatement> typedAst, ITypedAuraStatement expected)
	{
		Assert.Multiple(() =>
		{
			Assert.That(typedAst, Is.Not.Null);
			Assert.That(typedAst, Has.Count.EqualTo(2));

			var expectedJson = JsonConvert.SerializeObject(expected);
			var actualJson = JsonConvert.SerializeObject(typedAst[1]);
			Assert.That(actualJson, Is.EqualTo(expectedJson));
		});
	}
}
