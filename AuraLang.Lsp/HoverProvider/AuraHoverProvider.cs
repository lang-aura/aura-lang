using AuraLang.AST;
using AuraLang.Location;
using Newtonsoft.Json;

namespace AuraLang.Lsp.HoverProvider;

public class AuraHoverProvider
{
	public ITypedAuraStatement FindStmtByPosition(Position position, IEnumerable<ITypedAuraStatement> typedAst)
	{
		var node = typedAst.First(stmt => stmt is TypedExpressionStmt es && es.Expression is TypedCall);
		Console.Error.WriteLine(JsonConvert.SerializeObject(node));
		return typedAst.First(stmt => stmt.Range.Contains(position));
	}
}
