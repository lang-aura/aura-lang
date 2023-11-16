using System.Diagnostics;
using AuraLang.Cli.Options;

namespace AuraLang.Cli.Commands;

public class Run
{
	private Build Build { get; init; }
	private bool Verbose { get; init; }

	public Run(RunOptions opts)
	{
		var buildOpts = new BuildOptions
		{
			Path = opts.Path,
			Verbose = opts.Verbose ?? false
		};

		Build = new Build(buildOpts);
		Verbose = opts.Verbose ?? false;
	}

	public int Execute()
	{
		Build.Execute();
		var run = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "./Examples"
			}
		};
		run.Start();
		run.WaitForExit();
		
		return 0;
	}
}

