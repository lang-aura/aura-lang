using System.Diagnostics;
using AuraLang.Cli.Options;
using AuraLang.Cli.Toml;

namespace AuraLang.Cli.Commands;

public class Run : AuraCommand
{
	private Build Build { get; }
	/// <summary>
	/// Used to read the TOML config file located at the project's root
	/// </summary>
	private readonly AuraToml _toml = new();

	public Run(RunOptions opts) : base(opts)
	{
		var buildOpts = new BuildOptions
		{
			Verbose = opts.Verbose ?? false
		};

		Build = new Build(buildOpts);
	}

	/// <summary>
	/// Runs the Aura project
	/// </summary>
	/// <returns>An integer status indicating if the process succeeded</returns>
	protected override async Task<int> ExecuteCommandAsync()
	{
		await Build.ExecuteAsync();
		// After build is finished executing, the current directory will be the `build/pkg` directory, so we return to the project's root directory here
		Directory.SetCurrentDirectory("../..");
		var projName = _toml.GetProjectName();
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

