using AuraLang.AST;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using AuraString = AuraLang.Types.String;

namespace AuraLang.Stdlib;

public class AuraStdlib
{
	private readonly Dictionary<string, Module> _modules = new()
	{
		{
			"aura/io", new Module(
				name: "io",
				publicFunctions: new List<NamedFunction>
				{
					new(
						"printf",
						Visibility.Public,
						new Function(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "format", 1),
									new ParamType(new AuraString(), false, null)),
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(new Any(), true, null))
							},
							new Nil())
						),
					new(
						"println",
						Visibility.Public,
						new Function(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "s", 1),
									new ParamType(new AuraString(), false, null))
							},
							new Nil())
						),
					new(
						"print",
						Visibility.Public,
						new Function(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "s", 1),
									new ParamType(new AuraString(), false, null))
							},
							new Nil())
						),
					new(
						"eprintln",
						Visibility.Public,
						new Function(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "s", 1),
									new ParamType(new AuraString(), false, null))
							},
							new Nil())
						),
					new(
						"eprint",
						Visibility.Public,
						new Function(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "s", 1),
									new ParamType(new AuraString(), false, null))
							},
							new Nil())
						),
					new(
						"readln",
						Visibility.Public,
						new Function(
							new List<Param>(),
							new AuraString())
						),
					new(
						"read_file",
						Visibility.Public,
						new Function(
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
						new Function(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "path", 1),
									new ParamType(new AuraString(), false, null))
							},
							new List(new AuraString()))
						),
					new(
						"write_file",
						Visibility.Public,
						new Function(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "path", 1),
									new ParamType(new AuraString(), false, null)),
								new(
									new Tok(TokType.Identifier, "content", 1),
									new ParamType(new AuraString(), false, null))


							},
							new Nil()))
				},
				publicClasses: new List<Class>(),
				publicVariables: new Dictionary<string, ITypedAuraExpression>())
		},
		{
			"aura/strings", new Module(
				name: "strings",
				publicFunctions: new List<NamedFunction>
				{
					new(
						"to_lower",
						Visibility.Public,
						new Function(
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
						new Function(
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
						new Function(
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
							new Bool())
						),
					new(
						"length",
						Visibility.Public,
						new Function(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "s", 1),
									new ParamType(
										new AuraString(),
										false,
										null))
							},
							new Bool())
						),
					new(
						"split",
						Visibility.Public,
						new Function(
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
							new Bool())
						),
					new(
						"to_int",
						Visibility.Public,
						new Function(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "s", 1),
									new ParamType(
										new AuraString(),
										false,
										null))
							},
							new Bool())
						),
				},
				publicClasses: new List<Class>(),
				publicVariables: new Dictionary<string, ITypedAuraExpression>())
		},
		{
			"aura/lists", new Module(
				name: "lists",
				publicFunctions: new List<NamedFunction>
				{
					new(
					"contains",
					Visibility.Public,
					new Function(
						new List<Param>
						{
							new(
								new Tok(TokType.Identifier, "a", 1),
								new ParamType(
									new List(new Any()),
									false,
									null)),
							new(
								new Tok(TokType.Identifier, "item", 1),
								new ParamType(
									new Any(),
									false,
									null))
						},
						new Bool())
					),
					new(
					"is_empty",
					Visibility.Public,
					new Function(
						new List<Param>
						{
							new(
								new Tok(TokType.Identifier, "a", 1),
								new ParamType(
									new List(new Any()),
									false,
									null))
						},
						new Bool())
					),
					new(
						"length",
						Visibility.Public,
						new Function(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(
										new List(new Any()),
										false,
										null))
							},
							new Int())
						),
					new(
						"map_",
						Visibility.Public,
						new Function(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(
										new List(new Any()),
										false,
										null)),
								new(
									new Tok(TokType.Identifier, "b", 1),
									new ParamType(
										new Function(
											new List<Param>
											{
												new(
													new Tok(TokType.Identifier, "t", 1),
													new ParamType(
														new Any(),
														false,
														null))
											},
											new Any()),
										false,
										null))
							},
							new List(new Any()))
						),
					new(
						"filter",
						Visibility.Public,
						new Function(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(
										new List(new Any()),
										false,
										null)),
								new(
									new Tok(TokType.Identifier, "f", 1),
									new ParamType(
										new Function(
											new List<Param>
											{
												new(
													new Tok(TokType.Identifier, "t", 1),
													new ParamType(
														new Any(),
														false,
														null))
											},
											new Bool()),
										false,
										null))
							},
							new List(new Any()))
						),
					new(
						"reduce",
						Visibility.Public,
						new Function(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(
										new List(new Any()),
										false,
										null)),
								new(
									new Tok(TokType.Identifier, "f", 1),
									new ParamType(
										new Function(
											new List<Param>
											{
												new(
													new Tok(TokType.Identifier, "t1", 1),
													new ParamType(
														new Any(),
														false,
														null)),
												new(
													new Tok(TokType.Identifier, "t2", 1),
													new ParamType(
														new Any(),
														false,
														null))
											},
											new Bool()),
										false,
										null)),
								new(
									new Tok(TokType.Identifier, "t", 1),
									new ParamType(
										new Any(),
										false,
										null)),
							},
							new Any())
						),
					new(
						"min",
						Visibility.Public,
						new Function(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(
										new List(new Int()),
										false,
										null))
							},
							new Int())
						),
					new(
						"max",
						Visibility.Public,
						new Function(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(
										new List(new Int()),
										false,
										null))
							},
							new Int())
						),
					new(
						"push",
						Visibility.Public,
						new Function(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(
										new List(new Any()),
										false,
										null)),
								new(
									new Tok(TokType.Identifier, "t", 1),
									new ParamType(
										new Any(),
										false,
										null))
							},
							new Nil())
						),
					new(
						"pop",
						Visibility.Public,
						new Function(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(
										new List(new Any()),
										false,
										null))
							},
							new Nil())
						),
					new(
						"sum",
						Visibility.Public,
						new Function(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "a", 1),
									new ParamType(
										new List(new Int()),
										false,
										null))
							},
							new Int())
						),
				},
				publicClasses: new List<Class>(),
				publicVariables: new Dictionary<string, ITypedAuraExpression>())
		}
	};

	public Dictionary<string, Module> GetAllModules() => _modules;

	public bool TryGetModule(string name, out Module? mod) => _modules.TryGetValue(name, out mod);
}
