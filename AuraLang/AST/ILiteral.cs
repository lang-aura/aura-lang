using System.Globalization;
using AuraLang.Token;
using AuraLang.Types;
using AuraLang.Visitor;
using Range = AuraLang.Location.Range;

namespace AuraLang.AST;

/// <summary>
///     Represents a literal value, whose syntax is dependent on the literal's type. Because a literal's type is
///     self-evident from its representation in the source code, the <c>ILiteral</c> interface inherits from both
///     <see cref="IUntypedAuraExpression" /> and <see cref="ITypedAuraExpression" />
/// </summary>
public interface ILiteral : IUntypedAuraExpression, ITypedAuraExpression
{
}

/// <summary>
///     Represents a literal value whose type is represented by the generic type parameter <c>T</c>
/// </summary>
/// <typeparam name="T">The literal value's C# type</typeparam>
public interface ILiteral<out T> : ILiteral
{
	/// <summary>
	///     Returns the literal's inner value
	/// </summary>
	public T Value { get; }
}

/// <summary>
///     Represents an integer literal
/// </summary>
/// <param name="Int">The integer value</param>
public record IntLiteral(Tok Int) : ILiteral<long>
{
	public T Accept<T>(IUntypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraInt();
	public long Value => int.Parse(Int.Value);
	public override string ToString() { return $"{Value}"; }

	public Range Range => Int.Range;
}

/// <summary>
///     Represents a float literal
/// </summary>
/// <param name="Float">The float value</param>
public record FloatLiteral(Tok Float) : ILiteral<double>
{
	public T Accept<T>(IUntypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraFloat();
	public double Value => float.Parse(Float.Value, CultureInfo.InvariantCulture);
	public override string ToString() { return $"{Value}"; }

	public Range Range => Float.Range;
}

/// <summary>
///     Represents a string literal
/// </summary>
/// <param name="String">The string value</param>
public record StringLiteral(Tok String) : ILiteral<string>
{
	public T Accept<T>(IUntypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraString();
	public string Value => String.Value;
	public override string ToString() { return $"{Value}"; }

	public Range Range => String.Range;
}

/// <summary>
///     Represents a list literal
/// </summary>
/// <param name="OpeningBracket">
///     A token representing the list's opening bracket, which is used to determine the starting
///     point of the literal value's range in the source file
/// </param>
/// <param name="L">The list value</param>
/// <param name="Kind">The type of the values contained in the list</param>
/// <param name="ClosingBrace">
///     A token representing the list's closing bracket, which is used to determine the ending
///     point of the literal value's range in the source file
/// </param>
public record ListLiteral<T>(Tok OpeningBracket, List<T> L, AuraType Kind, Tok ClosingBrace) : ILiteral<List<T>>
	where T : IAuraAstNode
{
	public TU Accept<TU>(IUntypedAuraExprVisitor<TU> visitor) { return visitor.Visit(this); }

	public TU Accept<TU>(ITypedAuraExprVisitor<TU> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraList(Kind);
	public List<T> Value => L;
	public override string ToString() { return $"{Value}"; }

	public Range Range => new(
		OpeningBracket.Range.Start,
		ClosingBrace.Range.End
	);
}

/// <summary>
///     Represents a map data type in Aura. A simple map literal would be declared like this:
///     <code>
/// map[string : int]{
///     "Hello": 1,
///     "World": 2,
/// }
/// </code>
///     The map's type signature stored in this record will be used by the type checker to confirm that each element
///     in the map matches its expected type. Its important to note that, even though the key and value types are stored
///     in this untyped record, the map object has not yet been type checked, and it is not guaranteed that the map's
///     elements
///     match their expected type.
/// </summary>
/// <param name="Map">
///     A token representing the <c>map</c> keyword, which is the first token used to declare a map literal. This
///     token is used to determine the map's starting point in the map literal's range in the source file
/// </param>
/// <param name="M">The map value</param>
/// <param name="KeyType">The type of the map's keys</param>
/// <param name="ValueType">The type of the map's values</param>
/// <param name="ClosingBrace">
///     A token representing the map's closing brace, which is used to determine the map's ending point
///     in the Aura source file
/// </param>
public record MapLiteral<TK, TV>
	(Tok Map, Dictionary<TK, TV> M, AuraType KeyType, AuraType ValueType, Tok ClosingBrace)
	: ILiteral<Dictionary<TK, TV>>
	where TK : IAuraAstNode
	where TV : IAuraAstNode
{
	public T Accept<T>(IUntypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraMap(KeyType, ValueType);
	public Dictionary<TK, TV> Value => M;
	public override string ToString() { return $"{Value}"; }

	public Range Range => new(
		Map.Range.Start,
		ClosingBrace.Range.End
	);
}

/// <summary>
///     Represents a boolean literal
/// </summary>
/// <param name="Bool">The boolean value</param>
public record BoolLiteral(Tok Bool) : ILiteral<bool>
{
	public T Accept<T>(IUntypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraBool();
	public bool Value => bool.Parse(Bool.Value);
	public override string ToString() { return $"{Value}"; }

	public Range Range => Bool.Range;
}

/// <summary>
///     Represents a char literal
/// </summary>
/// <param name="Char">The char value</param>
public record CharLiteral(Tok Char) : ILiteral<char>
{
	public T Accept<T>(IUntypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraChar();
	public char Value => char.Parse(Char.Value);
	public override string ToString() { return $"{Value}"; }

	public Range Range => Char.Range;
}
