using AuraLang.Exceptions;
using AuraLang.Exceptions.Scanner;
using AuraLang.Scanner;
using AuraLang.Token;

namespace AuraLang.Test.Scanner;

public class ScannerTest
{
	[Test]
	public void TestScan_LeftParen()
	{
		var tokens = ArrangeAndAct("(");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.LeftParen, "(", 1));
	}

	[Test]
	public void TestScan_RightParen()
	{
		var tokens = ArrangeAndAct(")");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.RightParen, ")", 1));
	}

	[Test]
	public void TestScan_LeftBrace()
	{
		var tokens = ArrangeAndAct("{");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.LeftBrace, "{", 1));
	}

	[Test]
	public void TestScan_RightBrace()
	{
		var tokens = ArrangeAndAct("}");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.RightBrace, "}", 1));
	}

	[Test]
	public void TestScan_LeftBracket()
	{
		var tokens = ArrangeAndAct("[");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.LeftBracket, "[", 1));
	}

	[Test]
	public void TestScan_RightBracket()
	{
		var tokens = ArrangeAndAct("]");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.RightBracket, "]", 1));
	}

	[Test]
	public void TestScan_Equal()
	{
		var tokens = ArrangeAndAct("=");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.Equal, "=", 1));
	}

	[Test]
	public void TestScan_EqualEqual()
	{
		var tokens = ArrangeAndAct("==");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.EqualEqual, "==", 1));
	}

	[Test]
	public void TestScan_Plus()
	{
		var tokens = ArrangeAndAct("+");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.Plus, "+", 1));
	}

	[Test]
	public void TestScan_PlusPlus()
	{
		var tokens = ArrangeAndAct("++");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.PlusPlus, "++", 1));
	}

	[Test]
	public void TestScan_PlusEqual()
	{
		var tokens = ArrangeAndAct("+=");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.PlusEqual, "+=", 1));
	}

	[Test]
	public void TestScan_Minus()
	{
		var tokens = ArrangeAndAct("-");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.Minus, "-", 1));
	}

	[Test]
	public void TestScan_MinusMinus()
	{
		var tokens = ArrangeAndAct("--");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.MinusMinus, "--", 1));
	}

	[Test]
	public void TestScan_MinusEqual()
	{
		var tokens = ArrangeAndAct("-=");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.MinusEqual, "-=", 1));
	}

	[Test]
	public void TestScan_Slash()
	{
		var tokens = ArrangeAndAct("/");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.Slash, "/", 1));
	}

	[Test]
    public void TestScan_SlashEqual()
	{
        var tokens = ArrangeAndAct("/=");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.SlashEqual, "/=", 1));
	}

	[Test]
	public void TestScan_SingleLineComment()
	{
		var tokens = ArrangeAndAct("// comment");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.Comment, "// comment", 1));
	}

	[Test]
	public void TestScan_MultiLineComment()
	{
		var tokens = ArrangeAndAct("/* multi-line\ncomment*/");
		MakeAssertions_Valid(tokens, 4,
			new Tok(TokType.Comment, "/* multi-line", 1),
			new Tok(TokType.Semicolon, ";", 1),
			new Tok(TokType.Comment, "comment*/", 2));
	}

	[Test]
	public void TestScan_Star()
	{
		var tokens = ArrangeAndAct("*");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.Star, "*", 1));
	}

	[Test]
	public void TestScan_StarEqual()
	{
		var tokens = ArrangeAndAct("*=");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.StarEqual, "*=", 1));
	}

	[Test]
	public void TestScan_Greater()
	{
		var tokens = ArrangeAndAct(">");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.Greater, ">", 1));
	}

	[Test]
	public void TestScan_GreaterEqual()
	{
		var tokens = ArrangeAndAct(">=");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.GreaterEqual, ">=", 1));
	}

	[Test]
	public void TestScan_Less()
	{
		var tokens = ArrangeAndAct("<");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.Less, "<", 1));
	}

	[Test]
	public void TestScan_LessEqual()
	{
		var tokens = ArrangeAndAct("<=");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.LessEqual, "<=", 1));
	}

	[Test]
	public void TestScan_Bang()
	{
		var tokens = ArrangeAndAct("!");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.Bang, "!", 1));
	}

	[Test]
	public void TestScan_BangEqual()
	{
		var tokens = ArrangeAndAct("!=");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.BangEqual, "!=", 1));
	}

	[Test]
	public void TestScan_StringLiteral()
	{
		var tokens = ArrangeAndAct("\"string literal\"");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.StringLiteral, "string literal", 1));
	}

	[Test]
	public void TestScan_CharLiteral()
	{
		var tokens = ArrangeAndAct("'a'");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.CharLiteral, "a", 1));
	}

	[Test]
	public void TestScan_Colon()
	{
		var tokens = ArrangeAndAct(":");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.Colon, ":", 1));
	}

	[Test]
	public void TestScan_ColonEqual()
	{
		var tokens = ArrangeAndAct(":=");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.ColonEqual, ":=", 1));
	}

	[Test]
	public void TestScan_Semicolon()
	{
		var tokens = ArrangeAndAct(";");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.Semicolon, ";", 1));
	}

	[Test]
	public void TestScan_Dot()
	{
		var tokens = ArrangeAndAct(".");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.Dot, ".", 1));
	}

	[Test]
	public void TestScan_Comma()
	{
		var tokens = ArrangeAndAct(",");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.Comma, ",", 1));
	}

	[Test]
	public void TestScan_Whitespace_Space()
	{
		var tokens = ArrangeAndAct("    ");
		MakeAssertions_Valid(tokens, 1);
	}

	[Test]
	public void TestScan_Whitespace_Tab()
	{
		var tokens = ArrangeAndAct("\t");
		MakeAssertions_Valid(tokens, 1);
	}
	
	[Test]
	public void TestScan_Yield()
	{
		var tokens = ArrangeAndAct("yield");
		MakeAssertions_Valid(tokens, 2, new Tok(TokType.Yield, "yield", 1));
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
		var scanner = new AuraScanner(source);
		// Act
		return scanner.ScanTokens();
	}

	private static void ArrangeAndAct_Invalid(string source, Type expected)
	{
		// Arrange
		var scanner = new AuraScanner(source);
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
