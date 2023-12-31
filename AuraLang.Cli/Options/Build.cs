using CommandLine;

namespace AuraLang.Cli.Options;

[Verb("build", HelpText = "Compile the Aura project")]
public class BuildOptions : AuraOptions
{
	[Option("warnings-as-errors", Required = false, Default = false, HelpText = "Treats warnings as errors")]
	public bool WarningsAsErrors { get; set; }
}
