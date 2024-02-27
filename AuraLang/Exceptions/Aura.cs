using Range = AuraLang.Location.Range;

namespace AuraLang.Exceptions;

/// <summary>
///     Acts as a container for Aura exceptions thrown during the compilation process
/// </summary>
public abstract class AuraExceptionContainer : Exception
{
	/// <summary>
	///     The exceptions thrown during the compilation process
	/// </summary>
	public readonly List<AuraException> Exs = new();

	/// <summary>
	///     The path of the file that produced the exception(s)
	/// </summary>
	private string FilePath { get; }

	protected AuraExceptionContainer(string filePath)
	{
		FilePath = filePath;
	}

	/// <summary>
	///     Determines if the container contains any exceptions
	/// </summary>
	/// <returns>A boolean value indicating if the container is empty</returns>
	public bool IsEmpty() => Exs.Count == 0;

	/// <summary>
	///     Formats any contained errors into a human-readable format
	/// </summary>
	/// <returns>The formatted exception(s)</returns>
	public string Report()
	{
		var errs = Exs.Select(ex => ex.Error(FilePath));
		return string.Join("\n\n", errs);
	}
}

/// <summary>
///     Represents an error encountered during the compilation process
/// </summary>
public abstract class AuraException : Exception
{
	/// <summary>
	///     The range of the AST node that triggered the error
	/// </summary>
	public Range[] Range { get; }

	protected AuraException(string message, params Range[] range) : base(message)
	{
		Range = range;
	}

	/// <summary>
	///     Formats the error into a human-friendly format
	/// </summary>
	/// <param name="filePath">The path of the file where the error was encountered</param>
	/// <returns>The formatted error</returns>
	public string Error(string filePath)
	{
		// The starting and ending positions in the range store the line as a 0-based index, so we increment the
		// line by 1 to get a value that humans expect.
		return $"[{filePath} line {Range[0].Start.Line + 1}] {Message}";
	}
}

