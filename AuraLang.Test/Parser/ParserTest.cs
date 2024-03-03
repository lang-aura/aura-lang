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
			new List<Tok>
			{
				new(
					TokType.Identifier,
					"i"
				),
				new(
					TokType.Equal,
					"="
				),
				new(
					TokType.IntLiteral,
					"5"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedAssignment(
					new Tok(
						TokType.Identifier,
						"i"
					),
					new IntLiteral(
						new Tok(
							TokType.IntLiteral,
							"5"
						)
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_Binary()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.IntLiteral,
					"1"
				),
				new(
					TokType.Plus,
					"+"
				),
				new(
					TokType.IntLiteral,
					"2"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedBinary(
					new IntLiteral(
						new Tok(
							TokType.IntLiteral,
							"1"
						)
					),
					new Tok(
						TokType.Plus,
						"+"
					),
					new IntLiteral(
						new Tok(
							TokType.IntLiteral,
							"2"
						)
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_Block()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.IntLiteral,
					"1"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedBlock(
					new Tok(
						TokType.LeftBrace,
						"{"
					),
					new List<IUntypedAuraStatement>
					{
						new UntypedExpressionStmt(
							new IntLiteral(
								new Tok(
									TokType.IntLiteral,
									"1"
								)
							)
						)
					},
					new Tok(
						TokType.RightBrace,
						"}"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_Call()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Identifier,
					"f"
				),
				new(
					TokType.LeftParen,
					"("
				),
				new(
					TokType.RightParen,
					")"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedCall(
					new UntypedVariable(
						new Tok(
							TokType.Identifier,
							"f"
						)
					),
					new List<(Tok?, IUntypedAuraExpression)>(),
					new Tok(
						TokType.RightParen,
						")"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_Get()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Identifier,
					"greeter"
				),
				new(
					TokType.Dot,
					"."
				),
				new(
					TokType.Identifier,
					"name"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedGet(
					new UntypedVariable(
						new Tok(
							TokType.Identifier,
							"greeter"
						)
					),
					new Tok(
						TokType.Identifier,
						"name"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_GetIndex()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Identifier,
					"collection"
				),
				new(
					TokType.LeftBracket,
					"["
				),
				new(
					TokType.IntLiteral,
					"0"
				),
				new(
					TokType.RightBracket,
					"]"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedGetIndex(
					new UntypedVariable(
						new Tok(
							TokType.Identifier,
							"collection"
						)
					),
					new IntLiteral(
						new Tok(
							TokType.IntLiteral,
							"0"
						)
					),
					new Tok(
						TokType.RightBracket,
						"]"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_GetIndex_Empty()
	{
		ArrangeAndAct_Invalid(
			new List<Tok>
			{
				new(
					TokType.Identifier,
					"collection"
				),
				new(
					TokType.LeftBracket,
					"["
				),
				new(
					TokType.RightBracket,
					"]"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			},
			typeof(PostfixIndexCannotBeEmptyException)
		);
	}

	[Test]
	public void TestParse_GetIndexRange()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Identifier,
					"collection"
				),
				new(
					TokType.LeftBracket,
					"["
				),
				new(
					TokType.IntLiteral,
					"0"
				),
				new(
					TokType.Colon,
					":"
				),
				new(
					TokType.IntLiteral,
					"1"
				),
				new(
					TokType.RightBracket,
					"]"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedGetIndexRange(
					new UntypedVariable(
						new Tok(
							TokType.Identifier,
							"collection"
						)
					),
					new IntLiteral(
						new Tok(
							TokType.IntLiteral,
							"0"
						)
					),
					new IntLiteral(
						new Tok(
							TokType.IntLiteral,
							"1"
						)
					),
					new Tok(
						TokType.RightBracket,
						"]"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_Grouping()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.LeftParen,
					"("
				),
				new(
					TokType.IntLiteral,
					"1"
				),
				new(
					TokType.RightParen,
					")"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedGrouping(
					new Tok(
						TokType.LeftParen,
						"("
					),
					new IntLiteral(
						new Tok(
							TokType.IntLiteral,
							"1"
						)
					),
					new Tok(
						TokType.RightParen,
						")"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_If()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.If,
					"if"
				),
				new(
					TokType.True,
					"true"
				),
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.Return,
					"return"
				),
				new(
					TokType.IntLiteral,
					"1"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedIf(
					new Tok(
						TokType.If,
						"if"
					),
					new BoolLiteral(
						new Tok(
							TokType.True,
							"true"
						)
					),
					new UntypedBlock(
						new Tok(
							TokType.LeftBrace,
							"{"
						),
						new List<IUntypedAuraStatement>
						{
							new UntypedReturn(
								new Tok(
									TokType.Return,
									"return"
								),
								new List<IUntypedAuraExpression>
								{
									new IntLiteral(
										new Tok(
											TokType.IntLiteral,
											"1"
										)
									)
								}
							)
						},
						new Tok(
							TokType.RightBrace,
							"}"
						)
					),
					null
				)
			)
		);
	}

	[Test]
	public void TestParse_IntLiteral()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.IntLiteral,
					"5"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new IntLiteral(
					new Tok(
						TokType.IntLiteral,
						"5"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_FloatLiteral()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.FloatLiteral,
					"5.0"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new FloatLiteral(
					new Tok(
						TokType.FloatLiteral,
						"5.0"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_StringLiteral()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.StringLiteral,
					"Hello"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new StringLiteral(
					new Tok(
						TokType.StringLiteral,
						"Hello"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_ListLiteral()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.LeftBracket,
					"["
				),
				new(
					TokType.Int,
					"int"
				),
				new(
					TokType.RightBracket,
					"]"
				),
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.IntLiteral,
					"5"
				),
				new(
					TokType.Comma,
					","
				),
				new(
					TokType.IntLiteral,
					"6"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new ListLiteral<ITypedAuraExpression>(
					new Tok(
						TokType.LeftBracket,
						"["
					),
					new List<ITypedAuraExpression>
					{
						new IntLiteral(
							new Tok(
								TokType.IntLiteral,
								"5"
							)
						),
						new IntLiteral(
							new Tok(
								TokType.IntLiteral,
								"6"
							)
						)
					},
					new AuraInt(),
					new Tok(
						TokType.RightBrace,
						"}"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_MapLiteral_Get()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Map,
					"map"
				),
				new(
					TokType.LeftBracket,
					"["
				),
				new(
					TokType.String,
					"string"
				),
				new(
					TokType.Colon,
					":"
				),
				new(
					TokType.Int,
					"int"
				),
				new(
					TokType.RightBracket,
					"]"
				),
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.StringLiteral,
					"Hello"
				),
				new(
					TokType.Colon,
					":"
				),
				new(
					TokType.IntLiteral,
					"1"
				),
				new(
					TokType.Comma,
					","
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.StringLiteral,
					"World"
				),
				new(
					TokType.Colon,
					":"
				),
				new(
					TokType.IntLiteral,
					"2"
				),
				new(
					TokType.Comma,
					","
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.LeftBracket,
					"["
				),
				new(
					TokType.StringLiteral,
					"Hello"
				),
				new(
					TokType.RightBracket,
					"]"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedGetIndex(
					new MapLiteral<ITypedAuraExpression, ITypedAuraExpression>(
						new Tok(
							TokType.Map,
							"map"
						),
						new Dictionary<ITypedAuraExpression, ITypedAuraExpression>
						{
							{
								new StringLiteral(
									new Tok(
										TokType.StringLiteral,
										"Hello"
									)
								),
								new IntLiteral(
									new Tok(
										TokType.IntLiteral,
										"1"
									)
								)
							},
							{
								new StringLiteral(
									new Tok(
										TokType.StringLiteral,
										"World"
									)
								),
								new IntLiteral(
									new Tok(
										TokType.IntLiteral,
										"2"
									)
								)
							}
						},
						new AuraString(),
						new AuraInt(),
						new Tok(
							TokType.RightBrace,
							"}"
						)
					),
					new StringLiteral(
						new Tok(
							TokType.StringLiteral,
							"Hello"
						)
					),
					new Tok(
						TokType.RightBracket,
						"]"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_MapLiteral()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Map,
					"map"
				),
				new(
					TokType.LeftBracket,
					"["
				),
				new(
					TokType.String,
					"string"
				),
				new(
					TokType.Colon,
					":"
				),
				new(
					TokType.Int,
					"int"
				),
				new(
					TokType.RightBracket,
					"]"
				),
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.StringLiteral,
					"Hello"
				),
				new(
					TokType.Colon,
					":"
				),
				new(
					TokType.IntLiteral,
					"1"
				),
				new(
					TokType.Comma,
					","
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.StringLiteral,
					"World"
				),
				new(
					TokType.Colon,
					":"
				),
				new(
					TokType.IntLiteral,
					"2"
				),
				new(
					TokType.Comma,
					","
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new MapLiteral<ITypedAuraExpression, ITypedAuraExpression>(
					new Tok(
						TokType.Map,
						"map"
					),
					new Dictionary<ITypedAuraExpression, ITypedAuraExpression>
					{
						{
							new StringLiteral(
								new Tok(
									TokType.StringLiteral,
									"Hello"
								)
							),
							new IntLiteral(
								new Tok(
									TokType.IntLiteral,
									"1"
								)
							)
						},
						{
							new StringLiteral(
								new Tok(
									TokType.StringLiteral,
									"World"
								)
							),
							new IntLiteral(
								new Tok(
									TokType.IntLiteral,
									"2"
								)
							)
						}
					},
					new AuraString(),
					new AuraInt(),
					new Tok(
						TokType.RightBrace,
						"}"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_BoolLiteral()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.True,
					"true"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new BoolLiteral(
					new Tok(
						TokType.True,
						"true"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_Nil()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Nil,
					"nil"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedNil(
					new Tok(
						TokType.Nil,
						"nil"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_CharLiteral()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.CharLiteral,
					"c"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new CharLiteral(
					new Tok(
						TokType.CharLiteral,
						"c"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_Logical()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.True,
					"true"
				),
				new(
					TokType.Or,
					"or"
				),
				new(
					TokType.False,
					"false"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedLogical(
					new BoolLiteral(
						new Tok(
							TokType.True,
							"true"
						)
					),
					new Tok(
						TokType.Or,
						"or"
					),
					new BoolLiteral(
						new Tok(
							TokType.False,
							"false"
						)
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_Set()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Identifier,
					"greeter"
				),
				new(
					TokType.Dot,
					"."
				),
				new(
					TokType.Identifier,
					"name"
				),
				new(
					TokType.Equal,
					"="
				),
				new(
					TokType.StringLiteral,
					"Bob"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedSet(
					new UntypedVariable(
						new Tok(
							TokType.Identifier,
							"greeter"
						)
					),
					new Tok(
						TokType.Identifier,
						"name"
					),
					new StringLiteral(
						new Tok(
							TokType.StringLiteral,
							"Bob"
						)
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_This()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.This,
					"this"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedThis(
					new Tok(
						TokType.This,
						"this"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_Unary_Bang()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Bang,
					"!"
				),
				new(
					TokType.True,
					"true"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedUnary(
					new Tok(
						TokType.Bang,
						"!"
					),
					new BoolLiteral(
						new Tok(
							TokType.True,
							"true"
						)
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_Unary_Minus()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Minus,
					"-"
				),
				new(
					TokType.IntLiteral,
					"5"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedUnary(
					new Tok(
						TokType.Minus,
						"-"
					),
					new IntLiteral(
						new Tok(
							TokType.IntLiteral,
							"5"
						)
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_Variable()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Identifier,
					"variable"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedVariable(
					new Tok(
						TokType.Identifier,
						"variable"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_Defer()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Defer,
					"defer"
				),
				new(
					TokType.Identifier,
					"f"
				),
				new(
					TokType.LeftParen,
					"("
				),
				new(
					TokType.RightParen,
					")"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedDefer(
				new Tok(
					TokType.Defer,
					"defer"
				),
				new UntypedCall(
					new UntypedVariable(
						new Tok(
							TokType.Identifier,
							"f"
						)
					),
					new List<(Tok?, IUntypedAuraExpression)>(),
					new Tok(
						TokType.RightParen,
						")"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_For()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.For,
					"for"
				),
				new(
					TokType.Identifier,
					"i"
				),
				new(
					TokType.ColonEqual,
					":="
				),
				new(
					TokType.IntLiteral,
					"0"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Identifier,
					"i"
				),
				new(
					TokType.Less,
					"<"
				),
				new(
					TokType.IntLiteral,
					"10"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Identifier,
					"i"
				),
				new(
					TokType.PlusPlus,
					"++"
				),
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedFor(
				new Tok(
					TokType.For,
					"for"
				),
				new UntypedLet(
					null,
					new List<Tok>
					{
						new(
							TokType.Identifier,
							"i"
						)
					},
					new List<AuraType>(),
					false,
					new IntLiteral(
						new Tok(
							TokType.IntLiteral,
							"0"
						)
					)
				),
				new UntypedBinary(
					new UntypedVariable(
						new Tok(
							TokType.Identifier,
							"i"
						)
					),
					new Tok(
						TokType.Less,
						"<"
					),
					new IntLiteral(
						new Tok(
							TokType.IntLiteral,
							"10"
						)
					)
				),
				new UntypedPlusPlusIncrement(
					new UntypedVariable(
						new Tok(
							TokType.Identifier,
							"i"
						)
					),
					new Tok(
						TokType.PlusPlus,
						"++"
					)
				),
				new List<IUntypedAuraStatement>(),
				new Tok(
					TokType.RightBrace,
					"}"
				)
			)
		);
	}

	[Test]
	public void TestParse_ForEach()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.ForEach,
					"foreach"
				),
				new(
					TokType.Identifier,
					"i"
				),
				new(
					TokType.In,
					"in"
				),
				new(
					TokType.Identifier,
					"iter"
				),
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedForEach(
				new Tok(
					TokType.ForEach,
					"foreach"
				),
				new Tok(
					TokType.Identifier,
					"i"
				),
				new UntypedVariable(
					new Tok(
						TokType.Identifier,
						"iter"
					)
				),
				new List<IUntypedAuraStatement>(),
				new Tok(
					TokType.RightBrace,
					"}"
				)
			)
		);
	}

	[Test]
	public void TestParse_NamedFunction_ReturnError()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Fn,
					"fn"
				),
				new(
					TokType.Identifier,
					"f"
				),
				new(
					TokType.LeftParen,
					"("
				),
				new(
					TokType.RightParen,
					")"
				),
				new(
					TokType.Arrow,
					"->"
				),
				new(
					TokType.Error,
					"error"
				),
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedNamedFunction(
				new Tok(
					TokType.Fn,
					"fn"
				),
				new Tok(
					TokType.Identifier,
					"f"
				),
				new List<Param>(),
				new UntypedBlock(
					new Tok(
						TokType.LeftBrace,
						"{"
					),
					new List<IUntypedAuraStatement>(),
					new Tok(
						TokType.RightBrace,
						"}"
					)
				),
				new List<AuraType> { new AuraError() },
				Visibility.Private,
				null
			)
		);
	}

	[Test]
	public void TestParse_NamedFunction_ParamDefaultValue_Invalid()
	{
		ArrangeAndAct_Invalid(
			new List<Tok>
			{
				new(
					TokType.Fn,
					"fn"
				),
				new(
					TokType.Identifier,
					"f"
				),
				new(
					TokType.LeftParen,
					"("
				),
				new(
					TokType.Identifier,
					"i"
				),
				new(
					TokType.Colon,
					":"
				),
				new(
					TokType.Int,
					"int"
				),
				new(
					TokType.Equal,
					"="
				),
				new(
					TokType.Identifier,
					"var"
				),
				new(
					TokType.RightParen,
					")"
				),
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			},
			typeof(ParameterDefaultValueMustBeALiteralException)
		);
	}

	[Test]
	public void TestParse_NamedFunction_NoParams_NoReturnType_NoBody()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Fn,
					"fn"
				),
				new(
					TokType.Identifier,
					"f"
				),
				new(
					TokType.LeftParen,
					"("
				),
				new(
					TokType.RightParen,
					")"
				),
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedNamedFunction(
				new Tok(
					TokType.Fn,
					"fn"
				),
				new Tok(
					TokType.Identifier,
					"f"
				),
				new List<Param>(),
				new UntypedBlock(
					new Tok(
						TokType.LeftBrace,
						"{"
					),
					new List<IUntypedAuraStatement>(),
					new Tok(
						TokType.RightBrace,
						"}"
					)
				),
				null,
				Visibility.Private,
				null
			)
		);
	}

	[Test]
	public void TestParse_AnonymousFunction_NoParams_NoReturnType_NoBody()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Fn,
					"fn"
				),
				new(
					TokType.LeftParen,
					"("
				),
				new(
					TokType.RightParen,
					")"
				),
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedAnonymousFunction(
					new Tok(
						TokType.Fn,
						"fn"
					),
					new List<Param>(),
					new UntypedBlock(
						new Tok(
							TokType.LeftBrace,
							"{"
						),
						new List<IUntypedAuraStatement>(),
						new Tok(
							TokType.RightBrace,
							"}"
						)
					),
					null
				)
			)
		);
	}

	[Test]
	public void TestParse_Let_Long()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Let,
					"let"
				),
				new(
					TokType.Identifier,
					"i"
				),
				new(
					TokType.Colon,
					":"
				),
				new(
					TokType.Int,
					"int"
				),
				new(
					TokType.Equal,
					"="
				),
				new(
					TokType.IntLiteral,
					"5"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedLet(
				new Tok(
					TokType.Let,
					"let"
				),
				new List<Tok>
				{
					new(
						TokType.Identifier,
						"i"
					)
				},
				new List<AuraType> { new AuraInt() },
				false,
				new IntLiteral(
					new Tok(
						TokType.IntLiteral,
						"5"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_Let_Short()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Identifier,
					"i"
				),
				new(
					TokType.ColonEqual,
					":="
				),
				new(
					TokType.IntLiteral,
					"5"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedLet(
				null,
				new List<Tok>
				{
					new(
						TokType.Identifier,
						"i"
					)
				},
				new List<AuraType>(),
				false,
				new IntLiteral(
					new Tok(
						TokType.IntLiteral,
						"5"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_Mod()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Mod,
					"mod"
				),
				new(
					TokType.Identifier,
					"main"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedMod(
				new Tok(
					TokType.Mod,
					"mod"
				),
				new Tok(
					TokType.Identifier,
					"main"
				)
			)
		);
	}

	[Test]
	public void TestParse_Return()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Return,
					"return"
				),
				new(
					TokType.IntLiteral,
					"5"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedReturn(
				new Tok(
					TokType.Return,
					"return"
				),
				new List<IUntypedAuraExpression>
				{
					new IntLiteral(
						new Tok(
							TokType.IntLiteral,
							"5"
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
			new List<Tok>
			{
				new(
					TokType.Interface,
					"interface"
				),
				new(
					TokType.Identifier,
					"Greeter"
				),
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedInterface(
				new Tok(
					TokType.Interface,
					"interface"
				),
				new Tok(
					TokType.Identifier,
					"Greeter"
				),
				new List<UntypedFunctionSignature>(),
				ClosingBrace: new Tok(
					TokType.RightBrace,
					"}"
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
			new List<Tok>
			{
				new(
					TokType.Interface,
					"interface"
				),
				new(
					TokType.Identifier,
					"Greeter"
				),
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.Fn,
					"fn"
				),
				new(
					TokType.Identifier,
					"say_hi"
				),
				new(
					TokType.LeftParen,
					"("
				),
				new(
					TokType.RightParen,
					")"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedInterface(
				new Tok(
					TokType.Interface,
					"interface"
				),
				new Tok(
					TokType.Identifier,
					"Greeter"
				),
				new List<UntypedFunctionSignature>
				{
					new(
						null,
						new Tok(TokType.Fn, "fn"),
						new Tok(TokType.Identifier, "say_hi"),
						new List<Param>(),
						new Tok(TokType.RightParen, ")"),
						new AuraNil(),
						null
					)
				},
				Visibility.Private,
				new Tok(
					TokType.RightBrace,
					"}"
				),
				null
			)
		);
	}

	[Test]
	public void TestParse_Interface_OneMethod()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Interface,
					"interface"
				),
				new(
					TokType.Identifier,
					"Greeter"
				),
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.Fn,
					"fn"
				),
				new(
					TokType.Identifier,
					"say_hi"
				),
				new(
					TokType.LeftParen,
					"("
				),
				new(
					TokType.Identifier,
					"i"
				),
				new(
					TokType.Colon,
					":"
				),
				new(
					TokType.Int,
					"int"
				),
				new(
					TokType.RightParen,
					")"
				),
				new(
					TokType.Arrow,
					"->"
				),
				new(
					TokType.String,
					"string"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedInterface(
				new Tok(
					TokType.Interface,
					"interface"
				),
				new Tok(
					TokType.Identifier,
					"Greeter"
				),
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
						new AuraString(),
						null
					)
				},
				Visibility.Private,
				new Tok(
					TokType.RightBrace,
					"}"
				),
				null
			)
		);
	}

	[Test]
	public void TestParse_Class_ImplementingTwoInterfaces()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Class,
					"class"
				),
				new(
					TokType.Identifier,
					"c"
				),
				new(
					TokType.LeftParen,
					"("
				),
				new(
					TokType.RightParen,
					")"
				),
				new(
					TokType.Colon,
					":"
				),
				new(
					TokType.Identifier,
					"IClass"
				),
				new(
					TokType.Comma,
					","
				),
				new(
					TokType.Identifier,
					"IClass2"
				),
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedClass(
				new Tok(
					TokType.Class,
					"class"
				),
				new Tok(
					TokType.Identifier,
					"c"
				),
				new List<Param>(),
				new List<IUntypedAuraStatement>(),
				Visibility.Private,
				new List<Tok>
				{
					new(
						TokType.Identifier,
						"IClass"
					),
					new(
						TokType.Identifier,
						"IClass2"
					)
				},
				new Tok(
					TokType.RightBrace,
					"}"
				),
				null
			)
		);
	}

	[Test]
	public void TestParse_Class_ImplementingOneInterface()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Class,
					"class"
				),
				new(
					TokType.Identifier,
					"c"
				),
				new(
					TokType.LeftParen,
					"("
				),
				new(
					TokType.RightParen,
					")"
				),
				new(
					TokType.Colon,
					":"
				),
				new(
					TokType.Identifier,
					"IClass"
				),
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedClass(
				new Tok(
					TokType.Class,
					"class"
				),
				new Tok(
					TokType.Identifier,
					"c"
				),
				new List<Param>(),
				new List<IUntypedAuraStatement>(),
				Visibility.Private,
				new List<Tok>
				{
					new(
						TokType.Identifier,
						"IClass"
					)
				},
				new Tok(
					TokType.RightBrace,
					"}"
				),
				null
			)
		);
	}

	[Test]
	public void TestParse_Class_NoParams_NoMethods()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Class,
					"class"
				),
				new(
					TokType.Identifier,
					"c"
				),
				new(
					TokType.LeftParen,
					"("
				),
				new(
					TokType.RightParen,
					")"
				),
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedClass(
				new Tok(
					TokType.Class,
					"class"
				),
				new Tok(
					TokType.Identifier,
					"c"
				),
				new List<Param>(),
				new List<IUntypedAuraStatement>(),
				Visibility.Private,
				new List<Tok>(),
				new Tok(
					TokType.RightBrace,
					"}"
				),
				null
			)
		);
	}

	[Test]
	public void TestParse_While_EmptyBody()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.While,
					"while"
				),
				new(
					TokType.True,
					"true"
				),
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedWhile(
				new Tok(
					TokType.While,
					"while"
				),
				new BoolLiteral(
					new Tok(
						TokType.True,
						"true"
					)
				),
				new List<IUntypedAuraStatement>(),
				new Tok(
					TokType.RightBrace,
					"}"
				)
			)
		);
	}

	[Test]
	public void TestParse_Import_NoAlias()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Import,
					"import"
				),
				new(
					TokType.Identifier,
					"external_pkg"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedImport(
				new Tok(
					TokType.Import,
					"import"
				),
				new Tok(
					TokType.Identifier,
					"external_pkg"
				),
				null
			)
		);
	}

	[Test]
	public void TestParse_Import_Alias()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Import,
					"import"
				),
				new(
					TokType.Identifier,
					"external_pkg"
				),
				new(
					TokType.As,
					"as"
				),
				new(
					TokType.Identifier,
					"ep"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedImport(
				new Tok(
					TokType.Import,
					"import"
				),
				new Tok(
					TokType.Identifier,
					"external_pkg"
				),
				new Tok(
					TokType.Identifier,
					"ep"
				)
			)
		);
	}

	[Test]
	public void TestParse_Comment()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Comment,
					"// this is a comment"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedComment(
				new Tok(
					TokType.Comment,
					"// this is a comment"
				)
			)
		);
	}

	[Test]
	public void TestParse_Yield()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Yield,
					"yield"
				),
				new(
					TokType.IntLiteral,
					"5"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedYield(
				new Tok(
					TokType.Yield,
					"yield"
				),
				new IntLiteral(
					new Tok(
						TokType.IntLiteral,
						"5"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_ClassImplementingInterface()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Class,
					"class"
				),
				new(
					TokType.Identifier,
					"Greeter"
				),
				new(
					TokType.LeftParen,
					"("
				),
				new(
					TokType.RightParen,
					")"
				),
				new(
					TokType.Colon,
					":"
				),
				new(
					TokType.Identifier,
					"IGreeter"
				),
				new(
					TokType.LeftBrace,
					"{"
				),
				new(
					TokType.RightBrace,
					"}"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedClass(
				new Tok(
					TokType.Class,
					"class"
				),
				new Tok(
					TokType.Identifier,
					"Greeter"
				),
				new List<Param>(),
				new List<IUntypedAuraStatement>(),
				Visibility.Private,
				new List<Tok>
				{
					new(
						TokType.Identifier,
						"IGreeter"
					)
				},
				new Tok(
					TokType.RightBrace,
					"}"
				),
				null
			)
		);
	}

	[Test]
	public void TestParse_Is()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Identifier,
					"v"
				),
				new(
					TokType.Is,
					"is"
				),
				new(
					TokType.Identifier,
					"IGreeter"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedIs(
					new UntypedVariable(
						new Tok(
							TokType.Identifier,
							"v"
						)
					),
					new UntypedInterfacePlaceholder(
						new Tok(
							TokType.Identifier,
							"IGreeter"
						)
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_Check()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Check,
					"check"
				),
				new(
					TokType.Identifier,
					"f"
				),
				new(
					TokType.LeftParen,
					"("
				),
				new(
					TokType.RightParen,
					")"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedCheck(
				new Tok(
					TokType.Check,
					"check"
				),
				new UntypedCall(
					new UntypedVariable(
						new Tok(
							TokType.Identifier,
							"f"
						)
					),
					new List<(Tok?, IUntypedAuraExpression)>(),
					new Tok(
						TokType.RightParen,
						")"
					)
				)
			)
		);
	}

	[Test]
	public void TestParse_Struct()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(
					TokType.Struct,
					"struct"
				),
				new(
					TokType.Identifier,
					"s"
				),
				new(
					TokType.LeftParen,
					"("
				),
				new(
					TokType.RightParen,
					")"
				),
				new(
					TokType.Semicolon,
					";"
				),
				new(
					TokType.Eof,
					"eof"
				)
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedStruct(
				new Tok(
					TokType.Struct,
					"struct"
				),
				new Tok(
					TokType.Identifier,
					"s"
				),
				new List<Param>(),
				new Tok(
					TokType.RightParen,
					")"
				),
				null
			)
		);
	}

	[Test]
	public void TestParse_CallAsIndexExpression_NoArgs()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(TokType.Identifier, "collection"),
				new(TokType.LeftBracket, "["),
				new(TokType.Identifier, "f"),
				new(TokType.LeftParen, "("),
				new(TokType.RightParen, ")"),
				new(TokType.RightBracket, "]"),
				new(TokType.Semicolon, ";"),
				new(TokType.Eof, "eof")
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedGetIndex(
					new UntypedVariable(new Tok(TokType.Identifier, "collection")),
					new UntypedCall(
						new UntypedVariable(new Tok(TokType.Identifier, "f")),
						new List<(Tok?, IUntypedAuraExpression)>(),
						new Tok(TokType.RightParen, ")")
					),
					new Tok(TokType.RightBracket, "]")
				)
			)
		);
	}

	[Test]
	public void TestParse_CallAsIndexExpression()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(TokType.Identifier, "collection"),
				new(TokType.LeftBracket, "["),
				new(TokType.Identifier, "f"),
				new(TokType.LeftParen, "("),
				new(TokType.IntLiteral, "5"),
				new(TokType.RightParen, ")"),
				new(TokType.RightBracket, "]"),
				new(TokType.Semicolon, ";"),
				new(TokType.Eof, "eof")
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedGetIndex(
					new UntypedVariable(new Tok(TokType.Identifier, "collection")),
					new UntypedCall(
						new UntypedVariable(new Tok(TokType.Identifier, "f")),
						new List<(Tok?, IUntypedAuraExpression)>
						{
							(null, new IntLiteral(new Tok(TokType.IntLiteral, "5")))
						},
						new Tok(TokType.RightParen, ")")
					),
					new Tok(TokType.RightBracket, "]")
				)
			)
		);
	}

	[Test]
	public void TestParse_CallAsRangeIndexExpression()
	{
		var untypedAst = ArrangeAndAct(
			new List<Tok>
			{
				new(TokType.Identifier, "collection"),
				new(TokType.LeftBracket, "["),
				new(TokType.Identifier, "f"),
				new(TokType.LeftParen, "("),
				new(TokType.IntLiteral, "5"),
				new(TokType.RightParen, ")"),
				new(TokType.Colon, ":"),
				new(TokType.Identifier, "g"),
				new(TokType.LeftParen, "("),
				new(TokType.StringLiteral, "hello"),
				new(TokType.RightParen, ")"),
				new(TokType.RightBracket, "]"),
				new(TokType.Semicolon, ";"),
				new(TokType.Eof, "eof")
			}
		);
		MakeAssertions(
			untypedAst,
			new UntypedExpressionStmt(
				new UntypedGetIndexRange(
					new UntypedVariable(new Tok(TokType.Identifier, "collection")),
					new UntypedCall(
						new UntypedVariable(new Tok(TokType.Identifier, "f")),
						new List<(Tok?, IUntypedAuraExpression)>
						{
							(null, new IntLiteral(new Tok(TokType.IntLiteral, "5")))
						},
						new Tok(TokType.RightParen, ")")
					),
					new UntypedCall(
						new UntypedVariable(new Tok(TokType.Identifier, "g")),
						new List<(Tok?, IUntypedAuraExpression)>
						{
							(null, new StringLiteral(new Tok(TokType.StringLiteral, "hello")))
						},
						new Tok(TokType.RightParen, ")")
					),
					new Tok(TokType.RightBracket, "]")
				)
			)
		);
	}

	private List<IUntypedAuraStatement> ArrangeAndAct(List<Tok> tokens)
	{
		// Arrange
		tokens.Insert(
			0,
			new Tok(
				TokType.Newline,
				"\n"
			)
		);
		tokens.Insert(
			0,
			new Tok(
				TokType.Semicolon,
				";"
			)
		);
		tokens.Insert(
			0,
			new Tok(
				TokType.Identifier,
				"main"
			)
		);
		tokens.Insert(
			0,
			new Tok(
				TokType.Mod,
				"mod"
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
		Assert.Multiple(
			() =>
			{
				Assert.That(
					untypedAst,
					Is.Not.Null
				);
				Assert.That(
					untypedAst,
					Has.Count.EqualTo(1)
				);

				var expectedJson = JsonConvert.SerializeObject(expected);
				var actualJson = JsonConvert.SerializeObject(untypedAst[0]);
				Assert.That(
					actualJson,
					Is.EqualTo(expectedJson)
				);
			}
		);
	}
}
