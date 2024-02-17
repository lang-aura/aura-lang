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
					value: "i",
					line: 1
				),
				new(
					typ: TokType.Equal,
					value: "=",
					line: 1
				),
				new(
					typ: TokType.IntLiteral,
					value: "5",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedAssignment(
					Name: new Tok(
						typ: TokType.Identifier,
						value: "i",
						line: 1
					),
					Value: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "5",
							line: 1)))
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
					value: "1",
					line: 1
				),
				new(
					typ: TokType.Plus,
					value: "+",
					line: 1
				),
				new(
					typ: TokType.IntLiteral,
					value: "2",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
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
							value: "1",
							line: 1
						)
					),
					Operator: new Tok(
						typ: TokType.Plus,
						value: "+",
						line: 1
					),
					Right: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "2",
							line: 1
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
					value: "{",
					line: 1
				),
				new(
					typ: TokType.IntLiteral,
					value: "1",
					line: 2
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 2
				),
				new(
					typ: TokType.RightBrace,
					value: "}",
					line: 3
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 3
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 3
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedBlock(
					OpeningBrace: new Tok(
						typ: TokType.LeftBrace,
						value: "{",
						line: 1
					),
					Statements: new List<IUntypedAuraStatement>
					{
						new UntypedExpressionStmt(
							Expression: new IntLiteral(
								Int: new Tok(
									typ: TokType.IntLiteral,
									value: "1",
									line: 2
								))
						)
					},
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}",
						line: 3
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
					value: "f",
					line: 1
				),
				new(
					typ: TokType.LeftParen,
					value: "(",
					line: 1
				),
				new(
					typ: TokType.RightParen,
					value: ")",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
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
							value: "f",
							line: 1
						)
					),
					Arguments: new List<(Tok?, IUntypedAuraExpression)>(),
					ClosingParen: new Tok(
						typ: TokType.RightParen,
						value: ")",
						line: 1
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
					value: "greeter",
					line: 1
				),
				new(
					typ: TokType.Dot,
					value: ".",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "name",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
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
							value: "greeter",
							line: 1)
					),
					Name: new Tok(
						typ: TokType.Identifier,
						value: "name",
						line: 1
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
					value: "collection",
					line: 1
				),
				new(
					typ: TokType.LeftBracket,
					value: "[",
					line: 1
				),
				new(
					typ: TokType.IntLiteral,
					value: "0",
					line: 1
				),
				new(
					typ: TokType.RightBracket,
					value: "]",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
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
							value: "collection",
							line: 1
						)
					),
					Index: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "0",
							line: 1
						)
					),
					ClosingBracket: new Tok(
						typ: TokType.RightBracket,
						value: "]",
						line: 1
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
						value: "collection",
						line: 1
					),
					new(
						typ: TokType.LeftBracket,
						value: "[",
						line: 1
					),
					new(
						typ: TokType.RightBracket,
						value: "]",
						line: 1
					),
					new(
						typ: TokType.Semicolon,
						value: ";",
						line: 1
					),
					new(
						typ: TokType.Eof,
						value: "eof",
						line: 1
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
					value: "collection",
					line: 1
				),
				new(
					typ: TokType.LeftBracket,
					value: "[",
					line: 1
				),
				new(
					typ: TokType.IntLiteral,
					value: "0",
					line: 1
				),
				new(
					typ: TokType.Colon,
					value: ":",
					line: 1
				),
				new(
					typ: TokType.IntLiteral,
					value: "1",
					line: 1
				),
				new(
					typ: TokType.RightBracket,
					value: "]",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
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
							value: "collection",
							line: 1
						)
					),
					Lower: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "0",
							line: 1
						)
					),
					Upper: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "1",
							line: 1
						)
					),
					ClosingBracket: new Tok(
						typ: TokType.RightBracket,
						value: "]",
						line: 1
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
					value: "(",
					line: 1
				),
				new(
					typ: TokType.IntLiteral,
					value: "1",
					line: 1
				),
				new(
					typ: TokType.RightParen,
					value: ")",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedGrouping(
					OpeningParen: new Tok(
						typ: TokType.LeftParen,
						value: "(",
						line: 1
					),
					Expr: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "1",
							line: 1
						)
					),
					ClosingParen: new Tok(
						typ: TokType.RightParen,
						value: ")",
						line: 1
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
					value: "if",
					line: 1
				),
				new(
					typ: TokType.True,
					value: "true",
					line: 1
				),
				new(
					typ: TokType.LeftBrace,
					value: "{",
					line: 1
				),
				new(
					typ: TokType.Return,
					value: "return",
					line: 2
				),
				new(
					typ: TokType.IntLiteral,
					value: "1",
					line: 2
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 2
				),
				new(
					typ: TokType.RightBrace,
					value: "}",
					line: 3
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 3
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 3
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedIf(
					If: new Tok(
						typ: TokType.If,
						value: "if",
						line: 1
					),
					Condition: new BoolLiteral(
						Bool: new Tok(
							typ: TokType.True,
							value: "true",
							line: 1
						)
					),
					Then: new UntypedBlock(
						OpeningBrace: new Tok(
							typ: TokType.LeftBrace,
							value: "{",
							line: 1
						),
						Statements: new List<IUntypedAuraStatement>
						{
							new UntypedReturn(
								Return: new Tok(
									typ: TokType.Return,
									value: "return",
									line: 2
								),
								Value: new List<IUntypedAuraExpression>
								{
									new IntLiteral(
										Int: new Tok(
											typ: TokType.IntLiteral,
											value: "1",
											line: 2
										)
									)
								}
							),
						},
						ClosingBrace: new Tok(
							typ: TokType.RightBrace,
							value: "}",
							line: 3
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
					value: "5",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new IntLiteral(
					Int: new Tok(
						typ: TokType.IntLiteral,
						value: "5",
						line: 1
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
					value: "5.0",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new FloatLiteral(
					Float: new Tok(
						typ: TokType.FloatLiteral,
						value: "5.0",
						line: 1
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
					value: "Hello",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new StringLiteral(
					String: new Tok(
						typ: TokType.StringLiteral,
						value: "Hello",
						line: 1
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
					value: "[",
					line: 1
				),
				new(
					typ: TokType.Int,
					value: "int",
					line: 1
				),
				new(
					typ: TokType.RightBracket,
					value: "]",
					line: 1
				),
				new(
					typ: TokType.LeftBrace,
					value: "{",
					line: 1
				),
				new(
					typ: TokType.IntLiteral,
					value: "5",
					line: 1
				),
				new(
					typ: TokType.Comma,
					value: ",",
					line: 1
				),
				new(
					typ: TokType.IntLiteral,
					value: "6",
					line: 1
				),
				new(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new ListLiteral<ITypedAuraExpression>(
					OpeningBracket: new Tok(
						typ: TokType.LeftBracket,
						value: "[",
						line: 1
					),
					L: new List<ITypedAuraExpression>
					{
						new IntLiteral(
							Int: new Tok(
								typ: TokType.IntLiteral,
								value: "5",
								line: 1
							)
						),
						new IntLiteral(
							Int: new Tok(
								typ: TokType.IntLiteral,
								value: "6",
								line: 1
							)
						)
					},
					Kind: new AuraInt(),
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}",
						line: 1
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
				value: "map",
				line: 1
			),
			new(
				typ: TokType.LeftBracket,
				value: "[",
				line: 1
			),
			new(
				typ: TokType.String,
				value: "string",
				line: 1
			),
			new(
				typ: TokType.Colon,
				value: ":",
				line: 1
			),
			new(
				typ: TokType.Int,
				value: "int",
				line: 1
			),
			new(
				typ: TokType.RightBracket,
				value: "]",
				line: 1
			),
			new(
				typ: TokType.LeftBrace,
				value: "{",
				line: 1
			),
			new(
				typ: TokType.StringLiteral,
				value: "Hello",
				line: 2
			),
			new(
				typ: TokType.Colon,
				value: ":",
				line: 2
			),
			new(
				typ: TokType.IntLiteral,
				value: "1",
				line: 2
			),
			new(
				typ: TokType.Comma,
				value: ",",
				line: 2
			),
			new(
				typ: TokType.Semicolon,
				value: ";",
				line: 2
			),
			new(
				typ: TokType.StringLiteral,
				value: "World",
				line: 3
			),
			new(
				typ: TokType.Colon,
				value: ":",
				line: 3
			),
			new(
				typ: TokType.IntLiteral,
				value: "2",
				line: 3
			),
			new(
				typ: TokType.Comma,
				value: ",",
				line: 3
			),
			new(
				typ: TokType.Semicolon,
				value: ";",
				line: 3
			),
			new(
				typ: TokType.RightBrace,
				value: "}",
				line: 4
			),
			new(
				typ: TokType.LeftBracket,
				value: "[",
				line: 4
			),
			new(
				typ: TokType.StringLiteral,
				value: "Hello",
				line: 4
			),
			new(
				typ: TokType.RightBracket,
				value: "]",
				line: 4
			),
			new(
				typ: TokType.Semicolon,
				value: ";",
				line: 4
			),
			new(
				typ: TokType.Eof,
				value: "eof",
				line: 4
			)
		});
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedGetIndex(
					Obj: new MapLiteral<ITypedAuraExpression, ITypedAuraExpression>(
						Map: new Tok(
							typ: TokType.Map,
							value: "map",
							line: 1
						),
						M: new Dictionary<ITypedAuraExpression, ITypedAuraExpression>
						{
							{
								new StringLiteral(
									String: new Tok(
										typ: TokType.StringLiteral,
										value: "Hello",
										line: 2
									)
								),
								new IntLiteral(
									Int: new Tok(
										typ: TokType.IntLiteral,
										value: "1",
										line: 2
									)
								)
							},
							{
								new StringLiteral(
									String: new Tok(
										typ: TokType.StringLiteral,
										value: "World",
										line: 3
									)
								),
								new IntLiteral(
									Int: new Tok(
										typ: TokType.IntLiteral,
										value: "2",
										line: 3
									)
								)
							}
						},
						KeyType: new AuraString(),
						ValueType: new AuraInt(),
						ClosingBrace: new Tok(
							typ: TokType.RightBrace,
							value: "}",
							line: 4
						)
					),
					Index: new StringLiteral(
						String: new Tok(
							typ: TokType.StringLiteral,
							value: "Hello",
							line: 4
						)
					),
					ClosingBracket: new Tok(
						typ: TokType.RightBracket,
						value: "]",
						line: 4
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
					value: "map",
					line: 1
				),
				new(
					typ: TokType.LeftBracket,
					value: "[",
					line: 1
				),
				new(
					typ: TokType.String,
					value: "string",
					line: 1
				),
				new(
					typ: TokType.Colon,
					value: ":",
					line: 1
				),
				new(
					typ: TokType.Int,
					value: "int",
					line: 1
				),
				new(
					typ: TokType.RightBracket,
					value: "]",
					line: 1
				),
				new(
					typ: TokType.LeftBrace,
					value: "{",
					line: 1
				),
				new(
					typ: TokType.StringLiteral,
					value: "Hello",
					line: 2
				),
				new(
					typ: TokType.Colon,
					value: ":",
					line: 2
				),
				new(
					typ: TokType.IntLiteral,
					value: "1",
					line: 2
				),
				new(
					typ: TokType.Comma,
					value: ",",
					line: 2
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 2
				),
				new(
					typ: TokType.StringLiteral,
					value: "World",
					line: 3
				),
				new(
					typ: TokType.Colon,
					value: ":",
					line: 3
				),
				new(
					typ: TokType.IntLiteral,
					value: "2",
					line: 3
				),
				new(
					typ: TokType.Comma,
					value: ",",
					line: 3
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 3
				),
				new(
					typ: TokType.RightBrace,
					value: "}",
					line: 4
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 4
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 4
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new MapLiteral<ITypedAuraExpression, ITypedAuraExpression>(
					Map: new Tok(
						typ: TokType.Map,
						value: "map",
						line: 1
					),
					M: new Dictionary<ITypedAuraExpression, ITypedAuraExpression>
					{
						{
							new StringLiteral(
								String: new Tok(
									typ: TokType.StringLiteral,
									value: "Hello",
									line: 2
								)
							),
							new IntLiteral(
								Int: new Tok(
									typ: TokType.IntLiteral,
									value: "1",
									line: 2
								)
							)
						},
						{
							new StringLiteral(
								String: new Tok(
									typ: TokType.StringLiteral,
									value: "World",
									line: 3
								)
							),
							new IntLiteral(
								Int: new Tok(
									typ: TokType.IntLiteral,
									value: "2",
									line: 3
								)
							)
						}
					},
					KeyType: new AuraString(),
					ValueType: new AuraInt(),
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}",
						line: 4
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
					value: "true",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				),
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new BoolLiteral(
					Bool: new Tok(
						typ: TokType.True,
						value: "true",
						line: 1
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
					value: "nil",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedNil(
					Nil: new Tok(
						typ: TokType.Nil,
						value: "nil",
						line: 1
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
					value: "c",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new CharLiteral(
					Char: new Tok(
						typ: TokType.CharLiteral,
						value: "c",
						line: 1
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
					value: "true",
					line: 1
				),
				new(
					typ: TokType.Or,
					value: "or",
					line: 1
				),
				new(
					typ: TokType.False,
					value: "false",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
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
							value: "true",
							line: 1
						)
					),
					Operator: new Tok(
						typ: TokType.Or,
						value: "or",
						line: 1
					),
					Right: new BoolLiteral(
						Bool: new Tok(
							typ: TokType.False,
							value: "false",
							line: 1
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
					value: "greeter",
					line: 1
				),
				new(
					typ: TokType.Dot,
					value: ".",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "name",
					line: 1
				),
				new(
					typ: TokType.Equal,
					value: "=",
					line: 1
				),
				new(
					typ: TokType.StringLiteral,
					value: "Bob",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
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
							value: "greeter",
							line: 1
						)
					),
					Name: new Tok(
						typ: TokType.Identifier,
						value: "name",
						line: 1
					),
					Value: new StringLiteral(
						String: new Tok(
							typ: TokType.StringLiteral,
							value: "Bob",
							line: 1
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
					value: "this",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedThis(
					This: new Tok(
						typ: TokType.This,
						value: "this",
						line: 1
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
					value: "!",
					line: 1
				),
				new(
					typ: TokType.True,
					value: "true",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedUnary(
					Operator: new Tok(
						typ: TokType.Bang,
						value: "!",
						line: 1
					),
					Right: new BoolLiteral(
						Bool: new Tok(
							typ: TokType.True,
							value: "true",
							line: 1
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
					value: "-",
					line: 1
				),
				new(
					typ: TokType.IntLiteral,
					value: "5",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedUnary(
					Operator: new Tok(
						typ: TokType.Minus,
						value: "-",
						line: 1
					),
					Right: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "5",
							line: 1
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
					value: "variable",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedVariable(
					Name: new Tok(
						typ: TokType.Identifier,
						value: "variable",
						line: 1
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
					value: "defer",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "f",
					line: 1
				),
				new(
					typ: TokType.LeftParen,
					value: "(",
					line: 1
				),
				new(
					typ: TokType.RightParen,
					value: ")",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedDefer(
				Defer: new Tok(
					typ: TokType.Defer,
					value: "defer",
					line: 1
				),
				Call: new UntypedCall(
					Callee: new UntypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "f",
							line: 1
						)
					),
					Arguments: new List<(Tok?, IUntypedAuraExpression)>(),
					ClosingParen: new Tok(
						typ: TokType.RightParen,
						value: ")",
						line: 1
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
					value: "for",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "i",
					line: 1
				),
				new(
					typ: TokType.ColonEqual,
					value: ":=",
					line: 1
				),
				new(
					typ: TokType.IntLiteral,
					value: "0",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "i",
					line: 1
				),
				new(
					typ: TokType.Less,
					value: "<",
					line: 1
				),
				new(
					typ: TokType.IntLiteral,
					value: "10",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "i",
					line: 1
				),
				new(
					typ: TokType.PlusPlus,
					value: "++",
					line: 1
				),
				new(
					typ: TokType.LeftBrace,
					value: "{",
					line: 1
				),
				new(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedFor(
				For: new Tok(
					typ: TokType.For,
					value: "for",
					line: 1
				),
				Initializer: new UntypedLet(
					Let: null,
					Names: new List<Tok>
					{
						new(
							typ: TokType.Identifier,
							value: "i",
							line: 1
						)
					},
					NameTyps: new List<AuraType?>(),
					Mutable: false,
					Initializer: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "0",
							line: 1
						))
				),
				Condition: new UntypedBinary(
					Left: new UntypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "i",
							line: 1
						)
					),
					Operator: new Tok(
						typ: TokType.Less,
						value: "<",
						line: 1
					),
					Right: new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "10",
							line: 1
						))
				),
				Increment: new UntypedPlusPlusIncrement(
					Name: new UntypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "i",
							line: 1
						)
					),
					PlusPlus: new Tok(
						typ: TokType.PlusPlus,
						value: "++",
						line: 1
					)
				),
				Body: new List<IUntypedAuraStatement> { },
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
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
					value: "foreach",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "i",
					line: 1
				),
				new(
					typ: TokType.In,
					value: "in",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "iter",
					line: 1
				),
				new(
					typ: TokType.LeftBrace,
					value: "{",
					line: 1
				),
				new(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedForEach(
				ForEach: new Tok(
					typ: TokType.ForEach,
					value: "foreach",
					line: 1
				),
				EachName: new Tok(
					typ: TokType.Identifier,
					value: "i",
					line: 1
				),
				Iterable: new UntypedVariable(
					Name: new Tok(
						typ: TokType.Identifier,
						value: "iter",
						line: 1
					)
				),
				Body: new List<IUntypedAuraStatement>(),
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
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
					value: "fn",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "f",
					line: 1
				),
				new(
					typ: TokType.LeftParen,
					value: "(",
					line: 1
				),
				new(
					typ: TokType.RightParen,
					value: ")",
					line: 1
				),
				new(
					typ: TokType.Arrow,
					value: "->",
					line: 1
				),
				new(
					typ: TokType.Error,
					value: "error",
					line: 1
				),
				new(
					typ: TokType.LeftBrace,
					value: "{",
					line: 1
				),
				new(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedNamedFunction(
				Fn: new Tok(
					typ: TokType.Fn,
					value: "fn",
					line: 1
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "f",
					line: 1
				),
				Params: new List<Param>(),
				Body: new UntypedBlock(
					OpeningBrace: new Tok(
						typ: TokType.LeftBrace,
						value: "{",
						line: 1
					),
					Statements: new List<IUntypedAuraStatement>(),
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}",
						line: 1
					)
				),
				ReturnType: new List<AuraType> { new AuraError() },
				Public: Visibility.Private
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
					value: "fn",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "f",
					line: 1
				),
				new(
					typ: TokType.LeftParen,
					value: "(",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "i",
					line: 1
				),
				new(
					typ: TokType.Colon,
					value: ":",
					line: 1
				),
				new(
					typ: TokType.Int,
					value: "int",
					line: 1
				),
				new(
					typ: TokType.Equal,
					value: "=",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "var",
					line: 1
				),
				new(
					typ: TokType.RightParen,
					value: ")",
					line: 1
				),
				new(
					typ: TokType.LeftBrace,
					value: "{",
					line: 1
				),
				new(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
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
					value: "fn",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "f",
					line: 1
				),
				new(
					typ: TokType.LeftParen,
					value: "(",
					line: 1
				),
				new(
					typ: TokType.RightParen,
					value: ")",
					line: 1
				),
				new(
					typ: TokType.LeftBrace,
					value: "{",
					line: 1
				),
				new(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedNamedFunction(
				Fn: new Tok(
					typ: TokType.Fn,
					value: "fn",
					line: 1
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "f",
					line: 1
				),
				Params: new List<Param>(),
				Body: new UntypedBlock(
					OpeningBrace: new Tok(
						typ: TokType.LeftBrace,
						value: "{",
						line: 1
					),
					Statements: new List<IUntypedAuraStatement>(),
					ClosingBrace: new Tok(
						typ: TokType.RightBrace,
						value: "}",
						line: 1
					)
				),
				ReturnType: null,
				Public: Visibility.Private
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
					value: "fn",
					line: 1
				),
				new(
					typ: TokType.LeftParen,
					value: "(",
					line: 1
				),
				new(
					typ: TokType.RightParen,
					value: ")",
					line: 1
				),
				new(
					typ: TokType.LeftBrace,
					value: "{",
					line: 1
				),
				new(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedExpressionStmt(
				Expression: new UntypedAnonymousFunction(
					Fn: new Tok(
						typ: TokType.Fn,
						value: "fn",
						line: 1
					),
					Params: new List<Param>(),
					Body: new UntypedBlock(
						OpeningBrace: new Tok(
							typ: TokType.LeftBrace,
							value: "{",
							line: 1
						),
						Statements: new List<IUntypedAuraStatement>(),
						ClosingBrace: new Tok(
							typ: TokType.RightBrace,
							value: "}",
							line: 1
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
					value: "let",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "i",
					line: 1
				),
				new(
					typ: TokType.Colon,
					value: ":",
					line: 1
				),
				new(
					typ: TokType.Int,
					value: "int",
					line: 1
				),
				new(
					typ: TokType.Equal,
					value: "=",
					line: 1
				),
				new(
					typ: TokType.IntLiteral,
					value: "5",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedLet(
				Let: new Tok(
					typ: TokType.Let,
					value: "let",
					line: 1
				),
				Names: new List<Tok>
				{
					new(
						typ: TokType.Identifier,
						value: "i",
						line: 1
					)
				},
				NameTyps: new List<AuraType?> { new AuraInt() },
				Mutable: false,
				Initializer: new IntLiteral(
					Int: new Tok(
						typ: TokType.IntLiteral,
						value: "5",
						line: 1
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
					value: "i",
					line: 1
				),
				new(
					typ: TokType.ColonEqual,
					value: ":=",
					line: 1
				),
				new(
					typ: TokType.IntLiteral,
					value: "5",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
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
						value: "i",
						line: 1
					)
				},
				NameTyps: new List<AuraType?>(),
				Mutable: false,
				Initializer: new IntLiteral(
					Int: new Tok(
						typ: TokType.IntLiteral,
						value: "5",
						line: 1
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
					value: "mod",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "main",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedMod(
				Mod: new Tok(
					typ: TokType.Mod,
					value: "mod",
					line: 1
				),
				Value: new Tok(
					typ: TokType.Identifier,
					value: "main",
					line: 1
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
					value: "return",
					line: 1
				),
				new(
					typ: TokType.IntLiteral,
					value: "5",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedReturn(
				Return: new Tok(
					typ: TokType.Return,
					value: "return",
					line: 1
				),
				Value: new List<IUntypedAuraExpression>
				{
					new IntLiteral(
						Int: new Tok(
							typ: TokType.IntLiteral,
							value: "5",
							line: 1
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
					value: "interface",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "Greeter",
					line: 1
				),
				new(
					typ: TokType.LeftBrace,
					value: "{",
					line: 1
				),
				new(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedInterface(
				Interface: new Tok(
					typ: TokType.Interface,
					value: "interface",
					line: 1
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "Greeter",
					line: 1
				),
				Methods: new List<AuraNamedFunction>(),
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
				),
				Public: Visibility.Private
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
					value: "interface",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "Greeter",
					line: 1
				),
				new(
					typ: TokType.LeftBrace,
					value: "{",
					line: 1
				),
				new(
					typ: TokType.Fn,
					value: "fn",
					line: 2
				),
				new(
					typ: TokType.Identifier,
					value: "say_hi",
					line: 2
				),
				new(
					typ: TokType.LeftParen,
					value: "(",
					line: 2
				),
				new(
					typ: TokType.RightParen,
					value: ")",
					line: 2
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 2
				),
				new(
					typ: TokType.RightBrace,
					value: "}",
					line: 3
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 3
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 3
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedInterface(
				Interface: new Tok(
					typ: TokType.Interface,
					value: "interface",
					line: 1
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "Greeter",
					line: 1
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
					value: "}",
					line: 3
				)
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
					value: "interface",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "Greeter",
					line: 1
				),
				new(
					typ: TokType.LeftBrace,
					value: "{",
					line: 1
				),
				new(
					typ: TokType.Fn,
					value: "fn",
					line: 2
				),
				new(
					typ: TokType.Identifier,
					value: "say_hi",
					line: 2
				),
				new(
					typ: TokType.LeftParen,
					value: "(",
					line: 2
				),
				new(
					typ: TokType.Identifier,
					value: "i",
					line: 2
				),
				new(
					typ: TokType.Colon,
					value: ":",
					line: 1
				),
				new(
					typ: TokType.Int,
					value: "int",
					line: 1
				),
				new(
					typ: TokType.RightParen,
					value: ")",
					line: 2
				),
				new(
					typ: TokType.Arrow,
					value: "->",
					line: 2
				),
				new(
					typ: TokType.String,
					value: "string",
					line: 2
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 2
				),
				new(
					typ: TokType.RightBrace,
					value: "}",
					line: 3
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 3
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 3
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedInterface(
				Interface: new Tok(
					typ: TokType.Interface,
					value: "interface",
					line: 1
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "Greeter",
					line: 1
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
										value: "i",
										line: 2
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
					value: "}",
					line: 3
				)
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
					value: "class",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "c",
					line: 1
				),
				new(
					typ: TokType.LeftParen,
					value: "(",
					line: 1
				),
				new(
					typ: TokType.RightParen,
					value: ")",
					line: 1
				),
				new(
					typ: TokType.Colon,
					value: ":",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "IClass",
					line: 1
				),
				new(
					typ: TokType.Comma,
					value: ",",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "IClass2",
					line: 1
				),
				new(
					typ: TokType.LeftBrace,
					value: "{",
					line: 1
				),
				new(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedClass(
				Class: new Tok(
					typ: TokType.Class,
					value: "class",
					line: 1
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "c",
					line: 1
				),
				Params: new List<Param>(),
				Body: new List<IUntypedAuraStatement>(),
				Public: Visibility.Private,
				Implementing: new List<Tok>
				{
					new(
						typ: TokType.Identifier,
						value: "IClass",
						line: 1
					),
					new(
						typ: TokType.Identifier,
						value: "IClass2",
						line: 1
					)
				},
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
				)
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
					value: "class",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "c",
					line: 1
				),
				new(
					typ: TokType.LeftParen,
					value: "(",
					line: 1
				),
				new(
					typ: TokType.RightParen,
					value: ")",
					line: 1
				),
				new(
					typ: TokType.Colon,
					value: ":",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "IClass",
					line: 1
				),
				new(
					typ: TokType.LeftBrace,
					value: "{",
					line: 1
				),
				new(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedClass(
				Class: new Tok(
					typ: TokType.Class,
					value: "class",
					line: 1
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "c",
					line: 1
				),
				Params: new List<Param>(),
				Body: new List<IUntypedAuraStatement>(),
				Public: Visibility.Private,
				Implementing: new List<Tok>
				{
					new(
						typ: TokType.Identifier,
						value: "IClass",
						line: 1
					)
				},
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
				)
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
					value: "class",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "c",
					line: 1
				),
				new(
					typ: TokType.LeftParen,
					value: "(",
					line: 1
				),
				new(
					typ: TokType.RightParen,
					value: ")",
					line: 1
				),
				new(
					typ: TokType.LeftBrace,
					value: "{",
					line: 1
				),
				new(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedClass(
				Class: new Tok(
					typ: TokType.Class,
					value: "class",
					line: 1
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "c",
					line: 1
				),
				Params: new List<Param>(),
				Body: new List<IUntypedAuraStatement>(),
				Public: Visibility.Private,
				Implementing: new List<Tok>(),
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
				)
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
					value: "while",
					line: 1
				),
				new(
					typ: TokType.True,
					value: "true",
					line: 1
				),
				new(
					typ: TokType.LeftBrace,
					value: "{",
					line: 1
				),
				new(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedWhile(
				While: new Tok(
					typ: TokType.While,
					value: "while",
					line: 1
				),
				Condition: new BoolLiteral(
					Bool: new Tok(
						typ: TokType.True,
						value: "true",
						line: 1
					)
				),
				Body: new List<IUntypedAuraStatement>(),
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
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
					value: "import",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "external_pkg",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedImport(
				Import: new Tok(
					typ: TokType.Import,
					value: "import",
					line: 1
				),
				Package: new Tok(
					typ: TokType.Identifier,
					value: "external_pkg",
					line: 1
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
					value: "import",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "external_pkg",
					line: 1
				),
				new(
					typ: TokType.As,
					value: "as",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "ep",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedImport(
				Import: new Tok(
					typ: TokType.Import,
					value: "import",
					line: 1
				),
				Package: new Tok(
					typ: TokType.Identifier,
					value: "external_pkg",
					line: 1
				),
				Alias: new Tok(
					typ: TokType.Identifier,
					value: "ep",
					line: 1
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
					value: "// this is a comment",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedComment(
				Text: new Tok(
					typ: TokType.Comment,
					value: "// this is a comment",
					line: 1
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
					value: "yield",
					line: 1
				),
				new(
					typ: TokType.IntLiteral,
					value: "5",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedYield(
				Yield: new Tok(
					typ: TokType.Yield,
					value: "yield",
					line: 1
				),
				new IntLiteral(
					Int: new Tok(
						typ: TokType.IntLiteral,
						value: "5",
						line: 1
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
					value: "class",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "Greeter",
					line: 1
				),
				new(
					typ: TokType.LeftParen,
					value: "(",
					line: 1
				),
				new(
					typ: TokType.RightParen,
					value: ")",
					line: 1
				),
				new(
					typ: TokType.Colon,
					value: ":",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "IGreeter",
					line: 1
				),
				new(
					typ: TokType.LeftBrace,
					value: "{",
					line: 1
				),
				new(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedClass(
				Class: new Tok(
					typ: TokType.Class,
					value: "class",
					line: 1
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "Greeter",
					line: 1
				),
				Params: new List<Param>(),
				Body: new List<IUntypedAuraStatement>(),
				Public: Visibility.Private,
				Implementing: new List<Tok>
				{
					new(
						typ: TokType.Identifier,
						value: "IGreeter",
						line: 1
					)
				},
				ClosingBrace: new Tok(
					typ: TokType.RightBrace,
					value: "}",
					line: 1
				)
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
					value: "v",
					line: 1
				),
				new(
					typ: TokType.Is,
					value: "is",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "IGreeter",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
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
							value: "v",
							line: 1
						)
					),
					Expected: new Tok(
						typ: TokType.Identifier,
						value: "IGreeter",
						line: 1
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
					value: "check",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "f",
					line: 1
				),
				new(
					typ: TokType.LeftParen,
					value: "(",
					line: 1
				),
				new(
					typ: TokType.RightParen,
					value: ")",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedCheck(
				Check: new Tok(
					typ: TokType.Check,
					value: "check",
					line: 1
				),
				Call: new UntypedCall(
					Callee: new UntypedVariable(
						Name: new Tok(
							typ: TokType.Identifier,
							value: "f",
							line: 1
						)
					),
					Arguments: new List<(Tok?, IUntypedAuraExpression)>(),
					ClosingParen: new Tok(
						typ: TokType.RightParen,
						value: ")",
						line: 1
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
					value: "struct",
					line: 1
				),
				new(
					typ: TokType.Identifier,
					value: "s",
					line: 1
				),
				new(
					typ: TokType.LeftParen,
					value: "(",
					line: 1
				),
				new(
					typ: TokType.RightParen,
					value: ")",
					line: 1
				),
				new(
					typ: TokType.Semicolon,
					value: ";",
					line: 1
				),
				new(
					typ: TokType.Eof,
					value: "eof",
					line: 1
				)
			}
		);
		MakeAssertions(
			untypedAst: untypedAst,
			expected: new UntypedStruct(
				Struct: new Tok(
					typ: TokType.Struct,
					value: "struct",
					line: 1
				),
				Name: new Tok(
					typ: TokType.Identifier,
					value: "s",
					line: 1
				),
				Params: new List<Param>(),
				ClosingParen: new Tok(
					typ: TokType.RightParen,
					value: ")",
					line: 1
				)
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
				value: "\n",
				line: 1
			)
		);
		tokens.Insert(
			index: 0,
			item: new Tok(
				typ: TokType.Semicolon,
				value: ";",
				line: 1
			)
		);
		tokens.Insert(
			index: 0,
			item: new Tok(
				typ: TokType.Identifier,
				value: "main",
				line: 1
			)
		);
		tokens.Insert(
			index: 0,
			item: new Tok(
				typ: TokType.Mod,
				value: "mod",
				line: 1
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

