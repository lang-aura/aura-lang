using AuraLang.AST;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using AuraString = AuraLang.Types.String;

namespace AuraLang.Prelude;

public class AuraPrelude
{
	private readonly Module _prelude = new(
		name: "prelude",
		publicFunctions: new List<NamedFunction>
		{
			new(
				name: "err",
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
		},
		publicClasses: new List<Class>(),
		publicVariables: new Dictionary<string, ITypedAuraExpression>()
	);

	public Module GetPrelude() => _prelude;
}
