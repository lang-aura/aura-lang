using AuraLang.AST;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;

namespace AuraLang.Stdlib;

public static class AuraStdlib
{
	private static readonly Dictionary<string, AuraModule> Modules = new()
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
						),
						documentation: "Prints the supplied format string to stdout"
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
						),
						documentation: "Prints the supplied string to stdout, followed by a newline"
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
						),
						documentation: "Prints the supplied string to stdout"
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
						),
						documentation: "Prints the supplied string to stderr, followed by a newline"
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
						),
						documentation: "Prints the supplied string to stderr"
					),
					new(
						name: "readln",
						pub: Visibility.Public,
						f: new AuraFunction(
							fParams: new List<Param>(),
							returnType: new AuraString()
						),
						documentation: "Reads a single line from stdin"
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
						),
						documentation: "Reads the entire contents of the file located at the supplied path, returning the contents as a single string"
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
						),
						documentation: "Reads the entire contents of the file located at the supplied path, returning the contents as a list of strings, where each string represents a single line in the file"
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
						),
						documentation: "Writes the supplied content to the file located at the supplied path"
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
						),
						documentation: "Returns a new string where all characters in the original have been converted to lower case"
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
						),
						documentation: "Returns a new string where all characters in the original have been converted to upper case"
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
						),
						documentation: "Returns a boolean indicating if the string `s` contains the supplied substring"
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
						),
						documentation: "Returns the number of characters in the supplied string"
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
							returnType: new AuraList(kind: new AuraString())
						),
						documentation: "Separates `s` into all of the substrings separated by (but not including) `sep`"
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
						),
						documentation: "Converts the supplied string to its corresponding integer value"
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
						),
						documentation: "Returns a boolean value indicating if the supplied list contains `item`"
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
						),
						documentation: "Returns a boolean value indicating if the supplied list is empty (i.e. contains zero items)"
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
						),
						documentation: "Returns the number of items contained in the supplied list"
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
						),
						documentation: "Applies the supplied function to each item in the supplied list, returning a new list containing the result of each invocation"
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
						),
						documentation: "Returns a new list containing only those items in the original list that return a value of `true` when passed in to the supplied anonymous function"
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
						),
						documentation: "Reduces the supplied list to a single item"
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
						),
						documentation: "Returns the minimum value contained in the list"
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
						),
						documentation: "Returns the maximum value contained in the list"
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
						),
						documentation: "Adds a new item to the end of the supplied list"
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
						),
						documentation: "Removes the last item in the list"
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
						),
						documentation: "Sums together all items in the list"
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
						),
						documentation: "Returns the error's message"
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
						),
						documentation: "Returns a boolean value indicating if the result type contains a success value"
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
						),
						documentation: "Returns the result's success type, if it exists"
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
						),
						documentation: "Returns a boolean value indicating if the result type contains a failure value"
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
						),
						documentation: "Returns the result type's failure value, if it exists"
					),
				},
				publicInterfaces: new List<AuraInterface>(),
				publicClasses: new List<AuraClass>(),
				publicVariables: new Dictionary<string, ITypedAuraExpression>()
			)
		}
	};

	public static Dictionary<string, AuraModule> GetAllModules() => Modules;

	public static bool TryGetModule(string name, out AuraModule? mod) => Modules.TryGetValue(name, out mod);
}
