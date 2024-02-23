namespace AuraLang.Exceptions.TypeChecker;

/// <summary>
///     Contains exceptions thrown by the type checker
/// </summary>
public class TypeCheckerExceptionContainer : AuraExceptionContainer
{
	public TypeCheckerExceptionContainer(string filePath) : base(filePath) { }

	public void Add(TypeCheckerException ex)
	{
		Exs.Add(ex);
	}
}
