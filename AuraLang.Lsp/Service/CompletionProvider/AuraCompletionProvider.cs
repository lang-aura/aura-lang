using AuraLang.AST;
using AuraLang.Types;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Position = AuraLang.Location.Position;

namespace AuraLang.Lsp.Service.CompletionProvider;

/// <summary>
///     Responsible for providing completion options when triggered by a predefined trigger character. The completion items
///     returned depends on the immediately preceding AST node and the specific trigger character
/// </summary>
public class AuraCompletionProvider : AuraLspService
{
	/// <summary>
	///     Provides a list of completion options
	/// </summary>
	/// <param name="position">The position of the trigger character</param>
	/// <param name="triggerCharacter">The actual trigger character</param>
	/// <param name="typedAst">
	///     A typed Abstract Syntax Tree. The <see cref="Position" /> of the trigger character is understood
	///     to lie within this AST
	/// </param>
	/// <returns></returns>
	public CompletionList? ComputeCompletionOptions(
		Position position,
		string triggerCharacter,
		IEnumerable<ITypedAuraStatement> typedAst
	)
	{
		var immediatelyPrecedingNode = FindImmediatelyPrecedingNode(position, typedAst);
		if (immediatelyPrecedingNode?.Typ is ICompletable c) return c.ProvideCompletableOptions(triggerCharacter);
		return null;
	}
}
