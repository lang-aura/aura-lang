using Range = AuraLang.Location.Range;

namespace AuraLang.Exceptions;

public abstract class AuraExceptionContainer : Exception
{
	public readonly List<AuraException> Exs = new();
	private string FilePath { get; }

	protected AuraExceptionContainer(string filePath)
	{
		FilePath = filePath;
	}

	public bool IsEmpty() => Exs.Count == 0;

	public string Report()
	{
		var errs = Exs.Select(ex => ex.Error(FilePath));
		return string.Join("\n\n", errs);
	}
}

public abstract class AuraException : Exception
{
	public Range Range { get; }

	protected AuraException(string message, Range range) : base(message)
	{
		Range = range;
	}

	public string Error(string filePath)
	{
		// The starting and ending positions in the range store the line as a 0-based index, so we increment the
		// line by 1 to get a value that humans expect.
		return $"[{filePath} line {Range.Start.Line + 1}] {Message}";
	}
}

