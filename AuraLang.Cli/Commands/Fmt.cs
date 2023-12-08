using AuraLang.Cli.Options;

namespace AuraLang.Cli.Commands;

public class Fmt : AuraCommand
{

	public Fmt(FmtOptions opts) : base(opts) { }

	public override int Execute()
	{
		TraverseProject(FormatFile);
		return 0;
	}

	public void FormatFile(string path)
	{
		var contents = File.ReadAllText(path);
		if (contents[^1] is not '\n') contents += '\n';
		File.WriteAllText(path, contents);
	}
}
