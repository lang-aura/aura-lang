using AuraLang.AST;
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
			new LocalModuleReader());
	}

	public List<(string, string)> CompileModule()
	{
		return Directory.GetFiles(Path, "*.aura")
			.Select(p =>
			{
				var compiledOutput = new AuraFileCompiler(p, ProjectName, TypeChecker).CompileFile();
				return (p, compiledOutput);
			})
			.ToList();
	}
}
