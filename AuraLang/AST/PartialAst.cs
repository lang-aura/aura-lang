using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using AuraLang.Visitor;
using Range = AuraLang.Location.Range;

namespace AuraLang.AST;

/// <summary>
///     Represents a partially typed class
/// </summary>
/// <param name="Class">A token representing the class's <c>class</c> keyword, which is used to help determine its
/// starting position in the Aura source file</param>
/// <param name="Name">The partially typed class's name</param>
/// <param name="Params">The partially typed class's parameters</param>
/// <param name="Methods">The partially typed class's methods</param>
/// <param name="Public">Indicates if the partially typed class is public or not</param>
/// <param name="ClosingBrace">A token representing the class's closing brace, which is used to determine its ending
/// position in the Aura source file</param>
/// <param name="Typ">The class's type</param>
public record PartiallyTypedClass
(
    Tok Class,
    Tok Name,
    List<Param> Params,
    List<AuraNamedFunction> Methods,
    Visibility Public,
    Tok ClosingBrace,
    AuraType Typ
) : ITypedAuraStatement, IUntypedFunction
{
    public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

    public List<ParamType> GetParamTypes() { return Params.Select(param => param.ParamType).ToList(); }

    public List<Param> GetParams() { return Params; }

    public Range Range => new(
        Class.Range.Start,
        ClosingBrace.Range.End
    );
}