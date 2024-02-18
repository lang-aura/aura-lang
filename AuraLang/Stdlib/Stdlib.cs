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
						name: "printf",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "format"
									),
									ParamType: new(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								),
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "a"
									),
									ParamType: new ParamType(
										Typ: new AuraAny(),
										Variadic: true,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraNil()
						)
					),
					new(
						name: "println",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "s"
									),
									ParamType: new(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraNil()
						)
					),
					new(
						name: "print",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "s"
									),
									ParamType: new(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraNil()
						)
					),
					new(
						name: "eprintln",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "s"
									),
									ParamType: new(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraNil()
						)
					),
					new(
						name: "eprint",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "s"
									),
									ParamType: new(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraNil()
						)
					),
					new(
						name: "readln",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>(),
							returnType: new AuraString()
						)
					),
					new(
						name: "read_file",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "path"
									),
									ParamType: new(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraString()
						)
					),
					new(
						name: "read_lines",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "path"
									),
									ParamType: new(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraList(kind: new AuraString())
						)
					),
					new(
						name: "write_file",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "path"
									),
									ParamType: new(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								),
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "content"
									),
									ParamType: new(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraNil()
						)
					)
				},
				publicInterfaces: new List<AuraInterface>(),
				publicClasses: new List<AuraClass>(),
				publicVariables: new Dictionary<string, ITypedAuraExpression>()
			)
		},
		{
			"aura/strings", new AuraModule(
				name: "strings",
				publicFunctions: new List<AuraNamedFunction>
				{
					new(
						name: "to_lower",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "s"
									),
									ParamType: new(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraString()
						)
					),
					new(
						name: "to_upper",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "s"
									),
									ParamType: new(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraString()
						)
					),
					new(
						name: "contains",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "s"
									),
									ParamType: new(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								),
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "sub"
									),
									ParamType: new(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraBool()
						)
					),
					new(
						name: "length",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "s"
									),
									ParamType: new(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraBool()
						)
					),
					new(
						name: "split",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "s"
									),
									ParamType: new(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								),
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "sep"
									),
									ParamType: new ParamType(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraBool()
						)
					),
					new(
						name: "to_int",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "s"
									),
									ParamType: new ParamType(
										Typ: new AuraString(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraBool()
						)
					),
				},
				publicInterfaces: new List<AuraInterface>(),
				publicClasses: new List<AuraClass>(),
				publicVariables: new Dictionary<string, ITypedAuraExpression>())
		},
		{
			"aura/lists", new AuraModule(
				name: "lists",
				publicFunctions: new List<AuraNamedFunction>
				{
					new(
						name: "contains",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "a"
									),
									ParamType: new(
										Typ: new AuraList(kind: new AuraAny()),
										Variadic: false,
										DefaultValue: null
									)
								),
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "item"
									),
									ParamType: new(
										Typ: new AuraAny(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraBool()
						)
					),
					new(
						name: "is_empty",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "a"
									),
									ParamType: new ParamType(
										Typ: new AuraList(kind: new AuraAny()),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraBool()
						)
					),
					new(
						name: "length",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "a"
									),
									ParamType: new ParamType(
										Typ: new AuraList(kind: new AuraAny()),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraInt()
						)
					),
					new(
						name: "map_",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "a"
									),
									ParamType: new(
										Typ: new AuraList(kind: new AuraAny()),
										Variadic: false,
										DefaultValue: null
									)
								),
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "b"
									),
									ParamType: new(
										Typ: new AuraFunction(
											fParams: new List<Param>
											{
												new(
													Name: new Tok(
														typ: TokType.Identifier,
														value: "t"
													),
													ParamType: new(
														Typ: new AuraAny(),
														Variadic: false,
														DefaultValue: null
													)
												)
											},
											returnType: new AuraAny()
										),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraList(kind: new AuraAny())
						)
					),
					new(
						name: "filter",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "a"
									),
									ParamType: new(
										Typ: new AuraList(kind: new AuraAny()),
										Variadic: false,
										DefaultValue: null
									)
								),
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "f"
									),
									ParamType: new ParamType(
										Typ: new AuraFunction(
											fParams: new List<Param>
											{
												new(
													Name: new Tok(
														typ: TokType.Identifier,
														value: "t"
													),
													ParamType: new(
														Typ: new AuraAny(),
														Variadic: false,
														DefaultValue: null
													)
												)
											},
											returnType: new AuraBool()
										),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraList(kind: new AuraAny())
						)
					),
					new(
						name: "reduce",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "a"
									),
									ParamType: new(
										Typ: new AuraList(kind: new AuraAny()),
										Variadic: false,
										DefaultValue: null
									)
								),
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "f"
									),
									ParamType: new(
										Typ: new AuraFunction(
											fParams: new List<Param>
											{
												new(
													Name: new Tok(
														typ: TokType.Identifier,
														value: "t1"
													),
													ParamType: new(
														Typ: new AuraAny(),
														Variadic: false,
														DefaultValue: null
													)
												),
												new(
													Name: new Tok(
														typ: TokType.Identifier,
														value: "t2"
													),
													ParamType: new(
														Typ: new AuraAny(),
														Variadic: false,
														DefaultValue: null
													)
												)
											},
											returnType: new AuraBool()
										),
										Variadic: false,
										DefaultValue: null
									)
								),
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "t"
									),
									ParamType: new(
										Typ: new AuraAny(),
										Variadic: false,
										DefaultValue: null
									)
								),
							},
							returnType: new AuraAny()
						)
						),
					new(
						name: "min",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "a"
									),
									ParamType: new(
										Typ: new AuraList(new AuraInt()),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraInt()
						)
					),
					new(
						name: "max",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "a"
									),
									ParamType: new(
										Typ: new AuraList(kind: new AuraInt()),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraInt()
						)
					),
					new(
						name: "push",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "a"
									),
									ParamType: new(
										Typ: new AuraList(kind: new AuraAny()),
										Variadic: false,
										DefaultValue: null
									)
								),
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "t"
									),
									ParamType: new(
										Typ: new AuraAny(),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraNil()
						)
					),
					new(
						name: "pop",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "a"
									),
									ParamType: new(
										Typ: new AuraList(kind: new AuraAny()),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraNil()
						)
					),
					new(
						name: "sum",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "a"
									),
									ParamType: new(
										Typ: new AuraList(kind: new AuraInt()),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraInt()
						)
					),
				},
				publicInterfaces: new List<AuraInterface>(),
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
										typ: TokType.Identifier,
										value: "err"
									),
									ParamType: new(
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
				publicInterfaces: new List<AuraInterface>(),
				publicClasses: new List<AuraClass>(),
				publicVariables: new Dictionary<string, ITypedAuraExpression>()
			)
		},
		{
			"aura/results", new AuraModule(
				name: "results",
				publicFunctions: new List<AuraNamedFunction>
				{
					new(
						name: "is_success",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "st"
									),
									ParamType: new(
										Typ: new AuraAnonymousStruct(
											parameters: new List<Param>
											{
												new(
													Name: new Tok(
														typ: TokType.Identifier,
														value: "success"
													),
													ParamType: new(
														Typ: new AuraAny(),
														Variadic: false,
														DefaultValue: null
													)
												),
												new(
													Name: new Tok(
														typ: TokType.Identifier,
														value: "failure"
													),
													ParamType: new(
														Typ: new AuraError(),
														Variadic: false,
														DefaultValue: null
													)
												)
											},
											pub: Visibility.Private
										),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraBool()
						)
					),
					new(
						name: "success",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "st"
									),
									ParamType: new(
										Typ: new AuraAnonymousStruct(
											parameters: new List<Param>
											{
												new(
													Name: new Tok(
														typ: TokType.Identifier,
														value: "success"
													),
													ParamType: new(
														Typ: new AuraAny(),
														Variadic: false,
														DefaultValue: null
													)
												),
												new(
													Name: new Tok(
														typ: TokType.Identifier,
														value: "failure"
													),
													ParamType: new(
														Typ: new AuraError(),
														Variadic: false,
														DefaultValue: null
													)
												)
											},
											pub: Visibility.Private
										),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraAny()
						)
					),
					new(
						name: "is_failure",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "st"
									),
									ParamType: new(
										Typ: new AuraAnonymousStruct(
											parameters: new List<Param>
											{
												new(
													Name: new Tok(
														typ: TokType.Identifier,
														value: "success"
													),
													ParamType: new(
														Typ: new AuraAny(),
														Variadic: false,
														DefaultValue: null
													)
												),
												new(
													Name: new Tok(
														typ: TokType.Identifier,
														value: "failure"
													),
													ParamType: new(
														Typ: new AuraError(),
														Variadic: false,
														DefaultValue: null
													)
												)
											},
											pub: Visibility.Private
										),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraBool()
						)
					),
					new(
						name: "failure",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>
							{
								new(
									Name: new Tok(
										typ: TokType.Identifier,
										value: "st"
									),
									ParamType: new(
										Typ: new AuraAnonymousStruct(
											parameters: new List<Param>
											{
												new(
													Name: new Tok(
														typ: TokType.Identifier,
														value: "success"
													),
													ParamType: new(
														Typ: new AuraAny(),
														Variadic: false,
														DefaultValue: null
													)
												),
												new(
													Name: new Tok(
														typ: TokType.Identifier,
														value: "failure"
													),
													ParamType: new(
														Typ: new AuraError(),
														Variadic: false,
														DefaultValue: null
													)
												)
											},
											pub: Visibility.Private
										),
										Variadic: false,
										DefaultValue: null
									)
								)
							},
							returnType: new AuraError()
						)
					),
				},
				publicInterfaces: new List<AuraInterface>(),
				publicClasses: new List<AuraClass>(),
				publicVariables: new Dictionary<string, ITypedAuraExpression>()
			)
		}
	};

	public Dictionary<string, AuraModule> GetAllModules() => _modules;

	public bool TryGetModule(string name, out AuraModule? mod) => _modules.TryGetValue(name, out mod);
}
