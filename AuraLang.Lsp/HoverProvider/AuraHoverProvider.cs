using AuraLang.AST;
using AuraLang.Location;
using Microsoft.VisualBasic;
using Newtonsoft.Json;

namespace AuraLang.Lsp.HoverProvider;

public class AuraHoverProvider
{
	public ITypedAuraStatement FindStmtByPosition(Position position, IEnumerable<ITypedAuraStatement> typedAst)
	{
		// Flatten typed AST
		var flattenedTypedAst = typedAst.Select(
				stmt =>
				{
					if (stmt is TypedNamedFunction f) return f.Body.Statements;
					return new List<ITypedAuraStatement> { stmt };
				}
			)
			.Aggregate(
				new List<ITypedAuraStatement>(),
				(list, statements) =>
				{
					list.AddRange(statements);
					return list;
				}
			);
		return flattenedTypedAst.First(stmt => stmt.Range.Contains(position));
	}
}
