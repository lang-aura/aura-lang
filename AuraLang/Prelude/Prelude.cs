using AuraLang.AST;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;

namespace AuraLang.Prelude;

public class AuraPrelude
{
	private readonly AuraModule _prelude = new(
		name: "prelude",
		publicFunctions: new List<AuraNamedFunction>
		{
			new(
				name: "err",
				pub: Visibility.Public,
				new AuraFunction(
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
					returnType: new AuraError()
				)
			)
		},
		publicClasses: new List<AuraClass>(),
		publicVariables: new Dictionary<string, ITypedAuraExpression>()
	);

	public AuraModule GetPrelude() => _prelude;
}
