using AuraLang.AST;

namespace AuraLang.Compiler;

/// <summary>
///     Represents a Go output file
/// </summary>
public class GoDocument
{
	/// <summary>
	///     The Go package. This will be written to the top of the Go output file
	/// </summary>
	private readonly AuraStringBuilder _pkg = new();

	/// <summary>
	///     Any imports to include in the Go output file
	/// </summary>
	private readonly Dictionary<string, bool> _imports = new();

	/// <summary>
	///     The body of the Go file
	/// </summary>
	private readonly AuraStringBuilder _statements = new();

	/// <summary>
	///     Writes a statement to the Go output file
	/// </summary>
	/// <param name="s">A valid Go string representing an Aura statement</param>
	/// <param name="line">
	///     The statement's line. This parameter is more relative than absolute, and is intended to help
	///     order the statements within each section of the Go document. For example, because the <c>_pkg</c>, <c>_imports</c>
	///     and <c>_statements</c> sections will be assembled into a final Go file, a statement in the <c>_statements</c>
	///     section
	///     with a line of 1 won't appear on line 1 in the assembled Go file, but it will appear before a statement in the
	///     <c>_statements</c> section with a line of 2
	/// </param>
	/// <param name="typ">
	///     The statement's type, which is used to determine which section of the Go document this string
	///     should be written to
	/// </param>
	public void WriteStmt(string s, int line, ITypedAuraStatement typ)
	{
		switch (typ)
		{
			case TypedMod:
				_pkg.WriteString(
					s,
					line,
					typ
				);
				break;
			case TypedImport:
				_imports[s] = true;
				break;
			default:
				_statements.WriteString(
					s,
					line,
					typ
				);
				break;
		}
	}

	/// <summary>
	///     Assembles the final Go output file
	/// </summary>
	/// <returns>A valid Go file</returns>
	public string Assemble()
	{
		var imports = _imports.Count == 0
			? string.Empty
			: _imports.Select(i => i.Key).Aggregate("\n", (prev, curr) => $"{prev}\n{curr}").ToString();
		var statements = _statements.String() == string.Empty ? string.Empty : $"\n\n{_statements.String()}";
		return $"{_pkg.String()}{imports}{statements}\n";
	}
}
