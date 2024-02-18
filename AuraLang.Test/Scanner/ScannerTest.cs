using AuraLang.Exceptions.Scanner;
using AuraLang.Location;
using AuraLang.Scanner;
using AuraLang.Token;
using Range = AuraLang.Location.Range;

namespace AuraLang.Test.Scanner;

public class ScannerTest
{
	private readonly Position _startPosition = new(
		character: 0,
		line: 1
	);
	private readonly Range _singleCharRange = new(
		start: new Position(
			character: 0,
			line: 1
		),
		end: new Position(
			character: 1,
			line: 1
		)
	);
	private readonly Range _doubleCharRange = new(
		start: new Position(
			character: 0,
			line: 1
		),
		end: new Position(
			character: 2,
			line: 1
		)
	);

	[Test]
	public void TestScan_LeftParen()
	{
		var tokens = ArrangeAndAct("(");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.LeftParen,
				value: "(",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_RightParen()
	{
		var tokens = ArrangeAndAct(")");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.RightParen,
				value: ")",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_LeftBrace()
	{
		var tokens = ArrangeAndAct("{");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.LeftBrace,
				value: "{",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_RightBrace()
	{
		var tokens = ArrangeAndAct("}");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.RightBrace,
				value: "}",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_LeftBracket()
	{
		var tokens = ArrangeAndAct("[");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.LeftBracket,
				value: "[",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_RightBracket()
	{
		var tokens = ArrangeAndAct("]");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.RightBracket,
				value: "]",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Equal()
	{
		var tokens = ArrangeAndAct("=");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Equal,
				value: "=",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_EqualEqual()
	{
		var tokens = ArrangeAndAct("==");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.EqualEqual,
				value: "==",
				range: _doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Plus()
	{
		var tokens = ArrangeAndAct("+");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Plus,
				value: "+",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_PlusPlus()
	{
		var tokens = ArrangeAndAct("++");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.PlusPlus,
				value: "++",
				range: _doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_PlusEqual()
	{
		var tokens = ArrangeAndAct("+=");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.PlusEqual,
				value: "+=",
				range: _doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Minus()
	{
		var tokens = ArrangeAndAct("-");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Minus,
				value: "-",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_MinusMinus()
	{
		var tokens = ArrangeAndAct("--");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.MinusMinus,
				value: "--",
				range: _doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_MinusEqual()
	{
		var tokens = ArrangeAndAct("-=");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.MinusEqual,
				value: "-=",
				range: _doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Slash()
	{
		var tokens = ArrangeAndAct("/");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Slash,
				value: "/",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_SlashEqual()
	{
		var tokens = ArrangeAndAct("/=");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.SlashEqual,
				value: "/=",
				range: _doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_SingleLineComment()
	{
		var tokens = ArrangeAndAct("// comment");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Comment,
				value: "// comment",
				range: new Range(
					start: new Position(
						character: 0,
						line: 1
					),
					end: new Position(
						character: 11,
						line: 1
					)
				)
			)
		);
	}

	[Test]
	public void TestScan_MultiLineComment()
	{
		var tokens = ArrangeAndAct("/* multi-line\ncomment*/");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 4,
			expected: new Tok[]{
				new(
					typ: TokType.Comment,
					value:"/* multi-line",
					range: new Range(
						start: _startPosition,
						end: new Position(
							character: 14,
							line: 1
						)
					)
				),
				new(
					typ: TokType.Semicolon,
					value:";",
					range: new Range(
						start: new Position(
							character: 13,
							line: 1
						),
						end: new Position(
							character: 14,
							line: 1
						)
					)
				),
				new(
					typ: TokType.Comment,
					value:"comment*/",
					range: new Range(
						start: new Position(
							character: 0,
							line: 2
						),
						end: new Position(
							character: 10,
							line: 2
						)
					)
				)
			}
		);
	}

	[Test]
	public void TestScan_Star()
	{
		var tokens = ArrangeAndAct("*");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Star,
				value: "*",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_StarEqual()
	{
		var tokens = ArrangeAndAct("*=");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.StarEqual,
				value: "*=",
				range: _doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Greater()
	{
		var tokens = ArrangeAndAct(">");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Greater,
				value: ">",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_GreaterEqual()
	{
		var tokens = ArrangeAndAct(">=");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.GreaterEqual,
				value: ">=",
				range: _doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Less()
	{
		var tokens = ArrangeAndAct("<");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Less,
				value: "<",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_LessEqual()
	{
		var tokens = ArrangeAndAct("<=");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.LessEqual,
				value: "<=",
				range: _doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Bang()
	{
		var tokens = ArrangeAndAct("!");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Bang,
				value: "!",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_BangEqual()
	{
		var tokens = ArrangeAndAct("!=");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.BangEqual,
				value: "!=",
				range: _doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_StringLiteral()
	{
		var tokens = ArrangeAndAct("\"string literal\"");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.StringLiteral,
				value: "string literal",
				range: new Range(
					start: _startPosition,
					end: new Position(
						character: 15,
						line: 1
					)
				)
			)
		);
	}

	[Test]
	public void TestScan_CharLiteral()
	{
		var tokens = ArrangeAndAct("'a'");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.CharLiteral,
				value: "a",
				range: new Range(
					start: _startPosition,
					end: new Position(
						character: 3,
						line: 1
					)
				)
			)
		);
	}

	[Test]
	public void TestScan_Colon()
	{
		var tokens = ArrangeAndAct(":");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Colon,
				value: ":",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_ColonEqual()
	{
		var tokens = ArrangeAndAct(":=");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.ColonEqual,
				value: ":=",
				range: _doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Semicolon()
	{
		var tokens = ArrangeAndAct(";");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Semicolon,
				value: ";",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Dot()
	{
		var tokens = ArrangeAndAct(".");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Dot,
				value: ".",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Comma()
	{
		var tokens = ArrangeAndAct(",");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Comma,
				value: ",",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Whitespace_Space()
	{
		var tokens = ArrangeAndAct("    ");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 1,
			expected: Array.Empty<Tok>()
		);
	}

	[Test]
	public void TestScan_Whitespace_Tab()
	{
		var tokens = ArrangeAndAct("\t");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 1,
			expected: Array.Empty<Tok>()
		);
	}

	[Test]
	public void TestScan_Yield()
	{
		var tokens = ArrangeAndAct("yield");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Yield,
				value: "yield",
				range: new Range(
					start: _startPosition,
					end: new Position(
						character: 5,
						line: 1
					)
				)
			)
		);
	}

	[Test]
	public void TestScan_Newline()
	{
		var tokens = ArrangeAndAct("\n");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Newline,
				value: "\n",
				range: _singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_InvalidCharacter()
	{
		ArrangeAndAct_Invalid("?", typeof(InvalidCharacterException));
	}

	[Test]
	public void TestScan_UnterminatedString()
	{
		ArrangeAndAct_Invalid("\"unterminated string", typeof(UnterminatedStringException));
	}

	[Test]
	public void TestScan_UnterminatedChar()
	{
		ArrangeAndAct_Invalid("'c", typeof(UnterminatedCharException));
	}

	[Test]
	public void TestScan_TooLongChar()
	{
		ArrangeAndAct_Invalid("'aa'", typeof(CharLengthGreaterThanOneException));
	}

	[Test]
	public void TestScan_EmptyChar()
	{
		ArrangeAndAct_Invalid("''", typeof(EmptyCharException));
	}

	private static List<Tok> ArrangeAndAct(string source)
	{
		// Arrange
		var scanner = new AuraScanner(source, "Test");
		// Act
		return scanner.ScanTokens();
	}

	[Test]
	public void TestScan_Interface()
	{
		var tokens = ArrangeAndAct("interface");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Interface,
				value: "interface",
				range: new Range(
					start: _startPosition,
					end: new Position(
						character: 9,
						line: 1
					)
				)
			)
		);
	}

	[Test]
	public void TestScan_ClassImplementingInterface()
	{
		var tokens = ArrangeAndAct("class Greeter() : IGreeter");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 7,
			expected: new Tok[]
			{
				new(
					typ: TokType.Class,
					value:"class",
					range: new Range(
						start: _startPosition,
						end: new Position(
							character: 5,
							line: 1
						)
					)
				),
				new(
					typ: TokType.Identifier,
					value:"Greeter",
					range: new Range(
						start: new Position(
							character: 6,
							line: 1
						),
						end: new Position(
							character: 13,
							line: 1
						)
					)
				),
				new(
					typ: TokType.LeftParen,
					value:"(",
					range: new Range(
						start: new Position(
							character: 13,
							line: 1
						),
						end: new Position(
							character: 14,
							line: 1
						)
					)
				),
				new(
					typ: TokType.RightParen,
					value:")",
					range: new Range(
						start: new Position(
							character: 14,
							line: 1
						),
						end: new Position(
							character: 15,
							line: 1
						)
					)
				),
				new(
					typ: TokType.Colon,
					value:":",
					range: new Range(
						start: new Position(
							character: 16,
							line: 1
						),
						end: new Position(
							character: 17,
							line: 1
						)
					)
				),
				new(
					typ: TokType.Identifier,
					value:"IGreeter",
					range: new Range(
						start: new Position(
							character: 18,
							line: 1
						),
						end: new Position(
							character: 26,
							line: 1
						)
					)
				)
			}
		);

	}

	[Test]
	public void TestScan_Is()
	{
		var tokens = ArrangeAndAct("is");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Is,
				value: "is",
				range: _doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Error()
	{
		var tokens = ArrangeAndAct("error");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Error,
				value: "error",
				range: new Range(
					start: _startPosition,
					end: new Position(
						character: 5,
						line: 1
					)
				)
			)
		);
	}

	[Test]
	public void TestScan_Check()
	{
		var tokens = ArrangeAndAct("check");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Check,
				value: "check",
				range: new Range(
					start: _startPosition,
					end: new Position(
						character: 5,
						line: 1
					)
				)
			)
		);
	}

	[Test]
	public void TestScan_Struct()
	{
		var tokens = ArrangeAndAct("struct");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Struct,
				value: "struct",
				range: new Range(
					start: _startPosition,
					end: new Position(
						character: 6,
						line: 1
					)
				)
			)
		);
	}

	[Test]
	public void TestScan_Result()
	{
		var tokens = ArrangeAndAct("result");
		MakeAssertions_Valid(
			tokens: tokens,
			count: 2,
			expected: new Tok(
				typ: TokType.Result,
				value: "result",
				range: new Range(
					start: _startPosition,
					end: new Position(
						character: 6,
						line: 1
					)
				)
			)
		);
	}

	private static void ArrangeAndAct_Invalid(string source, Type expected)
	{
		// Arrange
		var scanner = new AuraScanner(source, "Test");
		try
		{
			scanner.ScanTokens();
			Assert.Fail();
		}
		catch (ScannerExceptionContainer e)
		{
			Assert.That(e.Exs.First(), Is.TypeOf(expected));
		}
	}

	private static void MakeAssertions_Valid(List<Tok> tokens, int count, params Tok[] expected)
	{
		Assert.Multiple(() =>
		{
			Assert.That(tokens, Is.Not.Null);
			Assert.That(tokens, Has.Count.EqualTo(count));
		});
		for (int i = 0; i < expected.Length; i++)
		{
			Assert.That(tokens[i], Is.EqualTo(expected[i]));
		}
	}
}
