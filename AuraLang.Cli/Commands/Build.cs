using AuraLang.Cli.Options;

namespace AuraLang.Cli.Commands;

public static class Build
{
	public static int ExecuteBuild(BuildOptions opts)
	{
		Console.WriteLine($"Executing build with verbose = {opts.Verbose}");
		return 0;
	}
}

