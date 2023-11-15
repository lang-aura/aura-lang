namespace AuraLang.TypeChecker;

/// <summary>
/// Stores variables on behalf of the Type Checker
/// </summary>
public interface IVariableStore
{
    /// <summary>
    /// Adds a new local variable
    /// </summary>
    /// <param name="local">The local variable to be added</param>
    void Add(Local local);
    /// <summary>
    /// Find an existing local variable, if it exists
    /// </summary>
    /// <param name="varName">The name of the variable</param>
    /// <param name="modName">The name of the module where the variable was originally defined</param>
    /// <returns>The existing local variable, if it exists, else null</returns>
    Local? Find(string varName, string modName);
    /// <summary>
    /// Clears all local variables that were defined in the specified scope
    /// </summary>
    /// <param name="scope">The scope to be cleared</param>
    void ExitScope(int scope);
}

public class VariableStore : IVariableStore
{
    private readonly List<Local> _variables = new();

    public void Add(Local local) => _variables.Add(local);
    
    public Local? Find(string varName, string modName) => _variables.Find(v => v.Name == varName && v.Module == modName);

    public void ExitScope(int scope)
    {
        for (var i = _variables.Count - 1; i >= 0; i--)
        {
            if (_variables[i].Scope < scope)
            {
                break;
            }
            _variables.RemoveAt(_variables.Count - 1);
        }
    }
}