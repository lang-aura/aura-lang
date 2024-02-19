using System.Globalization;
using AuraLang.Token;
using AuraLang.Types;
using AuraLang.Visitor;
using Range = AuraLang.Location.Range;

namespace AuraLang.AST;

public interface ILiteral : IUntypedAuraExpression, ITypedAuraExpression { }

public interface ILiteral<out T> : ILiteral
{
	public T Value { get; }
}

/// <summary>
/// Represents an integer literal
/// </summary>
/// <param name="Int">The integer value</param>
public record IntLiteral(Tok Int) : ILiteral<long>
{
	public T Accept<T>(IUntypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraInt();
	public long Value => int.Parse(Int.Value);
	public override string ToString() => $"{Value}";
	public Range Range => Int.Range;
	public string HoverText => "int literal";
}

/// <summary>
/// Represents a float literal
/// </summary>
/// <param name="Float">The float value</param>
public record FloatLiteral(Tok Float) : ILiteral<double>
{
	public T Accept<T>(IUntypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraFloat();
	public double Value => float.Parse(Float.Value, CultureInfo.InvariantCulture);
	public override string ToString() => $"{Value}";
	public Range Range => Float.Range;
	public string HoverText => "float literal";
}

/// <summary>
/// Represents a string literal
/// </summary>
/// <param name="String">The string value</param>
public record StringLiteral(Tok String) : ILiteral<string>
{
	public T Accept<T>(IUntypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraString();
	public string Value => String.Value;
	public override string ToString() => $"{Value}";
	public Range Range => String.Range;
	public string HoverText => "string literal";
}

/// <summary>
/// Represents a list literal
/// </summary>
/// <param name="L">The list value</param>
public record ListLiteral<T>(Tok OpeningBracket, List<T> L, AuraType Kind, Tok ClosingBrace) : ILiteral<List<T>>
	where T : IAuraAstNode
{
	public U Accept<U>(IUntypedAuraExprVisitor<U> visitor) => visitor.Visit(this);
	public U Accept<U>(ITypedAuraExprVisitor<U> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraList(Kind);
	public List<T> Value => L;
	public override string ToString() => $"{Value}";
	public Range Range => new(
		start: OpeningBracket.Range.Start,
		end: ClosingBrace.Range.End
	);
	public string HoverText => "list literal";
}

/// <summary>
/// Represents a map data type in Aura. A simple map literal would be declared like this:
/// <code>
/// map[string : int]{
///     "Hello": 1,
///     "World": 2,
/// }
/// </code>
/// The map's type signature stored in this record will be used by the type checker to confirm that each element
/// in the map matches its expected type. Its important to note that, even though the key and value types are stored
/// in this untyped record, the map object has not yet been type checked, and it is not guaranteed that the map's elements
/// match their expected type.
/// </summary>
/// <param name="M">The map value</param>
public record MapLiteral<TK, TV>(Tok Map, Dictionary<TK, TV> M, AuraType KeyType, AuraType ValueType, Tok ClosingBrace) : ILiteral<Dictionary<TK, TV>>
	where TK : IAuraAstNode
	where TV : IAuraAstNode
{
	public T Accept<T>(IUntypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraMap(KeyType, ValueType);
	public Dictionary<TK, TV> Value => M;
	public override string ToString() => $"{Value}";
	public Range Range => new(
		start: Map.Range.Start,
		end: ClosingBrace.Range.End
	);
	public string HoverText => "map literal";
}

/// <summary>
/// Represents a boolean literal
/// </summary>
/// <param name="Bool">The boolean value</param>
public record BoolLiteral(Tok Bool) : ILiteral<bool>
{
	public T Accept<T>(IUntypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraBool();
	public bool Value => bool.Parse(Bool.Value);
	public override string ToString() => $"{Value}";
	public Range Range => Bool.Range;
	public string HoverText => "bool literal";
}

/// <summary>
/// Represents a char literal
/// </summary>
/// <param name="Char">The char value</param>
public record CharLiteral(Tok Char) : ILiteral<char>
{
	public T Accept<T>(IUntypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraChar();
	public char Value => char.Parse(Char.Value);
	public override string ToString() => $"{Value}";
	public Range Range => Char.Range;
	public string HoverText => "char literal";
}
