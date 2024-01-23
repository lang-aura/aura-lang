using AuraLang.Types;
using AuraLang.Visitor;

namespace AuraLang.AST;

public interface ILiteral : IUntypedAuraExpression, ITypedAuraExpression { }

public interface ILiteral<out T> : ILiteral
{
	public T Value { get; }
}

/// <summary>
/// Represents an integer literal
/// </summary>
/// <param name="I">The integer value</param>
public record IntLiteral(long I, int Line) : ILiteral<long>
{
	public T Accept<T>(IUntypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraInt();
	public long Value => I;
}

/// <summary>
/// Represents a float literal
/// </summary>
/// <param name="F">The float value</param>
public record FloatLiteral(double F, int Line) : ILiteral<double>
{
	public T Accept<T>(IUntypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraFloat();
	public double Value => F;
}

/// <summary>
/// Represents a string literal
/// </summary>
/// <param name="S">The string value</param>
public record StringLiteral(string S, int Line) : ILiteral<string>
{
	public T Accept<T>(IUntypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraString();
	public string Value => S;
}

/// <summary>
/// Represents a list literal
/// </summary>
/// <param name="L">The list value</param>
public record ListLiteral<T>(List<T> L, AuraType Kind, int Line) : ILiteral<List<T>>
	where T : IAuraAstNode
{
	public U Accept<U>(IUntypedAuraExprVisitor<U> visitor) => visitor.Visit(this);
	public U Accept<U>(ITypedAuraExprVisitor<U> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraList(Kind);
	public List<T> Value => L;
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
public record MapLiteral<TK, TV>(Dictionary<TK, TV> M, AuraType KeyType, AuraType ValueType, int Line) : ILiteral<Dictionary<TK, TV>>
	where TK : IAuraAstNode
	where TV : IAuraAstNode
{
	public T Accept<T>(IUntypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraMap(KeyType, ValueType);
	public Dictionary<TK, TV> Value => M;
}

/// <summary>
/// Represents a boolean literal
/// </summary>
/// <param name="B">The boolean value</param>
public record BoolLiteral(bool B, int Line) : ILiteral<bool>
{
	public T Accept<T>(IUntypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraBool();
	public bool Value => B;
}

/// <summary>
/// Represents a char literal
/// </summary>
/// <param name="C">The char value</param>
public record CharLiteral(char C, int Line) : ILiteral<char>
{
	public T Accept<T>(IUntypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraChar();
	public char Value => C;
}
