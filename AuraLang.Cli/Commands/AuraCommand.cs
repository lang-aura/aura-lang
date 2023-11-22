using AuraLang.Cli.Options;

namespace AuraLang.Cli.Commands;

public abstract class AuraCommand
{
	protected string FilePath { get; }
	protected bool Verbose { get; init; }

	protected AuraCommand(AuraOptions opts)
	{
		FilePath = opts.Path;
		Verbose = opts.Verbose ?? false;
	}
	public abstract int Execute();

	protected List<string> GetAllAuraFiles(string path)
	{
		var files = Directory.GetFiles(path).ToList();
		var dirFiles = Directory.GetDirectories(path).Select(GetAllAuraFiles);
		return dirFiles.Aggregate(files, (prev, curr) =>
		{
			prev.AddRange(curr);
			return prev;
		});
	}
}
