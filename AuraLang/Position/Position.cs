using System.Diagnostics.CodeAnalysis;

namespace AuraLang.Location;

/// <summary>
/// Represents a single position in an Aura source file
/// </summary>
public readonly struct Position
{
	/// <summary>
	/// The zero-based character position. Because Aura source files are indented with four
	/// spaces instead of a single tab character, the first character on a line with a single
	/// indentation would be at position 4.
	/// </summary>
	readonly int Character;
	/// <summary>
	/// The zero-based lined in the Aura source file. Blank lines are included when counting
	/// lines.
	/// </summary>
	readonly int Line;

	public Position(int character, int line)
	{
		Character = character;
		Line = line;
	}

	public Position()
	{
		Character = 0;
		Line = 0;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is null) return false;
		if (obj is not Position p) return false;
		if (p.Character != Character) return false;
		if (p.Line != Line) return false;

		return true;
	}

	public static bool operator ==(Position left, Position right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Position left, Position right)
	{
		return !(left == right);
	}

	public override int GetHashCode() => ToString().GetHashCode();

	public override string ToString() => $"{Line}:{Character}";
}
