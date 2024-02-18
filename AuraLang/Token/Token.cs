using Range = AuraLang.Location.Range;

namespace AuraLang.Token;

public readonly record struct Tok
{
	/// <summary>
	/// The token's type
	/// </summary>
	public TokType Typ { get; }
	/// <summary>
	/// The token's value as it appears in the Aura source code
	/// </summary>
	public string Value { get; }
	/// <summary>
	/// The range in the Aura source file where the token appears
	/// </summary>
	public Range Range { get; }

	public Tok(TokType typ, string value, Range range)
	{
		Typ = typ;
		Value = value;
		Range = range;
	}

	public Tok(TokType typ, string value)
	{
		Typ = typ;
		Value = value;
		Range = new Range();
	}
}
