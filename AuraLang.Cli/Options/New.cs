using CommandLine;

namespace AuraLang.Cli.Options;

[Verb("new", HelpText = "Create a new Aura project")]
public class NewOptions : AuraOptions
{
	[Value(0, Required = true, HelpText = "The name of the new Aura project")]
	public string Name { get; set; }
}
