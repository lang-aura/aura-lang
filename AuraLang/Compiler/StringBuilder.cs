using System.Text;
using AuraLang.AST;

namespace AuraLang.Compiler;

/// <summary>
/// Compiles a section of an Aura source to Go. The compilation process tracks three different sections (<c>pkg</c>, <c>imports</c>,
/// <c>statements</c>), compiling a separate string for each section and then combining them at the end.
/// </summary>
public class AuraStringBuilder
{
	private readonly StringBuilder _sb = new(string.Empty);
	/// <summary>
	/// Keeps track of the line of the most recent string that was written to the string builder
	/// </summary>
	private int _lastLine;
	
	public void WriteString(string s, int line, ITypedAuraStatement typ)
	{
		// A value of 0 for `lastLine` indicates that no string has previously been written to this struct
		if (_lastLine == 0)
		{
			_sb.Append(s);
			_lastLine = line;
		}
		// If `lastLine` is less than the current string's line, we prepend a newline to the string before writing it to the string builder
		else if (_lastLine < line)
		{
			_sb.Append($"\n{s}");
			_lastLine = line;
		}
		else
		{
			// If the current string will be written to the same line as the previous string, we separate the two strings with either a space
			// (if the new string is a comment), or an explicit semicolon
			if (typ is TypedComment)
			{
				_sb.Append($" {s}");
			}
			else
			{
				_sb.Append($"; {s}");
			}
		}
	}

	public string String() => _sb.ToString();
}
