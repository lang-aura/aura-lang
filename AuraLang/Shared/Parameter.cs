using AuraLang.AST;
using AuraLang.Token;
using AuraLang.Types;

namespace AuraLang.Shared;

public record struct Param(Tok Name, ParamType ParamType);

public record struct ParamType(AuraType Typ, bool Variadic, ILiteral? DefaultValue);