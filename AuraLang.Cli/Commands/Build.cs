using AuraLang.Cli.Options;
using AuraLang.Compiler;
using AuraLang.Parser;
using AuraLang.Scanner;
using AuraLang.TypeChecker;

namespace AuraLang.Cli.Commands;

public class Build
{
	private string Path { get; init; }
	private bool Verbose { get; init; }

	public Build(BuildOptions opts)
	{
		Path = opts.Path;
		Verbose = opts.Verbose ?? false;
	}

	public int Execute()
	{
		// Read source file's contents
		var contents = File.ReadAllText(Path);
		// Scan tokens
		var tokens = new AuraScanner(contents).ScanTokens();
		// Parse tokens
		var untypedAst = new AuraParser(tokens).Parse();
		// Type check AST
		var typedAst = new AuraTypeChecker().CheckTypes(untypedAst);
		// Compile
		var output = new AuraCompiler(typedAst).Compile();
		// Create Go output file
		var auraPath = Path.Replace(".aura", ".go");
		File.AppendAllText(auraPath, output);

		return 0;
	}
}

