namespace AuraLang;

public abstract class AuraWarningContainer : Exception
{
	public readonly List<AuraWarning> Warnings = new();
	protected string FilePath { get; init; }

	public AuraWarningContainer(string filePath)
	{
		FilePath = filePath;
	}

	public bool IsEmpty() => Warnings.Count == 0;

	public void Report()
	{
		var warnings = Warnings.Select(w => w.Warn(FilePath));
		var output = string.Join("\n\n", warnings);
		Console.WriteLine(output);
	}
}

public abstract class AuraWarning : Exception
{
	public int Line { get; }

	protected AuraWarning(string message, int line) : base(message)
	{
		Line = line;
	}

	public string Warn(string filePath) => $"[{filePath} line {Line}] {Message}";
}
