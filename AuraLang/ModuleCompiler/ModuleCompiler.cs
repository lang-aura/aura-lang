using AuraLang.AST;
using AuraLang.Exceptions.TypeChecker;
using AuraLang.FileCompiler;
using AuraLang.ImportedModuleProvider;
using AuraLang.LocalFileSystemModuleProvider;
using AuraLang.TypeChecker;
using Range = AuraLang.Location.Range;

namespace AuraLang.ModuleCompiler;

/// <summary>
///     Responsible for compiling an entire Aura module
/// </summary>
public class AuraModuleCompiler
{
	/// <summary>
	///     The path of the module
	/// </summary>
	private string Path { get; }

	/// <summary>
	///     The name of the enclosing Aura project
	/// </summary>
	private string ProjectName { get; }

	/// <summary>
	///     Used to fetch imported Aura modules
	/// </summary>
	private IImportedModuleProvider ImportedModuleProvider { get; }

	/// <summary>
	///     The same type checker will be used for all Aura source files in the module
	/// </summary>
	private AuraTypeChecker TypeChecker { get; }

	public AuraModuleCompiler(string path, string projectName)
	{
		Path = path;
		ProjectName = projectName;
		ImportedModuleProvider = new AuraLocalFileSystemImportedModuleProvider();
		TypeChecker = new AuraTypeChecker(
			ImportedModuleProvider,
			path,
			ProjectName
		);
	}

	public AuraModuleCompiler(string path, string projectName, AuraTypeChecker typeChecker)
	{
		Path = path;
		ProjectName = projectName;
		ImportedModuleProvider = typeChecker.ImportedModuleProvider;
		TypeChecker = typeChecker;
	}

	/// <summary>
	///     Type checks a single Aura module
	/// </summary>
	/// <returns>
	///     A list of tuples representing each source file in the module, where the first item in the tuple is the file's
	///     path and the second item is the file's typed AST
	/// </returns>
	/// <exception cref="DirectoryCannotContainMultipleModulesException">
	///     Thrown if the module contains more than one module
	///     name
	/// </exception>
	public List<(string, List<ITypedAuraStatement>)> TypeCheckModule()
	{
		var untypedAsts = Directory
			.GetFiles(Path, "*.aura")
			.Select(
				p =>
				{
					var parsedOutput = new AuraFileCompiler(p, ProjectName).ParseFile();
					return (p, parsedOutput);
				}
			)
			.ToList();

		// Ensure all source files in the module have the same `mod` name
		var modNames = untypedAsts.Select(pair => pair.parsedOutput.Find(node => node is UntypedMod)).ToList();
		if (!modNames.All(name => ((UntypedMod)name!).Value.Value == ((UntypedMod)modNames.First()!).Value.Value))
			// TODO get the name of the file whose module name doesn't match
			throw new DirectoryCannotContainMultipleModulesException(
				modNames.Select(name => ((UntypedMod)name!).Value.Value).ToList(),
				new Range()
			);

		// Build symbols table
		foreach (var (_, untypedAst) in untypedAsts) TypeChecker.BuildSymbolsTable(untypedAst);
		// Type check source files
		return untypedAsts
			.Select(
				pair => (pair.p,
					new AuraFileCompiler(pair.p, ProjectName).TypeCheckUntypedAst(TypeChecker, pair.parsedOutput))
			)
			.ToList();
	}

	/// <summary>
	///     Compiles a single Aura module
	/// </summary>
	/// <returns>
	///     A list of tuples representing each file in the module, where the first item in the module contains the file's
	///     path and the second item contains the Go string representing the file's contents
	/// </returns>
	/// <exception cref="DirectoryCannotContainMultipleModulesException">
	///     Thrown if the module contains more than one module
	///     name
	/// </exception>
	public List<(string, string)> CompileModule()
	{
		var untypedAsts = Directory
			.GetFiles(Path, "*.aura")
			.Select(
				p =>
				{
					var parsedOutput = new AuraFileCompiler(p, ProjectName).ParseFile();
					return (p, parsedOutput);
				}
			)
			.ToList();

		// Ensure all source files in the module have the same `mod` name
		var modNames = untypedAsts.Select(pair => pair.parsedOutput.Find(node => node is UntypedMod)).ToList();
		if (!modNames.All(name => ((UntypedMod)name!).Value.Value == ((UntypedMod)modNames.First()!).Value.Value))
			// TODO get the name of the file whose module name doesn't match
			throw new DirectoryCannotContainMultipleModulesException(
				modNames.Select(name => ((UntypedMod)name!).Value.Value).ToList(),
				new Range()
			);

		// Build symbols table
		foreach (var (_, untypedAst) in untypedAsts) TypeChecker.BuildSymbolsTable(untypedAst);

		// Type check and compile all source files in module
		return untypedAsts
			.Select(
				pair => (pair.p,
					new AuraFileCompiler(pair.p, ProjectName).TypeCheckAndCompileUntypedAst(
						TypeChecker,
						pair.parsedOutput
					))
			)
			.ToList();
	}
}
