using AuraLang.Shared;

namespace AuraLang.Types;

public interface ICallable
{
    List<TypedParamType> GetParamTypes();
    AuraType GetReturnType();
}

