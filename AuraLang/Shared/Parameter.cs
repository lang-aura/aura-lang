using AuraLang.AST;
using AuraLang.Token;
using AuraLang.Types;

namespace AuraLang.Shared;

public record struct UntypedParam(Tok Name, UntypedParamType ParamType);

public record struct UntypedParamType(Tok Typ, bool Variadic, UntypedAuraExpression? DefaultValue);

public record struct TypedParam(Tok Name, TypedParamType ParamType);

public record struct TypedParamType(AuraType Typ, bool Variadic, TypedAuraExpression? DefaultValue);