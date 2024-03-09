using AuraLang.AST;

namespace AuraLang.Exceptions.Parser;

/// <summary>
///     A container for exceptions thrown by the parser
/// </summary>
public class ParserExceptionContainer : AuraExceptionContainer<List<IUntypedAuraStatement>>
{
	public override List<IUntypedAuraStatement>? Valid { get; set; }

	public ParserExceptionContainer(string filePath) : base(filePath) { }

	public void Add(ParserException ex)
	{
		Exs.Add(ex);
	}
}
