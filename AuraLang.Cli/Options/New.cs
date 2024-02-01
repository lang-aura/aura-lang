using CommandLine;

namespace AuraLang.Cli.Options;

[Verb("new", HelpText = "Create a new Aura project")]
public class NewOptions : AuraOptions
{
	[Value(0, Required = true, HelpText = "The name of the new Aura project")]
	public string? Name { get; set; }
	[Option('o', "output", Required = false, HelpText = "The directory where the new Aura project will be created")]
	public string? OutputPath { get; set; }
}
