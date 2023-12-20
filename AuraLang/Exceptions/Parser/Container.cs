namespace AuraLang.Exceptions.Parser;

public class ParserExceptionContainer : AuraExceptionContainer
{
	public ParserExceptionContainer(string filePath) : base(filePath) { }

	public void Add(ParserException ex)
	{
		Exs.Add(ex);
	}
}
