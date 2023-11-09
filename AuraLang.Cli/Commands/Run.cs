using AuraLang.Cli.Options;

namespace AuraLang.Cli.Commands;

public static class Run
{
	public static int ExecuteRun(RunOptions opts)
	{
		Console.WriteLine($"Executing run with verbose = {opts.Verbose}");
		return 0;
	}
}

