namespace AuraLang.Location;

/// <summary>
/// Represents a range in an Aura source file. Ranges can be used to place both tokens and AST nodes in an source file.
/// </summary>
/// <param name="Start">The starting position, inclusive</param>
/// <param name="End">The ending position, exclusive</param>
public record Range(Position Start, Position End)
{
	public Range() : this(new Position(), new Position()) { }

	/// <summary>
	///     Checks if the supplied position is located inside this range
	/// </summary>
	/// <param name="position">The position in question</param>
	/// <returns>A boolean value indicating if the supplied position is located inside this range</returns>
	public bool Contains(Position position)
	{
		if (position.Line < Start.Line || position.Line > End.Line) return false;
		return position.Character >= Start.Character && position.Character < End.Character;
	}

	public override string ToString() => $"{Start}-{End}";
}
