using AuraLang.AST;

namespace AuraLang.Compiler;

public class GoDocument
{
	/// <summary>
	/// The Go package. This will be written to the top of hte Go output file
	/// </summary>
	private readonly AuraStringBuilder _pkg = new();
	/// <summary>
	/// Any imports to include in the Go output file
	/// </summary>
	private readonly Dictionary<string, bool> _imports = new();
	/// <summary>
	/// The body of the Go file
	/// </summary>
	private readonly AuraStringBuilder _statements = new();

	public void WriteStmt(string s, int line, ITypedAuraStatement typ)
	{
		switch (typ)
		{
			case TypedMod:
				_pkg.WriteString(s, line, typ);
				break;
			case TypedImport:
				_imports[s] = true;
				break;
			default:
				_statements.WriteString(s, line, typ);
				break;
		}
	}

	public string Assemble()
	{
		var imports = _imports.Count == 0 ? string.Empty : _imports.Select(i => i.Key).Aggregate("\n", (prev, curr) => $"{prev}\n{curr}").ToString();
		var statements = _statements.String() == string.Empty ? string.Empty : $"\n\n{_statements.String()}";
		return $"{_pkg.String()}{imports}{statements}\n";
	}
}
