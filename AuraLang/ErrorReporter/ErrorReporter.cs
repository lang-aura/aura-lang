using AuraLang.Exceptions;

namespace AuraLang.Parser;

public class ErrorReporter
{
	private readonly List<AuraException> _errors = new();
	private readonly string _sep = "\n\n\n";

	public void Report(TextWriter tw)
	{
		tw.Write(Format());
	}

	public string Format()
	{
		return _errors
			.Select(e => $"[{e.Line}] {e.Message}")
			.Aggregate("", (prev, curr) => prev + _sep + curr);
	}

	public void Add(AuraException ex)
	{
		_errors.Add(ex);
	}
}
