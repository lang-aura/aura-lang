using Range = AuraLang.Location.Range;

namespace AuraLang.AST;

public interface IAuraAstNode
{
	public Range Range { get; }
}
