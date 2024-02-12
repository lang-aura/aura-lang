using System.Diagnostics.CodeAnalysis;

namespace AuraLang.Location;

/// <summary>
/// Represents a range in an Aura source file, with both the starting and ending positions
/// included. Ranges can be used to place both tokens and AST nodes in an source file.
/// </summary>
public readonly struct Range
{
	/// <summary>
	/// The starting position, inclusive
	/// </summary>
	readonly Position Start;
	/// <summary>
	/// The ending position, inclusive
	/// </summary>
	readonly Position End;

	public Range(Position start, Position end)
	{
		Start = start;
		End = end;
	}

	public Range()
	{
		Start = new Position();
		End = new Position();
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is null) return false;
		if (obj is not Range r) return false;
		if (r.Start != Start) return false;
		if (r.End != End) return false;

		return true;
	}

	public static bool operator ==(Range left, Range right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Range left, Range right)
	{
		return !(left == right);
	}

	public override int GetHashCode() => ToString().GetHashCode();

	public override string ToString() => $"{Start}-{End}";
}
