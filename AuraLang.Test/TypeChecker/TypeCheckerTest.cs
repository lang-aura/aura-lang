using AuraLang.AST;
using AuraLang.Cli.LocalFileSystemModuleProvider;
using AuraLang.Exceptions.TypeChecker;
using AuraLang.Location;
using AuraLang.Shared;
using AuraLang.Stores;
using AuraLang.Symbol;
using AuraLang.Token;
using AuraLang.TypeChecker;
using AuraLang.Types;
using Moq;
using Newtonsoft.Json;
using Range = AuraLang.Location.Range;

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
		_symbolsTable.Setup(v => v.GetSymbol("i", It.IsAny<string>())).Returns(new AuraSymbol("i", new AuraInt()));

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedAssignment(
						new Tok(TokType.Identifier, "i"),
						new IntLiteral(new Tok(TokType.IntLiteral, "6"))
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new TypedAssignment(
					new Tok(TokType.Identifier, "i"),
					new IntLiteral(new Tok(TokType.IntLiteral, "6")),
					new AuraInt()
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Binary()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedBinary(
						new BoolLiteral(new Tok(TokType.True, "true")),
						new Tok(TokType.And, "and"),
						new BoolLiteral(new Tok(TokType.True, "false"))
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new TypedBinary(
					new BoolLiteral(new Tok(TokType.True, "true")),
					new Tok(TokType.And, "and"),
					new BoolLiteral(new Tok(TokType.True, "false")),
					new AuraInt()
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Block_EmptyBody()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedBlock(
						new Tok(TokType.LeftBrace, "{"),
						new List<IUntypedAuraStatement>(),
						new Tok(TokType.RightBrace, "}")
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new TypedBlock(
					new Tok(TokType.LeftBrace, "{"),
					new List<ITypedAuraStatement>(),
					new Tok(TokType.RightBrace, "}"),
					new AuraNil()
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Block()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedBlock(
						new Tok(TokType.LeftBrace, "{"),
						new List<IUntypedAuraStatement>
						{
							new UntypedLet(
								new Tok(TokType.Let, "let"),
								new List<Tok> { new(TokType.Identifier, "i") },
								new List<AuraType> { new AuraInt() },
								false,
								new IntLiteral(new Tok(TokType.IntLiteral, "5"))
							)
						},
						new Tok(TokType.RightBrace, "}")
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new TypedBlock(
					new Tok(TokType.LeftBrace, "{"),
					new List<ITypedAuraStatement>
					{
						new TypedLet(
							new Tok(TokType.Let, "let"),
							new List<Tok> { new(TokType.Identifier, "i") },
							true,
							false,
							new IntLiteral(new Tok(TokType.IntLiteral, "5"))
						)
					},
					new Tok(TokType.RightBrace, "}"),
					new AuraNil()
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Call_NoArgs()
	{
		_symbolsTable
			.Setup(v => v.GetSymbol("f", It.IsAny<string>()))
			.Returns(
				new AuraSymbol(
					"f",
					new AuraNamedFunction(
						"f",
						Visibility.Private,
						new AuraFunction(new List<Param>(), new AuraNil())
					)
				)
			);

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedCall(
						new UntypedVariable(new Tok(TokType.Identifier, "f")),
						new List<(Tok?, IUntypedAuraExpression)>(),
						new Tok(
							TokType.RightParen,
							")",
							new Range(new Position(2, 1), new Position(3, 1))
						)
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
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
						new Range(new Position(2, 1), new Position(3, 1))
					),
					new AuraNamedFunction(
						"f",
						Visibility.Private,
						new AuraFunction(new List<Param>(), new AuraNil())
					)
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_TwoArgs_WithTags()
	{
		_symbolsTable
			.Setup(v => v.GetSymbol("f", It.IsAny<string>()))
			.Returns(
				new AuraSymbol(
					"f",
					new AuraNamedFunction(
						"f",
						Visibility.Private,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "i"),
									new ParamType(
										new AuraInt(),
										false,
										null
									)
								),
								new(
									new Tok(TokType.Identifier, "s"),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
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
						new UntypedVariable(new Tok(TokType.Identifier, "f")),
						new List<(Tok?, IUntypedAuraExpression)>
						{
							(new Tok(TokType.Identifier, "s"),
								new StringLiteral(new Tok(TokType.StringLiteral, "Hello world"))),
							(new Tok(TokType.Identifier, "i"), new IntLiteral(new Tok(TokType.IntLiteral, "5")))
						},
						new Tok(TokType.RightParen, ")")
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new TypedCall(
					new TypedVariable(
						new Tok(TokType.Identifier, "f"),
						new AuraNamedFunction(
							"f",
							Visibility.Private,
							new AuraFunction(
								new List<Param>
								{
									new(
										new Tok(TokType.Identifier, "i"),
										new ParamType(
											new AuraInt(),
											false,
											null
										)
									),
									new(
										new Tok(TokType.Identifier, "s"),
										new ParamType(
											new AuraString(),
											false,
											null
										)
									)
								},
								new AuraNil()
							)
						)
					),
					new List<ITypedAuraExpression>
					{
						new IntLiteral(new Tok(TokType.IntLiteral, "5")),
						new StringLiteral(new Tok(TokType.StringLiteral, "Hello world"))
					},
					new Tok(TokType.RightParen, ")"),
					new AuraNamedFunction(
						"f",
						Visibility.Private,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "i"),
									new ParamType(
										new AuraInt(),
										false,
										null
									)
								),
								new(
									new Tok(TokType.Identifier, "s"),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new AuraNil()
						)
					)
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Call_DefaultValues()
	{
		_symbolsTable
			.Setup(v => v.GetSymbol("f", It.IsAny<string>()))
			.Returns(
				new AuraSymbol(
					"f",
					new AuraNamedFunction(
						"f",
						Visibility.Private,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "i"),
									new ParamType(
										new AuraInt(),
										false,
										new IntLiteral(new Tok(TokType.IntLiteral, "10"))
									)
								),
								new(
									new Tok(TokType.Identifier, "s"),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
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
						new UntypedVariable(new Tok(TokType.Identifier, "f")),
						new List<(Tok?, IUntypedAuraExpression)>
						{
							(new Tok(TokType.Identifier, "s"),
								new StringLiteral(new Tok(TokType.StringLiteral, "Hello world")))
						},
						new Tok(TokType.RightParen, ")")
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new TypedCall(
					new TypedVariable(
						new Tok(TokType.Identifier, "f"),
						new AuraNamedFunction(
							"f",
							Visibility.Private,
							new AuraFunction(
								new List<Param>
								{
									new(
										new Tok(TokType.Identifier, "i"),
										new ParamType(
											new AuraInt(),
											false,
											new IntLiteral(new Tok(TokType.IntLiteral, "10"))
										)
									),
									new(
										new Tok(TokType.Identifier, "s"),
										new ParamType(
											new AuraString(),
											false,
											null
										)
									)
								},
								new AuraNil()
							)
						)
					),
					new List<ITypedAuraExpression>
					{
						new IntLiteral(new Tok(TokType.IntLiteral, "10")),
						new StringLiteral(new Tok(TokType.StringLiteral, "Hello world"))
					},
					new Tok(TokType.RightParen, ")"),
					new AuraNamedFunction(
						"f",
						Visibility.Private,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "i"),
									new ParamType(
										new AuraInt(),
										false,
										new IntLiteral(new Tok(TokType.IntLiteral, "10"))
									)
								),
								new(
									new Tok(TokType.Identifier, "s"),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new AuraNil()
						)
					)
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Call_NoValueForParameterWithoutDefaultValue()
	{
		_symbolsTable
			.Setup(v => v.GetSymbol("f", It.IsAny<string>()))
			.Returns(
				new AuraSymbol(
					"f",
					new AuraNamedFunction(
						"f",
						Visibility.Private,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "i"),
									new ParamType(
										new AuraInt(),
										false,
										null
									)
								),
								new(
									new Tok(TokType.Identifier, "s"),
									new ParamType(
										new AuraString(),
										false,
										new StringLiteral(new Tok(TokType.StringLiteral, "Hello world"))
									)
								)
							},
							new AuraNil()
						)
					)
				)
			);

		ArrangeAndAct_Invalid(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedCall(
						new UntypedVariable(new Tok(TokType.Identifier, "f")),
						new List<(Tok?, IUntypedAuraExpression)>
						{
							(new Tok(TokType.Identifier, "s"),
								new StringLiteral(new Tok(TokType.StringLiteral, "Hello world")))
						},
						new Tok(TokType.RightParen, ")")
					)
				)
			},
			typeof(MustSpecifyValueForArgumentWithoutDefaultValueException)
		);
	}

	[Test]
	public void TestTypeCheck_Call_MixNamedAndUnnamedArguments()
	{
		_symbolsTable
			.Setup(v => v.GetSymbol("f", It.IsAny<string>()))
			.Returns(
				new AuraSymbol(
					"f",
					new AuraNamedFunction(
						"f",
						Visibility.Private,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "i"),
									new ParamType(
										new AuraInt(),
										false,
										null
									)
								),
								new(
									new Tok(TokType.Identifier, "s"),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new AuraNil()
						)
					)
				)
			);

		ArrangeAndAct_Invalid(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedCall(
						new UntypedVariable(new Tok(TokType.Identifier, "f")),
						new List<(Tok?, IUntypedAuraExpression)>
						{
							(new Tok(TokType.Identifier, "s"),
								new StringLiteral(new Tok(TokType.StringLiteral, "Hello world"))),
							(null, new IntLiteral(new Tok(TokType.IntLiteral, "5")))
						},
						new Tok(TokType.RightParen, ")")
					)
				)
			},
			typeof(CannotMixNamedAndUnnamedArgumentsException)
		);
	}

	[Test]
	public void TestTypeCheck_Get()
	{
		_symbolsTable
			.Setup(v => v.GetSymbol("greeter", It.IsAny<string>()))
			.Returns(
				new AuraSymbol(
					"greeter",
					new AuraClass(
						"Greeter",
						new List<Param>
						{
							new(
								new Tok(TokType.Identifier, "name"),
								new ParamType(
									new AuraString(),
									false,
									null
								)
							)
						},
						new List<AuraNamedFunction>(),
						new List<AuraInterface>(),
						Visibility.Private
					)
				)
			);

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedGet(
						new UntypedVariable(new Tok(TokType.Identifier, "greeter")),
						new Tok(TokType.Identifier, "name")
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new TypedGet(
					new TypedVariable(
						new Tok(TokType.Identifier, "greeter"),
						new AuraClass(
							"Greeter",
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "name"),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new List<AuraNamedFunction>(),
							new List<AuraInterface>(),
							Visibility.Private
						)
					),
					new Tok(TokType.Identifier, "name"),
					new AuraString()
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_GetIndex()
	{
		_symbolsTable
			.Setup(v => v.GetSymbol("names", It.IsAny<string>()))
			.Returns(new AuraSymbol("names", new AuraList(new AuraString())));

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedGetIndex(
						new UntypedVariable(new Tok(TokType.Identifier, "names")),
						new IntLiteral(new Tok(TokType.IntLiteral, "0")),
						new Tok(TokType.RightBracket, "]")
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new TypedGetIndex(
					new TypedVariable(new Tok(TokType.Identifier, "names"), new AuraList(new AuraString())),
					new IntLiteral(new Tok(TokType.IntLiteral, "0")),
					new Tok(TokType.RightBracket, "]"),
					new AuraString()
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_GetIndexRange()
	{
		_symbolsTable
			.Setup(v => v.GetSymbol("names", It.IsAny<string>()))
			.Returns(new AuraSymbol("names", new AuraList(new AuraString())));

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedGetIndexRange(
						new UntypedVariable(new Tok(TokType.Identifier, "names")),
						new IntLiteral(new Tok(TokType.IntLiteral, "0")),
						new IntLiteral(new Tok(TokType.IntLiteral, "2")),
						new Tok(TokType.RightBracket, "]")
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new TypedGetIndexRange(
					new TypedVariable(new Tok(TokType.Identifier, "names"), new AuraList(new AuraString())),
					new IntLiteral(new Tok(TokType.IntLiteral, "0")),
					new IntLiteral(new Tok(TokType.IntLiteral, "2")),
					new Tok(TokType.RightBracket, "]"),
					new AuraList(new AuraString())
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Grouping()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedGrouping(
						new Tok(TokType.LeftParen, "("),
						new StringLiteral(new Tok(TokType.StringLiteral, "Hello world")),
						new Tok(TokType.RightParen, ")")
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new TypedGrouping(
					new Tok(TokType.LeftParen, "("),
					new StringLiteral(new Tok(TokType.StringLiteral, "Hello world")),
					new Tok(TokType.RightParen, ")"),
					new AuraString()
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_If_EmptyThenBranch()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedIf(
						new Tok(TokType.If, "if"),
						new BoolLiteral(new Tok(TokType.True, "true")),
						new UntypedBlock(
							new Tok(TokType.LeftBrace, "{"),
							new List<IUntypedAuraStatement>(),
							new Tok(TokType.RightBrace, "}")
						),
						null
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new TypedIf(
					new Tok(TokType.If, "if"),
					new BoolLiteral(new Tok(TokType.True, "true")),
					new TypedBlock(
						new Tok(TokType.LeftBrace, "{"),
						new List<ITypedAuraStatement>(),
						new Tok(TokType.RightBrace, "}"),
						new AuraNil()
					),
					null,
					new AuraNil()
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_IntLiteral()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(new IntLiteral(new Tok(TokType.IntLiteral, "5")))
			}
		);
		MakeAssertions(typedAst, new TypedExpressionStmt(new IntLiteral(new Tok(TokType.IntLiteral, "5"))));
	}

	[Test]
	public void TestTypeCheck_FloatLiteral()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(new FloatLiteral(new Tok(TokType.FloatLiteral, "5.1")))
			}
		);
		MakeAssertions(typedAst, new TypedExpressionStmt(new FloatLiteral(new Tok(TokType.FloatLiteral, "5.1"))));
	}

	[Test]
	public void TestTypeCheck_StringLiteral()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(new StringLiteral(new Tok(TokType.StringLiteral, "Hello world")))
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(new StringLiteral(new Tok(TokType.StringLiteral, "Hello world")))
		);
	}

	[Test]
	public void TestTypeCheck_ListLiteral()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new ListLiteral<IUntypedAuraExpression>(
						new Tok(TokType.LeftBracket, "["),
						new List<IUntypedAuraExpression> { new IntLiteral(new Tok(TokType.IntLiteral, "1")) },
						new AuraInt(),
						new Tok(TokType.RightBrace, "]")
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new ListLiteral<ITypedAuraExpression>(
					new Tok(TokType.LeftBracket, "["),
					new List<ITypedAuraExpression> { new IntLiteral(new Tok(TokType.IntLiteral, "1")) },
					new AuraInt(),
					new Tok(TokType.RightBrace, "]")
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_MapLiteral()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new MapLiteral<IUntypedAuraExpression, IUntypedAuraExpression>(
						new Tok(TokType.Map, "map"),
						new Dictionary<IUntypedAuraExpression, IUntypedAuraExpression>
						{
							{
								new StringLiteral(new Tok(TokType.StringLiteral, "Hello")),
								new IntLiteral(new Tok(TokType.IntLiteral, "1"))
							}
						},
						new AuraString(),
						new AuraInt(),
						new Tok(TokType.RightBrace, "}")
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new MapLiteral<ITypedAuraExpression, ITypedAuraExpression>(
					new Tok(TokType.Map, "map"),
					new Dictionary<ITypedAuraExpression, ITypedAuraExpression>
					{
						{
							new StringLiteral(new Tok(TokType.StringLiteral, "Hello")),
							new IntLiteral(new Tok(TokType.IntLiteral, "1"))
						}
					},
					new AuraString(),
					new AuraInt(),
					new Tok(TokType.RightBrace, "}")
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_BoolLiteral()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(new BoolLiteral(new Tok(TokType.True, "true")))
			}
		);
		MakeAssertions(typedAst, new TypedExpressionStmt(new BoolLiteral(new Tok(TokType.True, "true"))));
	}

	[Test]
	public void TestTypeCheck_NilLiteral()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement> { new UntypedExpressionStmt(new UntypedNil(new Tok(TokType.Nil, "nil"))) }
		);
		MakeAssertions(typedAst, new TypedExpressionStmt(new TypedNil(new Tok(TokType.Nil, "nil"))));
	}

	[Test]
	public void TestTypeCheck_CharLiteral()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(new CharLiteral(new Tok(TokType.CharLiteral, "a")))
			}
		);
		MakeAssertions(typedAst, new TypedExpressionStmt(new CharLiteral(new Tok(TokType.CharLiteral, "a"))));
	}

	[Test]
	public void TestTypeCheck_Logical()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedLogical(
						new IntLiteral(new Tok(TokType.IntLiteral, "5")),
						new Tok(TokType.Less, "<"),
						new IntLiteral(new Tok(TokType.IntLiteral, "10"))
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new TypedLogical(
					new IntLiteral(new Tok(TokType.IntLiteral, "5")),
					new Tok(TokType.Less, "<"),
					new IntLiteral(new Tok(TokType.IntLiteral, "10")),
					new AuraBool()
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Set()
	{
		_symbolsTable
			.Setup(v => v.GetSymbol("greeter", It.IsAny<string>()))
			.Returns(
				new AuraSymbol(
					"greeter",
					new AuraClass(
						"Greeter",
						new List<Param>
						{
							new(
								new Tok(TokType.Identifier, "name"),
								new ParamType(
									new AuraString(),
									false,
									null
								)
							)
						},
						new List<AuraNamedFunction>(),
						new List<AuraInterface>(),
						Visibility.Private
					)
				)
			);

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedSet(
						new UntypedVariable(new Tok(TokType.Identifier, "greeter")),
						new Tok(TokType.Identifier, "name"),
						new StringLiteral(new Tok(TokType.StringLiteral, "Bob"))
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new TypedSet(
					new TypedVariable(
						new Tok(TokType.Identifier, "greeter"),
						new AuraClass(
							"Greeter",
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "name"),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new List<AuraNamedFunction>(),
							new List<AuraInterface>(),
							Visibility.Private
						)
					),
					new Tok(TokType.Identifier, "name"),
					new StringLiteral(new Tok(TokType.StringLiteral, "Bob")),
					new AuraString()
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_This()
	{
		_enclosingClassStore
			.Setup(ecs => ecs.Peek())
			.Returns(
				new PartiallyTypedClass(
					new Tok(TokType.Class, "class"),
					new Tok(TokType.Identifier, "Greeter"),
					new List<Param>(),
					new List<AuraNamedFunction>(),
					Visibility.Public,
					new Tok(TokType.RightBrace, "}"),
					new AuraClass(
						"Greeter",
						new List<Param>(),
						new List<AuraNamedFunction>(),
						new List<AuraInterface>(),
						Visibility.Private
					)
				)
			);

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(new UntypedThis(new Tok(TokType.This, "this")))
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new TypedThis(
					new Tok(TokType.This, "this"),
					new AuraClass(
						"Greeter",
						new List<Param>(),
						new List<AuraNamedFunction>(),
						new List<AuraInterface>(),
						Visibility.Private
					)
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Unary_Bang()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedUnary(new Tok(TokType.Bang, "!"), new BoolLiteral(new Tok(TokType.True, "true")))
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new TypedUnary(
					new Tok(TokType.Bang, "!"),
					new BoolLiteral(new Tok(TokType.True, "true")),
					new AuraBool()
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Unary_Minus()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedUnary(new Tok(TokType.Minus, "-"), new IntLiteral(new Tok(TokType.IntLiteral, "5")))
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new TypedUnary(
					new Tok(TokType.Minus, "-"),
					new IntLiteral(new Tok(TokType.IntLiteral, "5")),
					new AuraInt()
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Variable()
	{
		_symbolsTable
			.Setup(v => v.GetSymbol("name", It.IsAny<string>()))
			.Returns(new AuraSymbol("name", new AuraString()));

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(new UntypedVariable(new Tok(TokType.Identifier, "name")))
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(new TypedVariable(new Tok(TokType.Identifier, "name"), new AuraString()))
		);
	}

	[Test]
	public void TestTypeCheck_Defer()
	{
		_symbolsTable
			.Setup(v => v.GetSymbol("f", It.IsAny<string>()))
			.Returns(
				new AuraSymbol(
					"f",
					new AuraNamedFunction(
						"f",
						Visibility.Private,
						new AuraFunction(new List<Param>(), new AuraNil())
					)
				)
			);

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedDefer(
					new Tok(TokType.Defer, "defer"),
					new UntypedCall(
						new UntypedVariable(new Tok(TokType.Identifier, "f")),
						new List<(Tok?, IUntypedAuraExpression)>(),
						new Tok(TokType.RightParen, ")")
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedDefer(
				new Tok(TokType.Defer, "defer"),
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
					new Tok(TokType.RightParen, ")"),
					new AuraNamedFunction(
						"f",
						Visibility.Private,
						new AuraFunction(new List<Param>(), new AuraNil())
					)
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_For_EmptyBody()
	{
		_symbolsTable.Setup(v => v.GetSymbol("i", It.IsAny<string>())).Returns(new AuraSymbol("i", new AuraInt()));

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedFor(
					new Tok(TokType.For, "for"),
					new UntypedLet(
						null,
						new List<Tok> { new(TokType.Identifier, "i") },
						new List<AuraType>(),
						false,
						new IntLiteral(new Tok(TokType.IntLiteral, "0"))
					),
					new UntypedLogical(
						new UntypedVariable(new Tok(TokType.Identifier, "i")),
						new Tok(TokType.Less, "<"),
						new IntLiteral(new Tok(TokType.IntLiteral, "10"))
					),
					null,
					new List<IUntypedAuraStatement>(),
					new Tok(TokType.RightBrace, "}")
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedFor(
				new Tok(TokType.For, "for"),
				new TypedLet(
					null,
					new List<Tok> { new(TokType.Identifier, "i") },
					false,
					false,
					new IntLiteral(new Tok(TokType.IntLiteral, "0"))
				),
				new TypedLogical(
					new TypedVariable(new Tok(TokType.Identifier, "i"), new AuraInt()),
					new Tok(TokType.Less, "<"),
					new IntLiteral(new Tok(TokType.IntLiteral, "10")),
					new AuraBool()
				),
				null,
				new List<ITypedAuraStatement>(),
				new Tok(TokType.RightBrace, "}")
			)
		);
	}

	[Test]
	public void TestTypeCheck_ForEach_EmptyBody()
	{
		_symbolsTable
			.Setup(v => v.GetSymbol("names", It.IsAny<string>()))
			.Returns(new AuraSymbol("names", new AuraList(new AuraString())));

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedForEach(
					new Tok(TokType.ForEach, "foreach"),
					new Tok(TokType.Identifier, "name"),
					new UntypedVariable(new Tok(TokType.Identifier, "names")),
					new List<IUntypedAuraStatement>(),
					new Tok(TokType.RightBrace, "}")
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedForEach(
				new Tok(TokType.ForEach, "foreach"),
				new Tok(TokType.Identifier, "name"),
				new TypedVariable(new Tok(TokType.Identifier, "names"), new AuraList(new AuraString())),
				new List<ITypedAuraStatement>(),
				new Tok(TokType.RightBrace, "}")
			)
		);
	}

	[Test]
	public void TestTypeCheck_NamedFunction_NoParams_ReturnError()
	{
		_symbolsTable
			.Setup(v => v.GetSymbol("error", It.IsAny<string>()))
			.Returns(
				new AuraSymbol(
					"error",
					new AuraNamedFunction(
						"error",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "message"),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new AuraError()
						)
					)
				)
			);

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedNamedFunction(
					new Tok(TokType.Fn, "fn"),
					new Tok(TokType.Identifier, "f"),
					new List<Param>(),
					new UntypedBlock(
						new Tok(TokType.LeftBrace, "{"),
						new List<IUntypedAuraStatement>
						{
							new UntypedReturn(
								new Tok(TokType.Return, "return"),
								new List<IUntypedAuraExpression>
								{
									new UntypedCall(
										new UntypedVariable(new Tok(TokType.Identifier, "error")),
										new List<(Tok?, IUntypedAuraExpression)>
										{
											(null,
												new StringLiteral(
													new Tok(TokType.StringLiteral, "Helpful error message")
												))
										},
										new Tok(TokType.RightParen, ")")
									)
								}
							)
						},
						new Tok(TokType.RightBrace, "}")
					),
					new List<AuraType> { new AuraError() },
					Visibility.Public,
					string.Empty
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedNamedFunction(
				new Tok(TokType.Fn, "fn"),
				new Tok(TokType.Identifier, "f"),
				new List<Param>(),
				new TypedBlock(
					new Tok(TokType.LeftBrace, "{"),
					new List<ITypedAuraStatement>
					{
						new TypedReturn(
							new Tok(TokType.Return, "return"),
							new TypedCall(
								new TypedVariable(
									new Tok(TokType.Identifier, "error"),
									new AuraNamedFunction(
										"error",
										Visibility.Public,
										new AuraFunction(
											new List<Param>
											{
												new(
													new Tok(TokType.Identifier, "message"),
													new ParamType(
														new AuraString(),
														false,
														null
													)
												)
											},
											new AuraError()
										)
									)
								),
								new List<ITypedAuraExpression>
								{
									new StringLiteral(new Tok(TokType.StringLiteral, "Helpful error message"))
								},
								new Tok(TokType.RightParen, ")"),
								new AuraNamedFunction(
									"error",
									Visibility.Public,
									new AuraFunction(
										new List<Param>
										{
											new(
												new Tok(TokType.Identifier, "message"),
												new ParamType(
													new AuraString(),
													false,
													null
												)
											)
										},
										new AuraError()
									)
								)
							)
						)
					},
					new Tok(TokType.RightBrace, "}"),
					new AuraError()
				),
				new AuraError(),
				Visibility.Public,
				string.Empty
			)
		);
	}

	[Test]
	public void TestTypeCheck_NamedFunction_NoParams_NoReturnType_NoBody()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedNamedFunction(
					new Tok(TokType.Fn, "fn"),
					new Tok(TokType.Identifier, "f"),
					new List<Param>(),
					new UntypedBlock(
						new Tok(TokType.LeftBrace, "{"),
						new List<IUntypedAuraStatement>(),
						new Tok(TokType.RightBrace, "}")
					),
					null,
					Visibility.Public,
					string.Empty
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedNamedFunction(
				new Tok(TokType.Fn, "fn"),
				new Tok(TokType.Identifier, "f"),
				new List<Param>(),
				new TypedBlock(
					new Tok(TokType.LeftBrace, "{"),
					new List<ITypedAuraStatement>(),
					new Tok(TokType.RightBrace, "}"),
					new AuraNil()
				),
				new AuraNil(),
				Visibility.Public,
				string.Empty
			)
		);
	}

	[Test]
	public void TestTypeCheck_AnonymousFunction_NoParams_NoReturnType_NoBody()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedAnonymousFunction(
						new Tok(TokType.Fn, "fn"),
						new List<Param>(),
						new UntypedBlock(
							new Tok(TokType.LeftBrace, "{"),
							new List<IUntypedAuraStatement>(),
							new Tok(TokType.RightBrace, "}")
						),
						null
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new TypedAnonymousFunction(
					new Tok(TokType.Fn, "fn"),
					new List<Param>(),
					new TypedBlock(
						new Tok(TokType.LeftBrace, "{"),
						new List<ITypedAuraStatement>(),
						new Tok(TokType.RightBrace, "}"),
						new AuraNil()
					),
					new AuraNil()
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Let_Long()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedLet(
					new Tok(TokType.Let, "let"),
					new List<Tok> { new(TokType.Identifier, "i") },
					new List<AuraType> { new AuraInt() },
					false,
					new IntLiteral(new Tok(TokType.IntLiteral, "1"))
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedLet(
				new Tok(TokType.Let, "let"),
				new List<Tok> { new(TokType.Identifier, "i") },
				true,
				false,
				new IntLiteral(new Tok(TokType.IntLiteral, "1"))
			)
		);
	}

	[Test]
	public void TestTypeCheck_Let_Uninitialized()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedLet(
					new Tok(TokType.Let, "let"),
					new List<Tok> { new(TokType.Identifier, "i") },
					new List<AuraType> { new AuraInt() },
					false,
					null
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedLet(
				new Tok(TokType.Let, "let"),
				new List<Tok> { new(TokType.Identifier, "i") },
				true,
				false,
				new IntLiteral(new Tok(TokType.IntLiteral, "0"))
			)
		);
	}

	[Test]
	public void TestTypeCheck_Let_Uninitialized_NonDefaultable()
	{
		ArrangeAndAct_Invalid(
			new List<IUntypedAuraStatement>
			{
				new UntypedLet(
					new Tok(TokType.Let, "let"),
					new List<Tok> { new(TokType.Identifier, "c") },
					new List<AuraType> { new AuraChar() },
					false,
					null
				)
			},
			typeof(MustSpecifyInitialValueForNonDefaultableTypeException)
		);
	}

	[Test]
	public void TestTypeCheck_Long_Short()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedLet(
					null,
					new List<Tok> { new(TokType.Identifier, "i") },
					new List<AuraType>(),
					false,
					new IntLiteral(new Tok(TokType.IntLiteral, "1"))
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedLet(
				null,
				new List<Tok> { new(TokType.Identifier, "i") },
				false,
				false,
				new IntLiteral(new Tok(TokType.IntLiteral, "1"))
			)
		);
	}

	[Test]
	public void TestTypeCheck_Return_NoValue()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement> { new UntypedReturn(new Tok(TokType.Return, "return"), null) }
		);
		MakeAssertions(typedAst, new TypedReturn(new Tok(TokType.Return, "return"), null));
	}

	[Test]
	public void TestTypeCheck_Return()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedReturn(
					new Tok(TokType.Return, "return"),
					new List<IUntypedAuraExpression> { new IntLiteral(new Tok(TokType.IntLiteral, "5")) }
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedReturn(new Tok(TokType.Return, "return"), new IntLiteral(new Tok(TokType.IntLiteral, "5")))
		);
	}

	[Test]
	public void TestTypeCheck_Class_NoParams_NoMethods()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedClass(
					new Tok(TokType.Class, "class"),
					new Tok(TokType.Identifier, "Greeter"),
					new List<Param>(),
					new List<IUntypedAuraStatement>(),
					Visibility.Private,
					new List<Tok>(),
					new Tok(TokType.RightBrace, "}"),
					string.Empty
				)
			}
		);
		MakeAssertions(
			typedAst,
			new FullyTypedClass(
				new Tok(TokType.Class, "class"),
				new Tok(TokType.Identifier, "Greeter"),
				new List<Param>(),
				new List<TypedNamedFunction>(),
				Visibility.Private,
				new List<AuraInterface>(),
				new Tok(TokType.RightBrace, "}"),
				string.Empty
			)
		);
	}

	[Test]
	public void TestTypeCheck_While_EmptyBody()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedWhile(
					new Tok(TokType.While, "while"),
					new BoolLiteral(new Tok(TokType.True, "true")),
					new List<IUntypedAuraStatement>(),
					new Tok(TokType.RightBrace, "}")
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedWhile(
				new Tok(TokType.While, "while"),
				new BoolLiteral(new Tok(TokType.True, "true")),
				new List<ITypedAuraStatement>(),
				new Tok(TokType.RightBrace, "}")
			)
		);
	}

	[Test]
	public void TestTypeCheck_Comment()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement> { new UntypedComment(new Tok(TokType.Comment, "// this is a comment")) }
		);
		MakeAssertions(typedAst, new TypedComment(new Tok(TokType.Comment, "// this is a comment")));
	}

	[Test]
	public void TestTypeCheck_Yield()
	{
		_enclosingExprStore
			.Setup(expr => expr.Peek())
			.Returns(
				new UntypedBlock(
					new Tok(TokType.LeftBrace, "{"),
					new List<IUntypedAuraStatement>(),
					new Tok(TokType.RightBrace, "}")
				)
			);

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedYield(new Tok(TokType.Yield, "yield"), new IntLiteral(new Tok(TokType.IntLiteral, "5")))
			}
		);
		MakeAssertions(
			typedAst,
			new TypedYield(new Tok(TokType.Yield, "yield"), new IntLiteral(new Tok(TokType.IntLiteral, "5")))
		);
	}

	[Test]
	public void TestTypeCheck_Yield_Invalid()
	{
		_enclosingExprStore.Setup(expr => expr.Peek()).Returns(new UntypedNil(new Tok(TokType.Nil, "nil")));

		ArrangeAndAct_Invalid(
			new List<IUntypedAuraStatement>
			{
				new UntypedYield(new Tok(TokType.Yield, "yield"), new IntLiteral(new Tok(TokType.IntLiteral, "5")))
			},
			typeof(InvalidUseOfYieldKeywordException)
		);
	}

	[Test]
	public void TestTypeCheck_Break()
	{
		_enclosingStmtStore
			.Setup(stmt => stmt.Peek())
			.Returns(
				new UntypedWhile(
					new Tok(TokType.While, "while"),
					new BoolLiteral(new Tok(TokType.True, "true")),
					new List<IUntypedAuraStatement>(),
					new Tok(TokType.RightBrace, "}")
				)
			);

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement> { new UntypedBreak(new Tok(TokType.Break, "break")) }
		);
		MakeAssertions(typedAst, new TypedBreak(new Tok(TokType.Break, "break")));
	}

	[Test]
	public void TestTypeCheck_Break_Invalid()
	{
		_enclosingStmtStore
			.Setup(stmt => stmt.Peek())
			.Returns(new UntypedExpressionStmt(new UntypedNil(new Tok(TokType.Nil, "nil"))));

		ArrangeAndAct_Invalid(
			new List<IUntypedAuraStatement> { new UntypedBreak(new Tok(TokType.Break, "break")) },
			typeof(InvalidUseOfBreakKeywordException)
		);
	}

	[Test]
	public void TestTypeCheck_Continue()
	{
		_enclosingStmtStore
			.Setup(stmt => stmt.Peek())
			.Returns(
				new UntypedWhile(
					new Tok(TokType.While, "while"),
					new BoolLiteral(new Tok(TokType.True, "true")),
					new List<IUntypedAuraStatement>(),
					new Tok(TokType.RightBrace, "}")
				)
			);

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement> { new UntypedContinue(new Tok(TokType.Continue, "continue")) }
		);
		MakeAssertions(typedAst, new TypedContinue(new Tok(TokType.Continue, "continue")));
	}

	[Test]
	public void TestTypeCheck_Continue_Invalid()
	{
		_enclosingStmtStore
			.Setup(stmt => stmt.Peek())
			.Returns(new UntypedExpressionStmt(new UntypedNil(new Tok(TokType.Nil, "nil"))));

		ArrangeAndAct_Invalid(
			new List<IUntypedAuraStatement> { new UntypedContinue(new Tok(TokType.Continue, "continue")) },
			typeof(InvalidUseOfContinueKeywordException)
		);
	}

	[Test]
	public void TestTypeCheck_Interface_NoMethods()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedInterface(
					new Tok(TokType.Interface, "interface"),
					new Tok(TokType.Identifier, "IGreeter"),
					new List<UntypedFunctionSignature>(),
					Visibility.Public,
					new Tok(TokType.RightBrace, "}"),
					string.Empty
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedInterface(
				new Tok(TokType.Interface, "interface"),
				new Tok(TokType.Identifier, "IGreeter"),
				new List<TypedFunctionSignature>(),
				Visibility.Public,
				new Tok(TokType.RightBrace, "}"),
				string.Empty
			)
		);
	}

	[Test]
	public void TestTypeCheck_Interface_OneMethod()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedInterface(
					new Tok(TokType.Interface, "interface"),
					new Tok(TokType.Identifier, "IGreeter"),
					new List<UntypedFunctionSignature>
					{
						new(
							null,
							new Tok(TokType.Fn, "fn"),
							new Tok(TokType.Identifier, "say_hi"),
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "i"),
									new ParamType(
										new AuraInt(),
										false,
										null
									)
								)
							},
							new Tok(TokType.RightParen, ")"),
							new AuraString()
						)
					},
					Visibility.Public,
					new Tok(TokType.RightBrace, "}"),
					string.Empty
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedInterface(
				new Tok(TokType.Interface, "interface"),
				new Tok(TokType.Identifier, "IGreeter"),
				new List<TypedFunctionSignature>
				{
					new(
						null,
						new Tok(TokType.Fn, "fn"),
						new Tok(TokType.Identifier, "say_hi"),
						new List<Param>
						{
							new(
								new Tok(TokType.Identifier, "i"),
								new ParamType(
									new AuraInt(),
									false,
									null
								)
							)
						},
						new Tok(TokType.RightParen, ")"),
						new AuraString()
					)
				},
				Visibility.Public,
				new Tok(TokType.RightBrace, "}"),
				string.Empty
			)
		);
	}

	[Test]
	public void TestTypeCheck_ClassImplementingTwoInterfaces_NoMethods()
	{
		_symbolsTable
			.Setup(v => v.GetSymbol("IGreeter", It.IsAny<string>()))
			.Returns(
				new AuraSymbol(
					"IGreeter",
					new AuraInterface(
						"IGreeter",
						new List<AuraNamedFunction>(),
						Visibility.Private
					)
				)
			);
		_symbolsTable
			.Setup(v => v.GetSymbol("IGreeter2", It.IsAny<string>()))
			.Returns(
				new AuraSymbol(
					"IGreeter2",
					new AuraInterface(
						"IGreeter2",
						new List<AuraNamedFunction>(),
						Visibility.Private
					)
				)
			);

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedClass(
					new Tok(TokType.Class, "class"),
					new Tok(TokType.Identifier, "Greeter"),
					new List<Param>(),
					new List<IUntypedAuraStatement>(),
					Visibility.Private,
					new List<Tok> { new(TokType.Identifier, "IGreeter"), new(TokType.Identifier, "IGreeter2") },
					new Tok(TokType.RightBrace, "}"),
					string.Empty
				)
			}
		);
		MakeAssertions(
			typedAst,
			new FullyTypedClass(
				new Tok(TokType.Class, "class"),
				new Tok(TokType.Identifier, "Greeter"),
				new List<Param>(),
				new List<TypedNamedFunction>(),
				Visibility.Private,
				new List<AuraInterface>
				{
					new(
						"IGreeter",
						new List<AuraNamedFunction>(),
						Visibility.Private
					),
					new(
						"IGreeter2",
						new List<AuraNamedFunction>(),
						Visibility.Private
					)
				},
				new Tok(TokType.RightBrace, "}"),
				string.Empty
			)
		);
	}

	[Test]
	public void TestTypeCheck_ClassImplementingInterface_OneMethod_MissingImplementation()
	{
		_symbolsTable
			.Setup(v => v.GetSymbol("IGreeter", It.IsAny<string>()))
			.Returns(
				new AuraSymbol(
					"IGreeter",
					new AuraInterface(
						"IGreeter",
						new List<AuraNamedFunction>
						{
							new(
								"f",
								Visibility.Public,
								new AuraFunction(
									new List<Param>
									{
										new(
											new Tok(TokType.Identifier, "i"),
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
						Visibility.Private
					)
				)
			);

		ArrangeAndAct_Invalid(
			new List<IUntypedAuraStatement>
			{
				new UntypedClass(
					new Tok(TokType.Class, "class"),
					new Tok(TokType.Identifier, "Greeter"),
					new List<Param>(),
					new List<IUntypedAuraStatement>(),
					Visibility.Private,
					new List<Tok> { new(TokType.Identifier, "IGreeter") },
					new Tok(TokType.RightBrace, "}"),
					string.Empty
				)
			},
			typeof(MissingInterfaceMethodException)
		);
	}

	[Test]
	public void TestTypeCheck_ClassImplementingInterface_OneMethod_ImplementationNotPublic()
	{
		_symbolsTable
			.Setup(v => v.GetSymbol("IGreeter", It.IsAny<string>()))
			.Returns(
				new AuraSymbol(
					"IGreeter",
					new AuraInterface(
						"IGreeter",
						new List<AuraNamedFunction>
						{
							new(
								"f",
								Visibility.Public,
								new AuraFunction(
									new List<Param>
									{
										new(
											new Tok(TokType.Identifier, "i"),
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
						Visibility.Private
					)
				)
			);

		ArrangeAndAct_Invalid(
			new List<IUntypedAuraStatement>
			{
				new UntypedClass(
					new Tok(TokType.Class, "class"),
					new Tok(TokType.Identifier, "Greeter"),
					new List<Param>(),
					new List<IUntypedAuraStatement>
					{
						new UntypedNamedFunction(
							new Tok(TokType.Fn, "fn"),
							new Tok(TokType.Identifier, "f"),
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "i"),
									new ParamType(
										new AuraInt(),
										false,
										null
									)
								)
							},
							new UntypedBlock(
								new Tok(TokType.LeftBrace, "{"),
								new List<IUntypedAuraStatement>
								{
									new UntypedReturn(
										new Tok(TokType.Return, "return"),
										new List<IUntypedAuraExpression>
										{
											new IntLiteral(new Tok(TokType.IntLiteral, "5"))
										}
									)
								},
								new Tok(TokType.RightBrace, "}")
							),
							new List<AuraType> { new AuraInt() },
							Visibility.Private,
							string.Empty
						)
					},
					Visibility.Private,
					new List<Tok> { new(TokType.Identifier, "IGreeter") },
					new Tok(TokType.RightBrace, "}"),
					string.Empty
				)
			},
			typeof(MissingInterfaceMethodException)
		);
	}

	[Test]
	public void TestTypeCheck_ClassImplementingInterface_OneMethod()
	{
		_symbolsTable
			.Setup(v => v.GetSymbol("IGreeter", It.IsAny<string>()))
			.Returns(
				new AuraSymbol(
					"IGreeter",
					new AuraInterface(
						"IGreeter",
						new List<AuraNamedFunction>
						{
							new(
								"f",
								Visibility.Public,
								new AuraFunction(
									new List<Param>
									{
										new(
											new Tok(TokType.Identifier, "i"),
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
						Visibility.Private
					)
				)
			);

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedClass(
					new Tok(TokType.Class, "class"),
					new Tok(TokType.Identifier, "Greeter"),
					new List<Param>(),
					new List<IUntypedAuraStatement>
					{
						new UntypedNamedFunction(
							new Tok(TokType.Fn, "fn"),
							new Tok(TokType.Identifier, "f"),
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "i"),
									new ParamType(
										new AuraInt(),
										false,
										null
									)
								)
							},
							new UntypedBlock(
								new Tok(TokType.LeftBrace, "{"),
								new List<IUntypedAuraStatement>
								{
									new UntypedReturn(
										new Tok(TokType.Return, "return"),
										new List<IUntypedAuraExpression>
										{
											new IntLiteral(new Tok(TokType.IntLiteral, "5"))
										}
									)
								},
								new Tok(TokType.RightBrace, "}")
							),
							new List<AuraType> { new AuraInt() },
							Visibility.Public,
							string.Empty
						)
					},
					Visibility.Private,
					new List<Tok> { new(TokType.Identifier, "IGreeter") },
					new Tok(TokType.RightBrace, "}"),
					string.Empty
				)
			}
		);
		MakeAssertions(
			typedAst,
			new FullyTypedClass(
				new Tok(TokType.Class, "class"),
				new Tok(TokType.Identifier, "Greeter"),
				new List<Param>(),
				new List<TypedNamedFunction>
				{
					new(
						new Tok(TokType.Fn, "fn"),
						new Tok(TokType.Identifier, "f"),
						new List<Param>
						{
							new(
								new Tok(TokType.Identifier, "i"),
								new ParamType(
									new AuraInt(),
									false,
									null
								)
							)
						},
						new TypedBlock(
							new Tok(TokType.LeftBrace, "{"),
							new List<ITypedAuraStatement>
							{
								new TypedReturn(
									new Tok(TokType.Return, "return"),
									new IntLiteral(new Tok(TokType.IntLiteral, "5"))
								)
							},
							new Tok(TokType.RightBrace, "}"),
							new AuraInt()
						),
						new AuraInt(),
						Visibility.Public,
						string.Empty
					)
				},
				Visibility.Private,
				new List<AuraInterface>
				{
					new(
						"IGreeter",
						new List<AuraNamedFunction>
						{
							new(
								"f",
								Visibility.Public,
								new AuraFunction(
									new List<Param>
									{
										new(
											new Tok(TokType.Identifier, "i"),
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
						Visibility.Private
					)
				},
				new Tok(TokType.RightBrace, "}"),
				string.Empty
			)
		);
	}

	[Test]
	public void TestTypeCheck_ClassImplementingInterface_NoMethods()
	{
		_symbolsTable
			.Setup(v => v.GetSymbol("IGreeter", It.IsAny<string>()))
			.Returns(
				new AuraSymbol(
					"IGreeter",
					new AuraInterface(
						"IGreeter",
						new List<AuraNamedFunction>(),
						Visibility.Private
					)
				)
			);

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedClass(
					new Tok(TokType.Class, "class"),
					new Tok(TokType.Identifier, "Greeter"),
					new List<Param>(),
					new List<IUntypedAuraStatement>(),
					Visibility.Private,
					new List<Tok> { new(TokType.Identifier, "IGreeter") },
					new Tok(TokType.RightBrace, "}"),
					string.Empty
				)
			}
		);
		MakeAssertions(
			typedAst,
			new FullyTypedClass(
				new Tok(TokType.Class, "class"),
				new Tok(TokType.Identifier, "Greeter"),
				new List<Param>(),
				new List<TypedNamedFunction>(),
				Visibility.Private,
				new List<AuraInterface>
				{
					new(
						"IGreeter",
						new List<AuraNamedFunction>(),
						Visibility.Private
					)
				},
				new Tok(TokType.RightBrace, "}"),
				string.Empty
			)
		);
	}

	[Test]
	public void TestTypeCheck_Set_Invalid()
	{
		_symbolsTable.Setup(v => v.GetSymbol("v", It.IsAny<string>())).Returns(new AuraSymbol("v", new AuraInt()));

		ArrangeAndAct_Invalid(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedSet(
						new UntypedVariable(new Tok(TokType.Identifier, "v")),
						new Tok(TokType.Identifier, "name"),
						new StringLiteral(new Tok(TokType.StringLiteral, "Bob"))
					)
				)
			},
			typeof(CannotSetOnNonClassException)
		);
	}

	[Test]
	public void TestTypeCheck_Is()
	{
		_symbolsTable.Setup(v => v.GetSymbol("v", It.IsAny<string>())).Returns(new AuraSymbol("v", new AuraInt()));

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedIs(
						new UntypedVariable(new Tok(TokType.Identifier, "v")),
						new Tok(TokType.Identifier, "IGreeter")
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedExpressionStmt(
				new TypedIs(
					new TypedVariable(new Tok(TokType.Identifier, "v"), new AuraInt()),
					new AuraInterface(
						"IGreeter",
						new List<AuraNamedFunction>(),
						Visibility.Private
					)
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Check()
	{
		_enclosingFunctionDeclarationStore
			.Setup(f => f.Peek())
			.Returns(
				new UntypedNamedFunction(
					new Tok(TokType.Fn, "fn"),
					new Tok(TokType.Identifier, "f"),
					new List<Param>(),
					new UntypedBlock(
						new Tok(TokType.LeftBrace, "{"),
						new List<IUntypedAuraStatement>(),
						new Tok(TokType.RightBrace, "}")
					),
					new List<AuraType> { new AuraResult(new AuraString(), new AuraError()) },
					Visibility.Public,
					string.Empty
				)
			);
		_symbolsTable
			.Setup(st => st.GetSymbol("c", It.IsAny<string>()))
			.Returns(
				new AuraSymbol(
					"c",
					new AuraNamedFunction(
						"c",
						Visibility.Public,
						new AuraFunction(new List<Param>(), new AuraResult(new AuraString(), new AuraError()))
					)
				)
			);

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedCheck(
					new Tok(TokType.Check, "check"),
					new UntypedCall(
						new UntypedVariable(new Tok(TokType.Identifier, "c")),
						new List<(Tok?, IUntypedAuraExpression)>(),
						new Tok(TokType.RightParen, ")")
					)
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedCheck(
				new Tok(TokType.Check, "check"),
				new TypedCall(
					new TypedVariable(
						new Tok(TokType.Identifier, "c"),
						new AuraNamedFunction(
							"c",
							Visibility.Public,
							new AuraFunction(new List<Param>(), new AuraResult(new AuraString(), new AuraError()))
						)
					),
					new List<ITypedAuraExpression>(),
					new Tok(TokType.RightParen, ")"),
					new AuraNamedFunction(
						"c",
						Visibility.Public,
						new AuraFunction(new List<Param>(), new AuraResult(new AuraString(), new AuraError()))
					)
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Struct()
	{
		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedStruct(
					new Tok(TokType.Struct, "struct"),
					new Tok(TokType.Identifier, "s"),
					new List<Param>(),
					new Tok(TokType.RightParen, ")"),
					string.Empty
				)
			}
		);
		MakeAssertions(
			typedAst,
			new TypedStruct(
				new Tok(TokType.Struct, "struct"),
				new Tok(TokType.Identifier, "s"),
				new List<Param>(),
				new Tok(TokType.RightParen, ")"),
				string.Empty
			)
		);
	}

	private List<ITypedAuraStatement> ArrangeAndAct(List<IUntypedAuraStatement> untypedAst)
	{
		return new AuraTypeChecker(
			_symbolsTable.Object,
			_enclosingClassStore.Object,
			_enclosingFunctionDeclarationStore.Object,
			_enclosingExprStore.Object,
			_enclosingStmtStore.Object,
			new AuraLocalFileSystemImportedModuleProvider(),
			"Test",
			"Test"
		).CheckTypes(AddModStmtIfNecessary(untypedAst));
	}

	private void ArrangeAndAct_Invalid(List<IUntypedAuraStatement> untypedAst, Type expected)
	{
		try
		{
			new AuraTypeChecker(
				_symbolsTable.Object,
				_enclosingClassStore.Object,
				_enclosingFunctionDeclarationStore.Object,
				_enclosingExprStore.Object,
				_enclosingStmtStore.Object,
				new AuraLocalFileSystemImportedModuleProvider(),
				"Test",
				"Test"
			).CheckTypes(AddModStmtIfNecessary(untypedAst));
			Assert.Fail();
		}
		catch (TypeCheckerExceptionContainer e)
		{
			Assert.That(e.Exs.First(), Is.TypeOf(expected));
		}
	}

	private List<IUntypedAuraStatement> AddModStmtIfNecessary(List<IUntypedAuraStatement> untypedAst)
	{
		if (untypedAst.Count > 0 &&
			untypedAst[0] is not UntypedMod)
		{
			var untypedAstWithMod = new List<IUntypedAuraStatement>
			{
				new UntypedMod(new Tok(TokType.Mod, "mod"), new Tok(TokType.Identifier, "main"))
			};
			untypedAstWithMod.AddRange(untypedAst);
			return untypedAstWithMod;
		}

		return untypedAst;
	}

	private void MakeAssertions(List<ITypedAuraStatement> typedAst, ITypedAuraStatement expected)
	{
		Assert.Multiple(
			() =>
			{
				Assert.That(typedAst, Is.Not.Null);
				Assert.That(typedAst, Has.Count.EqualTo(2));

				var expectedJson = JsonConvert.SerializeObject(expected);
				var actualJson = JsonConvert.SerializeObject(typedAst[1]);
				Assert.That(actualJson, Is.EqualTo(expectedJson));
			}
		);
	}
}
