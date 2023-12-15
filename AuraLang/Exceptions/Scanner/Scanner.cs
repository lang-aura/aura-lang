namespace AuraLang.Exceptions.Scanner;

public class ScannerExceptionContainer : AuraExceptionContainer
{
	public void Add(ScannerException ex)
	{
		Exs.Add(ex);
	}
}

public abstract class ScannerException : AuraException
{
	protected ScannerException(string message, string filePath, int line) : base(message, filePath, line) { }
}

/// <summary>
/// Indicates that a string literal in the source code does not have a closing double quote
/// </summary>
public class UnterminatedStringException : ScannerException
{
	public UnterminatedStringException(string filePath, int line) : base("Unterminated string", filePath, line) { }
}

/// <summary>
/// Indicates that a char literal in the source code does not have a closing single quote
/// </summary>
public class UnterminatedCharException : ScannerException
{
	public UnterminatedCharException(string filePath, int line) : base("Unterminated char", filePath, line) { }
}

/// <summary>
/// Indicates that a char literal in the source code contains more than one character
/// </summary>
public class CharLengthGreaterThanOneException : ScannerException
{
	public CharLengthGreaterThanOneException(string filePath, int line) : base("Char length greater than 1", filePath,
		line)
	{
	}
}

/// <summary>
/// Indicates that a char literal in the source code contains zero characters
/// </summary>
public class EmptyCharException : ScannerException
{
	public EmptyCharException(string filePath, int line) : base("Empty char", filePath, line) { }
}

/// <summary>
/// Indicates that an invalid character is present in the source code
/// </summary>
public class InvalidCharacterException : ScannerException
{
	public InvalidCharacterException(string filePath, int line) : base("Invalid character", filePath, line) { }
}
