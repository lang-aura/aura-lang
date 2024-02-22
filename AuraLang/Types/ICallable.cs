using AuraLang.Shared;

namespace AuraLang.Types;

public interface ICallable
{
	List<Param> GetParams();
	List<ParamType> GetParamTypes();
	AuraType GetReturnType();
	int GetParamIndex(string name);
	bool HasVariadicParam();
	string ToAuraString();
	string Documentation { get; }
}

