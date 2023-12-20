namespace AuraLang.Exceptions.TypeChecker;

public class TypeCheckerExceptionContainer : AuraExceptionContainer
{
	public TypeCheckerExceptionContainer(string filePath) : base(filePath) { }

	public void Add(TypeCheckerException ex)
	{
		Exs.Add(ex);
	}
}
