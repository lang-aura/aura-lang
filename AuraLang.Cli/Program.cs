using AuraLang.Cli.Commands;
using AuraLang.Cli.Options;
using CommandLine;

return Parser.Default.ParseArguments<NewOptions, BuildOptions, RunOptions, FmtOptions, LspOptions>(args)
		.MapResult(
			(NewOptions opts) => await new New(opts).ExecuteAsync(),
			(BuildOptions opts) => await await new Build(opts).ExecuteAsync(),
			(RunOptions opts) => await await new Run(opts).ExecuteAsync(),
			(FmtOptions opts) => await await new AuraFmt(opts).ExecuteAsync(),
			(LspOptions opts) => await await new Lsp(opts).ExecuteAsync(),
			errs => 1);
