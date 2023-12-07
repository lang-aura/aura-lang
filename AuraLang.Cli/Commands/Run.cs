using System.Diagnostics;
using AuraLang.Cli.Options;
using AuraLang.Cli.Toml;

namespace AuraLang.Cli.Commands;

public class Run : AuraCommand
{
	private Build Build { get; }
	private AuraToml toml = new();

	public Run(RunOptions opts) : base(opts)
	{
		var buildOpts = new BuildOptions
		{
			Verbose = opts.Verbose ?? false
		};

		Build = new Build(buildOpts);
	}

	public override int Execute()
	{
		Build.Execute();
		Directory.SetCurrentDirectory("../..");
		var projName = toml.GetProjectName();
		var run = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = $"./build/pkg/{projName}"
			}
		};
		run.Start();
		run.WaitForExit();

		return 0;
	}
}

