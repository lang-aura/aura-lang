using AuraLang.Shared;

namespace AuraLang.AST;

public interface IUntypedFunction
{
	public List<UntypedParam> GetParams();
	public List<UntypedParamType> GetParamTypes();
}

public interface ITypedFunction
{
	public List<TypedParam> GetParams();
	public List<TypedParamType> GetParamTypes();
}