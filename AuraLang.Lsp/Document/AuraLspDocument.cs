using AuraLang.AST;

namespace AuraLang.Lsp.Document;

public record AuraLspDocument(string Contents, List<ITypedAuraStatement> TypedAst);
