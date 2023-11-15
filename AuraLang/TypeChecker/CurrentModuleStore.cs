using AuraLang.AST;

namespace AuraLang.TypeChecker;

/// <summary>
/// Contains the type checker's current module
/// </summary>
public interface ICurrentModuleStore
{
    /// <summary>
    /// Gets the name of the current module
    /// </summary>
    /// <returns>The name of the current module</returns>
    string? GetName();
    /// <summary>
    /// Sets the current module
    /// </summary>
    /// <param name="mod">The new current module</param>
    void Set(TypedMod mod);
}

public class CurrentModuleStore : ICurrentModuleStore
{
    private TypedMod? _currentModule;

    public string? GetName() => _currentModule?.Value.Value;
    public void Set(TypedMod mod) => _currentModule = mod;
}