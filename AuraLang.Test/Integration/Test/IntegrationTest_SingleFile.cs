using System.Diagnostics;
using AuraLang.AST;
using AuraLang.Exceptions;
using AuraLang.FileCompiler;
using AuraLang.TypeChecker;

namespace AuraLang.Test.Integration.Test;

public class IntegrationTest
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
		var output = await ArrangeAndAct("src/hello_world.aura");
		MakeAssertions(output, "Hello world!\n");
	}

	[Test]
	public async Task TestIntegration_Yield()
	{
		var output = await ArrangeAndAct("src/yield.aura");
		MakeAssertions(output, "0\n");
	}

	[Test]
	public async Task TestIntegration_Functions()
	{
		var output = await ArrangeAndAct("src/functions.aura");
		MakeAssertions(output,
			"Hello from f!\nYou provided the number 5 and the string Hello world\nYou provided the number 5 and the string Hello world\n5\nThe value of i is 10\nThe value of i is 5\n");
	}

	[Test]
	public async Task TestIntegration_AnonymousFunctions()
	{
		var output = await ArrangeAndAct("src/anonymous_functions.aura");
		MakeAssertions(output, "10\n20\n");
	}

	[Test]
	public async Task TestIntegration_Interfaces()
	{
		var output = await ArrangeAndAct("src/interfaces.aura");
		MakeAssertions(output, "Hi, Bob!\nHi, Bob!\nHow about this overcast weather, huh?\n");
	}

	[Test]
	public async Task TestIntegration_LocalImport()
	{
		var output = await ArrangeAndAct("src/local_imports.aura");
		MakeAssertions(output, "HelloHello\n10\ntrue\n");
	}

	private async Task<string> ArrangeAndAct(string path)
	{
		var fileName = Path.GetFileNameWithoutExtension(path);
		var typeChecker = new AuraTypeChecker(new VariableStore(), new EnclosingClassStore(),
			new EnclosingNodeStore<IUntypedAuraExpression>(), new EnclosingNodeStore<IUntypedAuraStatement>(),
			new LocalModuleReader(), "Test");
		var compiler = new AuraFileCompiler(path, "Examples", typeChecker);

		try
		{
			var output = compiler.CompileFile();
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
			ex.Report();
			throw;
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
