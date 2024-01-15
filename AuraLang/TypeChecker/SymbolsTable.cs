using AuraLang.Prelude;
using AuraLang.Types;

namespace AuraLang.TypeChecker;

/// <summary>
/// Stores variables on behalf of the Type Checker
/// </summary>
public interface ISymbolsTable
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
	Local? Find(string varName, string? modName);

	/// <summary>
	/// Clears all local variables that were defined in the specified scope
	/// </summary>
	/// <param name="scope">The scope to be cleared</param>
	void ExitScope(int scope);
}

public class SymbolsTable : ISymbolsTable
{
	private readonly List<Local> _symbols = new();

	public SymbolsTable()
	{
		foreach (var p in AuraPrelude.Prelude)
		{
			_symbols.Add(new Local(
				Name: ((NamedFunction)p).Name,
				Kind: p,
				Scope: 1,
				Defining: null
			));
		}
	}

	public void Add(Local local) => _symbols.Add(local);

	public Local? Find(string varName, string? modName)
	{
		var local = _symbols.Find(v => v.Name == varName &&
										 (v.Defining is null && modName is null ||
										  v.Defining == modName));
		if (local.Equals(default)) return null;
		return local;
	}

	public void ExitScope(int scope)
	{
		for (var i = _symbols.Count - 1; i >= 0; i--)
		{
			if (_symbols[i].Scope < scope)
			{
				break;
			}

			_symbols.RemoveAt(_symbols.Count - 1);
		}
	}
}
