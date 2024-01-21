using AuraLang.AST;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;

namespace AuraLang.Stdlib;

public class AuraStdlib
{
	private readonly Dictionary<string, AuraModule> _modules = new()
	{
		{
			"aura/io", new AuraModule(
				name: "io",
				publicFunctions: new List<AuraNamedFunction>
				{
					new(
						"printf",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "format", 1),
									new ParamType(new AuraString(), false, null)),
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(new AuraAny(), true, null))
							},
							new AuraNil())
						),
					new(
						"println",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "s", 1),
									new ParamType(new AuraString(), false, null))
							},
							new AuraNil())
						),
					new(
						"print",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "s", 1),
									new ParamType(new AuraString(), false, null))
							},
							new AuraNil())
						),
					new(
						"eprintln",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "s", 1),
									new ParamType(new AuraString(), false, null))
							},
							new AuraNil())
						),
					new(
						"eprint",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "s", 1),
									new ParamType(new AuraString(), false, null))
							},
							new AuraNil())
						),
					new(
						"readln",
						Visibility.Public,
						new AuraFunction(
							new List<Param>(),
							new AuraString())
						),
					new(
						"read_file",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "path", 1),
									new ParamType(new AuraString(), false, null))
							},
							new AuraString())
						),
					new(
						"read_lines",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "path", 1),
									new ParamType(new AuraString(), false, null))
							},
							new AuraList(new AuraString()))
						),
					new(
						"write_file",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "path", 1),
									new ParamType(new AuraString(), false, null)),
								new(
									new Tok(TokType.Identifier, "content", 1),
									new ParamType(new AuraString(), false, null))


							},
							new AuraNil()))
				},
				publicClasses: new List<AuraClass>(),
				publicVariables: new Dictionary<string, ITypedAuraExpression>())
		},
		{
			"aura/strings", new AuraModule(
				name: "strings",
				publicFunctions: new List<AuraNamedFunction>
				{
					new(
						"to_lower",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "s", 1),
									new ParamType(
										new AuraString(),
										false,
										null))
							},
							new AuraString())
						),
					new(
						"to_upper",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "s", 1),
									new ParamType(
										new AuraString(),
										false,
										null))
							},
							new AuraString())
						),
					new(
						"contains",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "s", 1),
									new ParamType(
										new AuraString(),
										false,
										null)),
								new(
									new Tok(TokType.Identifier, "sub", 1),
									new ParamType(
										new AuraString(),
										false,
										null))
							},
							new AuraBool())
						),
					new(
						"length",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "s", 1),
									new ParamType(
										new AuraString(),
										false,
										null))
							},
							new AuraBool())
						),
					new(
						"split",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "s", 1),
									new ParamType(
										new AuraString(),
										false,
										null)),
								new(
									new Tok(TokType.Identifier, "sep", 1),
									new ParamType(
										new AuraString(),
										false,
										null))
							},
							new AuraBool())
						),
					new(
						"to_int",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "s", 1),
									new ParamType(
										new AuraString(),
										false,
										null))
							},
							new AuraBool())
						),
				},
				publicClasses: new List<AuraClass>(),
				publicVariables: new Dictionary<string, ITypedAuraExpression>())
		},
		{
			"aura/lists", new AuraModule(
				name: "lists",
				publicFunctions: new List<AuraNamedFunction>
				{
					new(
					"contains",
					Visibility.Public,
					new AuraFunction(
						new List<Param>
						{
							new(
								new Tok(TokType.Identifier, "a", 1),
								new ParamType(
									new AuraList(new AuraAny()),
									false,
									null)),
							new(
								new Tok(TokType.Identifier, "item", 1),
								new ParamType(
									new AuraAny(),
									false,
									null))
						},
						new AuraBool())
					),
					new(
					"is_empty",
					Visibility.Public,
					new AuraFunction(
						new List<Param>
						{
							new(
								new Tok(TokType.Identifier, "a", 1),
								new ParamType(
									new AuraList(new AuraAny()),
									false,
									null))
						},
						new AuraBool())
					),
					new(
						"length",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(
										new AuraList(new AuraAny()),
										false,
										null))
							},
							new AuraInt())
						),
					new(
						"map_",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(
										new AuraList(new AuraAny()),
										false,
										null)),
								new(
									new Tok(TokType.Identifier, "b", 1),
									new ParamType(
										new AuraFunction(
											new List<Param>
											{
												new(
													new Tok(TokType.Identifier, "t", 1),
													new ParamType(
														new AuraAny(),
														false,
														null))
											},
											new AuraAny()),
										false,
										null))
							},
							new AuraList(new AuraAny()))
						),
					new(
						"filter",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(
										new AuraList(new AuraAny()),
										false,
										null)),
								new(
									new Tok(TokType.Identifier, "f", 1),
									new ParamType(
										new AuraFunction(
											new List<Param>
											{
												new(
													new Tok(TokType.Identifier, "t", 1),
													new ParamType(
														new AuraAny(),
														false,
														null))
											},
											new AuraBool()),
										false,
										null))
							},
							new AuraList(new AuraAny()))
						),
					new(
						"reduce",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(
										new AuraList(new AuraAny()),
										false,
										null)),
								new(
									new Tok(TokType.Identifier, "f", 1),
									new ParamType(
										new AuraFunction(
											new List<Param>
											{
												new(
													new Tok(TokType.Identifier, "t1", 1),
													new ParamType(
														new AuraAny(),
														false,
														null)),
												new(
													new Tok(TokType.Identifier, "t2", 1),
													new ParamType(
														new AuraAny(),
														false,
														null))
											},
											new AuraBool()),
										false,
										null)),
								new(
									new Tok(TokType.Identifier, "t", 1),
									new ParamType(
										new AuraAny(),
										false,
										null)),
							},
							new AuraAny())
						),
					new(
						"min",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(
										new AuraList(new AuraInt()),
										false,
										null))
							},
							new AuraInt())
						),
					new(
						"max",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(
										new AuraList(new AuraInt()),
										false,
										null))
							},
							new AuraInt())
						),
					new(
						"push",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(
										new AuraList(new AuraAny()),
										false,
										null)),
								new(
									new Tok(TokType.Identifier, "t", 1),
									new ParamType(
										new AuraAny(),
										false,
										null))
							},
							new AuraNil())
						),
					new(
						"pop",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(
										new AuraList(new AuraAny()),
										false,
										null))
							},
							new AuraNil())
						),
					new(
						"sum",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(
										new AuraList(new AuraInt()),
										false,
										null))
							},
							new AuraInt())
						),
				},
				publicClasses: new List<AuraClass>(),
				publicVariables: new Dictionary<string, ITypedAuraExpression>())
		},
		{
			"aura/errors", new AuraModule(
				name: "errors",
				publicFunctions: new List<AuraNamedFunction>
				{
					new(
						name: "message",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										Typ: TokType.Identifier,
										Value: "err",
										Line: 1
									),
									ParamType: new ParamType(
										Typ: new AuraError(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraString()
						)
					)
				},
				publicClasses: new List<AuraClass>(),
				publicVariables: new Dictionary<string, ITypedAuraExpression>()
			)
		}
	};

	public Dictionary<string, AuraModule> GetAllModules() => _modules;

	public bool TryGetModule(string name, out AuraModule? mod) => _modules.TryGetValue(name, out mod);
}
