using AuraLang.AST;
using AuraLang.Location;

namespace AuraLang.Lsp.HoverProvider;

public class AuraHoverProvider
{
	public IHoverable FindStmtByPosition(Position position, IEnumerable<ITypedAuraStatement> typedAst)
	{
		return typedAst.SelectMany(stmt => stmt.ExtractHoverables()).First(stmt => stmt.HoverableRange.Contains(position));
	}
}
