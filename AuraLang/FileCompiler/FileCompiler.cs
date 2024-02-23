using AuraLang.AST;
using AuraLang.Compiler;
using AuraLang.Parser;
using AuraLang.Scanner;
using AuraLang.Token;
using AuraLang.TypeChecker;

namespace AuraLang.FileCompiler;

/// <summary>
///     Responsible for compiling a single Aura source file
/// </summary>
public class AuraFileCompiler
{
	/// <summary>
    ///     The path of the file
	/// </summary>
	private string Path { get; }

    /// <summary>
    ///     The name of the enclosing Aura project, as defined in <c>aura.toml</c>
    /// </summary>
    private string ProjectName { get; }

    public AuraFileCompiler(string path, string projectName)
    {
        Path = path;
        ProjectName = projectName;
    }

    /// <summary>
    ///     Parses an Aura source file
    /// </summary>
    /// <returns>An untyped Abstract Syntax Tree representing the file's contents</returns>
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

    /// <summary>
    ///     Type checks the supplied untyped Abstract Syntax Tree
    /// </summary>
    /// <param name="typeChecker">
    ///     The type checker to use. The type checker must be specified because an Aura module may
    ///     contain multiple source files, in which case the type checker may contain symbol information from other Aura source
    ///     files in the same module
    /// </param>
    /// <param name="untypedAst">The untyped Abstract Syntax Tree to type check</param>
    /// <returns>A typed Abstract Syntax Tree</returns>
    public List<ITypedAuraStatement> TypeCheckUntypedAst(
        AuraTypeChecker typeChecker,
        List<IUntypedAuraStatement> untypedAst
    )
    {
        return typeChecker.CheckTypes(untypedAst);
    }

    /// <summary>
    ///     Type checks and compiles the supplied untyped Abstract Syntax Tree
    /// </summary>
    /// <param name="typeChecker">
    ///     The type checker to use. The type checker must be specified because an Aura module may
    ///     contain multiple source files, in which case the type checker may contain symbol information from other Aura source
    ///     files in the same module
    /// </param>
    /// <param name="untypedAst">The untyped Abstract Syntax Tree to type check and compile</param>
    /// <returns>A valid Go string</returns>
    public string TypeCheckAndCompileUntypedAst(AuraTypeChecker typeChecker, List<IUntypedAuraStatement> untypedAst)
    {
        var typedAst = typeChecker.CheckTypes(untypedAst);
        return new AuraCompiler(
                typedAst,
                ProjectName,
                new CompiledOutputWriter(),
                new Stack<TypedNamedFunction>(),
                Path
            )
            .Compile();
    }

    /// <summary>
    ///     Compiles an Aura source file, executing each stage of the compilation process
    /// </summary>
    /// <param name="typeChecker">
    ///     The type checker to use. The type checker must be specified because an Aura module may
    ///     contain multiple source files, in which case the type checker may contain symbol information from other Aura source
    ///     files in the same module
    /// </param>
    /// <returns>A valid Go string</returns>
    public string CompileFile(AuraTypeChecker typeChecker)
    {
        var untypedAst = ParseFile();
        typeChecker.BuildSymbolsTable(untypedAst);
        return TypeCheckAndCompileUntypedAst(typeChecker, untypedAst);
    }
}