using AuraLang.Cli.Options;

namespace AuraLang.Cli.Commands;

public class Fmt : AuraCommand
{

	public Fmt(FmtOptions opts) : base(opts) { }

	public override int Execute()
	{
		var contents = File.ReadAllText("./src");
		if (contents[^1] is not '\n') contents += '\n';
		File.WriteAllText("./src", contents);

		return 0;
	}
}
