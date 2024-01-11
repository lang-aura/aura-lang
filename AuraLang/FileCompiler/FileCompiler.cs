using AuraLang.AST;
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

	public AuraFileCompiler(string path, string projectName)
	{
		Path = path;
		ProjectName = projectName;
	}

	public List<IUntypedAuraStatement> ParseFile()
	{
		var contents = File.ReadAllText(Path);
		var tokens = new AuraScanner(contents, Path)
						.ScanTokens()
						// Newline characters are retained by the scanner for use by `aura fmt` -- they are not
						// relevant for the compilation process so we filter them out here before the parsing stage.
						.Where(tok => tok.Typ is not TokType.Newline)
						.ToList();
		return new AuraParser(tokens, Path).Parse();
	}

	public string TypeCheckAndCompileUntypedAst(AuraTypeChecker typeChecker, List<IUntypedAuraStatement> untypedAst)
	{
		var typedAst = typeChecker.CheckTypes(untypedAst);
		return new AuraCompiler(typedAst, ProjectName, new LocalModuleReader(), new CompiledOutputWriter(), Path)
			.Compile();
	}

	public string CompileFile(AuraTypeChecker typeChecker)
	{
		var untypedAst = ParseFile();
		typeChecker.BuildSymbolsTable(untypedAst);
		return TypeCheckAndCompileUntypedAst(typeChecker, untypedAst);
	}
}
