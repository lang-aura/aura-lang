using System.Diagnostics;
using AuraLang.Cli.Options;
using AuraLang.Compiler;
using AuraLang.Parser;
using AuraLang.Scanner;
using AuraLang.TypeChecker;

namespace AuraLang.Cli.Commands;

public class Build
{
	private string FilePath { get; init; }
	private bool Verbose { get; init; }

	public Build(BuildOptions opts)
	{
		FilePath = opts.Path;
		Verbose = opts.Verbose ?? false;
	}

	public int Execute()
	{
		// Build all Aura files in project
		var auraFiles = GetAllAuraFiles("./src");
		foreach (var af in auraFiles)
		{
			BuildFile(af);
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
			new CurrentModuleStore()).CheckTypes(untypedAst);
		// Compile
		var output = new AuraCompiler(typedAst).Compile();
		// Create Go output file
		var fileName = Path.ChangeExtension(Path.GetFileName(FilePath), "aura");
		var goPath = $"./build/pkg/{fileName}";
		File.AppendAllText(goPath, output);
		FormatGoOutputFile(goPath);
	}

	private List<string> GetAllAuraFiles(string path)
	{
		var files = Directory.GetFiles(path).ToList();
		var dirFiles = Directory.GetDirectories(path).Select(GetAllAuraFiles);
		return dirFiles.Aggregate(files, (prev, curr) =>
		{
			prev.AddRange(curr);
			return prev;
		});
	}

	private void FormatGoOutputFile(string goPath)
	{
		var cmd = new Process();
		cmd.StartInfo.FileName = "go";
		cmd.StartInfo.Arguments = $"fmt {goPath}";
		cmd.Start();
		cmd.WaitForExit();
	}
}
