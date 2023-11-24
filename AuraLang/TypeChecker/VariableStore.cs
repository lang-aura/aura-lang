using AuraLang.Exceptions.TypeChecker;
using AuraLang.Types;

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
	/// Find an existing local variable, if it exists, and confirm it matches the expected type
	/// </summary>
	/// <param name="varName">The name of the variable</param>
	/// <param name="modName">The name of the module where the variable was originally defined</param>
	/// <param name="expected">The expected type of the variable</param>
	/// <param name="line">The line where the variable is being fetched from. Used in the exceptions, if any are thrown.</param>
	/// <exception cref="UnknownVariableException">Thrown if the local variable doesn't exist</exception>
	/// <exception cref="UnexpectedTypeException">Thrown if the local variable doesn't match the expected type</exception>
	/// <returns>The local variable, if it exists and matches the expected type</returns>
	Local FindAndConfirm(string varName, string modName, AuraType expected, int line);
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

	public Local FindAndConfirm(string varName, string modName, AuraType expected, int line)
	{
		var local = Find(varName, modName) ?? throw new UnknownVariableException(line);
		if (!expected.IsSameOrInheritingType(local.Kind)) throw new UnexpectedTypeException(line);
		return local;
	}

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
