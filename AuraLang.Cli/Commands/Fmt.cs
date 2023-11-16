using AuraLang.Cli.Options;

namespace AuraLang.Cli.Commands;

public class Fmt : AuraCommand
{

    public Fmt(FmtOptions opts) : base(opts) { }

    public override int Execute()
    {
        var contents = File.ReadAllText(FilePath);
        if (contents[^1] is not '\n') contents += '\n';
        File.WriteAllText(FilePath, contents);

        return 0;
    }
}