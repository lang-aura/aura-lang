using AuraLang.AST;
using AuraLang.Exceptions.TypeChecker;
using AuraLang.FileCompiler;
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
		var untypedAsts = Directory.GetFiles(Path, "*.aura")
			.Select(p =>
			{
				var parsedOutput = new AuraFileCompiler(p, ProjectName).ParseFile();
				return (p, parsedOutput);
			});

		// Ensure all source files in the module have the same `mod` name
		var modNames = untypedAsts.Select(pair => pair.parsedOutput.Find(node => node is UntypedMod));
		if (!modNames.All(name => ((UntypedMod)name!).Value.Value == ((UntypedMod)modNames.First()!).Value.Value))
		{
			throw new DirectoryCannotContainMultipleModulesException(1);
		}

		// Build symbols table
		foreach (var (_, untypedAst) in untypedAsts) TypeChecker.BuildSymbolsTable(untypedAst);

		// Type check and compile all source files in module
		return untypedAsts.Select(pair => (pair.p, new AuraFileCompiler(pair.p, ProjectName).TypeCheckAndCompileUntypedAst(TypeChecker, pair.parsedOutput))).ToList();
	}
}
