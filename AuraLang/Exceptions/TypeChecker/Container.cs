using AuraLang.AST;

namespace AuraLang.Exceptions.TypeChecker;

/// <summary>
///     Contains exceptions thrown by the type checker
/// </summary>
public class TypeCheckerExceptionContainer : AuraExceptionContainer<List<ITypedAuraStatement>>
{
	public override List<ITypedAuraStatement>? Valid { get; set; }

	public TypeCheckerExceptionContainer(string filePath) : base(filePath)
	{
	}

	public void Add(TypeCheckerException ex)
	{
		Exs.Add(ex);
	}

	public void Add(TypeCheckerExceptionContainer exC)
	{
		foreach (var ex in exC.Exs) Exs.Add(ex);
	}
}
