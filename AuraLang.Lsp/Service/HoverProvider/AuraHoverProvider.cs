using AuraLang.AST;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using MsPosition = Microsoft.VisualStudio.LanguageServer.Protocol.Position;
using Position = AuraLang.Location.Position;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace AuraLang.Lsp.Service.HoverProvider;

/// <summary>
///     Responsible for providing hover information to display in the LSP client
/// </summary>
public class AuraHoverProvider : AuraLspService
{
	/// <summary>
	///     Finds the statement being hovered over in the LSP client
	/// </summary>
	/// <param name="position">The current position of the cursor</param>
	/// <param name="typedAst">The typed Abstract Syntax Tree representing the current Aura source file</param>
	/// <returns>The typed AST node being hovered over, if it provides hoverable information; else null</returns>
	private IHoverable? FindStmt(Position position, IEnumerable<ITypedAuraStatement> typedAst)
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

	/// <summary>
	///     Gets the hover text of the typed AST node currently being hovered over in the LSP client
	/// </summary>
	/// <param name="position">The current position of the cursor</param>
	/// <param name="typedAst">The typed Abstract Syntax Tree representing the current Aura source file</param>
	/// <returns>
	///     The hover text of the AST node being hovered over. If the cursor is currently hovering over a non-node (i.e.
	///     whitespace) or the current node doesn't provide any hover text, the returned <see cref="Hover" /> object is empty
	/// </returns>
	public Hover GetHoverText(Position position, IEnumerable<ITypedAuraStatement> typedAst)
	{
		var node = FindStmt(position, typedAst);
		if (node is null) return new Hover();
		return new Hover
		{
			Contents = new MarkupContent { Value = $"```\n{node.HoverText}\n```", Kind = MarkupKind.Markdown },
			Range = new Range
			{
				Start = new MsPosition
				{
					Character = node.HoverableRange.Start.Character,
					Line = node.HoverableRange.Start.Line
				},
				End = new MsPosition
				{
					Character = node.HoverableRange.End.Character,
					Line = node.HoverableRange.End.Line
				}
			}
		};
	}
}
