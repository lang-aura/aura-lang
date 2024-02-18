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
		_symbolsTable.Setup(v => v.GetSymbol("i", It.IsAny<string>()))
			.Returns(new AuraSymbol("i", new AuraInt()));

		var typedAst = ArrangeAndAct(
			new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					new UntypedAssignment(
						new Tok(TokType.Identifier, "i"),
						new IntLiteral(
							Int: new Tok(
								typ: TokType.IntLiteral,
								value: "6"
							))))
			});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedAssignment(
				new Tok(TokType.Identifier, "i"),
				new IntLiteral(
					Int: new Tok(
						typ: TokType.IntLiteral,
						value: "6"
					)
				),
				new AuraInt())));
	}

	[Test]
	public void TestTypeCheck_Binary()
	{
		var typedAst = ArrangeAndAct(new List<IUntypedAuraStatement>
		{
			new UntypedExpressionStmt(
				new UntypedBinary(
					new BoolLiteral(new Tok(TokType.True, "true")),
					new Tok(TokType.And, "and"),
					new BoolLiteral(new Tok(TokType.True, "false"))))
		});
		MakeAssertions(typedAst, new TypedExpressionStmt(
			new TypedBinary(
				new BoolLiteral(new Tok(TokType.True, "true")),
				new Tok(TokType.And, "and"),
				new BoolLiteral(new Tok(TokType.True, "false")),
				new AuraInt())));
	}

	[Test]
	public void TestTypeCheck_Block_EmptyBody()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedBlock(
						OpeningBrace: new Tok(
							typ: TokType.LeftBrace,
							value: "{"
						),
						Statements: new List<IUntypedAuraStatement>(),
						ClosingBrace: new Tok(
							typ: TokType.RightBrace,
							value: "}"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new TypedBlock(
					OpeningBrace: new Tok(
						typ: TokType.LeftBrace,
						value: "{"
					),
					Statements: new List<ITypedAuraStatement>(),
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					),
					Typ: new AuraNil())
			)
		);
	}

	[Test]
	public void TestTypeCheck_Block()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedBlock(
						OpeningBrace: new Tok(
							typ: TokType.LeftBrace,
							value: "{"
						),
						Statements: new List<IUntypedAuraStatement>
						{
							new UntypedLet(
								Let: new Tok(
									typ: TokType.Let,
									value: "let"
								),
								Names: new List<Tok>{ new(typ: TokType.Identifier, value: "i") },
								NameTyps: new List<AuraType?>{ new AuraInt() },
								Mutable: false,
								Initializer: new IntLiteral(
									Int: new Tok(
										typ: TokType.IntLiteral,
										value: "5"
									))
							)
						},
						ClosingBrace: new Tok(
							typ: TokType.RightBrace,
							value: "}"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new TypedBlock(
					OpeningBrace: new Tok(
						typ: TokType.LeftBrace,
						value: "{"
					),
					Statements: new List<ITypedAuraStatement>
					{
						new TypedLet(
							Let: new Tok(
								typ: TokType.Let,
								value: "let"
							),
							Names: new List<Tok>{ new(typ: TokType.Identifier, value: "i") },
							TypeAnnotation: true,
							Mutable: false,
							Initializer: new IntLiteral(
								Int: new Tok(
									typ: TokType.IntLiteral,
									value: "5"
								))
						)
					},
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					),
					Typ: new AuraNil()
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Call_NoArgs()
	{
		_symbolsTable.Setup(v => v.GetSymbol("f", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "f",
				Kind: new AuraNamedFunction(
					name: "f",
					pub: Visibility.Private,
					f: new AuraFunction(
						fParams: new List<Param>(),
						returnType: new AuraNil()
					)
				)
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedCall(
						Callee: new UntypedVariable(
							Name: new Tok(
								typ: TokType.Identifier,
								value: "f"
							)
						),
						Arguments: new List<(Tok?, IUntypedAuraExpression)>(),
						ClosingParen: new Tok(
							typ: TokType.RightParen,
							value: ")",
							range: new Range(
								start: new Position(
									character: 2,
									line: 1
								),
								end: new Position(
									character: 3,
									line: 1
								)
							)
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new TypedCall(
					Callee: new TypedVariable(
						new Tok(TokType.Identifier, "f"),
						new AuraNamedFunction("f", Visibility.Private, new AuraFunction(new List<Param>(), new AuraNil()))),
					Arguments: new List<ITypedAuraExpression>(),
					ClosingParen: new Tok(
						typ: TokType.RightParen,
						value: ")",
						range: new Range(
							start: new Position(
								character: 2,
								line: 1
							),
							end: new Position(
								character: 3,
								line: 1
							)
						)
					),
					Typ: new AuraNil())
			)
		);
	}

	[Test]
	public void TestTypeCheck_TwoArgs_WithTags()
	{
		_symbolsTable.Setup(v => v.GetSymbol("f", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "f",
				Kind: new AuraNamedFunction(
					name: "f",
					pub: Visibility.Private,
					f: new AuraFunction(
						fParams: new List<Param>
						{
							new(
								Name: new Tok(
									typ: TokType.Identifier,
									value: "i"
								),
								ParamType: new(
									Typ: new AuraInt(),
									Variadic: false,
									DefaultValue: null
								)
							),
							new(
								Name: new Tok(
									typ: TokType.Identifier,
									value: "s"
								),
								ParamType: new(
									Typ: new AuraString(),
									Variadic: false,
									DefaultValue: null
								)
							)
						},
						returnType: new AuraNil()
					)
				)
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedCall(
						Callee: new UntypedVariable(
							Name: new Tok(
								typ: TokType.Identifier,
								value: "f"
							)
						),
						Arguments: new List<(Tok?, IUntypedAuraExpression)>
						{
							(
								new Tok(
									typ: TokType.Identifier,
									value: "s"
								),
								new StringLiteral(
									String: new Tok(
										typ: TokType.StringLiteral,
										value: "Hello world"
									)
								)
							),
							(
								new Tok(
									typ: TokType.Identifier,
									value: "i"
								),
								new IntLiteral(
									Int: new Tok(
										typ: TokType.IntLiteral,
										value: "5"
									)
								)
							)
						},
						ClosingParen: new Tok(
							typ: TokType.RightParen,
							value: ")"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new TypedCall(
					Callee: new TypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "f"
						),
						Typ: new AuraNamedFunction(
							name: "f",
							pub: Visibility.Private,
							f: new AuraFunction(
								fParams: new List<Param>(),
								returnType: new AuraNil()
							)
						)
					),
					Arguments: new List<ITypedAuraExpression>
					{
						new IntLiteral(
							Int: new Tok(
								typ: TokType.IntLiteral,
								value: "5"
							)
						),
						new StringLiteral(
							String: new Tok(
								typ: TokType.StringLiteral,
								value: "Hello world"
							)
						)
					},
					ClosingParen: new Tok(
						typ: TokType.RightParen,
						value: ")"
					),
					Typ: new AuraNil())
			)
		);
	}

	[Test]
	public void TestTypeCheck_Call_DefaultValues()
	{
		_symbolsTable.Setup(v => v.GetSymbol("f", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "f",
				Kind: new AuraNamedFunction(
					name: "f",
					pub: Visibility.Private,
					f: new AuraFunction(
						fParams: new List<Param>
						{
							new(
								Name: new Tok(
									typ: TokType.Identifier,
									value: "i"
								),
								ParamType: new(
									Typ: new AuraInt(),
									Variadic: false,
									DefaultValue: new IntLiteral(
										Int: new Tok(
											typ: TokType.IntLiteral,
											value: "10"
										)
									)
								)
							),
							new(
								Name: new Tok(
									typ: TokType.Identifier,
									value: "s"
								),
								ParamType: new(
									Typ: new AuraString(),
									Variadic: false,
									DefaultValue: null
								)
							)
						},
						returnType: new AuraNil()
					)
				)
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedCall(
						Callee: new UntypedVariable(
							Name: new Tok(
								typ: TokType.Identifier,
								value: "f"
							)
						),
						Arguments: new List<(Tok?, IUntypedAuraExpression)>
						{
							(
								new Tok(
									typ: TokType.Identifier,
									value: "s"
								),
								new StringLiteral(
									String: new Tok(
										typ: TokType.StringLiteral,
										value: "Hello world"
									)
								)
							)
						},
						ClosingParen: new Tok(
							typ: TokType.RightParen,
							value: ")"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new TypedCall(
					Callee: new TypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "f"
						),
						Typ: new AuraNamedFunction(
							name: "f",
							pub: Visibility.Private,
							f: new AuraFunction(
								fParams: new List<Param>(),
								returnType: new AuraNil()
							)
						)
					),
					Arguments: new List<ITypedAuraExpression>
					{
						new IntLiteral(
							Int: new Tok(
								typ: TokType.IntLiteral,
								value: "10"
							)
						),
						new StringLiteral(
							String: new Tok(
								typ: TokType.StringLiteral,
								value: "Hello world"
							)
						)
					},
					ClosingParen: new Tok(
						typ: TokType.RightParen,
						value: ")"
					),
					Typ: new AuraNil())
			)
		);
	}

	[Test]
	public void TestTypeCheck_Call_NoValueForParameterWithoutDefaultValue()
	{
		_symbolsTable.Setup(v => v.GetSymbol("f", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "f",
				Kind: new AuraNamedFunction(
					name: "f",
					pub: Visibility.Private,
					f: new AuraFunction(
						fParams: new List<Param>
						{
							new(
								Name: new Tok(
									typ: TokType.Identifier,
									value: "i"
								),
								ParamType: new(
									Typ: new AuraInt(),
									Variadic: false,
									DefaultValue: null
								)
							),
							new(
								Name: new Tok(
									typ: TokType.Identifier,
									value: "s"
								),
								ParamType: new(
									Typ: new AuraString(),
									Variadic: false,
									DefaultValue: new StringLiteral(
										String: new Tok(
											typ: TokType.StringLiteral,
											value: "Hello world"
										)
									)
								)
							)
						},
						returnType: new AuraNil()
					)
				)
			));

		ArrangeAndAct_Invalid(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedCall(
						Callee: new UntypedVariable(
							Name: new Tok(
								typ: TokType.Identifier,
								value: "f"
							)
						),
						Arguments: new List<(Tok?, IUntypedAuraExpression)>
						{
							(
								new Tok(
									typ: TokType.Identifier,
									value: "s"
								),
								new StringLiteral(
									String: new Tok(
										typ: TokType.StringLiteral,
										value: "Hello world"
									)
								)
							)
						},
						ClosingParen: new Tok(
							typ: TokType.RightParen,
							value: ")"
						))
				)
			},
			expected: typeof(MustSpecifyValueForArgumentWithoutDefaultValueException)
		);
	}

	[Test]
	public void TestTypeCheck_Call_MixNamedAndUnnamedArguments()
	{
		_symbolsTable.Setup(v => v.GetSymbol("f", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "f",
				Kind: new AuraNamedFunction(
					name: "f",
					pub: Visibility.Private,
					f: new AuraFunction(
						fParams: new List<Param>
						{
							new(
								Name: new Tok(
									typ: TokType.Identifier,
									value: "i"
								),
								ParamType: new(
									Typ: new AuraInt(),
									Variadic: false,
									DefaultValue: null
								)
							),
							new(
								Name: new Tok(
									typ: TokType.Identifier,
									value: "s"
								),
								ParamType: new(
									Typ: new AuraString(),
									Variadic: false,
									DefaultValue: null
								)
							)
						},
						returnType: new AuraNil()
					)
				)
			)
		);

		ArrangeAndAct_Invalid(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedCall(
						Callee: new UntypedVariable(
							Name: new Tok(
								typ: TokType.Identifier,
								value: "f"
							)
						),
						Arguments: new List<(Tok?, IUntypedAuraExpression)>
						{
							(
								new Tok(
									typ: TokType.Identifier,
									value: "s"
								),
								new StringLiteral(
									String: new Tok(
										typ: TokType.StringLiteral,
										value: "Hello world"
									)
								)
							),
							(
								null,
								new IntLiteral(
									Int: new Tok(
										typ: TokType.IntLiteral,
										value: "5"
									)
								)
							)
						},
						ClosingParen: new Tok(
							typ: TokType.RightParen,
							value: ")"
						))
				)
			},
			expected: typeof(CannotMixNamedAndUnnamedArgumentsException)
		);
	}

	[Test]
	public void TestTypeCheck_Get()
	{
		_symbolsTable.Setup(v => v.GetSymbol("greeter", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "greeter",
				Kind: new AuraClass(
					name: "Greeter",
					parameters: new List<Param>
					{
						new(
							Name: new Tok(
								typ: TokType.Identifier,
								value: "name"
							),
							ParamType: new(
								Typ: new AuraString(),
								Variadic: false,
								DefaultValue: null
							)
						)
					},
					methods: new List<AuraNamedFunction>(),
					implementing: new List<AuraInterface>(),
					pub: Visibility.Private
				)
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedGet(
						Obj: new UntypedVariable(
							Name: new Tok(
								typ: TokType.Identifier,
								value: "greeter"
							)
						),
						Name: new Tok(
							typ: TokType.Identifier,
							value: "name"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new TypedGet(
					Obj: new TypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "greeter"
						),
						Typ: new AuraClass(
							name: "Greeter",
							parameters: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "name"
									),
									ParamType: new(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							methods: new List<AuraNamedFunction>(),
							implementing: new List<AuraInterface>(),
							pub: Visibility.Private
						)
					),
					Name: new Tok(
						typ: TokType.Identifier,
						value: "name"
					),
					Typ: new AuraString())
			)
		);
	}

	[Test]
	public void TestTypeCheck_GetIndex()
	{
		_symbolsTable.Setup(v => v.GetSymbol("names", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "names",
				Kind: new AuraList(kind: new AuraString())
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedGetIndex(
						Obj: new UntypedVariable(
							Name: new Tok(
								typ: TokType.Identifier,
								value: "names"
							)
						),
						Index: new IntLiteral(
							Int: new Tok(
								typ: TokType.IntLiteral,
								value: "0"
							)
						),
						ClosingBracket: new Tok(
							typ: TokType.RightBracket,
							value: "]"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new TypedGetIndex(
					Obj: new TypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "names"
						),
						Typ: new AuraList(kind: new AuraString())
					),
					Index: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "0"
						)
					),
					ClosingBracket: new Tok(
						typ: TokType.RightBracket,
						value: "]"
					),
					Typ: new AuraString())
			)
		);
	}

	[Test]
	public void TestTypeCheck_GetIndexRange()
	{
		_symbolsTable.Setup(v => v.GetSymbol("names", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "names",
				Kind: new AuraList(kind: new AuraString())
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedGetIndexRange(
						Obj: new UntypedVariable(
							Name: new Tok(
								typ: TokType.Identifier,
								value: "names"
							)
						),
						Lower: new IntLiteral(
							Int: new Tok(
								typ: TokType.IntLiteral,
								value: "0"
							)
						),
						Upper: new IntLiteral(
							Int: new Tok(
								typ: TokType.IntLiteral,
								value: "2"
							)
						),
						ClosingBracket: new Tok(
							typ: TokType.RightBracket,
							value: "]"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new TypedGetIndexRange(
					Obj: new TypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "names"
						),
						Typ: new AuraList(kind: new AuraString())
					),
					Lower: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "0"
						)
					),
					Upper: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "2"
						)
					),
					ClosingBracket: new Tok(
						typ: TokType.RightBracket,
						value: "]"
					),
					Typ: new AuraList(new AuraString()))
			)
		);
	}

	[Test]
	public void TestTypeCheck_Grouping()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedGrouping(
						OpeningParen: new Tok(
							typ: TokType.LeftParen,
							value: "("
						),
						Expr: new StringLiteral(
							String: new Tok(
								typ: TokType.StringLiteral,
								value: "Hello world"
							)
						),
						ClosingParen: new Tok(
							typ: TokType.RightParen,
							value: ")"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				new TypedGrouping(
					OpeningParen: new Tok(
						typ: TokType.LeftParen,
						value: "("
					),
					Expr: new StringLiteral(
						String: new Tok(
							typ: TokType.StringLiteral,
							value: "Hello world"
						)
					),
					ClosingParen: new Tok(
						typ: TokType.RightParen,
						value: ")"
					),
					Typ: new AuraString())
			)
		);
	}

	[Test]
	public void TestTypeCheck_If_EmptyThenBranch()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedIf(
						If: new Tok(
							typ: TokType.If,
							value: "if"
						),
						Condition: new BoolLiteral(
							Bool: new Tok(
								typ: TokType.True,
								value: "true"
							)
						),
						Then: new UntypedBlock(
							OpeningBrace: new Tok(
								typ: TokType.LeftBrace,
								value: "{"
							),
							Statements: new List<IUntypedAuraStatement>(),
							ClosingBrace: new Tok(
								typ: TokType.RightBrace,
								value: "}"
							)
						),
						Else: null)
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new TypedIf(
					If: new Tok(
						typ: TokType.If,
						value: "if"
					),
					Condition: new BoolLiteral(
						Bool: new Tok(
							typ: TokType.True,
							value: "true"
						)
					),
					Then: new TypedBlock(
						OpeningBrace: new Tok(
							typ: TokType.LeftBrace,
							value: "{"
						),
						Statements: new List<ITypedAuraStatement>(),
						ClosingBrace: new Tok(
							typ: TokType.RightBrace,
							value: "}"
						),
						Typ: new AuraNil()
					),
					Else: null,
					Typ: new AuraNil())
			)
		);
	}

	[Test]
	public void TestTypeCheck_IntLiteral()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "5"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				new IntLiteral(
					Int: new Tok(
						typ: TokType.IntLiteral,
						value: "5"
					))
			)
		);
	}

	[Test]
	public void TestTypeCheck_FloatLiteral()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new FloatLiteral(
						Float: new Tok(
							typ: TokType.FloatLiteral,
							value: "5.1"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new FloatLiteral(
					Float: new Tok(
						typ: TokType.FloatLiteral,
						value: "5.1"
					))
			)
		);
	}

	[Test]
	public void TestTypeCheck_StringLiteral()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new StringLiteral(
						String: new Tok(
							typ: TokType.StringLiteral,
							value: "Hello world"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				new StringLiteral(
					String: new Tok(
						typ: TokType.StringLiteral,
						value: "Hello world"
					)
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_ListLiteral()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new ListLiteral<IUntypedAuraExpression>(
						OpeningBracket: new Tok(
							typ: TokType.LeftBracket,
							value: "["
						),
						L: new List<IUntypedAuraExpression>
						{
							new IntLiteral(
								Int: new Tok(
									typ: TokType.IntLiteral,
									value: "1"
								)
							)
						},
						Kind: new AuraInt(),
						ClosingBrace: new Tok(
							typ: TokType.RightBrace,
							value: "]"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				new ListLiteral<ITypedAuraExpression>(
					OpeningBracket: new Tok(
						typ: TokType.LeftBracket,
						value: "["
					),
					L: new List<ITypedAuraExpression>
					{
						new IntLiteral(
							Int: new Tok(
								typ: TokType.IntLiteral,
								value: "1"
							)
						)
					},
					Kind: new AuraInt(),
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "]"
					))
			)
		);
	}

	[Test]
	public void TestTypeCheck_MapLiteral()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new MapLiteral<IUntypedAuraExpression, IUntypedAuraExpression>(
						Map: new Tok(
							typ: TokType.Map,
							value: "map"
						),
						M: new Dictionary<IUntypedAuraExpression, IUntypedAuraExpression>
						{
							{
								new StringLiteral(
									String: new Tok(
										typ: TokType.StringLiteral,
										value: "Hello"
									)
								),
								new IntLiteral(
									Int: new Tok(
										typ: TokType.IntLiteral,
										value: "1"
									)
								)
							}
						},
						KeyType: new AuraString(),
						ValueType: new AuraInt(),
						ClosingBrace: new Tok(
							typ: TokType.RightBrace,
							value: "}"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new MapLiteral<ITypedAuraExpression, ITypedAuraExpression>(
					Map: new Tok(
						typ: TokType.Map,
						value: "map"
					),
					M: new Dictionary<ITypedAuraExpression, ITypedAuraExpression>
					{
						{
							new StringLiteral(
								String: new Tok(
									typ: TokType.StringLiteral,
									value: "Hello"
								)
							),
							new IntLiteral(
								Int: new Tok(
									typ: TokType.IntLiteral,
									value: "1"
								)
							)
						}
					},
					KeyType: new AuraString(),
					ValueType: new AuraInt(),
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					))
			)
		);
	}

	[Test]
	public void TestTypeCheck_BoolLiteral()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new BoolLiteral(
						Bool: new Tok(
							typ: TokType.True,
							value: "true"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				new BoolLiteral(
					Bool: new Tok(
						typ: TokType.True,
						value: "true"
					))
			)
		);
	}

	[Test]
	public void TestTypeCheck_NilLiteral()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedNil(
						Nil: new Tok(
							typ: TokType.Nil,
							value: "nil"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				new TypedNil(
					Nil: new Tok(
						typ: TokType.Nil,
						value: "nil"
					))
			)
		);
	}

	[Test]
	public void TestTypeCheck_CharLiteral()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new CharLiteral(
						Char: new Tok(
							typ: TokType.CharLiteral,
							value: "a"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				new CharLiteral(
					Char: new Tok(
						typ: TokType.CharLiteral,
						value: "a"
					))
			)
		);
	}

	[Test]
	public void TestTypeCheck_Logical()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedLogical(
						Left: new IntLiteral(
							Int: new Tok(
								typ: TokType.IntLiteral,
								value: "5"
							)
						),
						Operator: new Tok(
							typ: TokType.Less,
							value: "<"
						),
						Right: new IntLiteral(
							Int: new Tok(
								typ: TokType.IntLiteral,
								value: "10"
							)))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new TypedLogical(
					Left: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "5"
						)
					),
					Operator: new Tok(
						typ: TokType.Less,
						value: "<"
					),
					Right: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "10"
						)
					),
					Typ: new AuraBool())
			)
		);
	}

	[Test]
	public void TestTypeCheck_Set()
	{
		_symbolsTable.Setup(v => v.GetSymbol("greeter", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "greeter",
				Kind: new AuraClass(
					name: "Greeter",
					parameters: new List<Param>
					{
						new(
							Name: new Tok(
								typ: TokType.Identifier,
								value: "name"
							),
							ParamType: new(
								Typ: new AuraString(),
								Variadic: false,
								DefaultValue: null
							)
						)
					},
					methods: new List<AuraNamedFunction>(),
					implementing: new List<AuraInterface>(),
					pub: Visibility.Private
				)
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedSet(
						Obj: new UntypedVariable(
							Name: new Tok(
								typ: TokType.Identifier,
								value: "greeter"
							)
						),
						Name: new Tok(
							typ: TokType.Identifier,
							value: "name"
						),
						Value: new StringLiteral(
							String: new Tok(
								typ: TokType.StringLiteral,
								value: "Bob"
							)))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new TypedSet(
					Obj: new TypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "greeter"
						),
						Typ: new AuraClass(
							name: "Greeter",
							parameters: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "name"
									),
									ParamType: new(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							methods: new List<AuraNamedFunction>(),
							implementing: new List<AuraInterface>(),
							pub: Visibility.Private
						)),
					Name: new Tok(
						typ: TokType.Identifier,
						value: "name"
					),
					Value: new StringLiteral(
						String: new Tok(
							typ: TokType.StringLiteral,
							value: "Bob"
						)
					),
					Typ: new AuraString())
			)
		);
	}

	[Test]
	public void TestTypeCheck_This()
	{
		_enclosingClassStore.Setup(ecs => ecs.Peek())
			.Returns(new PartiallyTypedClass(
				Class: new Tok(
					typ: TokType.Class,
					value: "class"
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "Greeter"
				),
				Params: new List<Param>(),
				Methods: new List<AuraNamedFunction>(),
				Public: Visibility.Public,
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				),
				Typ: new AuraClass(
					name: "Greeter",
					parameters: new List<Param>(),
					methods: new List<AuraNamedFunction>(),
					implementing: new List<AuraInterface>(),
					pub: Visibility.Private
				)
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedThis(
						This: new Tok(
							typ: TokType.This,
							value: "this"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new TypedThis(
					This: new Tok(
						typ: TokType.This,
						value: "this"
					),
					Typ: new AuraClass(
						name: "Greeter",
						parameters: new List<Param>(),
						methods: new List<AuraNamedFunction>(),
						implementing: new List<AuraInterface>(),
						pub: Visibility.Private
					))
			)
		);
	}

	[Test]
	public void TestTypeCheck_Unary_Bang()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedUnary(
						Operator: new Tok(
							typ: TokType.Bang,
							value: "!"
						),
						Right: new BoolLiteral(
							Bool: new Tok(
								typ: TokType.True,
								value: "true"
							)))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new TypedUnary(
					Operator: new Tok(
						typ: TokType.Bang,
						value: "!"
					),
					Right: new BoolLiteral(
						Bool: new Tok(
							typ: TokType.True,
							value: "true"
						)
					),
					Typ: new AuraBool())
			)
		);
	}

	[Test]
	public void TestTypeCheck_Unary_Minus()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedUnary(
						Operator: new Tok(
							typ: TokType.Minus,
							value: "-"
						),
						Right: new IntLiteral(
							Int: new Tok(
								typ: TokType.IntLiteral,
								value: "5"
							)))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new TypedUnary(
					Operator: new Tok(
						typ: TokType.Minus,
						value: "-"
					),
					Right: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "5"
						)
					),
					Typ: new AuraInt())
			)
		);
	}

	[Test]
	public void TestTypeCheck_Variable()
	{
		_symbolsTable.Setup(v => v.GetSymbol("name", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "name",
				Kind: new AuraString()
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "name"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new TypedVariable(
					Name: new Tok(
						typ: TokType.Identifier,
						value: "name"
					),
					Typ: new AuraString()
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Defer()
	{
		_symbolsTable.Setup(v => v.GetSymbol("f", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "f",
				Kind: new AuraNamedFunction(
					name: "f",
					pub: Visibility.Private,
					f: new AuraFunction(
						fParams: new List<Param>(),
						returnType: new AuraNil()
					)
				)
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedDefer(
					Defer: new Tok(
						typ: TokType.Defer,
						value: "defer"
					),
					Call: new UntypedCall(
						Callee: new UntypedVariable(
							new Tok(TokType.Identifier, "f")),
						Arguments: new List<(Tok?, IUntypedAuraExpression)>(),
						ClosingParen: new Tok(
							typ: TokType.RightParen,
							value: ")"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedDefer(
				Defer: new Tok(
					typ: TokType.Defer,
					value: "defer"
				),
				Call: new TypedCall(
					Callee: new TypedVariable(
						new Tok(TokType.Identifier, "f"),
						new AuraNamedFunction(
							"f",
							Visibility.Private,
							new AuraFunction(
								new List<Param>(),
								new AuraNil()
							)
						)
					),
					Arguments: new List<ITypedAuraExpression>(),
					ClosingParen: new Tok(
						typ: TokType.RightParen,
						value: ")"
					),
					Typ: new AuraNil())
			)
		);
	}

	[Test]
	public void TestTypeCheck_For_EmptyBody()
	{
		_symbolsTable.Setup(v => v.GetSymbol("i", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "i",
				Kind: new AuraInt()
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedFor(
					For: new Tok(
						typ: TokType.For,
						value: "for"
					),
					Initializer: new UntypedLet(
						Let: null,
						Names: new List<Tok>
						{
							new(
								typ: TokType.Identifier,
								value: "i"
							)
						},
						NameTyps: new List<AuraType?>(),
						Mutable: false,
						Initializer: new IntLiteral(
							Int: new Tok(
								typ: TokType.IntLiteral,
								value: "0"
							))
					),
					Condition: new UntypedLogical(
						Left: new UntypedVariable(
							Name: new Tok(
								typ: TokType.Identifier,
								value: "i"
							)
						),
						Operator: new Tok(
							typ: TokType.Less,
							value: "<"
						),
						Right: new IntLiteral(
							Int: new Tok(
								typ: TokType.IntLiteral,
								value: "10"
							))
					),
					Increment: null,
					Body: new List<IUntypedAuraStatement>(),
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					)
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedFor(
				For: new Tok(
					typ: TokType.For,
					value: "for"
				),
				Initializer: new TypedLet(
					Let: null,
					Names: new List<Tok>
					{
						new(
							typ: TokType.Identifier,
							value: "i"
						)
					},
					TypeAnnotation: false,
					Mutable: false,
					Initializer: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "0"
						))
				),
				Condition: new TypedLogical(
					Left: new TypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "i"
						),
						Typ: new AuraInt()
					),
					Operator: new Tok(
						typ: TokType.Less,
						value: "<"
					),
					Right: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "10"
						)
					),
					Typ: new AuraBool()
				),
				Increment: null,
				Body: new List<ITypedAuraStatement>(),
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_ForEach_EmptyBody()
	{
		_symbolsTable.Setup(v => v.GetSymbol("names", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "names",
				Kind: new AuraList(kind: new AuraString())
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedForEach(
					ForEach: new Tok(
						typ: TokType.ForEach,
						value: "foreach"
					),
					EachName: new Tok(
						typ: TokType.Identifier,
						value: "name"
					),
					Iterable: new UntypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "names"
						)
					),
					Body: new List<IUntypedAuraStatement>(),
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					)
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedForEach(
				ForEach: new Tok(
					typ: TokType.ForEach,
					value: "foreach"
				),
				EachName: new Tok(
					typ: TokType.Identifier,
					value: "name"
				),
				Iterable: new TypedVariable(
					Name: new Tok(
						typ: TokType.Identifier,
						value: "names"
					),
					Typ: new AuraList(kind: new AuraString())
				),
				Body: new List<ITypedAuraStatement>(),
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_NamedFunction_NoParams_ReturnError()
	{
		_symbolsTable.Setup(v => v.GetSymbol("error", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "error",
				Kind: new AuraNamedFunction(
					name: "error",
					pub: Visibility.Public,
					f: new AuraFunction(
						fParams: new List<Param>
						{
							new(
								Name: new Tok(
									typ: TokType.Identifier,
									value: "message"
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

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedNamedFunction(
					Fn: new Tok(
						typ: TokType.Fn,
						value: "fn"
					),
					Name: new Tok(
						typ: TokType.Identifier,
						value: "f"
					),
					Params: new List<Param>(),
					Body: new UntypedBlock(
						OpeningBrace: new Tok(
							typ: TokType.LeftBrace,
							value: "{"
						),
						Statements: new List<IUntypedAuraStatement>
						{
							new UntypedReturn(
								Return: new Tok(
									typ: TokType.Return,
									value: "return"
								),
								Value: new List<IUntypedAuraExpression>
								{
									new UntypedCall(
										Callee: new UntypedVariable(
											Name: new Tok(
												typ: TokType.Identifier,
												value: "error"
											)
										),
										Arguments: new List<(Tok?, IUntypedAuraExpression)>
										{
											(
												null,
												new StringLiteral(
													String: new Tok(
														typ: TokType.StringLiteral,
														value: "Helpful error message"
													)
												)
											)
										},
										ClosingParen: new Tok(
											typ: TokType.RightParen,
											value: ")"
										)
									)
								}
							)
						},
						ClosingBrace: new Tok(
							typ: TokType.RightBrace,
							value: "}"
						)
					),
					ReturnType: new List<AuraType>{ new AuraError() },
					Public: Visibility.Public
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedNamedFunction(
				Fn: new Tok(
					typ: TokType.Fn,
					value: "fn"
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "f"
				),
				Params: new List<Param>(),
				Body: new TypedBlock(
					OpeningBrace: new Tok(
						typ: TokType.LeftBrace,
						value: "{"
					),
					Statements: new List<ITypedAuraStatement>
					{
						new TypedReturn(
							Return: new Tok(
								typ: TokType.Return,
								value: "return"
							),
							Value: new TypedCall(
								Callee: new TypedVariable(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "error"
									),
									Typ: new AuraNamedFunction(
										name: "error",
										pub: Visibility.Public,
										f: new AuraFunction(
											fParams: new List<Param>
											{
												new(
													Name: new Tok(
														typ: TokType.Identifier,
														value: "message"
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
								),
								Arguments: new List<ITypedAuraExpression>
								{
									new StringLiteral(
										String: new Tok(
											typ: TokType.StringLiteral,
											value: "Helpful error message"
										)
									)
								},
								ClosingParen: new Tok(
									typ: TokType.RightParen,
									value: ")"
								),
								Typ: new AuraError())
						)
					},
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					),
					Typ: new AuraError()
				),
				ReturnType: new AuraError(),
				Public: Visibility.Public
			)
		);
	}

	[Test]
	public void TestTypeCheck_NamedFunction_NoParams_NoReturnType_NoBody()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedNamedFunction(
					Fn: new Tok(
						typ: TokType.Fn,
						value: "fn"
					),
					Name: new Tok(
						typ: TokType.Identifier,
						value: "f"
					),
					Params: new List<Param>(),
					Body: new UntypedBlock(
						OpeningBrace: new Tok(
							typ: TokType.LeftBrace,
							value: "{"
						),
						Statements: new List<IUntypedAuraStatement>(),
						ClosingBrace: new Tok(
							typ: TokType.RightBrace,
							value: "}"
						)
					),
					ReturnType: null,
					Public: Visibility.Public
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedNamedFunction(
				Fn: new Tok(
					typ: TokType.Fn,
					value: "fn"),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "f"
				),
				Params: new List<Param>(),
				Body: new TypedBlock(
					OpeningBrace: new Tok(
						typ: TokType.LeftBrace,
						value: "{"
					),
					Statements: new List<ITypedAuraStatement>(),
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					),
					Typ: new AuraNil()
				),
				ReturnType: new AuraNil(),
				Public: Visibility.Public
			)
		);
	}

	[Test]
	public void TestTypeCheck_AnonymousFunction_NoParams_NoReturnType_NoBody()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedAnonymousFunction(
						Fn: new Tok(
							typ: TokType.Fn,
							value: "fn"
						),
						Params: new List<Param>(),
						Body: new UntypedBlock(
							OpeningBrace: new Tok(
								typ: TokType.LeftBrace,
								value: "{"
							),
							Statements: new List<IUntypedAuraStatement>(),
							ClosingBrace: new Tok(
								typ: TokType.RightBrace,
								value: "}"
							)
						),
						ReturnType: null)
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new TypedAnonymousFunction(
					Fn: new Tok(
						typ: TokType.Fn,
						value: "fn"
					),
					Params: new List<Param>(),
					Body: new TypedBlock(
						OpeningBrace: new Tok(
							typ: TokType.LeftBrace,
							value: "{"
						),
						Statements: new List<ITypedAuraStatement>(),
						ClosingBrace: new Tok(
							typ: TokType.RightBrace,
							value: "}"
						),
						Typ: new AuraNil()
					),
					ReturnType: new AuraNil())
			)
		);
	}

	[Test]
	public void TestTypeCheck_Let_Long()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedLet(
					Let: new Tok(
						typ: TokType.Let,
						value: "let"
					),
					Names: new List<Tok>
					{
						new(
							typ: TokType.Identifier,
							value: "i"
						)
					},
					NameTyps: new List<AuraType?>{ new AuraInt() },
					Mutable: false,
					Initializer: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "1"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedLet(
				Let: new Tok(
					typ: TokType.Let,
					value: "let"
				),
				Names: new List<Tok>
				{
					new(
						typ: TokType.Identifier,
						value: "i"
					)
				},
				TypeAnnotation: true,
				Mutable: false,
				Initializer: new IntLiteral(
					Int: new Tok(
						typ: TokType.IntLiteral,
						value: "1"
					))
			)
		);
	}

	[Test]
	public void TestTypeCheck_Let_Uninitialized()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedLet(
					Let: new Tok(
						typ: TokType.Let,
						value: "let"
					),
					Names: new List<Tok>
					{
						new(
							typ: TokType.Identifier,
							value: "i"
						)
					},
					NameTyps: new List<AuraType?>{ new AuraInt() },
					Mutable: false,
					Initializer: null
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedLet(
				Let: new Tok(
					typ: TokType.Let,
					value: "let"
				),
				Names: new List<Tok>
				{
					new(
						typ: TokType.Identifier,
						value: "i"
					)
				},
				TypeAnnotation: true,
				Mutable: false,
				Initializer: new IntLiteral(
					Int: new Tok(
						typ: TokType.IntLiteral,
						value: "0"
					))
			)
		);
	}

	[Test]
	public void TestTypeCheck_Let_Uninitialized_NonDefaultable()
	{
		ArrangeAndAct_Invalid(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedLet(
					Let: new Tok(
						typ: TokType.Let,
						value: "let"
					),
					Names: new List<Tok>
					{
						new(
							typ: TokType.Identifier,
							value: "c"
						)
					},
					NameTyps: new List<AuraType?>{ new AuraChar() },
					Mutable: false,
					Initializer: null
				)
			},
			expected: typeof(MustSpecifyInitialValueForNonDefaultableTypeException));
	}

	[Test]
	public void TestTypeCheck_Long_Short()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedLet(
					Let: null,
					Names: new List<Tok>
					{
						new(
							typ: TokType.Identifier,
							value: "i"
						)
					},
					NameTyps: new List<AuraType?>(),
					Mutable: false,
					Initializer: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "1"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedLet(
				Let: null,
				Names: new List<Tok>
				{
					new(
						typ: TokType.Identifier,
						value: "i"
					)
				},
				TypeAnnotation: false,
				Mutable: false,
				Initializer: new IntLiteral(
					Int: new Tok(
						typ: TokType.IntLiteral,
						value: "1"
					))
			)
		);
	}

	[Test]
	public void TestTypeCheck_Return_NoValue()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedReturn(
					Return: new Tok(
						typ: TokType.Return,
						value: "return"
					),
					Value: null
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedReturn(
				Return: new Tok(
					typ: TokType.Return,
					value: "return"
				),
				Value: null
			)
		);
	}

	[Test]
	public void TestTypeCheck_Return()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedReturn(
					Return: new Tok(
						typ: TokType.Return,
						value: "return"
					),
					Value: new List<IUntypedAuraExpression>
					{
						new IntLiteral(
							Int: new Tok(
								typ: TokType.IntLiteral,
								value: "5"
							)
						)
					}
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedReturn(
				Return: new Tok(
					typ: TokType.Return,
					value: "return"
				),
				Value: new IntLiteral(
					Int: new Tok(
						typ: TokType.IntLiteral,
						value: "5"
					))
			)
		);
	}

	[Test]
	public void TestTypeCheck_Class_NoParams_NoMethods()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedClass(
					Class: new Tok(
						typ: TokType.Class,
						value: "class"
					),
					Name: new Tok(TokType.Identifier, "Greeter"),
					Params: new List<Param>(),
					Body: new List<IUntypedAuraStatement>(),
					Public: Visibility.Private,
					Implementing: new List<Tok>(),
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					)
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new FullyTypedClass(
				Class: new Tok(
					typ: TokType.Class,
					value: "class"
				),
				Name: new Tok(TokType.Identifier, "Greeter"),
				Params: new List<Param>(),
				Methods: new List<TypedNamedFunction>(),
				Public: Visibility.Private,
				Implementing: new List<AuraInterface>(),
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_While_EmptyBody()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedWhile(
					While: new Tok(
						typ: TokType.While,
						value: "while"
					),
					Condition: new BoolLiteral(
						Bool: new Tok(
							typ: TokType.True,
							value: "true"
						)
					),
					Body: new List<IUntypedAuraStatement>(),
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					)
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedWhile(
				While: new Tok(
					typ: TokType.While,
					value: "while"
				),
				Condition: new BoolLiteral(
					Bool: new Tok(
						typ: TokType.True,
						value: "true"
					)
				),
				Body: new List<ITypedAuraStatement>(),
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Comment()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedComment(
					Text: new Tok(
						typ: TokType.Comment,
						value: "// this is a comment"
					)
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedComment(
				Text: new Tok(
					typ: TokType.Comment,
					value: "// this is a comment"
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Yield()
	{
		_enclosingExprStore.Setup(expr => expr.Peek()).Returns(
			new UntypedBlock(
				OpeningBrace: new Tok(
					typ: TokType.LeftBrace,
					value: "{"
				),
				Statements: new List<IUntypedAuraStatement>(),
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				)
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedYield(
					Yield: new Tok(
						typ: TokType.Yield,
						value: "yield"
					),
					Value: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "5"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedYield(
				Yield: new Tok(
					typ: TokType.Yield,
					value: "yield"
				),
				Value: new IntLiteral(
					Int: new Tok(
						typ: TokType.IntLiteral,
						value: "5"
					))
			)
		);
	}

	[Test]
	public void TestTypeCheck_Yield_Invalid()
	{
		_enclosingExprStore.Setup(expr => expr.Peek()).Returns(
			new UntypedNil(
				Nil: new Tok(
					typ: TokType.Nil,
					value: "nil"
				)
			)
		);

		ArrangeAndAct_Invalid(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedYield(
					Yield: new Tok(
						typ: TokType.Yield,
						value: "yield"
					),
					Value: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "5"
						))
				)
			},
			expected: typeof(InvalidUseOfYieldKeywordException)
		);
	}

	[Test]
	public void TestTypeCheck_Break()
	{
		_enclosingStmtStore.Setup(stmt => stmt.Peek()).Returns(
			new UntypedWhile(
				While: new Tok(
					typ: TokType.While,
					value: "while"
				),
				Condition: new BoolLiteral(
					Bool: new Tok(
						typ: TokType.True,
						value: "true"
					)
				),
				Body: new List<IUntypedAuraStatement>(),
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				)
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedBreak(
					Break: new Tok(
						typ: TokType.Break,
						value: "break"
					)
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedBreak(
				Break: new Tok(
					typ: TokType.Break,
					value: "break"
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Break_Invalid()
	{
		_enclosingStmtStore.Setup(stmt => stmt.Peek()).Returns(
			new UntypedExpressionStmt(
				Expression: new UntypedNil(
					Nil: new Tok(
						typ: TokType.Nil,
						value: "nil"
					))
			)
		);

		ArrangeAndAct_Invalid(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedBreak(
					Break: new Tok(
						typ: TokType.Break,
						value: "break"
					)
				)
			},
			expected: typeof(InvalidUseOfBreakKeywordException)
		);
	}

	[Test]
	public void TestTypeCheck_Continue()
	{
		_enclosingStmtStore.Setup(stmt => stmt.Peek()).Returns(
			new UntypedWhile(
				While: new Tok(
					typ: TokType.While,
					value: "while"
				),
				Condition: new BoolLiteral(
					Bool: new Tok(
						typ: TokType.True,
						value: "true"
					)
				),
				Body: new List<IUntypedAuraStatement>(),
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				)
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedContinue(
					Continue: new Tok(
						typ: TokType.Continue,
						value: "continue"
					)
				)
			});
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedContinue(
				Continue: new Tok(
					typ: TokType.Continue,
					value: "continue"
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Continue_Invalid()
	{
		_enclosingStmtStore.Setup(stmt => stmt.Peek()).Returns(
			new UntypedExpressionStmt(
				Expression: new UntypedNil(
					Nil: new Tok(
						typ: TokType.Nil,
						value: "nil"
					))
			)
		);

		ArrangeAndAct_Invalid(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedContinue(
					Continue: new Tok(
						typ: TokType.Continue,
						value: "continue"
					)
				)
			},
			expected: typeof(InvalidUseOfContinueKeywordException)
		);
	}

	[Test]
	public void TestTypeCheck_Interface_NoMethods()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedInterface(
					Interface: new Tok(
						typ: TokType.Interface,
						value: "interface"
					),
					Name: new Tok(TokType.Identifier, "IGreeter"),
					Methods: new List<AuraNamedFunction>(),
					Public: Visibility.Public,
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					)
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedInterface(
				Interface: new Tok(
					typ: TokType.Interface,
					value: "interface"
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "IGreeter"
				),
				Methods: new List<AuraNamedFunction>(),
				Public: Visibility.Public,
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Interface_OneMethod()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedInterface(
					Interface: new Tok(
						typ: TokType.Interface,
						value: "interface"
					),
					Name: new Tok(TokType.Identifier, "IGreeter"),
					Methods: new List<AuraNamedFunction>
					{
						new(
							name: "say_hi",
							pub: Visibility.Private,
							f: new AuraFunction(
								fParams: new List<Param>
								{
									new(
										Name: new Tok(
											typ: TokType.Identifier,
											value: "i"
										),
										ParamType: new(
											Typ: new AuraInt(),
											Variadic: false,
											DefaultValue: null
										)
									)
								},
								returnType: new AuraString()
							)
						)
					},
					Public: Visibility.Public,
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					)
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedInterface(
				Interface: new Tok(
					typ: TokType.Interface,
					value: "interface"
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "IGreeter"
				),
				Methods: new List<AuraNamedFunction>
				{
					new(
						name: "say_hi",
						pub: Visibility.Private,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "i"
									),
									ParamType: new(
										Typ: new AuraInt(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraString()
						)
					)
				},
				Public: Visibility.Public,
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_ClassImplementingTwoInterfaces_NoMethods()
	{
		_symbolsTable.Setup(v => v.GetSymbol("IGreeter", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "IGreeter",
				Kind: new AuraInterface(
					name: "IGreeter",
					functions: new List<AuraNamedFunction>(),
					pub: Visibility.Private
				)
			)
		);
		_symbolsTable.Setup(v => v.GetSymbol("IGreeter2", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "IGreeter2",
				Kind: new AuraInterface(
					name: "IGreeter2",
					functions: new List<AuraNamedFunction>(),
					pub: Visibility.Private
				)
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedClass(
					Class: new Tok(
						typ: TokType.Class,
						value: "class"
					),
					Name: new Tok(
						typ: TokType.Identifier,
						value: "Greeter"
					),
					Params: new List<Param>(),
					Body: new List<IUntypedAuraStatement>(),
					Public: Visibility.Private,
					Implementing: new List<Tok>
					{
						new(
							typ: TokType.Identifier,
							value: "IGreeter"
						),
						new(
							typ: TokType.Identifier,
							value: "IGreeter2"
						)
					},
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					)
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new FullyTypedClass(
				Class: new Tok(
					typ: TokType.Class,
					value: "class"
				),
				Name: new Tok(TokType.Identifier, "Greeter"),
				Params: new List<Param>(),
				Methods: new List<TypedNamedFunction>(),
				Public: Visibility.Private,
				Implementing: new List<AuraInterface>
				{
					new(
						name: "IGreeter",
						functions: new List<AuraNamedFunction>(),
						pub: Visibility.Private
					),
					new(
						name: "IGreeter2",
						functions: new List<AuraNamedFunction>(),
						pub: Visibility.Private
					)
				},
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_ClassImplementingInterface_OneMethod_MissingImplementation()
	{
		_symbolsTable.Setup(v => v.GetSymbol("IGreeter", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "IGreeter",
				Kind: new AuraInterface(
					name: "IGreeter",
					functions: new List<AuraNamedFunction>
					{
						new(
							name: "f",
							pub: Visibility.Public,
							f: new AuraFunction(
								fParams: new List<Param>
								{
									new(
										Name: new Tok(
											typ: TokType.Identifier,
											value: "i"
										),
										ParamType: new(
											Typ: new AuraInt(),
											Variadic: false,
											DefaultValue: null
										)
									)
								},
								returnType: new AuraInt()
							)
						)
					},
					pub: Visibility.Private
				)
			)
		);

		ArrangeAndAct_Invalid(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedClass(
					Class: new Tok(
						typ: TokType.Class,
						value: "class"
					),
					Name: new Tok(
						typ: TokType.Identifier,
						value: "Greeter"
					),
					Params: new List<Param>(),
					Body: new List<IUntypedAuraStatement>(),
					Public: Visibility.Private,
					Implementing: new List<Tok>
					{
						new(
							typ: TokType.Identifier,
							value: "IGreeter"
						)
					},
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					)
				)
			},
			expected: typeof(MissingInterfaceMethodException)
		);
	}

	[Test]
	public void TestTypeCheck_ClassImplementingInterface_OneMethod_ImplementationNotPublic()
	{
		_symbolsTable.Setup(v => v.GetSymbol("IGreeter", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "IGreeter",
				Kind: new AuraInterface(
					name: "IGreeter",
					functions: new List<AuraNamedFunction>
					{
						new(
							name: "f",
							pub: Visibility.Public,
							f: new AuraFunction(
								fParams: new List<Param>
								{
									new(
										new Tok(
											typ: TokType.Identifier,
											value: "i"
										),
										new ParamType(
											Typ: new AuraInt(),
											Variadic: false,
											DefaultValue: null
										)
									)
								},
								returnType: new AuraInt()
							)
						)
					},
					pub: Visibility.Private
				)
			)
		);

		ArrangeAndAct_Invalid(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedClass(
					Class: new Tok(
						typ: TokType.Class,
						value: "class"
					),
					Name: new Tok(
						typ: TokType.Identifier,
						value: "Greeter"
					),
					Params: new List<Param>(),
					Body: new List<IUntypedAuraStatement>
					{
						new UntypedNamedFunction(
							Fn: new Tok(
								typ: TokType.Fn,
								value: "fn"
							),
							Name: new Tok(
								typ: TokType.Identifier,
								value: "f"
							),
							Params: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "i"
									),
									ParamType: new ParamType(
										Typ: new AuraInt(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							Body: new UntypedBlock(
								OpeningBrace: new Tok(
									typ: TokType.LeftBrace,
									value: "{"
								),
								Statements: new List<IUntypedAuraStatement>
								{
									new UntypedReturn(
										Return: new Tok(
											typ: TokType.Return,
											value: "return"
										),
										Value: new List<IUntypedAuraExpression>
										{
											new IntLiteral(
												Int: new Tok(
													typ: TokType.IntLiteral,
													value: "5"
												)
											)
										}
									)
								},
								ClosingBrace: new Tok(
									typ: TokType.RightBrace,
									value: "}"
								)
							),
							ReturnType: new List<AuraType>{ new AuraInt() },
							Public: Visibility.Private
						)
					},
					Public: Visibility.Private,
					Implementing: new List<Tok>
					{
						new(
							typ: TokType.Identifier,
							value: "IGreeter"
						)
					},
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					)
				)
			},
			expected: typeof(MissingInterfaceMethodException)
		);
	}

	[Test]
	public void TestTypeCheck_ClassImplementingInterface_OneMethod()
	{
		_symbolsTable.Setup(v => v.GetSymbol("IGreeter", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "IGreeter",
				Kind: new AuraInterface(
					name: "IGreeter",
					functions: new List<AuraNamedFunction>
					{
						new(
							name: "f",
							pub: Visibility.Public,
							f: new AuraFunction(
								fParams: new List<Param>
								{
									new(
										Name: new Tok(
											typ: TokType.Identifier,
											value: "i"
										),
										ParamType: new(
											Typ: new AuraInt(),
											Variadic: false,
											DefaultValue: null
										)
									)
								},
								returnType: new AuraInt()
							)
						)
					},
					pub: Visibility.Private
				)
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedClass(
					Class: new Tok(
						typ: TokType.Class,
						value: "class"
					),
					Name: new Tok(
						typ: TokType.Identifier,
						value: "Greeter"
					),
					Params: new List<Param>(),
					Body: new List<IUntypedAuraStatement>
					{
						new UntypedNamedFunction(
							Fn: new Tok(
								typ: TokType.Fn,
								value: "fn"
							),
							Name: new Tok(
								typ: TokType.Identifier,
								value: "f"
							),
							Params: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "i"
									),
									ParamType: new(
										Typ: new AuraInt(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							Body: new UntypedBlock(
								OpeningBrace: new Tok(
									typ: TokType.LeftBrace,
									value: "{"
								),
								Statements: new List<IUntypedAuraStatement>
								{
									new UntypedReturn(
										Return: new Tok(
											typ: TokType.Return,
											value: "return"
										),
										Value: new List<IUntypedAuraExpression>
										{
											new IntLiteral(
												Int: new Tok(
													typ: TokType.IntLiteral,
													value: "5"
												)
											)
										}
									)
								},
								ClosingBrace: new Tok(
									typ: TokType.RightBrace,
									value: "}"
								)
							),
							ReturnType: new List<AuraType>{ new AuraInt() },
							Public: Visibility.Public
						)
					},
					Public: Visibility.Private,
					Implementing: new List<Tok>
					{
						new(
							typ: TokType.Identifier,
							value: "IGreeter"
						)
					},
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					)
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new FullyTypedClass(
				Class: new Tok(
					typ: TokType.Class,
					value: "class"
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "Greeter"
				),
				Params: new List<Param>(),
				Methods: new List<TypedNamedFunction>
				{
					new(
						Fn: new Tok(
							typ: TokType.Fn,
							value: "fn"
						),
						Name: new Tok(
							typ: TokType.Identifier,
							value: "f"
						),
						Params: new List<Param>
						{
							new(
								Name: new Tok(
									typ: TokType.Identifier,
									value: "i"
								),
								ParamType: new(
									Typ: new AuraInt(),
									Variadic: false,
									DefaultValue: null
								)
							)
						},
						Body: new TypedBlock(
							OpeningBrace: new Tok(
								typ: TokType.LeftBrace,
								value: "{"
							),
							Statements: new List<ITypedAuraStatement>
							{
								new TypedReturn(
									Return: new Tok(
										typ: TokType.Return,
										value: "return"
									),
									Value: new IntLiteral(
										Int: new Tok(
											typ: TokType.IntLiteral,
											value: "5"
										)
									)
								)
							},
							ClosingBrace: new Tok(
								typ: TokType.RightBrace,
								value: "}"
							),
							Typ: new AuraInt()
						),
						ReturnType: new AuraInt(),
						Public: Visibility.Public
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
								name: "f",
								pub: Visibility.Public,
								f: new AuraFunction(
									fParams: new List<Param>
									{
										new(
											Name: new Tok(
												typ: TokType.Identifier,
												value: "i"
											),
											ParamType: new(
												Typ: new AuraInt(),
												Variadic: false,
												DefaultValue: null
											)
										)
									},
									returnType: new AuraInt()
								)
							)
						},
						pub: Visibility.Private
					)
				},
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_ClassImplementingInterface_NoMethods()
	{
		_symbolsTable.Setup(v => v.GetSymbol("IGreeter", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "IGreeter",
				Kind: new AuraInterface(
					name: "IGreeter",
					functions: new List<AuraNamedFunction>(),
					pub: Visibility.Private
				)
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedClass(
					Class: new Tok(
						typ: TokType.Class,
						value: "class"
					),
					Name: new Tok(
						typ: TokType.Identifier,
						value: "Greeter"
					),
					Params: new List<Param>(),
					Body: new List<IUntypedAuraStatement>(),
					Public: Visibility.Private,
					Implementing: new List<Tok>
					{
						new(
							typ: TokType.Identifier,
							value: "IGreeter"
						)
					},
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					)
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new FullyTypedClass(
				Class: new Tok(
					typ: TokType.Class,
					value: "class"
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "Greeter"
				),
				Params: new List<Param>(),
				Methods: new List<TypedNamedFunction>(),
				Public: Visibility.Private,
				Implementing: new List<AuraInterface>
				{
					new(
						name: "IGreeter",
						functions: new List<AuraNamedFunction>(),
						pub: Visibility.Private
					)
				},
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				)
			)
		);
	}

	[Test]
	public void TestTypeCheck_Set_Invalid()
	{
		_symbolsTable.Setup(v => v.GetSymbol("v", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "v",
				Kind: new AuraInt()
			)
		);

		ArrangeAndAct_Invalid(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedSet(
						Obj: new UntypedVariable(
							Name: new Tok(
								typ: TokType.Identifier,
								value: "v"
							)
						),
						Name: new Tok(
							typ: TokType.Identifier,
							value: "name"
						),
						Value: new StringLiteral(
							String: new Tok(
								typ: TokType.StringLiteral,
								value: "Bob"
							)))
				)
			},
			expected: typeof(CannotSetOnNonClassException)
		);
	}

	[Test]
	public void TestTypeCheck_Is()
	{
		_symbolsTable.Setup(v => v.GetSymbol("v", It.IsAny<string>())).Returns(
			new AuraSymbol(
				Name: "v",
				Kind: new AuraInt()
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedExpressionStmt(
					Expression: new UntypedIs(
						Expr: new UntypedVariable(
							Name: new Tok(
								typ: TokType.Identifier,
								value: "v"
							)
						),
						Expected: new Tok(
							typ: TokType.Identifier,
							value: "IGreeter"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedExpressionStmt(
				Expression: new TypedIs(
					Expr: new TypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "v"
						),
						Typ: new AuraInt()
					),
					Expected: new AuraInterface(
						name: "IGreeter",
						functions: new List<AuraNamedFunction>(),
						pub: Visibility.Private
					))
			)
		);
	}

	[Test]
	public void TestTypeCheck_Check()
	{
		_enclosingFunctionDeclarationStore.Setup(f => f.Peek()).Returns(
			new UntypedNamedFunction(
				Fn: new Tok(
					typ: TokType.Fn,
					value: "fn"
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "f"
				),
				Params: new List<Param>(),
				Body: new UntypedBlock(
					OpeningBrace: new Tok(
						typ: TokType.LeftBrace,
						value: "{"
					),
					Statements: new List<IUntypedAuraStatement>(),
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					)
				),
				ReturnType: new List<AuraType>
				{
					new AuraResult(
						success: new AuraString(),
						failure: new AuraError()
					)
				},
				Public: Visibility.Public
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
						returnType: new AuraResult(
							success: new AuraString(),
							failure: new AuraError()
						)
					)
				)
			)
		);

		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedCheck(
					Check: new Tok(
						typ: TokType.Check,
						value: "check"
					),
					Call: new UntypedCall(
						Callee: new UntypedVariable(
							Name: new Tok(
								typ: TokType.Identifier,
								value: "c"
							)
						),
						Arguments: new List<(Tok?, IUntypedAuraExpression)>(),
						ClosingParen: new Tok(
							typ: TokType.RightParen,
							value: ")"
						))
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedCheck(
				Check: new Tok(
					typ: TokType.Check,
					value: "check"
				),
				Call: new TypedCall(
					Callee: new TypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "c"
						),
						Typ: new AuraNamedFunction(
							name: "c",
							pub: Visibility.Public,
							f: new AuraFunction(
								fParams: new List<Param>(),
								returnType: new AuraResult(
									success: new AuraString(),
									failure: new AuraError()
								)
							)
						)
					),
					Arguments: new List<ITypedAuraExpression>(),
					ClosingParen: new Tok(
						typ: TokType.RightParen,
						value: ")"
					),
					Typ: new AuraResult(
						success: new AuraString(),
						failure: new AuraError()
					))
			)
		);
	}

	[Test]
	public void TestTypeCheck_Struct()
	{
		var typedAst = ArrangeAndAct(
			untypedAst: new List<IUntypedAuraStatement>
			{
				new UntypedStruct(
					Struct: new Tok(
						typ: TokType.Struct,
						value: "struct"
					),
					Name: new Tok(
						typ: TokType.Identifier,
						value: "s"
					),
					Params: new List<Param>(),
					ClosingParen: new Tok(
						typ: TokType.RightParen,
						value: ")"
					)
				)
			}
		);
		MakeAssertions(
			typedAst: typedAst,
			expected: new TypedStruct(
				Struct: new Tok(
					typ: TokType.Struct,
					value: "struct"
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "s"
				),
				Params: new List<Param>(),
				ClosingParen: new Tok(
					typ: TokType.RightParen,
					value: ")"
				)
			)
		);
	}

	private List<ITypedAuraStatement> ArrangeAndAct(List<IUntypedAuraStatement> untypedAst)
		=> new AuraTypeChecker(_symbolsTable.Object, _enclosingClassStore.Object, _enclosingFunctionDeclarationStore.Object, _enclosingExprStore.Object,
				_enclosingStmtStore.Object, new AuraLocalFileSystemImportedModuleProvider(), "Test", "Test")
			.CheckTypes(AddModStmtIfNecessary(untypedAst));

	private void ArrangeAndAct_Invalid(List<IUntypedAuraStatement> untypedAst, Type expected)
	{
		try
		{
			new AuraTypeChecker(_symbolsTable.Object, _enclosingClassStore.Object, _enclosingFunctionDeclarationStore.Object, _enclosingExprStore.Object,
					_enclosingStmtStore.Object, new AuraLocalFileSystemImportedModuleProvider(), "Test", "Test")
				.CheckTypes(AddModStmtIfNecessary(untypedAst));
			Assert.Fail();
		}
		catch (TypeCheckerExceptionContainer e)
		{
			Assert.That(
				actual: e.Exs.First(),
				expression: Is.TypeOf(expected)
			);
		}
	}

	private List<IUntypedAuraStatement> AddModStmtIfNecessary(List<IUntypedAuraStatement> untypedAst)
	{
		if (untypedAst.Count > 0 && untypedAst[0] is not UntypedMod)
		{
			var untypedAstWithMod = new List<IUntypedAuraStatement>
			{
				new UntypedMod(
					Mod: new Tok(
						typ: TokType.Mod,
						value: "mod"
					),
					Value: new Tok(
						typ: TokType.Identifier,
						value: "main"
					)
				)
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
			Assert.That(
				actual: typedAst,
				expression: Is.Not.Null
			);
			Assert.That(
				actual: typedAst,
				expression: Has.Count.EqualTo(2)
			);

			var expectedJson = JsonConvert.SerializeObject(expected);
			var actualJson = JsonConvert.SerializeObject(typedAst[1]);
			Assert.That(
				actual: actualJson,
				expression: Is.EqualTo(expectedJson)
			);
		});
	}
}
