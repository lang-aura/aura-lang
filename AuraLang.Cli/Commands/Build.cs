using System.Diagnostics;
using AuraLang.AST;
using AuraLang.Cli.Options;
using AuraLang.Cli.Toml;
using AuraLang.Compiler;
using AuraLang.Exceptions;
using AuraLang.Parser;
using AuraLang.Scanner;
using AuraLang.TypeChecker;

namespace AuraLang.Cli.Commands;

public class Build : AuraCommand
{
	public Build(BuildOptions opts) : base(opts) { }
	private AuraToml _toml = new();

	public override int Execute()
	{
		// Before building the project, clear out all Go files from the `build` directory. This will prevent issues arising
		// from, for example, old Go files previously built from Aura source files that have since been deleted.
		ResetBuildDirectory();

		try
		{
			BuildProject();
		}
		catch (AuraExceptionContainer ex)
		{
			ex.Report();
			return 1;
		}
		// Build Go binary executable
		Directory.SetCurrentDirectory("./build/pkg");
		var build = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "go",
				Arguments = "build"
			}
		};
		build.Start();
		build.WaitForExit();

		return 0;
	}

	private void BuildProject()
	{
		TraverseProject(BuildFile);
	}

	private void BuildFile(string path)
	{
		// Read source file's contents
		var contents = File.ReadAllText(path);
		// Scan tokens
		var tokens = new AuraScanner(contents).ScanTokens();
		// Parse tokens
		var untypedAst = new AuraParser(tokens).Parse();
		// Type check AST
		var typedAst = new AuraTypeChecker(
			new VariableStore(),
			new EnclosingClassStore(),
			//new CurrentModuleStore(),
			new EnclosingNodeStore<IUntypedAuraExpression>(),
			new EnclosingNodeStore<IUntypedAuraStatement>(),
			new LocalModuleReader()).CheckTypes(untypedAst);
		// Compile
		var toml = new AuraToml();
		var output = new AuraCompiler(typedAst, toml.GetProjectName(), new LocalModuleReader(), new CompiledOutputWriter()).Compile();
		// Create Go output file
		var fileName = Path.ChangeExtension(Path.GetFileName(path), "go");
		var goPath = $"./build/pkg/{fileName}";
		File.WriteAllText(goPath, output);
		FormatGoOutputFile(goPath);
	}

	private void FormatGoOutputFile(string goPath)
	{
		var cmd = new Process();
		cmd.StartInfo.FileName = "go";
		cmd.StartInfo.Arguments = $"fmt {goPath}";
		cmd.Start();
		cmd.WaitForExit();
	}

	private void ResetBuildDirectory()
	{
		// Delete all Go files in `build` directory
		var paths = Directory.GetFiles("./build/pkg", "*.go");
		foreach (var path in paths)
		{
			File.Delete(path);
		}
		// Delete any sub-directories containing Go source files
		var dirs = Directory.GetDirectories("./build/pkg").Where(p => p.Split("/")[^1] != "stdlib");
		foreach (var dir in dirs)
		{
			Directory.Delete(dir);
		}
		// Delete executable file
		var projName = _toml.GetProjectName();
		File.Delete($"./build/pkg/{projName}");
	}
}
