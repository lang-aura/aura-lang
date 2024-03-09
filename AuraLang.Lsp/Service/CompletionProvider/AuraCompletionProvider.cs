using AuraLang.AST;
using AuraLang.Symbol;
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
	/// <param name="symbolsTable">The symbols table associated with the file from where the completion request was triggered</param>
	/// <returns></returns>
	public CompletionList? ComputeCompletionOptions(
		Position position,
		string? triggerCharacter,
		IEnumerable<ITypedAuraStatement> typedAst,
		IGlobalSymbolsTable symbolsTable
	)
	{
		if (triggerCharacter is null)
			return ComputeCompletionOptionsForNullTriggerCharacter(
				position,
				typedAst,
				symbolsTable
			);
		var immediatelyPrecedingNode = FindImmediatelyPrecedingNode(position, typedAst);
		if (immediatelyPrecedingNode?.Typ is ICompletable c) return c.ProvideCompletableOptions(triggerCharacter);
		return null;
	}

	private CompletionList? ComputeCompletionOptionsForNullTriggerCharacter(
		Position position,
		IEnumerable<ITypedAuraStatement> typedAst,
		IGlobalSymbolsTable symbolsTable
	)
	{
		var immediatelyPrecedingNode = FindImmediatelyPrecedingNode(position, typedAst);
		if (immediatelyPrecedingNode is not TypedVariable) return null;
		// TODO Find all local symbols and keyword that start with the immediately preceding node (need the symbols table)
	}
}
