namespace AuraLang.Exceptions.Scanner;

/// <summary>
///     A container that holds exceptions thrown by the scanner
/// </summary>
public class ScannerExceptionContainer : AuraExceptionContainer
{
	public ScannerExceptionContainer(string filePath) : base(filePath) { }

	public void Add(ScannerException ex)
	{
		Exs.Add(ex);
	}
}
