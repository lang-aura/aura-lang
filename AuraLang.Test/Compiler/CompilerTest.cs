using AuraLang.AST;
using AuraLang.Compiler;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.TypeChecker;
using AuraLang.Types;
using Moq;
using AuraList = AuraLang.Types.List;
using AuraString = AuraLang.Types.String;

namespace AuraLang.Test.Compiler;

public class CompilerTest
{
	private readonly Mock<CompiledOutputWriter> _outputWriter = new();
	private readonly Mock<LocalModuleReader> _localModuleReader = new();

	[Test]
	public void TestCompile_Assignment()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedAssignment(
					new Tok(TokType.Identifier, "i", 1),
					new IntLiteral(5, 1),
					new Int(),
					1),
				1)
		});
		MakeAssertions(output, "i = 5");
	}

	[Test]
	public void TestCompile_Binary()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedBinary(
					new IntLiteral(5, 1),
					new Tok(TokType.Plus, "+", 1),
					new IntLiteral(5, 1),
					new Int(),
					1),
				1)
		});
		MakeAssertions(output, "5 + 5");
	}

	[Test]
	public void TestCompile_Block_EmptyBody()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedBlock(
					new List<ITypedAuraStatement>(),
					new Nil(),
					1),
				1)
		});
		MakeAssertions(output, "{}");
	}

	[Test]
	public void TestCompile_Block()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedBlock(
					new List<ITypedAuraStatement>
					{
						new TypedLet(
							new Tok(TokType.Identifier, "i", 2),
							true,
							false,
							new IntLiteral(5, 2),
							2),
					},
					new Nil(),
					1),
				1)
		});
		MakeAssertions(output, "{\nvar i int = 5\n}");
	}

	[Test]
	public void TestCompile_Call_Stdlib()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedCall(
					new TypedGet(
						new TypedVariable(
							new Tok(TokType.Identifier, "io", 1),
							new Module(
								"io",
								new List<NamedFunction>
								{
									new(
										"Println",
										Visibility.Private,
										new Function(
											new List<Param>
											{
												new(
													new Tok(TokType.Identifier, "s", 1),
													new ParamType(new AuraString(), false, null))
											},
											new Nil()))
								},
								new List<Class>(),
								new Dictionary<string, ITypedAuraExpression>()),
							1),
						new Tok(TokType.Identifier, "println", 1),
						new NamedFunction(
							"println",
							Visibility.Private,
							new Function(
								new List<Param>
								{
									new Param(
										new Tok(TokType.Identifier, "s", 1),
										new ParamType(
											new AuraString(),
											false,
											null))
								},
								new Nil())),
						1),
					new List<ITypedAuraExpression> { new StringLiteral("Hello world", 1) },
					new Nil(),
					1),
				1)
		});
		MakeAssertions(output, "io.Println(\"Hello world\")");
	}

	[Test]
	public void TestCompile_Call_NoArgs()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedCall(
					new TypedVariable(
						new Tok(TokType.Identifier, "f", 1),
						new NamedFunction(
							"f",
							Visibility.Private,
							new Function(
								new List<Param>(),
								new Nil())),
						1),
					new List<ITypedAuraExpression>(),
					new Nil(),
					1),
				1)
		});
		MakeAssertions(output, "f()");
	}

	[Test]
	public void TestCompile_Call_OneParam()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedCall(
					new TypedVariable(
						new Tok(TokType.Identifier, "f", 1),
						new NamedFunction(
							"f",
							Visibility.Private,
							new Function(
								new List<Param>
								{
									new(
										new Tok(TokType.Identifier, "i", 1),
										new ParamType(new Int(), false, null))
								},
								new Nil())),
						1),
					new List<ITypedAuraExpression> { new IntLiteral(5, 1) },
					new Nil(),
					1),
				1)
		});
		MakeAssertions(output, "f(5)");
	}

	[Test]
	public void TestCompile_Call_TwoParams()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedCall(
					new TypedVariable(
						new Tok(TokType.Identifier, "f", 1),
						new NamedFunction(
							"f",
							Visibility.Private,
							new Function(
								new List<Param>
								{
									new(
										new Tok(TokType.Identifier, "i", 1),
										new ParamType(new Int(), false, null)),
									new(
										new Tok(TokType.Identifier, "s", 1),
										new ParamType(new AuraString(), false, null))
								},
								new Nil())),
						1),
					new List<ITypedAuraExpression> { new IntLiteral(5, 1), new StringLiteral("Hello world", 1) },
					new Nil(),
					1),
				1)
		});
		MakeAssertions(output, "f(5, \"Hello world\")");
	}

	[Test]
	public void TestCompile_Get()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedGet(
					new TypedVariable(
						new Tok(TokType.Identifier, "greeter", 1),
						new Class(
							"Greeter",
							new List<Param>
							{
								new Param(
									new Tok(TokType.Identifier, "name", 1),
									new ParamType(new AuraString(), false, null))
							},
							new List<NamedFunction>(),
							new List<Interface>(),
							Visibility.Private),
						1),
					new Tok(TokType.Identifier, "name", 1),
					new AuraString(),
					1),
				1)
		});
		MakeAssertions(output, "greeter.name");
	}

	[Test]
	public void TestCompile_GetIndex()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedGetIndex(
					new TypedVariable(
						new Tok(TokType.Identifier, "names", 1),
						new Types.List(new AuraString()),
						1),
					new IntLiteral(0, 1),
					new AuraString(),
					1),
				1)
		});
		MakeAssertions(output, "names[0]");
	}

	[Test]
	public void TestCompile_GetIndexRange()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedGetIndexRange(
					new TypedVariable(
						new Tok(TokType.Identifier, "names", 1),
						new Types.List(new AuraString()),
						1),
					new IntLiteral(0, 1),
					new IntLiteral(2, 1),
					new Types.List(new AuraString()),
					1),
				1)
		});
		MakeAssertions(output, "names[0:2]");
	}

	[Test]
	public void TestCompile_Grouping()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedGrouping(
					new StringLiteral("Hello world", 1),
					new AuraString(),
					1),
				1)
		});
		MakeAssertions(output, "(\"Hello world\")");
	}

	[Test]
	public void TestCompile_If()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedIf(
					new BoolLiteral(true, 1),
					new TypedBlock(
						new List<ITypedAuraStatement>
						{
							new TypedReturn(
								new IntLiteral(1, 2),
								2)
						},
						new Int(),
						1),
					null,
					new Int(),
					1),
				1)
		});
		MakeAssertions(output, "if true {\nreturn 1\n}");
	}

	[Test]
	public void TestCompile_If_Else()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedIf(
					new BoolLiteral(true, 1),
					new TypedBlock(
						new List<ITypedAuraStatement>
						{
							new TypedReturn(
								new IntLiteral(1, 2),
								2)
						},
						new Int(),
						1),
					new TypedBlock(
						new List<ITypedAuraStatement>
						{
							new TypedReturn(
								new IntLiteral(2, 4),
								2)
						},
						new Int(),
						3),
					new Int(),
					1),
				1)
		});
		MakeAssertions(output, "if true {\nreturn 1\n} else {\nreturn 2\n}");
	}

	[Test]
	public void TestCompile_IntLiteral()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new IntLiteral(5, 1),
				1)
		});
		MakeAssertions(output, "5");
	}

	[Test]
	public void TestCompile_FloatLiteral()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new FloatLiteral(5.1, 1),
				1)
		});
		MakeAssertions(output, "5.1");
	}

	[Test]
	public void TestCompile_FloatLiteral_WithZeroDecimal()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new FloatLiteral(5.0, 1),
				1)
		});
		MakeAssertions(output, "5");
	}

	[Test]
	public void TestCompile_StringLiteral()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new StringLiteral("Hello world", 1),
				1)
		});
		MakeAssertions(output, "\"Hello world\"");
	}

	[Test]
	public void TestCompile_ListLiteral()
	{
		var output = ArrangeAndAct(new System.Collections.Generic.List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new ListLiteral<ITypedAuraExpression>(
					new List<ITypedAuraExpression> { new IntLiteral(1, 1), new IntLiteral(2, 1), new IntLiteral(3, 1) },
					new Int(),
					1),
				1)
		});
		MakeAssertions(output, "[]int{1, 2, 3}");
	}

	[Test]
	public void TestCompile_MapLiteral()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new MapLiteral<ITypedAuraExpression, ITypedAuraExpression>(
					new Dictionary<ITypedAuraExpression, ITypedAuraExpression>
					{
						{ new StringLiteral("Hello", 1), new IntLiteral(1, 1) }
					},
					new AuraString(),
					new Int(),
					1),
				1)
		});
		MakeAssertions(output, "map[string]int{\n\"Hello\": 1\n}");
	}

	[Test]
	public void TestCompile_MapLiteral_Empty()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new MapLiteral<ITypedAuraExpression, ITypedAuraExpression>(
					new Dictionary<ITypedAuraExpression, ITypedAuraExpression>(),
					new AuraString(),
					new Int(),
					1),
				1)
		});
		MakeAssertions(output, "map[string]int{}");
	}

	[Test]
	public void TestCompile_BoolLiteral()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new BoolLiteral(true, 1),
				1)
		});
		MakeAssertions(output, "true");
	}

	[Test]
	public void TestCompile_NilLiteral()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedNil(1),
				1)
		});
		MakeAssertions(output, "nil");
	}

	[Test]
	public void TestCompile_CharLiteral()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new CharLiteral('a', 1),
				1)
		});
		MakeAssertions(output, "'a'");
	}

	[Test]
	public void TestCompile_Logical()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedLogical(
					new BoolLiteral(true, 1),
					new Tok(TokType.And, "and", 1),
					new BoolLiteral(false, 1),
					new Bool(),
					1),
				1)
		});
		MakeAssertions(output, "true && false");
	}

	[Test]
	public void TestCompile_Set()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedSet(
					new TypedVariable(
						new Tok(TokType.Identifier, "greeter", 1),
						new Class(
							"Greeter",
							new List<Param>
							{
								new Param(
									new Tok(TokType.Identifier, "name", 1),
									new ParamType(new AuraString(), false, null))
							},
							new List<NamedFunction>(),
							new List<Interface>(),
							Visibility.Private),
						1),
					new Tok(TokType.Identifier, "name", 1),
					new StringLiteral("Bob", 1),
					new AuraString(),
					1),
				1)
		});
		MakeAssertions(output, "greeter.name = \"Bob\"");
	}

	[Test]
	public void TestCompile_This()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedThis(
					new Tok(TokType.This, "this", 1),
					new Class(
						"Greeter",
						new List<Param>(),
						new List<NamedFunction>(),
						new List<Interface>(),
						Visibility.Private),
					1),
				1)
		});
		MakeAssertions(output, "this");
	}

	[Test]
	public void TestCompile_Unary_Bang()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedUnary(
					new Tok(TokType.Bang, "!", 1),
					new BoolLiteral(true, 1),
					new Bool(),
					1),
				1)
		});
		MakeAssertions(output, "!true");
	}

	[Test]
	public void TestCompile_Unary_Minus()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedUnary(
					new Tok(TokType.Minus, "-", 1),
					new IntLiteral(5, 1),
					new Int(),
					1),
				1)
		});
		MakeAssertions(output, "-5");
	}

	[Test]
	public void TestCompile_Variable()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedVariable(
					new Tok(TokType.Identifier, "name", 1),
					new AuraString(),
					1),
				1)
		});
		MakeAssertions(output, "name");
	}

	[Test]
	public void TestCompile_Defer()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedDefer(
				new TypedCall(
					new TypedVariable(
						new Tok(TokType.Identifier, "f", 1),
						new NamedFunction(
							"f",
							Visibility.Private,
							new Function(
								new List<Param>(),
								new Nil())),
						1),
					new List<ITypedAuraExpression>(),
					new Nil(),
					1),
				1)
		});
		MakeAssertions(output, "defer f()");
	}

	[Test]
	public void TestCompile_For_EmptyBody()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedFor(
				new TypedLet(
					new Tok(TokType.Identifier, "i", 1),
					false,
					true,
					new IntLiteral(0, 1),
					1),
				new TypedLogical(
					new TypedVariable(
						new Tok(TokType.Identifier, "i", 1),
						new Int(),
						1),
					new Tok(TokType.Less, "<", 1),
					new IntLiteral(10, 1),
					new Bool(),
					1),
				null,
				new List<ITypedAuraStatement>(),
				1)
		});
		MakeAssertions(output, "for i := 0; i < 10; {}");
	}

	[Test]
	public void TestCompile_For()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedFor(
				new TypedLet(
					new Tok(TokType.Identifier, "i", 1),
					false,
					true,
					new IntLiteral(0, 1),
					1),
				new TypedLogical(
					new TypedVariable(
						new Tok(TokType.Identifier, "i", 1),
						new Int(),
						1),
					new Tok(TokType.Less, "<", 1),
					new IntLiteral(10, 1),
					new Bool(),
					1),
				null,
				new List<ITypedAuraStatement>
				{
					new TypedLet(
						new Tok(TokType.Identifier, "name", 2),
						false,
						false,
						new StringLiteral("Bob", 2),
						2)
				},
				1)
		});
		MakeAssertions(output, "for i := 0; i < 10; {\nname := \"Bob\"\n}");
	}

	[Test]
	public void TestCompile_ForEach_EmptyBody()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedForEach(
				new Tok(TokType.Identifier, "name", 1),
				new TypedVariable(
					new Tok(TokType.Identifier, "names", 1),
					new AuraList(new AuraString()),
					1),
				new List<ITypedAuraStatement>(),
				1)
		});
		MakeAssertions(output, "for _, name := range names {}");
	}

	[Test]
	public void TestCompile_ForEach()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedForEach(
				new Tok(TokType.Identifier, "name", 1),
				new TypedVariable(
					new Tok(TokType.Identifier, "names", 1),
					new AuraList(new AuraString()),
					1),
				new List<ITypedAuraStatement>
				{
					new TypedLet(
						new Tok(TokType.Identifier, "i", 1),
						true,
						false,
						new IntLiteral(5, 1),
						2)
				},
				1)
		});
		MakeAssertions(output, "for _, name := range names {\nvar i int = 5\n}");
	}

	[Test]
	public void TestCompile_NamedFunction_NoParams_NoReturnType_NoBody()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedNamedFunction(
				new Tok(TokType.Identifier, "f", 1),
				new List<Param>(),
				new TypedBlock(new List<ITypedAuraStatement>(), new Nil(), 1),
				new Nil(),
				Visibility.Private,
				1)
		});
		MakeAssertions(output, "func f() {}");
	}

	[Test]
	public void TestCompile_AnonymousFunction_NoParams_NoReturnType_NoBody()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedAnonymousFunction(
					new List<Param>(),
					new TypedBlock(new List<ITypedAuraStatement>(), new Nil(), 1),
					new Nil(),
					1),
				1)
		});
		MakeAssertions(output, "func() {}");
	}

	[Test]
	public void TestCompile_Let_Long()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedLet(
				new Tok(TokType.Identifier, "i", 1),
				true,
				false,
				new IntLiteral(5, 1),
				1)
		});
		MakeAssertions(output, "var i int = 5");
	}

	[Test]
	public void TestCompile_Let_Short()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedLet(
				new Tok(TokType.Identifier, "i", 1),
				false,
				false,
				new IntLiteral(5, 1),
				1)
		});
		MakeAssertions(output, "i := 5");
	}

	[Test]
	public void TestCompile_Mod()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedMod(
				new Tok(TokType.Identifier, "main", 1),
				1)
		});
		MakeAssertions(output, "package main");
	}

	[Test]
	public void TestCompile_Return_NoValue()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement> { new TypedReturn(null, 1) });
		MakeAssertions(output, "return");
	}

	[Test]
	public void TestCompile_Return()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement> { new TypedReturn(new IntLiteral(5, 1), 1) });
		MakeAssertions(output, "return 5");
	}

	[Test]
	public void TestCompile_Class_ImplementingTwoInterfaces()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new FullyTypedClass(
				new Tok(TokType.Identifier, "Greeter", 1),
				new List<Param>(),
				new List<TypedNamedFunction>(),
				Visibility.Public,
				new List<Interface>
				{
					new("IGreeter", new List<NamedFunction>(), Visibility.Private),
					new("IGreeter2", new List<NamedFunction>(), Visibility.Private)
				},
				1)
		});
		// Since classes implicitly implement interfaces in Go, the compiler doesn't need any special handling
		// for classes that implement an interface
		MakeAssertions(output, "type GREETER struct {}");
	}

	[Test]
	public void TestCompile_Class_ImplementingInterface()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new FullyTypedClass(
				new Tok(TokType.Identifier, "Greeter", 1),
				new List<Param>(),
				new List<TypedNamedFunction>(),
				Visibility.Public,
				new List<Interface> { new("IGreeter", new List<NamedFunction>(), Visibility.Private) },
				1)
		});
		// Since classes implicitly implement interfaces in Go, the compiler doesn't need any special handling
		// for classes that implement an interface
		MakeAssertions(output, "type GREETER struct {}");
	}

	[Test]
	public void TestCompile_Class_NoParams_NoMethods()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new FullyTypedClass(
				new Tok(TokType.Identifier, "Greeter", 1),
				new List<Param>(),
				new List<TypedNamedFunction>(),
				Visibility.Public,
				new List<Interface>(),
				1)
		});
		MakeAssertions(output, "type GREETER struct {}");
	}

	[Test]
	public void TestCompile_While_EmptyBody()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedWhile(
				new BoolLiteral(true, 1),
				new List<ITypedAuraStatement>(),
				1)
		});
		MakeAssertions(output, "for true {}");
	}

	[Test]
	public void TestCompile_Import_NoAlias()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedImport(
				new Tok(TokType.Identifier, "test_pkg", 1),
				null,
				1)
		});
		MakeAssertions(output, "import \"test/test_pkg\"");
	}

	[Test]
	public void TestCompile_Import_Alias()
	{
		_localModuleReader.Setup(lmr => lmr.GetModuleSourcePaths(It.IsAny<string>())).Returns(Array.Empty<string>());

		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedImport(
				new Tok(TokType.Identifier, "test_pkg", 1),
				new Tok(TokType.Identifier, "tp", 1),
				1)
		});
		MakeAssertions(output, "import tp \"test/test_pkg\"");
	}

	[Test]
	public void TestCompile_Import_StdlibPkg()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedImport(
				new Tok(TokType.Identifier, "aura/io", 1),
				null,
				1)
		});
		MakeAssertions(output, "import io \"test/stdlib/io\"");
	}

	[Test]
	public void TestCompile_Comment()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedComment(
				new Tok(TokType.Comment, "// this is a comment", 1),
				1)
		});
		MakeAssertions(output, "// this is a comment");
	}

	[Test]
	public void TestCompile_Is()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedIs(
					new TypedVariable(
						new Tok(TokType.Identifier, "v", 1),
						new Int(),
						1),
					new Interface(
						"IGreeter",
						new List<NamedFunction>(),
						Visibility.Private),
					1),
				1)
		});
		MakeAssertions(output, "v.(IGreeter)");
	}

	[Test]
	public void TestCompile_Is_InLetStmt()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedLet(
				new Tok(TokType.Identifier, "b", 1),
				false,
				false,
				new TypedIs(
					new TypedVariable(
						new Tok(TokType.Identifier, "v", 1),
						new Int(),
						1),
					new Interface(
						"IGreeter",
						new List<NamedFunction>(),
						Visibility.Private),
					1),
				1)
		});
		MakeAssertions(output, "_, b := v.(IGreeter)");
	}

	[Test]
	public void TestCompile_Is_IfCondition()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedIf(
					new TypedIs(
						new TypedVariable(
							new Tok(TokType.Identifier, "v", 1),
							new Int(),
							1),
						new Interface(
							"IGreeter",
							new List<NamedFunction>(),
							Visibility.Private),
						1),
					new TypedBlock(
						new List<ITypedAuraStatement>(),
						new Nil(),
						1),
					null,
					new Nil(),
					1),
				1)
		});
		MakeAssertions(output, "if _, ok := v.(IGreeter); ok {}");
	}

	[Test]
	public void TestCompile_Interface_NoMethods()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedInterface(
				new Tok(TokType.Identifier, "IGreeter", 1),
				new List<NamedFunction>(),
				Visibility.Public,
				1)
		});
		MakeAssertions(output, "type IGREETER interface {}");
	}

    [Test]
    public void TestCompile_MultipleImports()
    {
        var output = ArrangeAndAct(new List<ITypedAuraStatement>
        {
            new TypedMultipleImport(
                Packages: new List<Tok>
                {
                    new(TokType.Identifier, "aura/io", 1),
                    new(TokType.Identifier, "aura/strings", 1)
                },
                Line: 1
            )
        });
        MakeAssertions(output, "import (\n\tio \"test/stdlib/io\"\n\tstrings \"test/stdlib/strings\"\n)");
    }

	[Test]
	public void TestCompile_Interface_OneMethod()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedInterface(
				new Tok(TokType.Identifier, "IGreeter", 1),
				new List<NamedFunction>
				{
					new NamedFunction(
						"say_hi",
						Visibility.Public,
						new Function(
							new List<Param>
							{
								new Param(
									new Tok(TokType.Identifier, "i", 1),
									new ParamType(
										new Int(),
										false,
										null))
							},
							new AuraString()))
				},
				Visibility.Public,
				1)
		});
		MakeAssertions(output, "type IGREETER interface {\nSAY_HI(i int) string\n}");
	}

	private string ArrangeAndAct(List<ITypedAuraStatement> typedAst)
	{
		_outputWriter.Setup(ow => ow.CreateDirectory(It.IsAny<string>()));
		_outputWriter.Setup(ow => ow.WriteOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
		// Arrange
		var compiler = new AuraCompiler(typedAst, "test", _localModuleReader.Object, _outputWriter.Object, "Test");
		// Act
		return compiler.Compile();
	}

	private void MakeAssertions(string output, string expected)
	{
		output = output.Trim();
		Assert.Multiple(() =>
		{
			Assert.That(output.Length, Is.EqualTo(expected.Length));
			Assert.That(output, Is.EqualTo(expected));
		});
	}
}
