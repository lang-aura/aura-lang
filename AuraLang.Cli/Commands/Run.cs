using System.Diagnostics;
using AuraLang.Cli.Options;

namespace AuraLang.Cli.Commands;

public class Run : AuraCommand
{
	private Build Build { get; init; }

	public Run(RunOptions opts) : base(opts)
	{
		var buildOpts = new BuildOptions
		{
			Path = opts.Path,
			Verbose = opts.Verbose ?? false
		};

		Build = new Build(buildOpts);
	}

	public override int Execute()
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

