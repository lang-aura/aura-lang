using AuraLang.Cli.Options;

namespace AuraLang.Cli.Commands;

public class Fmt : AuraCommand
{

    public Fmt(FmtOptions opts) : base(opts) { }

    public override int Execute()
    {
        return 0;
    }
}