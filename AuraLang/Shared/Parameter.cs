using AuraLang.AST;
using AuraLang.Token;
using AuraLang.Types;
using Range = AuraLang.Location.Range;

namespace AuraLang.Shared;

public readonly record struct Param(Tok Name, ParamType ParamType) : IHoverable
{
	public Range Range => Name.Range;
	public Range HoverableRange => Name.Range;
	public string HoverText => $"(parameter) {Name.Value}: {ParamType.Typ.ToAuraString()}";
}

public record struct ParamType(AuraType Typ, bool Variadic, ILiteral? DefaultValue);
