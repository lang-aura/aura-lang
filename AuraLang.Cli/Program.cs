using AuraLang.Cli.Commands;
using AuraLang.Cli.Options;
using CommandLine;

return Parser.Default.ParseArguments<NewOptions, BuildOptions, RunOptions, FmtOptions>(args)
		.MapResult(
			(NewOptions opts) => new New(opts).Execute(),
			(BuildOptions opts) => new Build(opts).Execute(),
			(RunOptions opts) => new Run(opts).Execute(),
			(FmtOptions opts) => new Fmt(opts).Execute(),
			errs => 1);
