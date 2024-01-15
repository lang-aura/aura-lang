using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using AuraString = AuraLang.Types.String;

namespace AuraLang.Prelude;

public static class AuraPrelude
{
	public static readonly List<AuraType> Prelude = new()
	{
		new NamedFunction(
			name: "error",
			pub: Visibility.Public,
			new Function(
				fParams: new List<Param>
				{
					new(
						Name: new Tok(
							Typ: TokType.Identifier,
							Value: "message",
							Line: 1
						),
						ParamType: new ParamType(
							Typ: new AuraString(),
							Variadic: false,
							DefaultValue: null
						)
					)
				},
				returnType: new Error()
			)
		)
	};
}
