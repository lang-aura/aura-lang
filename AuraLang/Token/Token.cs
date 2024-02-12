using Range = AuraLang.Location.Range;

namespace AuraLang.Token;

public readonly record struct Tok
{
	/// <summary>
	/// The token's type
	/// </summary>
	public readonly TokType Typ { get; }
	/// <summary>
	/// The token's value as it appears in the Aura source code
	/// </summary>
	public readonly string Value { get; }
	/// <summary>
	/// The range in the Aura source file where the token appears
	/// </summary>
	public readonly Range Range { get; }
	/// <summary>
	/// The line in the Aura source file where the token appears. While the <c>Range</c> attribute also contains a <c>Line</c> field,
	/// this attribute is 1-based, meaning that it represents the line of the Aura source file as a human would describe it, whereas the <c>Line</c>
	/// attribute in <c>Range</c> is 0-based.
	/// </summary>
	public readonly int Line { get; }

	public Tok(TokType typ, string value, Range range, int line)
	{
		Typ = typ;
		Value = value;
		Range = range;
		Line = line;
	}

	public Tok(TokType typ, string value, int line)
	{
		Typ = typ;
		Value = value;
		Range = new Range();
		Line = line;
	}
}
