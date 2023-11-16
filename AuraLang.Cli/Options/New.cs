using CommandLine;

namespace AuraLang.Cli.Options;

[Verb("new", HelpText = "Create a new Aura project")]
public class NewOptions
{
    [Option('v', "verbose", Required = false, Default = false, HelpText = "Set output level to verbose")]
    public bool? Verbose { get; set; }
    
    [Option('o', "output", Required = false, Default = ".", HelpText = "The directory where the new Aura project should be created. Defaults to the current directory")]
    public string? Path { get; set; }
    
    [Value(0, Required = true, HelpText = "The name of the new Aura project")]
    public string Name { get; set; }
}