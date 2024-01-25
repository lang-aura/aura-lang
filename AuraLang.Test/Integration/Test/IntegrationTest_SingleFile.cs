using System.Diagnostics;
using AuraLang.AST;
using AuraLang.Exceptions;
using AuraLang.FileCompiler;
using AuraLang.Symbol;
using AuraLang.TypeChecker;

namespace AuraLang.Test.Integration.Test;

public class IntegrationTest_SingleFile
{
	private const string BasePath = "../../../Integration/Examples";

	[SetUp]
	public void Setup()
	{
		Directory.SetCurrentDirectory(BasePath);
	}

	[TearDown]
	public void Teardown()
	{
		Directory.SetCurrentDirectory("../../obj/Debug/net7.0");
	}

	[Test]
	public async Task TestIntegration_HelloWorld()
	{
		var output = await ArrangeAndAct_SingleFile("src/hello_world.aura");
		MakeAssertions(output, "Hello world!\n");
	}

	[Test]
	public async Task TestIntegration_Yield()
	{
		var output = await ArrangeAndAct_SingleFile("src/yield.aura");
		MakeAssertions(output, "0\n");
	}

	[Test]
	public async Task TestIntegration_Functions()
	{
		var output = await ArrangeAndAct_SingleFile("src/functions.aura");
		MakeAssertions(output,
			"Hello from f!\nYou provided the number 5 and the string Hello world\nYou provided the number 5 and the string Hello world\n5\nThe value of i is 10\nThe value of i is 5\n");
	}

	[Test]
	public async Task TestIntegration_AnonymousFunctions()
	{
		var output = await ArrangeAndAct_SingleFile("src/anonymous_functions.aura");
		MakeAssertions(output, "10\n20\n");
	}

	[Test]
	public async Task TestIntegration_Interfaces()
	{
		var output = await ArrangeAndAct_SingleFile("src/interfaces.aura");
		MakeAssertions(output, "Hi, Bob!\nHi, Bob!\nHow about this overcast weather, huh?\n");
	}

	[Test]
	public async Task TestIntegration_LocalImport()
	{
		var output = await ArrangeAndAct_SingleFile("src/local_imports.aura");
		MakeAssertions(output, "HelloHello\n10\ntrue\n");
	}

	[Test]
	public async Task TestIntegration_ImportModuleWithTwoSourceFiles()
	{
		var output = await ArrangeAndAct_SingleFile("src/multiple_source_files.aura");
		MakeAssertions(output, "10\n");
	}

	[Test]
	public async Task TestIntegration_ImportModuleWithTwoModuleNames()
	{
		var output = await ArrangeAndAct_SingleFile("src/multiple_mod_names.aura");
		MakeAssertions(output, "[src/multiple_mod_names.aura line 1] Directory cannot contain multiple modules");
	}

	[Test]
	public async Task TestIntegration_Error()
	{
		var output = await ArrangeAndAct_SingleFile("src/error.aura");
		MakeAssertions(output, "Helpful error message\nHelpful error message\n");
	}

	[Test]
	public async Task TestIntegration_Strings()
	{
		var output = await ArrangeAndAct_SingleFile("src/strings.aura");
		MakeAssertions(output, "HELLO WORLD\nhello world\n");
	}

	[Test]
	public async Task TestIntegration_Lists()
	{
		var output = await ArrangeAndAct_SingleFile("src/lists.aura");
		MakeAssertions(output, "First index = Hello\nHello\nworld\nContains hello\n");
	}

	private async Task<string> ArrangeAndAct_SingleFile(string path)
	{
		var fileName = Path.GetFileNameWithoutExtension(path);
		var typeChecker = new AuraTypeChecker(new GlobalSymbolsTable(), new EnclosingClassStore(), new EnclosingFunctionDeclarationStore(),
			new EnclosingNodeStore<IUntypedAuraExpression>(), new EnclosingNodeStore<IUntypedAuraStatement>(),
			path, "Test");
		var compiler = new AuraFileCompiler(path, "Examples");

		try
		{
			var output = compiler.CompileFile(typeChecker);
			// Create Go output file
			await File.WriteAllTextAsync($"build/pkg/{fileName}.go", output);
			// Format Go output file
			var fmt = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "go",
					Arguments = $"fmt {fileName}.go",
					WorkingDirectory = "build/pkg",
					UseShellExecute = false
				}
			};
			fmt.Start();
			await fmt.WaitForExitAsync();
			// Run Go file
			var run = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "go",
					Arguments = $"run {fileName}.go",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					WorkingDirectory = "build/pkg"
				}
			};
			run.Start();
			var actual = await run.StandardOutput.ReadToEndAsync();
			var error = await run.StandardError.ReadToEndAsync();
			await run.WaitForExitAsync();

			return actual != string.Empty ? actual : error;
		}
		catch (AuraExceptionContainer ex)
		{
			return ex.Report();
		}
	}

	private void MakeAssertions(string actual, string expected)
	{
		Assert.Multiple(() =>
		{
			Assert.That(actual, Is.Not.Empty);
			Assert.That(actual, Has.Length.EqualTo(expected.Length));
			Assert.That(actual, Is.EqualTo(expected));
		});
	}
}
