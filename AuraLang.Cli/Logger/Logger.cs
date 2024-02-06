namespace AuraLang.Cli.Logger;

public class AuraCliLogger
{
	private bool Verbose { get; }

	public AuraCliLogger(bool verbose)
	{
		Verbose = verbose;
	}

	/// <summary>
	/// Logs the supplied message only if the logger was initialized with the <c>Verbose</c> level
	/// </summary>
	/// <param name="message">The message to log</param>
	public void LogVerbose(string message)
	{
		if (Verbose) Console.WriteLine(message);
	}

	/// <summary>
	/// Logs the supplied message, no matter the logger's level
	/// </summary>
	/// <param name="message">The message to log</param>
	public void LogSuccinct(string message) => Console.WriteLine(message);
}
