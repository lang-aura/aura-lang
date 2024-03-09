using AuraLang.Token;

namespace AuraLang.Exceptions.Scanner;

/// <summary>
///     A container that holds exceptions thrown by the scanner
/// </summary>
public class ScannerExceptionContainer : AuraExceptionContainer<List<Tok>>
{
	public override List<Tok>? Valid { get; set; }

	public ScannerExceptionContainer(string filePath) : base(filePath) { }

	public void Add(ScannerException ex)
	{
		Exs.Add(ex);
	}
}
