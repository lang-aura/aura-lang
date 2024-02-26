﻿using AuraLang.AST;
using AuraLang.Lsp.PrecedingNodeFinder;
using AuraLang.Types;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Position = AuraLang.Location.Position;

namespace AuraLang.Lsp.SignatureHelpProvider;

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
