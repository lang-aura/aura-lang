using AuraLang.AST;
using AuraLang.Types;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Position = AuraLang.Location.Position;

namespace AuraLang.Lsp.Service.SignatureHelpProvider;

/// <summary>
///     Responsible for providing signature help to display in the LSP client
/// </summary>
public class AuraSignatureHelpProvider : AuraLspService
{
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
