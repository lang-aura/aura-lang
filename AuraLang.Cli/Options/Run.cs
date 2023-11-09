using CommandLine;

namespace AuraLang.Cli.Options;

[Verb("run", HelpText = "Execute an Aura source file")]
public class RunOptions
{
	[Option('v', "verbose", Required = false, Default = false, HelpText = "Set output level to verbose")]
	public bool? Verbose { get; set; }
}

