using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using AuraLang.Visitor;
using Range = AuraLang.Location.Range;

namespace AuraLang.AST;

/// <summary>
/// Represents a partially typed function
/// </summary>
/// <param name="Name">The partially typed function's name</param>
/// <param name="Params">The partially type function's parameters</param>
/// <param name="Body">The partially typed function's body</param>
/// <param name="ReturnType">The partially typed function's return type</param>
public record PartiallyTypedFunction(Tok Fn, Tok Name, List<Param> Params, UntypedBlock Body, AuraType ReturnType, Visibility Public) : ITypedAuraStatement, IUntypedFunction
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => ReturnType;
	public List<ParamType> GetParamTypes() => Params.Select(param => param.ParamType).ToList();
	public List<Param> GetParams() => Params;
	public Range Range => new(
		start: Public == Visibility.Public ? Fn.Range.Start with { Character = Fn.Range.Start.Character - 4 } : Fn.Range.Start,
		end: Body.Range.End
	);
	public string HoverText => "partially typed function";
}

/// <summary>
/// Represents a partially typed class
/// </summary>
/// <param name="Name">The partially typed class's name</param>
/// <param name="Params">The partially typed class's parameters</param>
/// <param name="Methods">The partially typed class's methods</param>
/// <param name="Public">Indicates if the partially typed class is public or not</param>
public record PartiallyTypedClass(Tok Class, Tok Name, List<Param> Params, List<AuraNamedFunction> Methods, Visibility Public, Tok ClosingBrace, AuraType Typ) : ITypedAuraStatement, IUntypedFunction
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public List<ParamType> GetParamTypes() => Params.Select(param => param.ParamType).ToList();
	public List<Param> GetParams() => Params;
	public Range Range => new(
		start: Class.Range.Start,
		end: ClosingBrace.Range.End
	);
	public string HoverText => "partially typed class";
}
