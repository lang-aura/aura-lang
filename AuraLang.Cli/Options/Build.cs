using CommandLine;

namespace AuraLang.Cli.Options;

[Verb("build", HelpText = "Compile an Aura source file")]
public class BuildOptions
{
	[Option('v', "verbose", Required = false, Default = false, HelpText = "Set output level to verbose")]
	public bool? Verbose { get; set; }
}

