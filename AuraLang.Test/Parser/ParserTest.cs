using AuraLang.AST;
using AuraLang.Exceptions.Parser;
using AuraLang.Parser;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using Newtonsoft.Json;

namespace AuraLang.Test.Parser;

public class ParserTest
{
	[Test]
	public void TestParse_Assignment()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Identifier,
					value: "i"
				),
				new(
					typ: TokType.Equal,
					value: "="
				),
				new(
					typ: TokType.IntLiteral,
					value: "5"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedAssignment(
					Name: new Tok(
						typ: TokType.Identifier,
						value: "i"
					),
					Value: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "5")))
			)
		);
	}

	[Test]
	public void TestParse_Binary()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.IntLiteral,
					value: "1"
				),
				new(
					typ: TokType.Plus,
					value: "+"
				),
				new(
					typ: TokType.IntLiteral,
					value: "2"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedBinary(
					Left: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "1"
						)
					),
					Operator: new Tok(
						typ: TokType.Plus,
						value: "+"
					),
					Right: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "2"
						)))
			)
		);
	}

	[Test]
	public void TestParse_Block()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.LeftBrace,
					value: "{"
				),
				new(
					typ: TokType.IntLiteral,
					value: "1"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.RightBrace,
					value: "}"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedBlock(
					OpeningBrace: new Tok(
						typ: TokType.LeftBrace,
						value: "{"
					),
					Statements: new List<IUntypedAuraStatement>
					{
						new UntypedExpressionStmt(
							Expression: new IntLiteral(
								Int: new Tok(
									typ: TokType.IntLiteral,
									value: "1"
								))
						)
					},
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					))
			)
		);
	}

	[Test]
	public void TestParse_Call()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Identifier,
					value: "f"
				),
				new(
					typ: TokType.LeftParen,
					value: "("
				),
				new(
					typ: TokType.RightParen,
					value: ")"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
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
						value: ")"
					))
			)
		);
	}

	[Test]
	public void TestParse_Get()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Identifier,
					value: "greeter"
				),
				new(
					typ: TokType.Dot,
					value: "."
				),
				new(
					typ: TokType.Identifier,
					value: "name"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				),
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedGet(
					Obj: new UntypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "greeter")
					),
					Name: new Tok(
						typ: TokType.Identifier,
						value: "name"
					))
			)
		);
	}

	[Test]
	public void TestParse_GetIndex()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Identifier,
					value: "collection"
				),
				new(
					typ: TokType.LeftBracket,
					value: "["
				),
				new(
					typ: TokType.IntLiteral,
					value: "0"
				),
				new(
					typ: TokType.RightBracket,
					value: "]"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedGetIndex(
					Obj: new UntypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "collection"
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
		);
	}

	[Test]
	public void TestParse_GetIndex_Empty()
	{
		ArrangeAndAct_Invalid(
			tokens: new List<Tok>
				{
					new(
						typ: TokType.Identifier,
						value: "collection"
					),
					new(
						typ: TokType.LeftBracket,
						value: "["
					),
					new(
						typ: TokType.RightBracket,
						value: "]"
					),
					new(
						typ: TokType.Semicolon,
						value: ";"
					),
					new(
						typ: TokType.Eof,
						value: "eof"
					)
				},
			expected: typeof(PostfixIndexCannotBeEmptyException)
		);
	}

	[Test]
	public void TestParse_GetIndexRange()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Identifier,
					value: "collection"
				),
				new(
					typ: TokType.LeftBracket,
					value: "["
				),
				new(
					typ: TokType.IntLiteral,
					value: "0"
				),
				new(
					typ: TokType.Colon,
					value: ":"
				),
				new(
					typ: TokType.IntLiteral,
					value: "1"
				),
				new(
					typ: TokType.RightBracket,
					value: "]"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedGetIndexRange(
					Obj: new UntypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "collection"
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
							value: "1"
						)
					),
					ClosingBracket: new Tok(
						typ: TokType.RightBracket,
						value: "]"
					))
			)
		);
	}

	[Test]
	public void TestParse_Grouping()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.LeftParen,
					value: "("
				),
				new(
					typ: TokType.IntLiteral,
					value: "1"
				),
				new(
					typ: TokType.RightParen,
					value: ")"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedGrouping(
					OpeningParen: new Tok(
						typ: TokType.LeftParen,
						value: "("
					),
					Expr: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "1"
						)
					),
					ClosingParen: new Tok(
						typ: TokType.RightParen,
						value: ")"
					))
			)
		);
	}

	[Test]
	public void TestParse_If()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.If,
					value: "if"
				),
				new(
					typ: TokType.True,
					value: "true"
				),
				new(
					typ: TokType.LeftBrace,
					value: "{"
				),
				new(
					typ: TokType.Return,
					value: "return"
				),
				new(
					typ: TokType.IntLiteral,
					value: "1"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.RightBrace,
					value: "}"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
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
											value: "1"
										)
									)
								}
							),
						},
						ClosingBrace: new Tok(
							typ: TokType.RightBrace,
							value: "}"
						)
					),
					Else: null
				)
			)
		);
	}

	[Test]
	public void TestParse_IntLiteral()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.IntLiteral,
					value: "5"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new IntLiteral(
					Int: new Tok(
						typ: TokType.IntLiteral,
						value: "5"
					))
			)
		);
	}

	[Test]
	public void TestParse_FloatLiteral()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.FloatLiteral,
					value: "5.0"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new FloatLiteral(
					Float: new Tok(
						typ: TokType.FloatLiteral,
						value: "5.0"
					))
			)
		);
	}

	[Test]
	public void TestParse_StringLiteral()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.StringLiteral,
					value: "Hello"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new StringLiteral(
					String: new Tok(
						typ: TokType.StringLiteral,
						value: "Hello"
					))
			)
		);
	}

	[Test]
	public void TestParse_ListLiteral()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.LeftBracket,
					value: "["
				),
				new(
					typ: TokType.Int,
					value: "int"
				),
				new(
					typ: TokType.RightBracket,
					value: "]"
				),
				new(
					typ: TokType.LeftBrace,
					value: "{"
				),
				new(
					typ: TokType.IntLiteral,
					value: "5"
				),
				new(
					typ: TokType.Comma,
					value: ","
				),
				new(
					typ: TokType.IntLiteral,
					value: "6"
				),
				new(
					typ: TokType.RightBrace,
					value: "}"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new ListLiteral<ITypedAuraExpression>(
					OpeningBracket: new Tok(
						typ: TokType.LeftBracket,
						value: "["
					),
					L: new List<ITypedAuraExpression>
					{
						new IntLiteral(
							Int: new Tok(
								typ: TokType.IntLiteral,
								value: "5"
							)
						),
						new IntLiteral(
							Int: new Tok(
								typ: TokType.IntLiteral,
								value: "6"
							)
						)
					},
					Kind: new AuraInt(),
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}"
					))
			)
		);
	}

	[Test]
	public void TestParse_MapLiteral_Get()
	{
		var untypedAst = ArrangeAndAct(new List<Tok>
		{
			new(
				typ: TokType.Map,
				value: "map"
			),
			new(
				typ: TokType.LeftBracket,
				value: "["
			),
			new(
				typ: TokType.String,
				value: "string"
			),
			new(
				typ: TokType.Colon,
				value: ":"
			),
			new(
				typ: TokType.Int,
				value: "int"
			),
			new(
				typ: TokType.RightBracket,
				value: "]"
			),
			new(
				typ: TokType.LeftBrace,
				value: "{"
			),
			new(
				typ: TokType.StringLiteral,
				value: "Hello"
			),
			new(
				typ: TokType.Colon,
				value: ":"
			),
			new(
				typ: TokType.IntLiteral,
				value: "1"
			),
			new(
				typ: TokType.Comma,
				value: ","
			),
			new(
				typ: TokType.Semicolon,
				value: ";"
			),
			new(
				typ: TokType.StringLiteral,
				value: "World"
			),
			new(
				typ: TokType.Colon,
				value: ":"
			),
			new(
				typ: TokType.IntLiteral,
				value: "2"
			),
			new(
				typ: TokType.Comma,
				value: ","
			),
			new(
				typ: TokType.Semicolon,
				value: ";"
			),
			new(
				typ: TokType.RightBrace,
				value: "}"
			),
			new(
				typ: TokType.LeftBracket,
				value: "["
			),
			new(
				typ: TokType.StringLiteral,
				value: "Hello"
			),
			new(
				typ: TokType.RightBracket,
				value: "]"
			),
			new(
				typ: TokType.Semicolon,
				value: ";"
			),
			new(
				typ: TokType.Eof,
				value: "eof"
			)
		});
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedGetIndex(
					Obj: new MapLiteral<ITypedAuraExpression, ITypedAuraExpression>(
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
							},
							{
								new StringLiteral(
									String: new Tok(
										typ: TokType.StringLiteral,
										value: "World"
									)
								),
								new IntLiteral(
									Int: new Tok(
										typ: TokType.IntLiteral,
										value: "2"
									)
								)
							}
						},
						KeyType: new AuraString(),
						ValueType: new AuraInt(),
						ClosingBrace: new Tok(
							typ: TokType.RightBrace,
							value: "}"
						)
					),
					Index: new StringLiteral(
						String: new Tok(
							typ: TokType.StringLiteral,
							value: "Hello"
						)
					),
					ClosingBracket: new Tok(
						typ: TokType.RightBracket,
						value: "]"
					))
			)
		);
	}

	[Test]
	public void TestParse_MapLiteral()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Map,
					value: "map"
				),
				new(
					typ: TokType.LeftBracket,
					value: "["
				),
				new(
					typ: TokType.String,
					value: "string"
				),
				new(
					typ: TokType.Colon,
					value: ":"
				),
				new(
					typ: TokType.Int,
					value: "int"
				),
				new(
					typ: TokType.RightBracket,
					value: "]"
				),
				new(
					typ: TokType.LeftBrace,
					value: "{"
				),
				new(
					typ: TokType.StringLiteral,
					value: "Hello"
				),
				new(
					typ: TokType.Colon,
					value: ":"
				),
				new(
					typ: TokType.IntLiteral,
					value: "1"
				),
				new(
					typ: TokType.Comma,
					value: ","
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.StringLiteral,
					value: "World"
				),
				new(
					typ: TokType.Colon,
					value: ":"
				),
				new(
					typ: TokType.IntLiteral,
					value: "2"
				),
				new(
					typ: TokType.Comma,
					value: ","
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.RightBrace,
					value: "}"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
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
						},
						{
							new StringLiteral(
								String: new Tok(
									typ: TokType.StringLiteral,
									value: "World"
								)
							),
							new IntLiteral(
								Int: new Tok(
									typ: TokType.IntLiteral,
									value: "2"
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
	public void TestParse_BoolLiteral()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.True,
					value: "true"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				),
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new BoolLiteral(
					Bool: new Tok(
						typ: TokType.True,
						value: "true"
					))
			)
		);
	}

	[Test]
	public void TestParse_Nil()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Nil,
					value: "nil"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedNil(
					Nil: new Tok(
						typ: TokType.Nil,
						value: "nil"
					))
			)
		);
	}

	[Test]
	public void TestParse_CharLiteral()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.CharLiteral,
					value: "c"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new CharLiteral(
					Char: new Tok(
						typ: TokType.CharLiteral,
						value: "c"
					))
			)
		);
	}

	[Test]
	public void TestParse_Logical()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.True,
					value: "true"
				),
				new(
					typ: TokType.Or,
					value: "or"
				),
				new(
					typ: TokType.False,
					value: "false"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedLogical(
					Left: new BoolLiteral(
						Bool: new Tok(
							typ: TokType.True,
							value: "true"
						)
					),
					Operator: new Tok(
						typ: TokType.Or,
						value: "or"
					),
					Right: new BoolLiteral(
						Bool: new Tok(
							typ: TokType.False,
							value: "false"
						)))
			)
		);
	}

	[Test]
	public void TestParse_Set()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Identifier,
					value: "greeter"
				),
				new(
					typ: TokType.Dot,
					value: "."
				),
				new(
					typ: TokType.Identifier,
					value: "name"
				),
				new(
					typ: TokType.Equal,
					value: "="
				),
				new(
					typ: TokType.StringLiteral,
					value: "Bob"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
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
		);
	}

	[Test]
	public void TestParse_This()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.This,
					value: "this"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedThis(
					This: new Tok(
						typ: TokType.This,
						value: "this"
					))
			)
		);
	}

	[Test]
	public void TestParse_Unary_Bang()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Bang,
					value: "!"
				),
				new(
					typ: TokType.True,
					value: "true"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
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
		);
	}

	[Test]
	public void TestParse_Unary_Minus()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Minus,
					value: "-"
				),
				new(
					typ: TokType.IntLiteral,
					value: "5"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
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
		);
	}

	[Test]
	public void TestParse_Variable()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Identifier,
					value: "variable"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedVariable(
					Name: new Tok(
						typ: TokType.Identifier,
						value: "variable"
					))
			)
		);
	}

	[Test]
	public void TestParse_Defer()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Defer,
					value: "defer"
				),
				new(
					typ: TokType.Identifier,
					value: "f"
				),
				new(
					typ: TokType.LeftParen,
					value: "("
				),
				new(
					typ: TokType.RightParen,
					value: ")"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedDefer(
				Defer: new Tok(
					typ: TokType.Defer,
					value: "defer"
				),
				Call: new UntypedCall(
					Callee: new UntypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "f"
						)
					),
					Arguments: new List<(Tok?, IUntypedAuraExpression)>(),
					ClosingParen: new Tok(
						typ: TokType.RightParen,
						value: ")"
					))
			)
		);
	}

	[Test]
	public void TestParse_For()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.For,
					value: "for"
				),
				new(
					typ: TokType.Identifier,
					value: "i"
				),
				new(
					typ: TokType.ColonEqual,
					value: ":="
				),
				new(
					typ: TokType.IntLiteral,
					value: "0"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Identifier,
					value: "i"
				),
				new(
					typ: TokType.Less,
					value: "<"
				),
				new(
					typ: TokType.IntLiteral,
					value: "10"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Identifier,
					value: "i"
				),
				new(
					typ: TokType.PlusPlus,
					value: "++"
				),
				new(
					typ: TokType.LeftBrace,
					value: "{"
				),
				new(
					typ: TokType.RightBrace,
					value: "}"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedFor(
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
					NameTyps: new List<AuraType>(),
					Mutable: false,
					Initializer: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "0"
						))
				),
				Condition: new UntypedBinary(
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
				Increment: new UntypedPlusPlusIncrement(
					Name: new UntypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "i"
						)
					),
					PlusPlus: new Tok(
						typ: TokType.PlusPlus,
						value: "++"
					)
				),
				Body: new List<IUntypedAuraStatement>(),
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				)
			)
		);
	}

	[Test]
	public void TestParse_ForEach()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.ForEach,
					value: "foreach"
				),
				new(
					typ: TokType.Identifier,
					value: "i"
				),
				new(
					typ: TokType.In,
					value: "in"
				),
				new(
					typ: TokType.Identifier,
					value: "iter"
				),
				new(
					typ: TokType.LeftBrace,
					value: "{"
				),
				new(
					typ: TokType.RightBrace,
					value: "}"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedForEach(
				ForEach: new Tok(
					typ: TokType.ForEach,
					value: "foreach"
				),
				EachName: new Tok(
					typ: TokType.Identifier,
					value: "i"
				),
				Iterable: new UntypedVariable(
					Name: new Tok(
						typ: TokType.Identifier,
						value: "iter"
					)
				),
				Body: new List<IUntypedAuraStatement>(),
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				)
			)
		);
	}

	[Test]
	public void TestParse_NamedFunction_ReturnError()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Fn,
					value: "fn"
				),
				new(
					typ: TokType.Identifier,
					value: "f"
				),
				new(
					typ: TokType.LeftParen,
					value: "("
				),
				new(
					typ: TokType.RightParen,
					value: ")"
				),
				new(
					typ: TokType.Arrow,
					value: "->"
				),
				new(
					typ: TokType.Error,
					value: "error"
				),
				new(
					typ: TokType.LeftBrace,
					value: "{"
				),
				new(
					typ: TokType.RightBrace,
					value: "}"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedNamedFunction(
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
				ReturnType: new List<AuraType> { new AuraError() },
				Public: Visibility.Private,
				Documentation: null
			)
		);
	}

	[Test]
	public void TestParse_NamedFunction_ParamDefaultValue_Invalid()
	{
		ArrangeAndAct_Invalid(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Fn,
					value: "fn"
				),
				new(
					typ: TokType.Identifier,
					value: "f"
				),
				new(
					typ: TokType.LeftParen,
					value: "("
				),
				new(
					typ: TokType.Identifier,
					value: "i"
				),
				new(
					typ: TokType.Colon,
					value: ":"
				),
				new(
					typ: TokType.Int,
					value: "int"
				),
				new(
					typ: TokType.Equal,
					value: "="
				),
				new(
					typ: TokType.Identifier,
					value: "var"
				),
				new(
					typ: TokType.RightParen,
					value: ")"
				),
				new(
					typ: TokType.LeftBrace,
					value: "{"
				),
				new(
					typ: TokType.RightBrace,
					value: "}"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			},
			expected: typeof(ParameterDefaultValueMustBeALiteralException)
		);
	}

	[Test]
	public void TestParse_NamedFunction_NoParams_NoReturnType_NoBody()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Fn,
					value: "fn"
				),
				new(
					typ: TokType.Identifier,
					value: "f"
				),
				new(
					typ: TokType.LeftParen,
					value: "("
				),
				new(
					typ: TokType.RightParen,
					value: ")"
				),
				new(
					typ: TokType.LeftBrace,
					value: "{"
				),
				new(
					typ: TokType.RightBrace,
					value: "}"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedNamedFunction(
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
				Public: Visibility.Private,
				Documentation: null
			)
		);
	}

	[Test]
	public void TestParse_AnonymousFunction_NoParams_NoReturnType_NoBody()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Fn,
					value: "fn"
				),
				new(
					typ: TokType.LeftParen,
					value: "("
				),
				new(
					typ: TokType.RightParen,
					value: ")"
				),
				new(
					typ: TokType.LeftBrace,
					value: "{"
				),
				new(
					typ: TokType.RightBrace,
					value: "}"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
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
					ReturnType: null
				)
			)
		);
	}

	[Test]
	public void TestParse_Let_Long()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Let,
					value: "let"
				),
				new(
					typ: TokType.Identifier,
					value: "i"
				),
				new(
					typ: TokType.Colon,
					value: ":"
				),
				new(
					typ: TokType.Int,
					value: "int"
				),
				new(
					typ: TokType.Equal,
					value: "="
				),
				new(
					typ: TokType.IntLiteral,
					value: "5"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedLet(
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
				NameTyps: new List<AuraType> { new AuraInt() },
				Mutable: false,
				Initializer: new IntLiteral(
					Int: new Tok(
						typ: TokType.IntLiteral,
						value: "5"
					))
			)
		);
	}

	[Test]
	public void TestParse_Let_Short()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Identifier,
					value: "i"
				),
				new(
					typ: TokType.ColonEqual,
					value: ":="
				),
				new(
					typ: TokType.IntLiteral,
					value: "5"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedLet(
				Let: null,
				Names: new List<Tok>
				{
					new(
						typ: TokType.Identifier,
						value: "i"
					)
				},
				NameTyps: new List<AuraType>(),
				Mutable: false,
				Initializer: new IntLiteral(
					Int: new Tok(
						typ: TokType.IntLiteral,
						value: "5"
					))
			)
		);
	}

	[Test]
	public void TestParse_Mod()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Mod,
					value: "mod"
				),
				new(
					typ: TokType.Identifier,
					value: "main"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedMod(
				Mod: new Tok(
					typ: TokType.Mod,
					value: "mod"
				),
				Value: new Tok(
					typ: TokType.Identifier,
					value: "main"
				)
			)
		);
	}

	[Test]
	public void TestParse_Return()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Return,
					value: "return"
				),
				new(
					typ: TokType.IntLiteral,
					value: "5"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedReturn(
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
		);
	}

	[Test]
	public void TestParse_Interface_NoMethods()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Interface,
					value: "interface"
				),
				new(
					typ: TokType.Identifier,
					value: "Greeter"
				),
				new(
					typ: TokType.LeftBrace,
					value: "{"
				),
				new(
					typ: TokType.RightBrace,
					value: "}"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedInterface(
				Interface: new Tok(
					typ: TokType.Interface,
					value: "interface"
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "Greeter"
				),
				Methods: new List<AuraNamedFunction>(),
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				),
				Public: Visibility.Private,
				Documentation: null
			)
		);
	}

	[Test]
	public void TestParse_Interface_OneMethod_NoParams_NoReturnType()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Interface,
					value: "interface"
				),
				new(
					typ: TokType.Identifier,
					value: "Greeter"
				),
				new(
					typ: TokType.LeftBrace,
					value: "{"
				),
				new(
					typ: TokType.Fn,
					value: "fn"
				),
				new(
					typ: TokType.Identifier,
					value: "say_hi"
				),
				new(
					typ: TokType.LeftParen,
					value: "("
				),
				new(
					typ: TokType.RightParen,
					value: ")"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.RightBrace,
					value: "}"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedInterface(
				Interface: new Tok(
					typ: TokType.Interface,
					value: "interface"
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "Greeter"
				),
				Methods: new List<AuraNamedFunction>
				{
					new(
						name: "say_hi",
						pub: Visibility.Private,
						f: new AuraFunction(
							fParams: new List<Param>(),
							returnType: new AuraNil()
						)
					)
				},
				Public: Visibility.Private,
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				),
				Documentation: null
			)
		);
	}

	[Test]
	public void TestParse_Interface_OneMethod()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Interface,
					value: "interface"
				),
				new(
					typ: TokType.Identifier,
					value: "Greeter"
				),
				new(
					typ: TokType.LeftBrace,
					value: "{"
				),
				new(
					typ: TokType.Fn,
					value: "fn"
				),
				new(
					typ: TokType.Identifier,
					value: "say_hi"
				),
				new(
					typ: TokType.LeftParen,
					value: "("
				),
				new(
					typ: TokType.Identifier,
					value: "i"
				),
				new(
					typ: TokType.Colon,
					value: ":"
				),
				new(
					typ: TokType.Int,
					value: "int"
				),
				new(
					typ: TokType.RightParen,
					value: ")"
				),
				new(
					typ: TokType.Arrow,
					value: "->"
				),
				new(
					typ: TokType.String,
					value: "string"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.RightBrace,
					value: "}"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedInterface(
				Interface: new Tok(
					typ: TokType.Interface,
					value: "interface"
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "Greeter"
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
				Public: Visibility.Private,
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				),
				Documentation: null
			)
		);
	}

	[Test]
	public void TestParse_Class_ImplementingTwoInterfaces()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Class,
					value: "class"
				),
				new(
					typ: TokType.Identifier,
					value: "c"
				),
				new(
					typ: TokType.LeftParen,
					value: "("
				),
				new(
					typ: TokType.RightParen,
					value: ")"
				),
				new(
					typ: TokType.Colon,
					value: ":"
				),
				new(
					typ: TokType.Identifier,
					value: "IClass"
				),
				new(
					typ: TokType.Comma,
					value: ","
				),
				new(
					typ: TokType.Identifier,
					value: "IClass2"
				),
				new(
					typ: TokType.LeftBrace,
					value: "{"
				),
				new(
					typ: TokType.RightBrace,
					value: "}"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedClass(
				Class: new Tok(
					typ: TokType.Class,
					value: "class"
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "c"
				),
				Params: new List<Param>(),
				Body: new List<IUntypedAuraStatement>(),
				Public: Visibility.Private,
				Implementing: new List<Tok>
				{
					new(
						typ: TokType.Identifier,
						value: "IClass"
					),
					new(
						typ: TokType.Identifier,
						value: "IClass2"
					)
				},
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				),
				Documentation: null
			)
		);
	}

	[Test]
	public void TestParse_Class_ImplementingOneInterface()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Class,
					value: "class"
				),
				new(
					typ: TokType.Identifier,
					value: "c"
				),
				new(
					typ: TokType.LeftParen,
					value: "("
				),
				new(
					typ: TokType.RightParen,
					value: ")"
				),
				new(
					typ: TokType.Colon,
					value: ":"
				),
				new(
					typ: TokType.Identifier,
					value: "IClass"
				),
				new(
					typ: TokType.LeftBrace,
					value: "{"
				),
				new(
					typ: TokType.RightBrace,
					value: "}"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedClass(
				Class: new Tok(
					typ: TokType.Class,
					value: "class"
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "c"
				),
				Params: new List<Param>(),
				Body: new List<IUntypedAuraStatement>(),
				Public: Visibility.Private,
				Implementing: new List<Tok>
				{
					new(
						typ: TokType.Identifier,
						value: "IClass"
					)
				},
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				),
				Documentation: null
			)
		);
	}

	[Test]
	public void TestParse_Class_NoParams_NoMethods()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Class,
					value: "class"
				),
				new(
					typ: TokType.Identifier,
					value: "c"
				),
				new(
					typ: TokType.LeftParen,
					value: "("
				),
				new(
					typ: TokType.RightParen,
					value: ")"
				),
				new(
					typ: TokType.LeftBrace,
					value: "{"
				),
				new(
					typ: TokType.RightBrace,
					value: "}"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedClass(
				Class: new Tok(
					typ: TokType.Class,
					value: "class"
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "c"
				),
				Params: new List<Param>(),
				Body: new List<IUntypedAuraStatement>(),
				Public: Visibility.Private,
				Implementing: new List<Tok>(),
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}"
				),
				Documentation: null
			)
		);
	}

	[Test]
	public void TestParse_While_EmptyBody()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.While,
					value: "while"
				),
				new(
					typ: TokType.True,
					value: "true"
				),
				new(
					typ: TokType.LeftBrace,
					value: "{"
				),
				new(
					typ: TokType.RightBrace,
					value: "}"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedWhile(
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
	}

	[Test]
	public void TestParse_Import_NoAlias()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Import,
					value: "import"
				),
				new(
					typ: TokType.Identifier,
					value: "external_pkg"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedImport(
				Import: new Tok(
					typ: TokType.Import,
					value: "import"
				),
				Package: new Tok(
					typ: TokType.Identifier,
					value: "external_pkg"
				),
				Alias: null
			)
		);
	}

	[Test]
	public void TestParse_Import_Alias()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Import,
					value: "import"
				),
				new(
					typ: TokType.Identifier,
					value: "external_pkg"
				),
				new(
					typ: TokType.As,
					value: "as"
				),
				new(
					typ: TokType.Identifier,
					value: "ep"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedImport(
				Import: new Tok(
					typ: TokType.Import,
					value: "import"
				),
				Package: new Tok(
					typ: TokType.Identifier,
					value: "external_pkg"
				),
				Alias: new Tok(
					typ: TokType.Identifier,
					value: "ep"
				)
			)
		);
	}

	[Test]
	public void TestParse_Comment()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Comment,
					value: "// this is a comment"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedComment(
				Text: new Tok(
					typ: TokType.Comment,
					value: "// this is a comment"
				)
			)
		);
	}

	[Test]
	public void TestParse_Yield()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Yield,
					value: "yield"
				),
				new(
					typ: TokType.IntLiteral,
					value: "5"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedYield(
				Yield: new Tok(
					typ: TokType.Yield,
					value: "yield"
				),
				new IntLiteral(
					Int: new Tok(
						typ: TokType.IntLiteral,
						value: "5"
					))
			)
		);
	}

	[Test]
	public void TestParse_ClassImplementingInterface()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Class,
					value: "class"
				),
				new(
					typ: TokType.Identifier,
					value: "Greeter"
				),
				new(
					typ: TokType.LeftParen,
					value: "("
				),
				new(
					typ: TokType.RightParen,
					value: ")"
				),
				new(
					typ: TokType.Colon,
					value: ":"
				),
				new(
					typ: TokType.Identifier,
					value: "IGreeter"
				),
				new(
					typ: TokType.LeftBrace,
					value: "{"
				),
				new(
					typ: TokType.RightBrace,
					value: "}"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedClass(
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
				),
				Documentation: null
			)
		);
	}

	[Test]
	public void TestParse_Is()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Identifier,
					value: "v"
				),
				new(
					typ: TokType.Is,
					value: "is"
				),
				new(
					typ: TokType.Identifier,
					value: "IGreeter"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
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
		);
	}

	[Test]
	public void TestParse_Check()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Check,
					value: "check"
				),
				new(
					typ: TokType.Identifier,
					value: "f"
				),
				new(
					typ: TokType.LeftParen,
					value: "("
				),
				new(
					typ: TokType.RightParen,
					value: ")"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedCheck(
				Check: new Tok(
					typ: TokType.Check,
					value: "check"
				),
				Call: new UntypedCall(
					Callee: new UntypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "f"
						)
					),
					Arguments: new List<(Tok?, IUntypedAuraExpression)>(),
					ClosingParen: new Tok(
						typ: TokType.RightParen,
						value: ")"
					))
			)
		);
	}

	[Test]
	public void TestParse_Struct()
	{
		var untypedAst = ArrangeAndAct(
			tokens: new List<Tok>
			{
				new(
					typ: TokType.Struct,
					value: "struct"
				),
				new(
					typ: TokType.Identifier,
					value: "s"
				),
				new(
					typ: TokType.LeftParen,
					value: "("
				),
				new(
					typ: TokType.RightParen,
					value: ")"
				),
				new(
					typ: TokType.Semicolon,
					value: ";"
				),
				new(
					typ: TokType.Eof,
					value: "eof"
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedStruct(
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
				),
				Documentation: null
			)
		);
	}

	private List<IUntypedAuraStatement> ArrangeAndAct(List<Tok> tokens)
	{
		// Arrange
		tokens.Insert(
			index: 0,
			item: new Tok(
				typ: TokType.Newline,
				value: "\n"
			)
		);
		tokens.Insert(
			index: 0,
			item: new Tok(
				typ: TokType.Semicolon,
				value: ";"
			)
		);
		tokens.Insert(
			index: 0,
			item: new Tok(
				typ: TokType.Identifier,
				value: "main"
			)
		);
		tokens.Insert(
			index: 0,
			item: new Tok(
				typ: TokType.Mod,
				value: "mod"
			)
		);
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
		untypedAst.RemoveAt(0); // Remove `mod` statement
		untypedAst.RemoveAt(0); // Remove newline after `mod` statement
		Assert.Multiple(() =>
		{
			Assert.That(
				actual: untypedAst,
				expression: Is.Not.Null
			);
			Assert.That(
				actual: untypedAst,
				expression: Has.Count.EqualTo(1)
			);

			var expectedJson = JsonConvert.SerializeObject(expected);
			var actualJson = JsonConvert.SerializeObject(untypedAst[0]);
			Assert.That(
				actual: actualJson,
				expression: Is.EqualTo(expectedJson)
			);
		});
	}
}

