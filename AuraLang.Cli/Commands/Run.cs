using AuraLang.Cli.Options;

namespace AuraLang.Cli.Commands;

public class Run
{
	private Build _build { get; init; }
	private bool _verbose { get; init; }

	public Run(RunOptions opts)
	{
		var buildOpts = new BuildOptions
		{
			Path = opts.Path,
			Verbose = opts.Verbose ?? false
		};

		_build = new Build(buildOpts);
		_verbose = opts.Verbose ?? false;
	}

	public int Execute()
	{
		var contents = _build.Execute();
		Console.WriteLine($"Contents = {contents}");
		return 0;
	}
}

