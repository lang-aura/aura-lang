using AuraLang.AST;
using AuraLang.Location;

namespace AuraLang.Lsp.Service.HoverProvider;

/// <summary>
///     Responsible for providing hover information to display in the LSP client
/// </summary>
public class AuraHoverProvider : AuraLspService
{
	public IHoverable? FindStmtByPosition(Position position, IEnumerable<ITypedAuraStatement> typedAst)
	{
		try
		{
			return typedAst.SelectMany(stmt => stmt.ExtractHoverables()).First(stmt => stmt.HoverableRange.Contains(position));
		}
		catch (InvalidOperationException)
		{
			return null;
		}
	}
}
