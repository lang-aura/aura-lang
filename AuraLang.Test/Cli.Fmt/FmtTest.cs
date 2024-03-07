using AuraLang.Cli.Commands;
using AuraLang.Cli.Options;

namespace AuraLang.Test.Cli.Fmt;

public class FmtTest
{
	[Test]
	public void TestFmt_HelloWorld_NoChange()
	{
		// Arrange
		const string source = """
		                      mod main

		                      import aura/io

		                      fn main() {
		                          io.println("Hello world")
		                      }

		                      """;
		var fmt = new AuraFmt(new FmtOptions());
		// Act
		var formatted = fmt.FormatAuraSourceCode(source, "test.aura");
		// Assert
		Assert.That(formatted, Is.EqualTo(source));
	}

	[Test]
	public void TestFmt_Defer_RemoveDoubleSpace()
	{
		const string expected = """
		                        defer f()

		                        """;
		var formatted = ArrangeAndAct_AddModStmt("defer  f()\n");
		MakeAssertions_WithModStmt(formatted, expected);
	}

	[Test]
	public void TestFmt_Let_Long_NoChange()
	{
		const string source = """
		                      let i: int = 5

		                      """;
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, source);
	}

	[Test]
	public void TestFmt_Let_Long_Mut_NoChange()
	{
		const string source = """
		                      let mut i: int = 5

		                      """;
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, source);
	}

	[Test]
	public void TestFmt_NoChange()
	{
		const string source = """
		                      let i: int

		                      """;
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, source);
	}

	[Test]
	public void TestFmt_Let_Long_RemoveDoubleSpaces()
	{
		const string expected = """
		                        let mut i: int = 5

		                        """;
		const string source = """
		                      let  mut  i:  int  =  5

		                      """;
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, expected);
	}

	[Test]
	public void TestFmt_Let_Short_NoChange()
	{
		const string source = """
		                      i := 5

		                      """;
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, source);
	}

	[Test]
	public void TestFmt_Let_Short_Mut_NoChange()
	{
		const string source = """
		                      mut i := 5

		                      """;
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, source);
	}

	[Test]
	public void TestFmt_For_NoChange()
	{
		const string source = """
		                      for i := 0; i < 10; i++ {
		                          io.println("Hi there")
		                      }

		                      """;
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, source);
	}

	[Test]
	public void TestFmt_Assign_NoChange()
	{
		const string source = """
		                      x = 5

		                      """;
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, source);
	}

	[Test]
	public void TestFmt_Assign_RemoveDoubleSpaces()
	{
		const string expected = """
		                        x = 5

		                        """;
		var formatted = ArrangeAndAct_AddModStmt("x  =  5\n");
		MakeAssertions_WithModStmt(formatted, expected);
	}

	[Test]
	public void TestFmt_Block_NoChange()
	{
		const string source = """
		                      {
		                          io.println("Hello world")
		                      }

		                      """;
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, source);
	}

	[Test]
	public void TestFmt_Block_RemoveDoubleNewlines()
	{
		const string expected = """
		                        {
		                            s := "Hello world"
		                            io.println(s)
		                        }

		                        """;
		const string source = """
		                      {
		                      s := "Hello world"

		                      io.println(s)
		                      }

		                      """;
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, expected);
	}

	[Test]
	public void TestFmt_Block_CorrectIndentation()
	{
		const string expected = """
		                        {
		                            let i: int = 5
		                            x := 5 * 2
		                        }

		                        """;
		const string source = """
		                      {
		                      let i: int = 5
		                      x := 5 * 2
		                      }

		                      """;
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, expected);
	}

	[Test]
	public void TestFmt_Function_NoChange()
	{
		const string source =
			"""
			fn main() {
			    for i := 0; i < 10; i++ {
			        io.println("Hello world")
			    }
			}

			""";
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, source);
	}

	[Test]
	public void TestFmt_Function_AddTabs()
	{
		const string expected =
			"""
			fn main() {
			    for i := 0; i < 10; i++ {
			        io.println("Hello world")
			    }
			}

			""";
		const string source = """
		                      fn main() {
		                          for i := 0; i < 10; i++ {
		                              io.println("Hello world")
		                          }
		                      }

		                      """;
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, expected);
	}

	[Test]
	public void TestFmt_MultipleImports_Combine()
	{
		const string expected = """
		                        import (
		                            aura/io
		                            aura/strings
		                        )

		                        """;
		const string source = """
		                      import aura/io
		                      import aura/strings

		                      """;
		var formatted = ArrangeAndAct_AddModStmt(source);
		MakeAssertions_WithModStmt(formatted, expected);
	}

	[Test]
	public void TestFmt_MultipleImports_OneImport()
	{
		const string expected = """
		                        import aura/io

		                        """;
		const string source = """
		                      import (
		                          aura/io
		                      )

		                      """;
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
