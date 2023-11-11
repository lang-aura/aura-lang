using AuraLang.Shared;

namespace AuraLang.Types;

public interface ICallable
{
    List<ParamType> GetParamTypes();
    AuraType GetReturnType();
}

