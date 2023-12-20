namespace AuraLang.Exceptions.Scanner;

public class ScannerExceptionContainer : AuraExceptionContainer
{
	public ScannerExceptionContainer(string filePath) : base(filePath) { }

	public void Add(ScannerException ex)
	{
		Exs.Add(ex);
	}
}
