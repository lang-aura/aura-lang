using System.Text;
using AuraLang.AST;

namespace AuraLang.Compiler;

/// <summary>
/// Compiles a section of an Aura source to Go. The compilation process tracks three different sections (<c>pkg</c>, <c>imports</c>,
/// <c>statements</c>), compiling a separate string for each section and then combining them at the end.
/// </summary>
public class AuraStringBuilder
{
	private StringBuilder sb;
	private int lastLine = 0;

	public AuraStringBuilder()
	{
		sb = new(string.Empty);
	}

	public AuraStringBuilder(string s)
	{
		sb = new(s);
	}

	public void WriteString(string s, int line, ITypedAuraStatement typ)
	{
		// A value of 0 for `lastLine` indicates that no string has previously been written to this struct
		if (lastLine == 0)
		{
			sb.Append(s);
			lastLine = line;
		}
		// If `lastLine` is less than the current string's line, we prepend a newlin to the string before writing it to the string builder
		else if (lastLine < line)
		{
			sb.Append($"\n{s}");
			lastLine = line;
		}
		else
		{
			// If the current string will be written to the same line as the previous string, we separate the two strings with either a space
			// (if the new string is a comment), or an explicit semicolon
			if (typ is TypedComment)
			{
				sb.Append($" {s}");
			}
			else
			{
				sb.Append($"; {s}");
			}
		}
	}

	public string String() => sb.ToString();
}
