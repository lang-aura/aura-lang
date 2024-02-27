using AuraLang.AST;
using AuraLang.Location;
using AuraLang.Lsp.PrecedingNodeFinder;

namespace AuraLang.Lsp.Service;

public abstract class AuraLspService
{
	/// <summary>
	///     Finds the immediately preceding node in the supplied Abstract Syntax Tree
	/// </summary>
	/// <param name="position">The position of the trigger character</param>
	/// <param name="typedAst">A typed Abstract Syntax Tree</param>
	/// <returns></returns>
	protected ITypedAuraAstNode? FindImmediatelyPrecedingNode(
		Position position,
		IEnumerable<ITypedAuraStatement> typedAst
	)
	{
		var rangeFinder = new AuraPrecedingNodeFinder(position, typedAst);
		return rangeFinder.FindImmediatelyPrecedingNode();
	}
}
