using MsPosition = Microsoft.VisualStudio.LanguageServer.Protocol.Position;

namespace AuraLang.Location;

/// <summary>
/// Represents a single position in an Aura source file
/// </summary>
public record Position
{
	/// <summary>
	/// The zero-based character position. Because Aura source files are indented with four
	/// spaces instead of a single tab character, the first character on a line with a single
	/// indentation would be at position 4.
	/// </summary>
	public int Character;
	/// <summary>
	/// The zero-based lined in the Aura source file. Blank lines are included when counting
	/// lines.
	/// </summary>
	public int Line;

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

	public override string ToString() => $"{Line}:{Character}";
}
