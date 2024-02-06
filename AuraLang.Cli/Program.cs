using AuraLang.Cli.Commands;
using AuraLang.Cli.Options;
using CommandLine;

return Parser.Default.ParseArguments<NewOptions, BuildOptions, RunOptions, FmtOptions, LspOptions>(args)
		.MapResult(
			(NewOptions opts) => new New(opts).ExecuteAsync().Result,
			(BuildOptions opts) => new Build(opts).ExecuteAsync().Result,
			(RunOptions opts) => new Run(opts).ExecuteAsync().Result,
			(FmtOptions opts) => new AuraFmt(opts).ExecuteAsync().Result,
			(LspOptions opts) => new Lsp(opts).ExecuteAsync().Result,
			errs => 1);
