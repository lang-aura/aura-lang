using CommandLine;

namespace AuraLang.Cli.Options;

public abstract class AuraOptions
{
	[Option('v', "verbose", Required = false, Default = false, HelpText = "Set output level to verbose")]
	public bool? Verbose { get; init; }

	[Value(0, Required = false, HelpText = "The Aura source file to build")]
	public string Path { get; init; }
}
