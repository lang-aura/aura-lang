namespace AuraLang.Exceptions;

public abstract class AuraExceptionContainer : Exception
{
	public List<AuraException> exs = new();

	public bool IsEmpty() => exs.Count == 0;
}

public abstract class AuraException : Exception
{
	public int Line { get; init; }

	public AuraException(string message, int line) : base(message)
	{
		Line = line;
	}
}

