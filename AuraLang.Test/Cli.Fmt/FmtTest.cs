using AuraLang.Cli.Commands;
using AuraLang.Cli.Options;

namespace AuraLang.Test.Cli.Fmt;

public class FmtTest
{
	[Test]
	public void TestFmt_NoChange()
	{
		// Arrange
		var source = "mod main\n\nimport aura/io\n\nfn main() {\nio.println(\"Hello world\")\n}\n";
		var fmt = new AuraFmt(new FmtOptions());
		// Act
		var formatted = fmt.FormatAuraSourceCode(source, "test.aura");
		// Assert
		Assert.That(formatted, Is.EqualTo(source));
	}

	[Test]
	public void TestFmt_AddNewline()
	{
		// Arrange
		var expected = "mod main\n\nimport aura/io\n\nfn main() {\nio.println(\"Hello world\")\n}\n";
		var source = "mod main\n\nimport aura/io\n\nfn main() {\nio.println(\"Hello world\")\n}";
		var fmt = new AuraFmt(new FmtOptions());
		// Act
		var formatted = fmt.FormatAuraSourceCode(source, "test.aura");
		// Assert
		Assert.That(formatted, Is.EqualTo(expected));
	}
}
