using MsPosition = Microsoft.VisualStudio.LanguageServer.Protocol.Position;

namespace AuraLang.Location;

/// <summary>
/// Represents a single position in an Aura source file
/// </summary>
/// <param name="Character">
/// The zero-based character position. Because Aura source files are indented with four
/// spaces instead of a single tab character, the first character on a line with a single
/// indentation would be at position 4.
/// </param>
/// <param name="Line">
/// The zero-based line in the Aura source file. Blank lines are included when counting
/// lines.
/// </param>
public record Position(int Character, int Line)
{
	public Position() : this(0, 0) { }

	/// <summary>
	///     Converts a Microsoft <see cref="MsPosition" /> object to an Aura <see cref="Position" /> object. This is useful
	///     when handling
	///     messages received from an LSP client
	/// </summary>
	/// <param name="pos">The Microsoft <see cref="MsPosition" /> to convert</param>
	/// <returns>An Aura <see cref="Position" /> record</returns>
	public static Position FromMicrosoftPosition(MsPosition pos)
	{
		return new Position
		{
			Line = pos.Line,
			Character = pos.Character
		};
	}

	/// <summary>
	///     Returns a new position located one position before the current position
	/// </summary>
	/// <returns>A new position located one position before the current position</returns>
	public Position OnePositionBefore()
	{
		// TODO what if character is 0?
		return this with { Character = Character - 1 };
	}

	public override string ToString() => $"{Line}:{Character}";
}
