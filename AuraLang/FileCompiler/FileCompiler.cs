using AuraLang.Compiler;
using AuraLang.Parser;
using AuraLang.Scanner;
using AuraLang.Token;
using AuraLang.TypeChecker;

namespace AuraLang.FileCompiler;

public class AuraFileCompiler
{
	/// <summary>
	/// The path of the file
	/// </summary>
	private string Path { get; }

	private string ProjectName { get; }

	/// <summary>
	/// Since a module may contain multiple source files, the FileCompiler accepts a type checker that
	/// may contain state from type checking other files in the module. In this way, variables declared
	/// in one source file can be reused across multiple files in the module.
	/// </summary>
	private AuraTypeChecker TypeChecker { get; }

	public AuraFileCompiler(string path, string projectName, AuraTypeChecker typeChecker)
	{
		Path = path;
		ProjectName = projectName;
		TypeChecker = typeChecker;
	}

	public string CompileFile()
	{
		var contents = File.ReadAllText(Path);
		var tokens = new AuraScanner(contents, Path)
						.ScanTokens()
						// Newline characters are retained by the scanner for use by `aura fmt` -- they are not
						// relevant for the compilation process so we filter them out here before the parsing stage.
						.Where(tok => tok.Typ is not TokType.Newline)
						.ToList();
		var untypedAst = new AuraParser(tokens, Path).Parse();
		TypeChecker.BuildSymbolsTable(untypedAst);
		var typedAst = TypeChecker.CheckTypes(untypedAst);
		return new AuraCompiler(typedAst, ProjectName, new LocalModuleReader(), new CompiledOutputWriter(), Path)
			.Compile();
	}
}
