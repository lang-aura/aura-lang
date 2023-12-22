using AuraLang.Cli.Commands;
using AuraLang.Cli.Options;

namespace AuraLang.Test.Cli.Fmt;

public class FmtTest
{
	[Test]
	public void TestFmt_HelloWorld_NoChange()
	{
		var source = "mod main\n\nimport aura/io\n\nfn main() {\nio.println(\"Hello world\")\n}\n";
		var formatted = ArrangeAndAct(source);
		MakeAssertions(formatted, source);
	}

	// [Test]
	// public void TestFmt_HelloWorld_AddNewline()
	// {
	// 	var expected = "mod main\n\nimport aura/io\n\nfn main() {\nio.println(\"Hello world\")\n}\n";
	// 	var formatted = ArrangeAndAct("mod main\n\nimport aura/io\n\nfn main() {\nio.println(\"Hello world\")\n}");
	// 	MakeAssertions(formatted, expected);
	// }

	[Test]
	public void TestFmt_Defer_NoChange()
	{
		var source = "defer f()\n";
		var formatted = ArrangeAndAct(source);
		MakeAssertions(formatted, source);
	}

	[Test]
	public void TestFmt_Defer_RemoveDoubleSpace()
	{
		var expected = "defer f()\n";
		var formatted = ArrangeAndAct("defer  f()\n");
		MakeAssertions(formatted, expected);
	}

	[Test]
	public void TestFmt_Let_Long_NoChange()
	{
		var source = "let i: int = 5\n";
		var formatted = ArrangeAndAct(source);
		MakeAssertions(formatted, source);
	}

	[Test]
	public void TestFmt_Let_Long_Mut_NoChange()
	{
		var source = "let mut i: int = 5\n";
		var formatted = ArrangeAndAct(source);
		MakeAssertions(formatted, source);
	}

	[Test]
	public void TestFmt_Let_Long_NoInit_NoChange()
	{
		var source = "let i: int\n";
		var formatted = ArrangeAndAct(source);
		MakeAssertions(formatted, source);
	}

	[Test]
	public void TestFmt_Let_Long_RemoveDoubleSpaces()
	{
		var expected = "let mut i: int = 5\n";
		var source = "let  mut  i:  int  =  5\n";
		var formatted = ArrangeAndAct(source);
		MakeAssertions(formatted, expected);
	}

	[Test]
	public void TestFmt_Let_Short_NoChange()
	{
		var source = "i := 5\n";
		var formatted = ArrangeAndAct(source);
		MakeAssertions(formatted, source);
	}

	[Test]
	public void TestFmt_Let_Short_Mut_NoChange()
	{
		var source = "mut i := 5\n";
		var formatted = ArrangeAndAct(source);
		MakeAssertions(formatted, source);
	}

	// [Test]
	// public void TestFmt_For_NoChange()
	// {
	// 	var source = "for i := 0; i < 10; i++ {\nio.println(\"Hi there\")\n}\n";
	// 	var formatted = ArrangeAndAct(source);
	// 	MakeAssertions(formatted, source);
	// }

	[Test]
	public void TestFmt_Assign_NoChange()
	{
		var source = "x = 5\n";
		var formatted = ArrangeAndAct(source);
		MakeAssertions(formatted, source);
	}

	[Test]
	public void TestFmt_Assign_RemoveDoubleSpaces()
	{
		var expected = "x = 5\n";
		var formatted = ArrangeAndAct("x  =  5\n");
		MakeAssertions(formatted, expected);
	}

	[Test]
	public void TestFmt_Block_NoChange()
	{
		var source = "{\nio.println(\"Hello world\")\n}\n";
		var formatted = ArrangeAndAct(source);
		MakeAssertions(formatted, source);
	}

	[Test]
	public void TestFmt_Block_RemoveDoubleNewlines()
	{
		var expected = "{\ns := \"Hello world\"\nio.println(s)\n}\n";
		var source = "{\ns := \"Hello world\"\n\nio.println(s)\n}\n";
		var formatted = ArrangeAndAct(source);
		MakeAssertions(formatted, expected);
	}

	private string ArrangeAndAct(string source)
	{
		// Arrange
		var fmt = new AuraFmt(new FmtOptions());
		// Act
		return fmt.FormatAuraSourceCode(source, "test.aura");
	}

	private void MakeAssertions(string formatted, string expected)
	{
		Assert.That(formatted, Is.EqualTo(expected));
	}
}
