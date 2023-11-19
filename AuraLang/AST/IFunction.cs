using AuraLang.Shared;

namespace AuraLang.AST;

public interface IFunction
{
	public List<Param> GetParams();
	public List<ParamType> GetParamTypes();
}

