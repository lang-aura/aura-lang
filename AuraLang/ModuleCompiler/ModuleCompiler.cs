using AuraLang.AST;
using AuraLang.Exceptions.TypeChecker;
using AuraLang.FileCompiler;
using AuraLang.Parser;
using AuraLang.Scanner;
using AuraLang.Token;
using AuraLang.TypeChecker;

namespace AuraLang.ModuleCompiler;

public class AuraModuleCompiler
{
	/// <summary>
	/// The path of the module
	/// </summary>
	private string Path { get; }

	private string ProjectName { get; }

	/// <summary>
	/// The same type checker will be used for all Aura source files in the module
	/// </summary>
	private AuraTypeChecker TypeChecker { get; }

	public AuraModuleCompiler(string path, string projectName)
	{
		Path = path;
		ProjectName = projectName;
		TypeChecker = new AuraTypeChecker(
			new VariableStore(),
			new EnclosingClassStore(),
			new EnclosingNodeStore<IUntypedAuraExpression>(),
			new EnclosingNodeStore<IUntypedAuraStatement>(),
			new LocalModuleReader(),
			path);
	}

	public List<(string, string)> CompileModule()
	{
		var paths = Directory.GetFiles(Path, "*.aura");
		var moduleNames = new List<string>();

		foreach (var path in paths)
		{
			var contents = File.ReadAllText(path);
			var tokens = new AuraScanner(contents, path)
							.ScanTokens()
							// Newline characters are retained by the scanner for use by `aura fmt` -- they are not
							// relevant for the compilation process so we filter them out here before the parsing stage.
							.Where(tok => tok.Typ is not TokType.Newline)
							.ToList();
			var untypedAst = new AuraParser(tokens, path).Parse();

			var modName = untypedAst.Find(node => node is UntypedMod);
			moduleNames.Add(((UntypedMod)modName!).Value.Value);

			TypeChecker.BuildSymbolsTable(untypedAst);
		}

		if (!moduleNames.All(name => name == moduleNames.First()))
		{
			throw new DirectoryCannotContainMultipleModulesException(1);
		}

		return paths
			.Select(p =>
			{
				var compiledOutput = new AuraFileCompiler(p, ProjectName, TypeChecker).CompileFile();
				return (p, compiledOutput);
			})
			.ToList();
	}
}
