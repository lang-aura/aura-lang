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
				"io",
				new List<AuraNamedFunction>
				{
					new(
						"printf",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"format"
									),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								),
								new(
									new Tok(
										TokType.Identifier,
										"a"
									),
									new ParamType(
										new AuraAny(),
										true,
										null
									)
								)
							},
							new AuraNil()
						),
						"Prints the supplied format string to stdout"
					),
					new(
						"println",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"s"
									),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new AuraNil()
						),
						"Prints the supplied string to stdout, followed by a newline"
					),
					new(
						"print",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"s"
									),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new AuraNil()
						),
						"Prints the supplied string to stdout"
					),
					new(
						"eprintln",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"s"
									),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new AuraNil()
						),
						"Prints the supplied string to stderr, followed by a newline"
					),
					new(
						"eprint",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"s"
									),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new AuraNil()
						),
						"Prints the supplied string to stderr"
					),
					new(
						"readln",
						Visibility.Public,
						new AuraFunction(
							new List<Param>(),
							new AuraString()
						),
						"Reads a single line from stdin"
					),
					new(
						"read_file",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"path"
									),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new AuraString()
						),
						"Reads the entire contents of the file located at the supplied path, returning the contents as a single string"
					),
					new(
						"read_lines",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"path"
									),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new AuraList(new AuraString())
						),
						"Reads the entire contents of the file located at the supplied path, returning the contents as a list of strings, where each string represents a single line in the file"
					),
					new(
						"write_file",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"path"
									),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								),
								new(
									new Tok(
										TokType.Identifier,
										"content"
									),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new AuraNil()
						),
						"Writes the supplied content to the file located at the supplied path"
					)
				},
				new List<AuraInterface>(),
				new List<AuraClass>(),
				new Dictionary<string, ITypedAuraExpression>()
			)
		},
		{
			"aura/strings", new AuraModule(
				"strings",
				new List<AuraNamedFunction>
				{
					new(
						"to_lower",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"s"
									),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new AuraString()
						),
						"Returns a new string where all characters in the original have been converted to lower case"
					),
					new(
						"to_upper",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"s"
									),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new AuraString()
						),
						"Returns a new string where all characters in the original have been converted to upper case"
					),
					new(
						"contains",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"s"
									),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								),
								new(
									new Tok(
										TokType.Identifier,
										"sub"
									),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new AuraBool()
						),
						"Returns a boolean indicating if the string `s` contains the supplied substring"
					),
					new(
						"length",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"s"
									),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new AuraBool()
						),
						"Returns the number of characters in the supplied string"
					),
					new(
						"split",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"s"
									),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								),
								new(
									new Tok(
										TokType.Identifier,
										"sep"
									),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new AuraList(new AuraString())
						),
						"Separates `s` into all of the substrings separated by (but not including) `sep`"
					),
					new(
						"to_int",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"s"
									),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new AuraInt()
						),
						"Converts the supplied string to its corresponding integer value"
					)
				},
				new List<AuraInterface>(),
				new List<AuraClass>(),
				new Dictionary<string, ITypedAuraExpression>()
			)
		},
		{
			"aura/lists", new AuraModule(
				"lists",
				new List<AuraNamedFunction>
				{
					new(
						"contains",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"a"
									),
									new ParamType(
										new AuraList(new AuraAny()),
										false,
										null
									)
								),
								new(
									new Tok(
										TokType.Identifier,
										"item"
									),
									new ParamType(
										new AuraAny(),
										false,
										null
									)
								)
							},
							new AuraBool()
						),
						"Returns a boolean value indicating if the supplied list contains `item`"
					),
					new(
						"is_empty",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"a"
									),
									new ParamType(
										new AuraList(new AuraAny()),
										false,
										null
									)
								)
							},
							new AuraBool()
						),
						"Returns a boolean value indicating if the supplied list is empty (i.e. contains zero items)"
					),
					new(
						"length",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"a"
									),
									new ParamType(
										new AuraList(new AuraAny()),
										false,
										null
									)
								)
							},
							new AuraInt()
						),
						"Returns the number of items contained in the supplied list"
					),
					new(
						"map_",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"a"
									),
									new ParamType(
										new AuraList(new AuraAny()),
										false,
										null
									)
								),
								new(
									new Tok(
										TokType.Identifier,
										"b"
									),
									new ParamType(
										new AuraFunction(
											new List<Param>
											{
												new(
													new Tok(
														TokType.Identifier,
														"t"
													),
													new ParamType(
														new AuraAny(),
														false,
														null
													)
												)
											},
											new AuraAny()
										),
										false,
										null
									)
								)
							},
							new AuraList(new AuraAny())
						),
						"Applies the supplied function to each item in the supplied list, returning a new list containing the result of each invocation"
					),
					new(
						"filter",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"a"
									),
									new ParamType(
										new AuraList(new AuraAny()),
										false,
										null
									)
								),
								new(
									new Tok(
										TokType.Identifier,
										"f"
									),
									new ParamType(
										new AuraFunction(
											new List<Param>
											{
												new(
													new Tok(
														TokType.Identifier,
														"t"
													),
													new ParamType(
														new AuraAny(),
														false,
														null
													)
												)
											},
											new AuraBool()
										),
										false,
										null
									)
								)
							},
							new AuraList(new AuraAny())
						),
						"Returns a new list containing only those items in the original list that return a value of `true` when passed in to the supplied anonymous function"
					),
					new(
						"reduce",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"a"
									),
									new ParamType(
										new AuraList(new AuraAny()),
										false,
										null
									)
								),
								new(
									new Tok(
										TokType.Identifier,
										"f"
									),
									new ParamType(
										new AuraFunction(
											new List<Param>
											{
												new(
													new Tok(
														TokType.Identifier,
														"t1"
													),
													new ParamType(
														new AuraAny(),
														false,
														null
													)
												),
												new(
													new Tok(
														TokType.Identifier,
														"t2"
													),
													new ParamType(
														new AuraAny(),
														false,
														null
													)
												)
											},
											new AuraBool()
										),
										false,
										null
									)
								),
								new(
									new Tok(
										TokType.Identifier,
										"t"
									),
									new ParamType(
										new AuraAny(),
										false,
										null
									)
								)
							},
							new AuraAny()
						),
						"Reduces the supplied list to a single item"
					),
					new(
						"min",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"a"
									),
									new ParamType(
										new AuraList(new AuraInt()),
										false,
										null
									)
								)
							},
							new AuraInt()
						),
						"Returns the minimum value contained in the list"
					),
					new(
						"max",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"a"
									),
									new ParamType(
										new AuraList(new AuraInt()),
										false,
										null
									)
								)
							},
							new AuraInt()
						),
						"Returns the maximum value contained in the list"
					),
					new(
						"push",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"a"
									),
									new ParamType(
										new AuraList(new AuraAny()),
										false,
										null
									)
								),
								new(
									new Tok(
										TokType.Identifier,
										"t"
									),
									new ParamType(
										new AuraAny(),
										false,
										null
									)
								)
							},
							new AuraNil()
						),
						"Adds a new item to the end of the supplied list"
					),
					new(
						"pop",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"a"
									),
									new ParamType(
										new AuraList(new AuraAny()),
										false,
										null
									)
								)
							},
							new AuraNil()
						),
						"Removes the last item in the list"
					),
					new(
						"sum",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"a"
									),
									new ParamType(
										new AuraList(new AuraInt()),
										false,
										null
									)
								)
							},
							new AuraInt()
						),
						"Sums together all items in the list"
					)
				},
				new List<AuraInterface>(),
				new List<AuraClass>(),
				new Dictionary<string, ITypedAuraExpression>()
			)
		},
		{
			"aura/errors", new AuraModule(
				"errors",
				new List<AuraNamedFunction>
				{
					new(
						"message",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"err"
									),
									new ParamType(
										new AuraError(),
										false,
										null
									)
								)
							},
							new AuraString()
						),
						"Returns the error's message"
					)
				},
				new List<AuraInterface>(),
				new List<AuraClass>(),
				new Dictionary<string, ITypedAuraExpression>()
			)
		},
		{
			"aura/results", new AuraModule(
				"results",
				new List<AuraNamedFunction>
				{
					new(
						"is_success",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"st"
									),
									new ParamType(
										new AuraAnonymousStruct(
											new List<Param>
											{
												new(
													new Tok(
														TokType.Identifier,
														"success"
													),
													new ParamType(
														new AuraAny(),
														false,
														null
													)
												),
												new(
													new Tok(
														TokType.Identifier,
														"failure"
													),
													new ParamType(
														new AuraError(),
														false,
														null
													)
												)
											},
											Visibility.Private
										),
										false,
										null
									)
								)
							},
							new AuraBool()
						),
						"Returns a boolean value indicating if the result type contains a success value"
					),
					new(
						"success",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"st"
									),
									new ParamType(
										new AuraAnonymousStruct(
											new List<Param>
											{
												new(
													new Tok(
														TokType.Identifier,
														"success"
													),
													new ParamType(
														new AuraAny(),
														false,
														null
													)
												),
												new(
													new Tok(
														TokType.Identifier,
														"failure"
													),
													new ParamType(
														new AuraError(),
														false,
														null
													)
												)
											},
											Visibility.Private
										),
										false,
										null
									)
								)
							},
							new AuraAny()
						),
						"Returns the result's success type, if it exists"
					),
					new(
						"is_failure",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"st"
									),
									new ParamType(
										new AuraAnonymousStruct(
											new List<Param>
											{
												new(
													new Tok(
														TokType.Identifier,
														"success"
													),
													new ParamType(
														new AuraAny(),
														false,
														null
													)
												),
												new(
													new Tok(
														TokType.Identifier,
														"failure"
													),
													new ParamType(
														new AuraError(),
														false,
														null
													)
												)
											},
											Visibility.Private
										),
										false,
										null
									)
								)
							},
							new AuraBool()
						),
						"Returns a boolean value indicating if the result type contains a failure value"
					),
					new(
						"failure",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(
										TokType.Identifier,
										"st"
									),
									new ParamType(
										new AuraAnonymousStruct(
											new List<Param>
											{
												new(
													new Tok(
														TokType.Identifier,
														"success"
													),
													new ParamType(
														new AuraAny(),
														false,
														null
													)
												),
												new(
													new Tok(
														TokType.Identifier,
														"failure"
													),
													new ParamType(
														new AuraError(),
														false,
														null
													)
												)
											},
											Visibility.Private
										),
										false,
										null
									)
								)
							},
							new AuraError()
						),
						"Returns the result type's failure value, if it exists"
					)
				},
				new List<AuraInterface>(),
				new List<AuraClass>(),
				new Dictionary<string, ITypedAuraExpression>()
			)
		},
		{
			"aura/maps", new AuraModule(
				"maps",
				new List<AuraNamedFunction>
				{
					new(
						"add",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "m"),
									new ParamType(
										new AuraMap(new AuraAny(), new AuraAny()),
										false,
										null
									)
								),
								new(
									new Tok(TokType.Identifier, "key"),
									new ParamType(
										new AuraAny(),
										false,
										null
									)
								),
								new(
									new Tok(TokType.Identifier, "value"),
									new ParamType(
										new AuraAny(),
										false,
										null
									)
								)
							},
							new AuraNil()
						),
						"Adds the supplied key and value pair to a dictionary"
					),
					new(
						"remove",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "m"),
									new ParamType(
										new AuraMap(new AuraAny(), new AuraAny()),
										false,
										null
									)
								),
								new(
									new Tok(TokType.Identifier, "key"),
									new ParamType(
										new AuraAny(),
										false,
										null
									)
								)
							},
							new AuraNil()
						),
						"Removes a key from a dictionary"
					),
					new(
						"contains",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "m"),
									new ParamType(
										new AuraMap(new AuraAny(), new AuraAny()),
										false,
										null
									)
								),
								new(
									new Tok(TokType.Identifier, "key"),
									new ParamType(
										new AuraAny(),
										false,
										null
									)
								)
							},
							new AuraBool()
						),
						"Determines if the supplied key is present in the map"
					)
				},
				new List<AuraInterface>(),
				new List<AuraClass>(),
				new Dictionary<string, ITypedAuraExpression>()
			)
		}
	};

	public static Dictionary<string, AuraModule> GetAllModules() { return Modules; }

	public static bool TryGetModule(string name, out AuraModule? mod) { return Modules.TryGetValue(name, out mod); }
}
