using AuraLang.Cli.Options;

namespace AuraLang.Cli.Commands;

public class Fmt : AuraCommand
{
	public Fmt(FmtOptions opts) : base(opts) { }

	/// <summary>
	/// Formats the entire Aura project
	/// </summary>
	/// <returns>An integer status indicating if the command succeeded</returns>
	public override int Execute()
	{
		TraverseProject(FormatFile);
		return 0;
	}

	/// <summary>
	/// Formats an individual Aura source file
	/// </summary>
	/// <param name="path">The path of the Aura source file</param>
	public void FormatFile(string path)
	{
		var contents = File.ReadAllText(path);
		if (contents[^1] is not '\n') contents += '\n';
		File.WriteAllText(path, contents);
	}
}
