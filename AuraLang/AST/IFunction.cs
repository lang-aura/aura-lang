using AuraLang.Shared;

namespace AuraLang.AST;

public interface IUntypedFunction
{
	public List<Param> GetParams();
	public List<ParamType> GetParamTypes();
}

public interface ITypedFunction
{
	public List<Param> GetParams();
	public List<ParamType> GetParamTypes();
}