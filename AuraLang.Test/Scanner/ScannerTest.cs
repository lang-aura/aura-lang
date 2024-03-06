using AuraLang.Exceptions.Scanner;
using AuraLang.Location;
using AuraLang.Scanner;
using AuraLang.Token;
using Range = AuraLang.Location.Range;

namespace AuraLang.Test.Scanner;

public class ScannerTest
{
	private readonly Position _startPosition = new(
		0,
		0
	);

	private readonly Range _singleCharRange = new(
		new Position(
			0,
			0
		),
		new Position(
			1,
			0
		)
	);

	private readonly Range _doubleCharRange = new(
		new Position(
			0,
			0
		),
		new Position(
			2,
			0
		)
	);

	[Test]
	public void TestScan_LeftParen()
	{
		var tokens = ArrangeAndAct("(");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.LeftParen,
				"(",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_RightParen()
	{
		var tokens = ArrangeAndAct(")");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.RightParen,
				")",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_LeftBrace()
	{
		var tokens = ArrangeAndAct("{");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.LeftBrace,
				"{",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_RightBrace()
	{
		var tokens = ArrangeAndAct("}");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.RightBrace,
				"}",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_LeftBracket()
	{
		var tokens = ArrangeAndAct("[");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.LeftBracket,
				"[",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_RightBracket()
	{
		var tokens = ArrangeAndAct("]");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.RightBracket,
				"]",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Equal()
	{
		var tokens = ArrangeAndAct("=");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.Equal,
				"=",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_EqualEqual()
	{
		var tokens = ArrangeAndAct("==");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.EqualEqual,
				"==",
				_doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Plus()
	{
		var tokens = ArrangeAndAct("+");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.Plus,
				"+",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_PlusPlus()
	{
		var tokens = ArrangeAndAct("++");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.PlusPlus,
				"++",
				_doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_PlusEqual()
	{
		var tokens = ArrangeAndAct("+=");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.PlusEqual,
				"+=",
				_doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Minus()
	{
		var tokens = ArrangeAndAct("-");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.Minus,
				"-",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_MinusMinus()
	{
		var tokens = ArrangeAndAct("--");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.MinusMinus,
				"--",
				_doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_MinusEqual()
	{
		var tokens = ArrangeAndAct("-=");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.MinusEqual,
				"-=",
				_doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Slash()
	{
		var tokens = ArrangeAndAct("/");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.Slash,
				"/",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_SlashEqual()
	{
		var tokens = ArrangeAndAct("/=");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.SlashEqual,
				"/=",
				_doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_SingleLineComment()
	{
		var tokens = ArrangeAndAct("// comment");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.Comment,
				"// comment",
				new Range(
					_startPosition,
					new Position(
						11,
						0
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
			tokens,
			4,
			new(
				TokType.Comment,
				"/* multi-line",
				new Range(
					_startPosition,
					new Position(
						14,
						0
					)
				)
			),
			new(
				TokType.Semicolon,
				";",
				new Range(
					new Position(
						13,
						0
					),
					new Position(
						14,
						0
					)
				)
			),
			new(
				TokType.Comment,
				"comment*/",
				new Range(
					new Position(
						0,
						1
					),
					new Position(
						10,
						1
					)
				)
			)
		);
	}

	[Test]
	public void TestScan_Star()
	{
		var tokens = ArrangeAndAct("*");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.Star,
				"*",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_StarEqual()
	{
		var tokens = ArrangeAndAct("*=");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.StarEqual,
				"*=",
				_doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Greater()
	{
		var tokens = ArrangeAndAct(">");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.Greater,
				">",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_GreaterEqual()
	{
		var tokens = ArrangeAndAct(">=");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.GreaterEqual,
				">=",
				_doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Less()
	{
		var tokens = ArrangeAndAct("<");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.Less,
				"<",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_LessEqual()
	{
		var tokens = ArrangeAndAct("<=");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.LessEqual,
				"<=",
				_doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Bang()
	{
		var tokens = ArrangeAndAct("!");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.Bang,
				"!",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_BangEqual()
	{
		var tokens = ArrangeAndAct("!=");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.BangEqual,
				"!=",
				_doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_StringLiteral()
	{
		var tokens = ArrangeAndAct("\"string literal\"");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.StringLiteral,
				"string literal",
				new Range(
					_startPosition,
					new Position(
						16,
						0
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
			tokens,
			2,
			new Tok(
				TokType.CharLiteral,
				"a",
				new Range(
					_startPosition,
					new Position(
						3,
						0
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
			tokens,
			2,
			new Tok(
				TokType.Colon,
				":",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_ColonEqual()
	{
		var tokens = ArrangeAndAct(":=");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.ColonEqual,
				":=",
				_doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Semicolon()
	{
		var tokens = ArrangeAndAct(";");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.Semicolon,
				";",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Dot()
	{
		var tokens = ArrangeAndAct(".");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.Dot,
				".",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Comma()
	{
		var tokens = ArrangeAndAct(",");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.Comma,
				",",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Whitespace_Space()
	{
		var tokens = ArrangeAndAct("    ");
		MakeAssertions_Valid(
			tokens,
			1,
			Array.Empty<Tok>()
		);
	}

	[Test]
	public void TestScan_Whitespace_Tab()
	{
		var tokens = ArrangeAndAct("\t");
		MakeAssertions_Valid(
			tokens,
			1,
			Array.Empty<Tok>()
		);
	}

	[Test]
	public void TestScan_Yield()
	{
		var tokens = ArrangeAndAct("yield");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.Yield,
				"yield",
				new Range(
					_startPosition,
					new Position(
						5,
						0
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
			tokens,
			2,
			new Tok(
				TokType.Newline,
				"\n",
				_singleCharRange
			)
		);
	}

	[Test]
	public void TestScan_InvalidCharacter() { ArrangeAndAct_Invalid("?", typeof(InvalidCharacterException)); }

	[Test]
	public void TestScan_UnterminatedString()
	{
		ArrangeAndAct_Invalid("\"unterminated string", typeof(UnterminatedStringException));
	}

	[Test]
	public void TestScan_UnterminatedChar() { ArrangeAndAct_Invalid("'c", typeof(UnterminatedCharException)); }

	[Test]
	public void TestScan_TooLongChar() { ArrangeAndAct_Invalid("'aa'", typeof(CharLengthGreaterThanOneException)); }

	[Test]
	public void TestScan_EmptyChar() { ArrangeAndAct_Invalid("''", typeof(EmptyCharException)); }

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
			tokens,
			2,
			new Tok(
				TokType.Interface,
				"interface",
				new Range(
					_startPosition,
					new Position(
						9,
						0
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
			tokens,
			7,
			new(
				TokType.Class,
				"class",
				new Range(
					_startPosition,
					new Position(
						5,
						0
					)
				)
			),
			new(
				TokType.Identifier,
				"Greeter",
				new Range(
					new Position(
						6,
						0
					),
					new Position(
						13,
						0
					)
				)
			),
			new(
				TokType.LeftParen,
				"(",
				new Range(
					new Position(
						13,
						0
					),
					new Position(
						14,
						0
					)
				)
			),
			new(
				TokType.RightParen,
				")",
				new Range(
					new Position(
						14,
						0
					),
					new Position(
						15,
						0
					)
				)
			),
			new(
				TokType.Colon,
				":",
				new Range(
					new Position(
						16,
						0
					),
					new Position(
						17,
						0
					)
				)
			),
			new(
				TokType.Identifier,
				"IGreeter",
				new Range(
					new Position(
						18,
						0
					),
					new Position(
						26,
						0
					)
				)
			)
		);
	}

	[Test]
	public void TestScan_Is()
	{
		var tokens = ArrangeAndAct("is");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.Is,
				"is",
				_doubleCharRange
			)
		);
	}

	[Test]
	public void TestScan_Error()
	{
		var tokens = ArrangeAndAct("error");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.Error,
				"error",
				new Range(
					_startPosition,
					new Position(
						5,
						0
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
			tokens,
			2,
			new Tok(
				TokType.Check,
				"check",
				new Range(
					_startPosition,
					new Position(
						5,
						0
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
			tokens,
			2,
			new Tok(
				TokType.Struct,
				"struct",
				new Range(
					_startPosition,
					new Position(
						6,
						0
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
			tokens,
			2,
			new Tok(
				TokType.Result,
				"result",
				new Range(
					_startPosition,
					new Position(
						6,
						0
					)
				)
			)
		);
	}

	[Test]
	public void TestScan_Modulo()
	{
		var tokens = ArrangeAndAct("%");
		MakeAssertions_Valid(
			tokens,
			2,
			new Tok(
				TokType.Modulo,
				"%",
				new Range(_startPosition, new Position(1, 0))
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
		Assert.Multiple(
			() =>
			{
				Assert.That(tokens, Is.Not.Null);
				Assert.That(tokens, Has.Count.EqualTo(count));
			}
		);
		for (var i = 0; i < expected.Length; i++) Assert.That(tokens[i], Is.EqualTo(expected[i]));
	}
}
