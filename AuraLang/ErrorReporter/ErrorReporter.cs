using AuraLang.Exceptions;

namespace AuraLang.ErrorReporter;

public class ErrorReporter
{
	private readonly List<AuraException> _errors = new();
	private const string Sep = "\n\n\n";

	public void Report(TextWriter tw)
	{
		tw.Write(Format());
	}

	public string Format()
	{
		return _errors
			.Select(e => $"[{e.Range.Start.Line + 1}] {e.Message}")
			.Aggregate("", (prev, curr) => prev + Sep + curr);
	}

	public void Add(AuraException ex)
	{
		_errors.Add(ex);
	}
}
