namespace AuraLang.Exceptions.Scanner;

public class IScannerException : Exception { }

/// <summary>
/// Indicates that a string literal in the source code does not have a closing double quote
/// </summary>
public class UnterminatedStringException : IScannerException { }

/// <summary>
/// Indicates that a char literal in the source code does not have a closing single quote
/// </summary>
public class UnterminatedCharException : IScannerException { }

/// <summary>
/// Indicates that a char literal in the source code contains more than one character
/// </summary>
public class CharLengthGreaterThanOneException : IScannerException { }

/// <summary>
/// Indicates that an invalid character is present in the source code
/// </summary>
public class InvalidCharacterException : IScannerException { }