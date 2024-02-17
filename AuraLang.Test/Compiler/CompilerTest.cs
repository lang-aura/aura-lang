﻿using AuraLang.AST;
using AuraLang.Compiler;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.TypeChecker;
using AuraLang.Types;
using Moq;

namespace AuraLang.Test.Compiler;

public class CompilerTest
{
	private readonly Mock<CompiledOutputWriter> _outputWriter = new();
	private readonly Mock<LocalModuleReader> _localModuleReader = new();

	[Test]
	public void TestCompile_Assignment()
	{
		var output = ArrangeAndAct(
			typedAst: new List<ITypedAuraStatement>
			{
				new TypedExpressionStmt(
					new TypedAssignment(
						new Tok(TokType.Identifier, "i", 1),
						new IntLiteral(new Tok(TokType.IntLiteral, "5", 1)),
						new AuraInt()))
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
					new IntLiteral(new Tok(TokType.IntLiteral, "5", 1)),
					new Tok(TokType.Plus, "+", 1),
					new IntLiteral(new Tok(TokType.IntLiteral, "5", 1)),
					new AuraInt()))
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
					new Tok(TokType.LeftBrace, "{", 1),
					new List<ITypedAuraStatement>(),
					new Tok(TokType.RightBrace, "}", 1),
					new AuraNil()))
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
					new Tok(TokType.LeftBrace, "{", 1),
					new List<ITypedAuraStatement>
					{
						new TypedLet(
							Let: new Tok(TokType.Let, "let", 1),
							Names: new List<Tok>{ new(TokType.Identifier, "i", 2) },
							TypeAnnotation: true,
							Mutable: false,
							Initializer: new IntLiteral(new Tok(TokType.IntLiteral, "5", 1))
						),
					},
					new Tok(TokType.RightBrace, "}", 1),
					new AuraNil()))
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
							new AuraModule(
								name: "io",
								publicFunctions: new List<AuraNamedFunction>
								{
									new(
										"Println",
										Visibility.Private,
										new AuraFunction(
											new List<Param>
											{
												new(
													new Tok(TokType.Identifier, "s", 1),
													new ParamType(new AuraString(), false, null))
											},
											new AuraNil()))
								},
								publicInterfaces: new List<AuraInterface>(),
								publicClasses: new List<AuraClass>(),
								publicVariables: new Dictionary<string, ITypedAuraExpression>())),
						new Tok(TokType.Identifier, "println", 1),
						new AuraNamedFunction(
							"println",
							Visibility.Private,
							new AuraFunction(
								new List<Param>
								{
									new Param(
										new Tok(TokType.Identifier, "s", 1),
										new ParamType(
											new AuraString(),
											false,
											null))
								},
								new AuraNil()))),
					new List<ITypedAuraExpression> { new StringLiteral(new Tok(TokType.StringLiteral, "Hello world", 1)) },
					new Tok(TokType.RightParen, ")", 1),
					new AuraNil()))
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
						new AuraNamedFunction(
							"f",
							Visibility.Private,
							new AuraFunction(
								new List<Param>(),
								new AuraNil()))),
					new List<ITypedAuraExpression>(),
					new Tok(TokType.RightParen, ")", 1),
					new AuraNil()))
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
						new AuraNamedFunction(
							"f",
							Visibility.Private,
							new AuraFunction(
								new List<Param>
								{
									new(
										new Tok(TokType.Identifier, "i", 1),
										new ParamType(new AuraInt(), false, null))
								},
								new AuraNil()))),
					new List<ITypedAuraExpression> { new IntLiteral(new Tok(TokType.IntLiteral, "5", 1)) },
					new Tok(TokType.RightParen, ")", 1),
					new AuraNil()))
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
								new AuraNil()))),
					new List<ITypedAuraExpression> { new IntLiteral(new Tok(TokType.IntLiteral, "5", 1)), new StringLiteral(new Tok(TokType.StringLiteral, "Hello world", 1)) },
					new Tok(TokType.RightParen, ")", 1),
					new AuraNil()))
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
							Visibility.Private)),
					new Tok(TokType.Identifier, "name", 1),
					new AuraString()))
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
						new AuraList(new AuraString())),
					new IntLiteral(new Tok(TokType.IntLiteral, "0", 1)),
					new Tok(TokType.RightBracket, "]", 1),
					new AuraString()))
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
						new AuraList(new AuraString())),
					new IntLiteral(new Tok(TokType.IntLiteral, "0", 1)),
					new IntLiteral(new Tok(TokType.IntLiteral, "2", 1)),
					new Tok(TokType.RightBracket, "]", 1),
					new AuraList(new AuraString())))
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
					new Tok(TokType.LeftParen, "(", 1),
					new StringLiteral(new Tok(TokType.StringLiteral, "Hello world", 1)),
					new Tok(TokType.RightParen, ")", 1),
					new AuraString()))
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
					new Tok(TokType.If, "if", 1),
					new BoolLiteral(new Tok(TokType.True, "true", 1)),
					new TypedBlock(
						new Tok(TokType.LeftBrace, "{", 1),
						new List<ITypedAuraStatement>
						{
							new TypedReturn(
								new Tok(TokType.Return, "return", 1),
								new IntLiteral(new Tok(TokType.IntLiteral, "1", 2)))
						},
						new Tok(TokType.RightBrace, "}", 1),
						new AuraInt()),
					null,
					new AuraInt()))
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
					new Tok(TokType.If, "if", 1),
					new BoolLiteral(new Tok(TokType.True, "true", 1)),
					new TypedBlock(
						new Tok(TokType.LeftBrace, "{", 1),
						new List<ITypedAuraStatement>
						{
							new TypedReturn(
								new Tok(TokType.Return, "return", 1),
								new IntLiteral(new Tok(TokType.IntLiteral, "1", 1)))
						},
						new Tok(TokType.RightBrace, "}", 1),
						new AuraInt()),
					new TypedBlock(
						new Tok(TokType.LeftBrace, "{", 1),
						new List<ITypedAuraStatement>
						{
							new TypedReturn(
								new Tok(TokType.Return, "return", 1),
								new IntLiteral(new Tok(TokType.IntLiteral, "2", 1)))
						},
						new Tok(TokType.RightBrace, "}", 1),
						new AuraInt()),
					new AuraInt()))
		});
		MakeAssertions(output, "if true {\nreturn 1\n} else {\nreturn 2\n}");
	}

	[Test]
	public void TestCompile_IntLiteral()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new IntLiteral(new Tok(TokType.IntLiteral, "5", 1)))
		});
		MakeAssertions(output, "5");
	}

	[Test]
	public void TestCompile_FloatLiteral()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new FloatLiteral(new Tok(TokType.FloatLiteral, "5.1", 1)))
		});
		MakeAssertions(output, "5.1");
	}

	[Test]
	public void TestCompile_FloatLiteral_WithZeroDecimal()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new FloatLiteral(new Tok(TokType.FloatLiteral, "5.0", 1)))
		});
		MakeAssertions(output, "5.0");
	}

	[Test]
	public void TestCompile_StringLiteral()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new StringLiteral(new Tok(TokType.StringLiteral, "Hello world", 1)))
		});
		MakeAssertions(output, "\"Hello world\"");
	}

	[Test]
	public void TestCompile_ListLiteral()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new ListLiteral<ITypedAuraExpression>(
					new Tok(TokType.LeftBrace, "{", 1),
					new List<ITypedAuraExpression> { new IntLiteral(new Tok(TokType.IntLiteral, "1", 1)), new IntLiteral(new Tok(TokType.IntLiteral, "2", 1)), new IntLiteral(new Tok(TokType.IntLiteral, "3", 1)) },
					new AuraInt(),
					new Tok(TokType.RightBrace, "}", 1)))
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
					new Tok(TokType.Map, "map", 1),
					new Dictionary<ITypedAuraExpression, ITypedAuraExpression>
					{
						{ new StringLiteral(new Tok(TokType.StringLiteral, "Hello", 1)), new IntLiteral(new Tok(TokType.IntLiteral, "1", 1)) }
					},
					new AuraString(),
					new AuraInt(),
					new Tok(TokType.RightBrace, "}", 1)))
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
					new Tok(TokType.Map, "map", 1),
					new Dictionary<ITypedAuraExpression, ITypedAuraExpression>(),
					new AuraString(),
					new AuraInt(),
					new Tok(TokType.RightBrace, "}", 1)))
		});
		MakeAssertions(output, "map[string]int{}");
	}

	[Test]
	public void TestCompile_BoolLiteral()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new BoolLiteral(new Tok(TokType.True, "true", 1)))
		});
		MakeAssertions(output, "true");
	}

	[Test]
	public void TestCompile_NilLiteral()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new TypedNil(new Tok(TokType.Nil, "nil", 1)))
		});
		MakeAssertions(output, "nil");
	}

	[Test]
	public void TestCompile_CharLiteral()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedExpressionStmt(
				new CharLiteral(new Tok(TokType.CharLiteral, "a", 1)))
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
					new BoolLiteral(new Tok(TokType.True, "true", 1)),
					new Tok(TokType.And, "and", 1),
					new BoolLiteral(new Tok(TokType.False, "false", 1)),
					new AuraBool()))
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
						new AuraClass(
							"Greeter",
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "name", 1),
									new ParamType(new AuraString(), false, null))
							},
							new List<AuraNamedFunction>(),
							new List<AuraInterface>(),
							Visibility.Private)),
					new Tok(TokType.Identifier, "name", 1),
					new StringLiteral(new Tok(TokType.StringLiteral, "Bob", 1)),
					new AuraString()))
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
					new AuraClass(
						"Greeter",
						new List<Param>(),
						new List<AuraNamedFunction>(),
						new List<AuraInterface>(),
						Visibility.Private)))
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
					new BoolLiteral(new Tok(TokType.True, "true", 1)),
					new AuraBool()))
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
					new IntLiteral(new Tok(TokType.IntLiteral, "5", 1)),
					new AuraInt()))
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
					new AuraString()))
		});
		MakeAssertions(output, "name");
	}

	[Test]
	public void TestCompile_Defer()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedDefer(
				new Tok(TokType.Defer, "defer", 1),
				new TypedCall(
					new TypedVariable(
						new Tok(TokType.Identifier, "f", 1),
						new AuraNamedFunction(
							"f",
							Visibility.Private,
							new AuraFunction(
								new List<Param>(),
								new AuraNil()))),
					new List<ITypedAuraExpression>(),
					new Tok(TokType.RightParen, ")", 1),
					new AuraNil()))
		});
		MakeAssertions(output, "defer f()");
	}

	[Test]
	public void TestCompile_For_EmptyBody()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedFor(
				new Tok(TokType.For, "for", 1),
				new TypedLet(
					new Tok(TokType.Let, "let", 1),
					new List<Tok>{ new(TokType.Identifier, "i", 1) },
					false,
					true,
					new IntLiteral(new Tok(TokType.IntLiteral, "0", 1))),
				new TypedLogical(
					new TypedVariable(
						new Tok(TokType.Identifier, "i", 1),
						new AuraInt()),
					new Tok(TokType.Less, "<", 1),
					new IntLiteral(new Tok(TokType.IntLiteral, "10", 1)),
					new AuraBool()),
				null,
				new List<ITypedAuraStatement>(),
				new Tok(TokType.RightBrace, "}", 1))
		});
		MakeAssertions(output, "for i := 0; i < 10; {}");
	}

	[Test]
	public void TestCompile_For()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedFor(
				new Tok(TokType.For, "for", 1),
				new TypedLet(
					new Tok(TokType.Let, "let", 1),
					new List<Tok>{ new(TokType.Identifier, "i", 1) },
					false,
					true,
					new IntLiteral(new Tok(TokType.IntLiteral, "0", 1))),
				new TypedLogical(
					new TypedVariable(
						new Tok(TokType.Identifier, "i", 1),
						new AuraInt()),
					new Tok(TokType.Less, "<", 1),
					new IntLiteral(new Tok(TokType.IntLiteral, "10", 1)),
					new AuraBool()),
				null,
				new List<ITypedAuraStatement>
				{
					new TypedLet(
						null,
						new List<Tok>{ new(TokType.Identifier, "name", 2) },
						false,
						false,
						new StringLiteral(new Tok(TokType.StringLiteral, "Bob", 1)))
				},
				new Tok(TokType.RightBrace, "}", 1))
		});
		MakeAssertions(output, "for i := 0; i < 10; {\nname := \"Bob\"\n}");
	}

	[Test]
	public void TestCompile_ForEach_EmptyBody()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedForEach(
				new Tok(TokType.ForEach, "foreach", 1),
				new Tok(TokType.Identifier, "name", 1),
				new TypedVariable(
					new Tok(TokType.Identifier, "names", 1),
					new AuraList(new AuraString())),
				new List<ITypedAuraStatement>(),
				new Tok(TokType.RightBrace, "}", 1))
		});
		MakeAssertions(output, "for _, name := range names {}");
	}

	[Test]
	public void TestCompile_ForEach()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedForEach(
				new Tok(TokType.ForEach, "foreach", 1),
				new Tok(TokType.Identifier, "name", 1),
				new TypedVariable(
					new Tok(TokType.Identifier, "names", 1),
					new AuraList(new AuraString())),
				new List<ITypedAuraStatement>
				{
					new TypedLet(
						null,
						new List<Tok>{ new(TokType.Identifier, "i", 1) },
						true,
						false,
						new IntLiteral(new Tok(TokType.IntLiteral, "5", 1)))
				},
				new Tok(TokType.RightBrace, "}", 1))
		});
		MakeAssertions(output, "for _, name := range names {\nvar i int = 5\n}");
	}

	[Test]
	public void TestCompile_NamedFunction_NoParams_NoReturnType_NoBody()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedNamedFunction(
				new Tok(TokType.Fn, "fn", 1),
				new Tok(TokType.Identifier, "f", 1),
				new List<Param>(),
				new TypedBlock(new Tok(TokType.LeftBrace, "{", 1), new List<ITypedAuraStatement>(), new Tok(TokType.RightBrace, "}", 1), new AuraNil()),
				new AuraNil(),
				Visibility.Private)
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
					new Tok(TokType.Fn, "fn", 1),
					new List<Param>(),
					new TypedBlock(new Tok(TokType.LeftBrace, "{", 1), new List<ITypedAuraStatement>(), new Tok(TokType.RightBrace, "}", 1), new AuraNil()),
					new AuraNil()))
		});
		MakeAssertions(output, "func() {}");
	}

	[Test]
	public void TestCompile_Let_Long()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedLet(
				null,
				new List<Tok>{ new(TokType.Identifier, "i", 1) },
				true,
				false,
				new IntLiteral(new Tok(TokType.IntLiteral, "5", 1)))
		});
		MakeAssertions(output, "var i int = 5");
	}

	[Test]
	public void TestCompile_Let_Short()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedLet(
				null,
				new List<Tok>{ new(TokType.Identifier, "i", 1) },
				false,
				false,
				new IntLiteral(new Tok(TokType.IntLiteral, "5", 1)))
		});
		MakeAssertions(output, "i := 5");
	}

	[Test]
	public void TestCompile_Mod()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedMod(
				new Tok(TokType.Mod, "mod", 1),
				new Tok(TokType.Identifier, "main", 1))
		});
		MakeAssertions(output, "package main");
	}

	[Test]
	public void TestCompile_Return_NoValue()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement> { new TypedReturn(new Tok(TokType.Return, "return", 1), null) });
		MakeAssertions(output, "return");
	}

	[Test]
	public void TestCompile_Return()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement> { new TypedReturn(new Tok(TokType.Return, "return", 1), new IntLiteral(new Tok(TokType.IntLiteral, "5", 1))) });
		MakeAssertions(output, "return 5");
	}

	[Test]
	public void TestCompile_Class_ImplementingTwoInterfaces()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new FullyTypedClass(
				new Tok(TokType.Class, "class", 1),
				new Tok(TokType.Identifier, "Greeter", 1),
				new List<Param>(),
				new List<TypedNamedFunction>(),
				Visibility.Public,
				new List<AuraInterface>
				{
					new("IGreeter", new List<AuraNamedFunction>(), Visibility.Private),
					new("IGreeter2", new List<AuraNamedFunction>(), Visibility.Private)
				},
				new Tok(TokType.RightBrace, "}", 1))
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
				new Tok(TokType.Class, "class", 1),
				new Tok(TokType.Identifier, "Greeter", 1),
				new List<Param>(),
				new List<TypedNamedFunction>(),
				Visibility.Public,
				new List<AuraInterface> { new("IGreeter", new List<AuraNamedFunction>(), Visibility.Private) },
				new Tok(TokType.RightBrace, "}", 1))
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
				new Tok(TokType.Class, "class", 1),
				new Tok(TokType.Identifier, "Greeter", 1),
				new List<Param>(),
				new List<TypedNamedFunction>(),
				Visibility.Public,
				new List<AuraInterface>(),
				new Tok(TokType.RightBrace, "}", 1))
		});
		MakeAssertions(output, "type GREETER struct {}");
	}

	[Test]
	public void TestCompile_While_EmptyBody()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedWhile(
				new Tok(TokType.While, "while", 1),
				new BoolLiteral(new Tok(TokType.True, "true", 1)),
				new List<ITypedAuraStatement>(),
				new Tok(TokType.RightBrace, "}", 1))
		});
		MakeAssertions(output, "for true {}");
	}

	[Test]
	public void TestCompile_Import_StdlibPkg()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedImport(
				new Tok(TokType.Import, "import", 1),
				new Tok(TokType.Identifier, "aura/io", 1),
				null)
		});
		MakeAssertions(output, "import io \"test/stdlib/io\"");
	}

	[Test]
	public void TestCompile_Comment()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedComment(
				new Tok(TokType.Comment, "// this is a comment", 1))
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
						new AuraInt()),
					new AuraInterface(
						"IGreeter",
						new List<AuraNamedFunction>(),
						Visibility.Private)))
		});
		MakeAssertions(output, "v.(IGreeter)");
	}

	[Test]
	public void TestCompile_Is_InLetStmt()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedLet(
				null,
				new List<Tok>{ new(TokType.Identifier, "b", 1) },
				false,
				false,
				new TypedIs(
					new TypedVariable(
						new Tok(TokType.Identifier, "v", 1),
						new AuraInt()),
					new AuraInterface(
						"IGreeter",
						new List<AuraNamedFunction>(),
						Visibility.Private)))
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
					new Tok(TokType.If, "if", 1),
					new TypedIs(
						new TypedVariable(
							new Tok(TokType.Identifier, "v", 1),
							new AuraInt()),
						new AuraInterface(
							"IGreeter",
							new List<AuraNamedFunction>(),
							Visibility.Private)),
					new TypedBlock(
						new Tok(TokType.LeftBrace, "{", 1),
						new List<ITypedAuraStatement>(),
						new Tok(TokType.RightBrace, "}", 1),
						new AuraNil()),
					null,
					new AuraNil()))
		});
		MakeAssertions(output, "if _, ok := v.(IGreeter); ok {}");
	}

	[Test]
	public void TestCompile_Interface_NoMethods()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedInterface(
				new Tok(TokType.Interface, "interface", 1),
				new Tok(TokType.Identifier, "IGreeter", 1),
				new List<AuraNamedFunction>(),
				Visibility.Public,
				new Tok(TokType.RightBrace, "}", 1))
		});
		MakeAssertions(output, "type IGREETER interface {}");
	}

	[Test]
	public void TestCompile_MultipleImports()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedMultipleImport(
				Import: new Tok(TokType.Import, "import", 1),
				Packages: new List<TypedImport>
				{
					new(new Tok(TokType.Import, "import", 1), new Tok(TokType.Identifier, "aura/io", 1), null),
					new(new Tok(TokType.Import, "import,", 1), new Tok(TokType.Identifier, "aura/strings", 1), null)
				},
				ClosingParen: new Tok(TokType.RightParen, ")", 1)
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
				new Tok(TokType.Interface, "interface", 1),
				new Tok(TokType.Identifier, "IGreeter", 1),
				new List<AuraNamedFunction>
				{
					new AuraNamedFunction(
						"say_hi",
						Visibility.Public,
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
				new Tok(TokType.RightBrace, "}", 1))
		});
		MakeAssertions(output, "type IGREETER interface {\nSAY_HI(i int) string\n}");
	}

	[Test]
	public void TestCompile_Yield()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedYield(
				Yield: new Tok(TokType.Yield, "yield", 1),
				Value: new IntLiteral(
					Int: new Tok(TokType.IntLiteral, "5", 1))
			)
		});
		MakeAssertions(output, "x = 5");
	}

	[Test]
	public void TestCompile_Check()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedCheck(
				Check: new Tok(TokType.Check, "check", 1),
				Call: new TypedCall(
					Callee: new TypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "f",
							line: 1
						),
						Typ: new AuraNamedFunction(
							name: "f",
							pub: Visibility.Public,
							f: new AuraFunction(
								fParams: new List<Param>(),
								returnType: new AuraError()
							)
						)
					),
					Arguments: new List<ITypedAuraExpression>(),
					Typ: new AuraError(),
					ClosingParen: new Tok(TokType.RightParen, ")", 1))
			)
		});
		MakeAssertions(output, "e := f()\nif e.Failure != nil {\nreturn e\n}");
	}

	[Test]
	public void TestCompile_Struct_NoParams()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedStruct(
				Struct: new Tok(TokType.Struct, "struct", 1),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "s",
					line: 1
				),
				Params: new List<Param>(),
				ClosingParen: new Tok(TokType.RightParen, ")", 1)
			)
		});
		MakeAssertions(output, "type s struct {}");
	}

	[Test]
	public void TestCompile_Struct_OneParam()
	{
		var output = ArrangeAndAct(new List<ITypedAuraStatement>
		{
			new TypedStruct(
				Struct: new Tok(TokType.Struct, "struct", 1),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "s",
					line: 1
				),
				Params: new List<Param>
				{
					new(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "i",
							line: 1
						),
						ParamType: new(
							Typ: new AuraInt(),
							Variadic: false,
							DefaultValue: null
						)
					)
				},
				ClosingParen: new Tok(TokType.RightParen, ")", 1)
			)
		});
		MakeAssertions(output, "type s struct {\ni int\n}");
	}

	private string ArrangeAndAct(List<ITypedAuraStatement> typedAst)
	{
		_outputWriter.Setup(ow => ow.CreateDirectory(It.IsAny<string>()));
		_outputWriter.Setup(ow => ow.WriteOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
		// Arrange
		var compiler = new AuraCompiler(typedAst, "test", _outputWriter.Object, new Stack<TypedNamedFunction>(), "Test");
		// Act
		return compiler.Compile();
	}

	private void MakeAssertions(string output, string expected)
	{
		output = output.Trim();
		Assert.Multiple(() =>
		{
			Assert.That(output, Has.Length.EqualTo(expected.Length));
			Assert.That(output, Is.EqualTo(expected));
		});
	}
}
