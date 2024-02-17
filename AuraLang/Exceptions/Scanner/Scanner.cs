using Range = AuraLang.Location.Range;

namespace AuraLang.Exceptions.Scanner;

public abstract class ScannerException : AuraException
{
	protected ScannerException(string message, Range range) : base(message, range) { }
}

/// <summary>
/// Indicates that a string literal in the source code does not have a closing double quote
/// </summary>
public class UnterminatedStringException : ScannerException
{
	public UnterminatedStringException(Range range) : base("Unterminated string", range) { }
}

/// <summary>
/// Indicates that a char literal in the source code does not have a closing single quote
/// </summary>
public class UnterminatedCharException : ScannerException
{
	public UnterminatedCharException(Range range) : base("Unterminated char", range) { }
}

/// <summary>
/// Indicates that a char literal in the source code contains more than one character
/// </summary>
public class CharLengthGreaterThanOneException : ScannerException
{
	public CharLengthGreaterThanOneException(Range range) : base("Char length greater than 1", range) { }
}

/// <summary>
/// Indicates that a char literal in the source code contains zero characters
/// </summary>
public class EmptyCharException : ScannerException
{
	public EmptyCharException(Range range) : base("Empty char", range) { }
}

/// <summary>
/// Indicates that an invalid character is present in the source code
/// </summary>
public class InvalidCharacterException : ScannerException
{
	public InvalidCharacterException(char found, Range range) : base($"Invalid character: `{found}`", range) { }
}
