using AuraLang.AST;
using AuraLang.Lsp.PrecedingNodeFinder;
using AuraLang.Types;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Position = AuraLang.Location.Position;

namespace AuraLang.Lsp.SignatureHelpProvider;

/// <summary>
///     Responsible for providing signature help to display in the LSP client
/// </summary>
public class AuraSignatureHelpProvider
{
	private ITypedAuraAstNode? FindImmediatelyPrecedingNode(
		Position position,
		IEnumerable<ITypedAuraStatement> typedAst
	)
	{
		var rangeFinder = new AuraPrecedingNodeFinder(position, typedAst);
		return rangeFinder.FindImmediatelyPrecedingNode();
	}

	/// <summary>
	///     Computes signature help
	/// </summary>
	/// <param name="position">The position of the trigger character</param>
	/// <param name="triggerCharacter">The specific trigger character</param>
	/// <param name="typedAst">A typed Abstract Syntax Tree that contains the trigger character</param>
	/// <returns>Signature help information</returns>
	public SignatureHelp? ComputeSignatureHelp(
		Position position,
		string triggerCharacter,
		IEnumerable<ITypedAuraStatement> typedAst
	)
	{
		var immediatelyPrecedingNode = FindImmediatelyPrecedingNode(position, typedAst);
		if (immediatelyPrecedingNode?.Typ is ISignatureHelper sh) return sh.ProvideSignatureHelp(triggerCharacter);
		return null;
	}
}
