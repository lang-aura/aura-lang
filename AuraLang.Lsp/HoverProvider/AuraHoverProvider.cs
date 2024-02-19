using AuraLang.AST;
using AuraLang.Location;

namespace AuraLang.Lsp.HoverProvider;

public class AuraHoverProvider
{
	public IHoverable FindStmtByPosition(Position position, IEnumerable<ITypedAuraStatement> typedAst)
	{
		var node = typedAst.Where(stmt => stmt is IHoverable).First(stmt => stmt.Range.Contains(position));
		return (IHoverable)node;
	}
}
