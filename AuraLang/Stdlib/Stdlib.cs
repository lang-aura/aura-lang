using AuraLang.Compiler;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using AuraString = AuraLang.Types.String;

namespace AuraLang.Stdlib;

public class AuraStdlib
{
    private readonly Dictionary<string, Module> _modules = new()
    {
        { "aura/io", new Module("io", new List<Function>
        {
            new(
                "println",
                new AnonymousFunction(
                    new List<TypedParamType>
                    {
                        new(new AuraString(), false, null)
                    },
                    new Nil())
                ),
            new(
                "printf",
                new AnonymousFunction(
                    new List<TypedParamType>
                    {
                        new(new AuraString(), false, null),
                        new(new Any(), true, null)
                    },
                    new Nil())
                )
        })}
    };

    public Dictionary<string, Module> GetAllModules() => _modules;

    public bool TryGetModule(string name, out Module mod) =>  _modules.TryGetValue(name, out mod);
}