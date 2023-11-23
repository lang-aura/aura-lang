using AuraLang.Compiler;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using AuraString = AuraLang.Types.String;

namespace AuraLang.Stdlib;

public class AuraStdlib
{
	private readonly Dictionary<string, Module> _modules = new()
	{
		{ "aura/io", new Module("io", new List<NamedFunction>
		{
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
				)
		})}
	};

	public Dictionary<string, Module> GetAllModules() => _modules;

	public bool TryGetModule(string name, out Module mod) => _modules.TryGetValue(name, out mod);
}
