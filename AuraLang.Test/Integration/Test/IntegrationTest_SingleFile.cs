using System.Diagnostics;
using AuraLang.AST;
using AuraLang.Exceptions;
using AuraLang.FileCompiler;
using AuraLang.LocalFileSystemModuleProvider;
using AuraLang.Stores;
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
	public async Task TestIntegration_HelloWorldAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/hello_world.aura");
		MakeAssertions(output, "Hello world!\n");
	}

	[Test]
	public async Task TestIntegration_YieldAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/yield.aura");
		MakeAssertions(output, "0\n");
	}

	[Test]
	public async Task TestIntegration_FunctionsAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/functions.aura");
		MakeAssertions(output,
			"Hello from f!\nYou provided the number 5 and the string Hello world\nYou provided the number 5 and the string Hello world\n5\nThe value of i is 10\nThe value of i is 5\n");
	}

	[Test]
	public async Task TestIntegration_AnonymousFunctionsAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/anonymous_functions.aura");
		MakeAssertions(output, "10\n20\n");
	}

	[Test]
	public async Task TestIntegration_InterfacesAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/interfaces.aura");
		MakeAssertions(output, "Hi, Bob!\nHi, Bob!\nHow about this overcast weather, huh?\n");
	}

	[Test]
	public async Task TestIntegration_LocalImportAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/local_imports.aura");
		MakeAssertions(output, "HelloHello\n10\ntrue\n");
	}

	[Test]
	public async Task TestIntegration_ImportModuleWithTwoSourceFilesAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/multiple_source_files.aura");
		MakeAssertions(output, "10\n");
	}

	[Test]
	public async Task TestIntegration_ImportModuleWithTwoModuleNamesAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/multiple_mod_names.aura");
		MakeAssertions(output, "[src/multiple_mod_names.aura line 1] Directory cannot contain multiple modules. Expected only one module name, but found [one, two]");
	}

	[Test]
	public async Task TestIntegration_ErrorAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/error.aura");
		MakeAssertions(output, "Helpful error message\nHelpful error message\n");
	}

	[Test]
	public async Task TestIntegration_StringsAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/strings.aura");
		MakeAssertions(output, "HELLO WORLD\nhello world\n");
	}

	[Test]
	public async Task TestIntegration_ListsAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/lists.aura");
		MakeAssertions(output, "First index = Hello\nHello\nworld\nContains hello\n");
	}

	[Test]
	public async Task TestIntegration_StructsAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/structs.aura");
		MakeAssertions(output, "5\n");
	}

	[Test]
	public async Task TestIntegration_ReturnTupleAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/return_tuple.aura");
		MakeAssertions(output, "s = HELLO WORLD; e = <nil>\nst = HELLO WORLD; er = <nil>\nupper = HELLO WORLD; lower = hello world; n = <nil>\n");
	}

	[Test]
	public async Task TestIntegration_IsAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/is.aura");
		MakeAssertions(output, "is IGreeter\n");
	}

	[Test]
	public async Task TestIntegration_ToIntAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/to_int.aura");
		MakeAssertions(output, "44\n");
	}

	[Test]
	public async Task TestIntegration_AnonymousFunction2Async()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/anonymous_functions_2.aura");
		MakeAssertions(output, "7\n");
	}

	[Test]
	public async Task TestIntegration_StructAsMapKeysAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/struct_as_map_key.aura");
		MakeAssertions(output, "1\n");
	}

	[Test]
	public async Task TestIntegration_MapsAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/maps.aura");
		MakeAssertions(output, "5\ntrue\n");
	}

	[Test]
	public async Task TestIntegration_ReturnStructAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/return_struct.aura");
		MakeAssertions(output, "1\n");
	}

	[Test]
	public async Task TestIntegration_MultipleImportsAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/multiple_imports.aura");
		MakeAssertions(output, "Hello world\n");
	}

	[Test]
	public async Task TestIntegration_ClassMethodAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/class_method.aura");
		MakeAssertions(output, "Hi there, Bob\n");
	}

	[Test]
	public async Task TestIntegration_ClassCallPrivateMethodAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/class_call_private_method.aura");
		MakeAssertions(
			output,
			"[src/class_call_private_method.aura line 7] Cannot invoke method `build_greeting` outside of its defining class because it has private visibility\n\n[src/class_call_private_method.aura line 8] Unknown variable `s`."
		);
	}

	[Test]
	public async Task TestIntegration_ClassIncompleteInterfaceImplementationAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/class_incomplete_interface_implementation.aura");
		MakeAssertions(
			output,
			"[src/class_incomplete_interface_implementation.aura line 23] `Greeter` implements the interface `IGreeter`, but does not implement all of the required functions. \n\nThe following methods are missing from `Greeter`: \nfn say_hello()"
		);
	}

	[Test]
	public async Task TestIntegration_MutAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/mut.aura");
		MakeAssertions(output, "6\n6\n");
	}

	[Test]
	public async Task TestIntegration_MutMultipleAsync()
	{
		var output = await ArrangeAndAct_SingleFileAsync("src/mut_multiple.aura");
		MakeAssertions(output, "5\n9\n5\n9\n");
	}

	private async Task<string> ArrangeAndAct_SingleFileAsync(string path)
	{
		var fileName = Path.GetFileNameWithoutExtension(path);
		var typeChecker = new AuraTypeChecker(new GlobalSymbolsTable(), new EnclosingClassStore(), new EnclosingFunctionDeclarationStore(),
			new EnclosingNodeStore<IUntypedAuraExpression>(), new EnclosingNodeStore<IUntypedAuraStatement>(),
			new AuraLocalFileSystemImportedModuleProvider(), path, "Test");
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
