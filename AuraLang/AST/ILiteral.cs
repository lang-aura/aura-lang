using AuraLang.Types;
using AuraChar = AuraLang.Types.Char;
using AuraString = AuraLang.Types.String;

namespace AuraLang.AST;

public interface ILiteral
{
    public object Value { get; }
}

/// <summary>
/// Represents an integer literal
/// </summary>
/// <param name="I">The integer value</param>
public record IntLiteral(long I, int Line) : IUntypedAuraExpression, ITypedAuraExpression, ILiteral
{
    public AuraType Typ => new Int();
    public object Value => I;
}

/// <summary>
/// Represents a float literal
/// </summary>
/// <param name="F">The float value</param>
public record FloatLiteral(double F, int Line) : IUntypedAuraExpression, ITypedAuraExpression, ILiteral
{
    public AuraType Typ => new Float();
    public object Value => F;
}

/// <summary>
/// Represents a string literal
/// </summary>
/// <param name="S">The string value</param>
public record StringLiteral(string S, int Line) : IUntypedAuraExpression, ITypedAuraExpression, ILiteral
{
    public AuraType Typ => new AuraString();
    public object Value => S;
}

/// <summary>
/// Represents a list literal
/// </summary>
/// <param name="L">The list value</param>
public record ListLiteral(List<IAuraAstNode> L, AuraType Kind, int Line) : IUntypedAuraExpression, ITypedAuraExpression, ILiteral
{
    public AuraType Typ => new List(Kind);
    public object Value => L;
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
public record MapLiteral(Dictionary<IAuraAstNode, IAuraAstNode> M, AuraType KeyType, AuraType ValueType, int Line) : IUntypedAuraExpression, ITypedAuraExpression, ILiteral
{
    public AuraType Typ => new Map(KeyType, ValueType);
    public object Value => M;
}

/// <summary>
/// Represents a boolean literal
/// </summary>
/// <param name="B">The boolean value</param>
public record BoolLiteral(bool B, int Line) : IUntypedAuraExpression, ITypedAuraExpression, ILiteral
{
    public AuraType Typ => new Bool();
    public object Value => B;
}

/// <summary>
/// Represents a char literal
/// </summary>
/// <param name="C">The char value</param>
public record CharLiteral(char C, int Line) : IUntypedAuraExpression, ITypedAuraExpression, ILiteral
{
    public AuraType Typ => new AuraChar();
    public object Value => C;
}