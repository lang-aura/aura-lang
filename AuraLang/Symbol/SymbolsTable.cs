using AuraLang.AST;
using AuraLang.Prelude;
using AuraLang.Types;
using AuraModule = AuraLang.Types.AuraModule;

namespace AuraLang.Symbol;

public interface IGlobalSymbolsTable
{
	ISymbolsNamespace? GetNamespace(string name);
	AuraModule? GetNamespaceAsModule(string name);
	AuraSymbol? GetSymbol(string name, string symbolsNamespace);
	void AddNamespace(string name);
	void AddModule(AuraModule module);
	bool TryAddSymbol(AuraSymbol symbol, string symbolsNamespace);
	void AddScope(string symbolsNamespace);
	void ExitScope(string namespace_);
}

public class GlobalSymbolsTable : IGlobalSymbolsTable
{
	private readonly Dictionary<string, ISymbolsNamespace> _symbolsTable = new();

	public GlobalSymbolsTable()
	{
		Initialize();
	}

	private void Initialize()
	{
		AddModule(new AuraPrelude().GetPrelude());
	}

	public ISymbolsNamespace? GetNamespace(string name)
	{
		if (_symbolsTable.TryGetValue(name, out ISymbolsNamespace? symbolsNamespace)) return symbolsNamespace;
		return null;
	}

	public AuraModule? GetNamespaceAsModule(string name)
	{
		var namespace_ = GetNamespace(name);
		if (namespace_ is null) return null;

		return namespace_.ParseAsModule();
	}

	public AuraSymbol? GetSymbol(string name, string symbolsNamespace)
	{
		var namespace_ = GetNamespace(symbolsNamespace);
		if (namespace_ is null) return null;

		return namespace_.Find(name);
	}

	public void AddNamespace(string name) => _symbolsTable.TryAdd(name, new SymbolsNamespace(name));

	public void AddModule(AuraModule module)
	{
		// First, add the module's namespace
		AddNamespace(module.Name);
		// Then, add all of the module's exported functions, classes, variables, etc.
		foreach (var f in module.PublicFunctions)
		{
			TryAddSymbol(
				symbol: new AuraSymbol(
					Name: f.Name,
					Kind: f
				),
				symbolsNamespace: module.Name
			);
		}
		foreach (var c in module.PublicClasses)
		{
			TryAddSymbol(
				symbol: new AuraSymbol(
					Name: c.Name,
					Kind: c
				),
				symbolsNamespace: module.Name
			);
		}
		foreach (var (varName, v) in module.PublicVariables)
		{
			TryAddSymbol(
				symbol: new AuraSymbol(
					Name: varName,
					Kind: v.Typ
				),
				symbolsNamespace: module.Name
			);
		}
	}

	public bool TryAddSymbol(AuraSymbol symbol, string symbolsNamespace)
	{
		var namespace_ = GetNamespace(symbolsNamespace);
		if (namespace_ is null)
		{
			AddNamespace(symbolsNamespace);
			namespace_ = GetNamespace(symbolsNamespace);
		}

		namespace_!.AddSymbol(symbol);
		return true;
	}

	public void AddScope(string symbolsNamespace)
	{
		// Get namespace (or create if it doesn't already exist)
		var n = GetNamespace(symbolsNamespace);
		if (n is null)
		{
			AddNamespace(symbolsNamespace);
			n = GetNamespace(symbolsNamespace);
		}

		n!.AddScope();
	}

	public void ExitScope(string namespace_)
	{
		var n = GetNamespace(namespace_);
		if (n is null) return;

		n.ExitScope();
	}
}

public interface ISymbolsNamespace
{
	void AddScope();
	void AddSymbol(AuraSymbol symbol);
	AuraSymbol? Find(string name);
	AuraModule ParseAsModule();
	void ExitScope();
}

public class SymbolsNamespace : ISymbolsNamespace
{
	private string Name { get; init; }
	private readonly List<ISymbolsTable> _scopes = new();

	public SymbolsNamespace(string name)
	{
		Name = name;
	}

	public void AddScope() => _scopes.Insert(0, new SymbolsTable());

	public void AddSymbol(AuraSymbol symbol)
	{
		// If the namespace contains no scopes, add one first
		if (_scopes.Count == 0) _scopes.Add(new SymbolsTable());
		_scopes.First().Add(symbol);
	}

	public AuraSymbol? Find(string name)
	{
		foreach (var scope in _scopes)
		{
			var symbol = scope.Find(name);
			if (symbol is not null) return symbol;
		}
		return null;
	}

	public AuraModule ParseAsModule()
	{
		var symbols = _scopes[0].GetAllSymbols();

		var functions = new List<AuraNamedFunction>();
		var classes = new List<AuraClass>();
		var variables = new Dictionary<string, ITypedAuraExpression>();
		foreach (var symbol in symbols)
		{
			if (symbol.Kind is AuraNamedFunction f) functions.Add(f);
			if (symbol.Kind is AuraClass c) classes.Add(c);
			// TODO parse exported variables
		}

		return new AuraModule(
			name: Name,
			publicFunctions: functions,
			publicClasses: classes,
			publicVariables: variables
		);
	}

	public void ExitScope() => _scopes.RemoveAt(0);
}

/// <summary>
/// Represents a single lexical scope in a single namespace
/// </summary>
public interface ISymbolsTable
{
	/// <summary>
	/// Adds a new symbol
	/// </summary>
	/// <param name="symbol">The local variable to be added</param>
	void Add(AuraSymbol symbol);

	/// <summary>
	/// Find an existing local variable, if it exists
	/// </summary>
	/// <param name="name">The name of the variable</param>
	AuraSymbol? Find(string name);

	List<AuraSymbol> GetAllSymbols();
}

public class SymbolsTable : ISymbolsTable
{
	private readonly Dictionary<string, AuraSymbol> _symbols = new();

	public void Add(AuraSymbol symbol) => _symbols.TryAdd(symbol.Name, symbol);

	public AuraSymbol? Find(string name)
	{
		if (_symbols.TryGetValue(name, out var symbol)) return symbol;
		return null;
	}

	public List<AuraSymbol> GetAllSymbols() => _symbols.Values.ToList();
}
