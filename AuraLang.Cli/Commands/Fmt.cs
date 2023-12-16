using AuraLang.Cli.Options;

namespace AuraLang.Cli.Commands;

public class AuraFmt : AuraCommand
{
	public AuraFmt(FmtOptions opts) : base(opts) { }

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
		var contents = FormatAuraSourceCode(File.ReadAllText(path));
		File.WriteAllText(path, contents);
	}

	public string FormatAuraSourceCode(string source)
	{
		if (source[^1] is not '\n') source += '\n';
		return source;
	}
}
