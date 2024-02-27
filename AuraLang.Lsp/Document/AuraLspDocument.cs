using AuraLang.AST;

namespace AuraLang.Lsp.Document;

/// <summary>
///     Represents a single Aura source file, as maintained by the <see cref="LanguageServer" />
/// </summary>
/// <param name="Contents">The file's contents, as a string</param>
/// <param name="TypedAst">A typed Abstract Syntax Tree representing the file's contents</param>
public record AuraLspDocument(string Contents, List<ITypedAuraStatement> TypedAst);
