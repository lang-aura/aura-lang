using AuraLang.Cli.Options;
using AuraLang.Compiler;
using AuraLang.Parser;
using AuraLang.Scanner;
using AuraLang.TypeChecker;

namespace AuraLang.Cli.Commands;

public class Build
{
	private string _path { get; init; }
	private bool _verbose { get; init; }

	public Build(BuildOptions opts)
	{
		_path = opts.Path;
		_verbose = opts.Verbose ?? false;
	}

	public int Execute()
	{
		// Read source file's contents
		var contents = File.ReadAllText(_path);
		// Scan tokens
		var tokens = new AuraScanner(contents).ScanTokens();
		// Parse tokens
		var untypedAst = new AuraParser(tokens).Parse();
		// Type check AST
		var typedAst = new AuraTypeChecker(untypedAst).CheckTypes();
		// Compile
		var output = new AuraCompiler(typedAst).Compile();

		Console.WriteLine(output);
		return 0;
	}
}

