using AuraLang.AST;
using AuraLang.Lsp.PrecedingNodeFinder;
using AuraLang.Types;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Position = AuraLang.Location.Position;

namespace AuraLang.Lsp.CompletionProvider;

public class AuraCompletionProvider
{
	private ITypedAuraAstNode? FindImmediatelyPrecedingNode(
		Position position,
		IEnumerable<ITypedAuraStatement> typedAst
	)
	{
		var rangeFinder = new AuraPrecedingNodeFinder(position, typedAst);
		return rangeFinder.FindImmediatelyPrecedingNode();
	}

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
