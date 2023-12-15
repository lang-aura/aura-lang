namespace AuraLang.Exceptions;

public abstract class AuraExceptionContainer : Exception
{
	public readonly List<AuraException> Exs = new();

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
	public string FilePath { get; }
	public int Line { get; }

	protected AuraException(string message, string filePath, int line) : base(message)
	{
		FilePath = filePath;
		Line = line;
	}

	public string Error() => $"[{FilePath} line {Line}] {Message}";
}

