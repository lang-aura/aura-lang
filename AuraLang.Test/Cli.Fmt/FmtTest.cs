using AuraLang.Cli.Commands;
using AuraLang.Cli.Options;

namespace AuraLang.Test.Cli.Fmt;

public class FmtTest
{
	[Test]
	public void TestFmt_HelloWorld_NoChange()
	{
		// Arrange
		const string source = "mod main\n\nimport aura/io\n\nfn main() {\n    io.println(\"Hello world\")\n}\n";
		var fmt = new AuraFmt(new FmtOptions());
		// Act
		var formatted = fmt.FormatAuraSourceCode(source, "test.aura");
		// Assert
		Assert.That(formatted, Is.EqualTo(source));
	}

	[Test]
	public void TestFmt_Defer_RemoveDoubleSpace()
	{
		const string expected = "defer f()\n";
		var formatted = ArrangeAndAct_AddModStmt("defer  f()\n");
		MakeAssertions_WithModStmt(formatted, expected);
	}

	[Test]
	public void TestFmt_Let_Long_NoChange()
	{
		const string source = "let i: int = 5\n";
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, source);
	}

	[Test]
	public void TestFmt_Let_Long_Mut_NoChange()
	{
		const string source = "let mut i: int = 5\n";
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, source);
	}

	[Test]
	public void TestFmt_NoChange()
	{
		const string source = "let i: int\n";
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, source);
	}

	[Test]
	public void TestFmt_Let_Long_RemoveDoubleSpaces()
	{
		const string expected = "let mut i: int = 5\n";
		const string source = "let  mut  i:  int  =  5\n";
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, expected);
	}

	[Test]
	public void TestFmt_Let_Short_NoChange()
	{
		const string source = "i := 5\n";
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, source);
	}

	[Test]
	public void TestFmt_Let_Short_Mut_NoChange()
	{
		const string source = "mut i := 5\n";
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, source);
	}

	[Test]
	public void TestFmt_For_NoChange()
	{
		const string source = "for i := 0; i < 10; i++ {\n    io.println(\"Hi there\")\n}\n";
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, source);
	}

	[Test]
	public void TestFmt_Assign_NoChange()
	{
		const string source = "x = 5\n";
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, source);
	}

	[Test]
	public void TestFmt_Assign_RemoveDoubleSpaces()
	{
		const string expected = "x = 5\n";
		var formatted = ArrangeAndAct_AddModStmt("x  =  5\n");
		MakeAssertions_WithModStmt(formatted, expected);
	}

	[Test]
	public void TestFmt_Block_NoChange()
	{
		const string source = "{\n    io.println(\"Hello world\")\n}\n";
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, source);
	}

	[Test]
	public void TestFmt_Block_RemoveDoubleNewlines()
	{
		const string expected = "{\n    s := \"Hello world\"\n    io.println(s)\n}\n";
		const string source = "{\ns := \"Hello world\"\n\nio.println(s)\n}\n";
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, expected);
	}

	[Test]
	public void TestFmt_Block_CorrectIndentation()
	{
		const string expected = "{\n    let i: int = 5\n    x := 5 * 2\n}\n";
		const string source = "{\nlet i: int = 5\nx := 5 * 2\n}\n";
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, expected);
	}

	[Test]
	public void TestFmt_Function_NoChange()
	{
		const string source =
			"fn main() {\n    for i := 0; i < 10; i++ {\n        io.println(\"Hello world\")\n    }\n}\n";
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, source);
	}

	[Test]
	public void TestFmt_Function_AddTabs()
	{
		const string expected =
			"fn main() {\n    for i := 0; i < 10; i++ {\n        io.println(\"Hello world\")\n    }\n}\n";
		const string source = "fn main() {\nfor i := 0; i < 10; i++ {\nio.println(\"Hello world\")\n}\n}\n";
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, expected);
	}

	[Test]
	public void TestFmt_MultipleImports_Combine()
	{
		const string expected = "import (\n    aura/io\n    aura/strings\n)\n";
		const string source = "import aura/io\nimport aura/strings\n";
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, expected);
	}

	[Test]
	public void TestFmt_MultipleImports_OneImport()
	{
		const string expected = "import aura/io\n";
		const string source = "import (\n    aura/io\n)\n";
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, expected);
	}

	private string ArrangeAndAct(string source)
	{
		var fmt = new AuraFmt(new FmtOptions());
		// Act
		return fmt.FormatAuraSourceCode(source, "test.aura");
	}

	private string ArrangeAndAct_AddModStmt(string source)
	{
		var sourceWithMod = "mod main\n" + source;
		return ArrangeAndAct(sourceWithMod);
	}

	private void MakeAssertions(string formatted, string expected) { Assert.That(formatted, Is.EqualTo(expected)); }

	private void MakeAssertions_WithModStmt(string formatted, string expected)
	{
		expected = "mod main\n" + expected;
		MakeAssertions(formatted, expected);
	}
}
