using AuraLang.Shared;

namespace AuraLang.Types;

public interface ICallable
{
    List<TypedParam> GetParams();
    List<TypedParamType> GetParamTypes();
    AuraType GetReturnType();
    int GetParamIndex(string name);
    bool HasVariadicParam();
}

