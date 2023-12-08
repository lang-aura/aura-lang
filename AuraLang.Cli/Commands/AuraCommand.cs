using AuraLang.Cli.Options;

namespace AuraLang.Cli.Commands;

public abstract class AuraCommand
{
	protected bool Verbose { get; init; }

	protected AuraCommand(AuraOptions opts)
	{
		Verbose = opts.Verbose ?? false;
	}
	public abstract int Execute();

	protected void TraverseProject(Action<string> a)
	{
		TraverseProjectRecur("./src", a);
	}

	private void TraverseProjectRecur(string path, Action<string> a)
	{
		var paths = Directory.GetFiles(path);
		foreach (var p in paths)
		{
			a(p);
		}

		var dirs = Directory.GetDirectories(path);
		foreach (var dir in dirs)
		{
			TraverseProjectRecur(dir, a);
		}
	}
}
