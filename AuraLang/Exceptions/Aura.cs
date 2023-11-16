namespace AuraLang.Exceptions;

public abstract class AuraExceptionContainer : Exception
{
	protected readonly List<AuraException> Exs = new();

	public bool IsEmpty() => Exs.Count == 0;

	public void Report()
	{
		var errs = Exs.Select(ex => ex.Error());
		var output = string.Join("\n\n", errs);
		Console.WriteLine(output);
	}
}

public abstract class AuraException : Exception
{
	public int Line { get; }

	protected AuraException(string message, int line) : base(message)
	{
		Line = line;
	}

	public string Error() => $"[{Line}] {Message}";
}

