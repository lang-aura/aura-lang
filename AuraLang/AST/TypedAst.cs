using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;

namespace AuraLang.AST;

public interface ITypedAuraAstNode : IAuraAstNode
{
    AuraType Typ { get; }
}

public interface ITypedAuraExpression : ITypedAuraAstNode { }

public interface ITypedAuraStatement : ITypedAuraAstNode { }

public interface ITypedAuraCallable
{
    string GetName();
}

/*type TypedClass interface {

    GetAttribute(string)(types.AuraType, bool)

    GetParam(string)(types.AuraType, bool)

    GetMethod(string)(types.AuraType, bool)

    GetName() string
}*/

/// <summary>
/// Represents a typed assignment expression.
/// </summary>
/// <param name="Name">The variable's name</param>
/// <param name="Value">the variable's new value</param>
public record TypedAssignment(Tok Name, ITypedAuraExpression Value, AuraType Typ, int Line) : ITypedAuraExpression;

/// <summary>
/// Represents a typed binary expression. Both the left and right expressions must have the same type.
/// </summary>
/// <param name="Left">The expression on the left side of the binary expression</param>
/// <param name="Operator">The binary expression's operator</param>
/// <param name="Right">The expression on the right side of the binary expression</param>
public record TypedBinary(ITypedAuraExpression Left, Tok Operator, ITypedAuraExpression Right, AuraType Typ, int Line) : ITypedAuraExpression;

/// <summary>
/// Represents a typed block expression
/// </summary>
/// <param name="Statements">A collection of statements</param>
public record TypedBlock(List<ITypedAuraStatement> Statements, AuraType Typ, int Line) : ITypedAuraExpression;

/// <summary>
/// Represents a typed call expression
/// </summary>
/// <param name="Callee">The callee expressions</param>
/// <param name="Arguments">The call's arguments</param>
public record TypedCall(ITypedAuraCallable Callee, List<ITypedAuraExpression> Arguments, AuraType Typ, int Line) : ITypedAuraExpression;

/// <summary>
/// Represents a typed get expression
/// </summary>
/// <param name="Obj">The compound object being queried. It will have an attribute matching the <see cref="Name"/> parameter</param>
/// <param name="Name">The name of the attribute to get</param>
public record TypedGet(ITypedAuraExpression Obj, Tok Name, AuraType Typ, int Line) : ITypedAuraExpression, ITypedAuraCallable
{
    public string GetName() => Name.Value;
}

/// <summary>
/// Represents a fully typed expression that fetched an individual item from an indexable data type
/// </summary>
/// <param name="Obj">The collection object being queried.</param>
/// <param name="Index">The index in the collection to fetch</param>
public record TypedGetIndex(ITypedAuraExpression Obj, ITypedAuraExpression Index, AuraType Typ, int Line) : ITypedAuraExpression;

/// <summary>
/// Represents a fully typed expression that fetches a range of items from an indexable data type
/// </summary>
/// <param name="Obj">The collection object being queried</param>
/// <param name="Lower">The lower bound of the range being fetched. This bound is inclusive.</param>
/// <param name="Upper">The upper bound of the range being fetched. This bound is exclusive.</param>
public record TypedGetIndexRange(ITypedAuraExpression Obj, ITypedAuraExpression Lower, ITypedAuraExpression Upper, AuraType Typ, int Line) : ITypedAuraExpression;

/// <summary>
/// Represents a typed <c>if</c> expression
/// </summary>
/// <param name="Condition">The condition that will determine which branch is executed</param>
/// <param name="Then">The branch that will be executed if the <see cref="Condition"/> evaluates to true</param>
/// <param name="Else">The branch that will be executed if the <see cref="Condition"/> evalutes to false</param>
public record TypedIf(ITypedAuraExpression Condition, TypedBlock Then, ITypedAuraExpression? Else, AuraType Typ, int Line) : ITypedAuraExpression;

/// <summary>
/// Represents a type-checked logical expression
/// </summary>
/// <param name="Left">The expression on the left side of the logical expression</param>
/// <param name="Operator">The logical expression's operator</param>
/// <param name="Right">The expression on the right side of the logical expression</param>
public record TypedLogical(ITypedAuraExpression Left, Tok Operator, ITypedAuraExpression Right, AuraType Typ, int Line): ITypedAuraExpression;

/// <summary>
/// Represents a valid type-checked <c>set</c> expression
/// </summary>
/// <param name="Obj">The compound object whose attribute is being assigned a new value</param>
/// <param name="Name">The name of the attribute that is being assigned a new value</param>
/// <param name="Value">The new value</param>
public record TypedSet(ITypedAuraExpression Obj, Tok Name, ITypedAuraExpression Value, AuraType Typ, int Line) : ITypedAuraExpression;

/// <summary>
/// Represents a type-checked <c>this</c> token
/// </summary>
/// <param name="Keyword">The <c>this</c> token</param>
public record TypedThis(Tok Keyword, AuraType Typ, int Line) : ITypedAuraExpression;

/// <summary>
/// Represents a type-checked unary expression
/// </summary>
/// <param name="Operator">The unary expression's operator. Will be one of <c>!</c> or <c>-</c></param>
/// <param name="Right">The expression onto which the operator will be applied</param>
public record TypedUnary(Tok Operator, ITypedAuraExpression Right, AuraType Typ, int Line) : ITypedAuraExpression;

/// <summary>
/// Represents a type-checked variable
/// </summary>
/// <param name="Name">The variable's name</param>
public record TypedVariable(Tok Name, AuraType Typ, int Line) : ITypedAuraExpression, ITypedAuraCallable
{
    public string GetName() => Name.Value;
}

/// <summary>
/// Represents a type-checked grouping expression
/// </summary>
/// <param name="Expr">The expression contained in the grouping expression</param>
public record TypedGrouping(ITypedAuraExpression Expr, AuraType Typ, int Line) : ITypedAuraExpression;

/// <summary>
/// Represents a type-checked <c>defer</c> statement
/// </summary>
/// <param name="Call">The call expression to be deferred until the end of the enclosing function's scope</param>
public record TypedDefer(TypedCall Call, int Line) : ITypedAuraStatement
{
    public AuraType Typ => new None();
}

/// <summary>
/// Represents a type-checked expression statement
/// </summary>
/// <param name="Expression">The enclosed expression</param>
public record TypedExpressionStmt(ITypedAuraExpression Expression, int Line) : ITypedAuraStatement
{
    public AuraType Typ => new None();
}

/// <summary>
/// Represents a valid type-checked <c>for</c> loop
/// </summary>
/// <param name="Initializer">The loop's initializer. The variable initialized here will be available inside the loop's body.</param>
/// <param name="Condition">The loop's condition. The loop will exit when the condition evaluates to false.</param>
/// <param name="Body">Collection of statements that will be executed once per iteration.</param>
public record TypedFor(ITypedAuraStatement? Initializer, ITypedAuraExpression? Condition,
    List<ITypedAuraStatement> Body, int Line) : ITypedAuraStatement
{
    public AuraType Typ => new None();
}

/// <summary>
/// Represents a valid type-checked  <c>foreach</c> loop
/// </summary>
/// <param name="EachName">Represents the current item in the collection being iterated over</param>
/// <param name="Iterable">The collection being iterated over</param>
/// <param name="Body">Collection of statements that will be executed once per iteration</param>
public record TypedForEach
    (Tok EachName, ITypedAuraExpression Iterable, List<ITypedAuraStatement> Body, int Line) : ITypedAuraStatement
{
    public AuraType Typ => new None();
}

/// <summary>
/// Represents a valid type-checked named function declaration
/// </summary>
/// <param name="Name">The function's name</param>
/// <param name="Params">The function's parameters</param>
/// <param name="Body">The function's body</param>
/// <param name="ReturnType">The function's return type</param>
public record TypedNamedFunction(Tok Name, List<Param> Params, TypedBlock Body, AuraType ReturnType,
    Visibility Public, int Line) : ITypedAuraStatement, ITypedFunction
{
    public AuraType Typ => new None();
    public List<Param> GetParams() => Params;
    public List<ParamType> GetParamTypes() => Params.Select(param => param.ParamType).ToList();
}

/// <summary>
/// Represents a valid type-checked anonymous function
/// </summary>
/// <param name="Params">The anonymous function's parameters</param>
/// <param name="Body">The anonymous function's body</param>
/// <param name="ReturnType">The anonymous function's return type</param>
public record TypedAnonymousFunction(List<Param> Params, TypedBlock Body, AuraType ReturnType, int Line) : ITypedAuraExpression, ITypedFunction
{
    public AuraType Typ => new AnonymousFunction(Params, ReturnType);
    public List<Param> GetParams() => Params;
    public List<ParamType> GetParamTypes() => Params.Select(param => param.ParamType).ToList();
}

/// <summary>
/// Represents a valid type-checked variable declaration
/// </summary>
/// <param name="Name">The variable's name</param>
/// <param name="TypeAnnotation">Indicates whether the variable was declared with a type annotation</param>
/// <param name="Mutable">Indicates whether the variable was declared as mutable</param>
/// <param name="Initializer">The initializer expression. This can be omitted.</param>
public record TypedLet
    (Tok Name, bool TypeAnnotation, bool Mutable, ITypedAuraExpression? Initializer, int Line) : ITypedAuraStatement
{
    public AuraType Typ => new None();
}

/// <summary>
/// Represents a type-checked module declaration
/// </summary>
/// <param name="Value">The module's name</param>
public record TypedMod(Tok Value, int Line) : ITypedAuraStatement
{
    public AuraType Typ => new None();
}

/// <summary>
/// Represents a valid type-checked <c>return</c> statement
/// </summary>
/// <param name="Value">The value to return</param>
public record TypedReturn(ITypedAuraExpression? Value, int Line) : ITypedAuraStatement
{
    public AuraType Typ => new None();
}

/// <summary>
/// Represents a valid type-checked class declaration
/// </summary>
/// <param name="Name">The class's name</param>
/// <param name="Params">The class's parameters</param>
/// <param name="Methods">The class's methods</param>
/// <param name="Public">Indicates whether the class is declared as public</param>
public record FullyTypedClass(Tok Name, List<Param> Params, List<TypedNamedFunction> Methods, Visibility Public,
    int Line) : ITypedAuraStatement, ITypedFunction
{
    public AuraType Typ => new None();
    public List<Param> GetParams() => Params;
    public List<ParamType> GetParamTypes() => Params.Select(param => param.ParamType).ToList();
    
}

/// <summary>
/// Represents a valid type-checked <c>while</c> loop
/// </summary>
/// <param name="Condition">The loop's condition. The loop will exit when the condition evaluates to false</param>
/// <param name="Body">Collection of statements executed once per iteration</param>
public record TypedWhile(ITypedAuraExpression Condition, List<ITypedAuraStatement> Body, int Line) : ITypedAuraStatement
{
    public AuraType Typ => new None();
}

/// <summary>
/// Represents a valid type-checked <c>import</c> statement
/// </summary>
/// <param name="Package">The name of the package being imported</param>
/// <param name="Alias">Will have a value if the package is being imported under an alias</param>
public record TypedImport(Tok Package, Tok? Alias, int Line) : ITypedAuraStatement
{
    public AuraType Typ => new None();
}

/// <summary>
/// Represents a type-checked comment
/// </summary>
/// <param name="Text">The text of the comment</param>
public record TypedComment(Tok Text, int Line) : ITypedAuraStatement
{
    public AuraType Typ => new None();
}

/// <summary>
/// Represents a type-checked <c>continue</c> statement
/// </summary>
public record TypedContinue(int Line) : ITypedAuraStatement
{
    public AuraType Typ => new None();
}

/// <summary>
/// Represents a type-checked <c>break</c> statement
/// </summary>
public record TypedBreak(int Line) : ITypedAuraStatement
{
    public AuraType Typ => new None();
}

/// <summary>
/// Represents a type-checked <c>nil</c> keyword
/// </summary>
public record TypedNil(int Line) : ITypedAuraExpression
{
    public AuraType Typ => new Nil();
}

/// <summary>
/// Represents a type-checked <c>yield</c> statement
/// </summary>
/// <param name="Value">The value to be yielded from the enclosing expression</param>
public record TypedYield(ITypedAuraExpression Value, int Line) : ITypedAuraStatement
{
    public AuraType Typ => new None();
}
