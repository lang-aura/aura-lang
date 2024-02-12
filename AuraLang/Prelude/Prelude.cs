using AuraLang.AST;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using Range = AuraLang.Location.Range;

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
								typ: TokType.Identifier,
								value: "message",
								range: new Range(),
								line: 1
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
		publicInterfaces: new List<AuraInterface>(),
		publicClasses: new List<AuraClass>(),
		publicVariables: new Dictionary<string, ITypedAuraExpression>()
	);

	public AuraModule GetPrelude() => _prelude;
}
