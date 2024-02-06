using AuraLang.Cli.Commands;
using AuraLang.Cli.Options;
using CommandLine;

return await Parser.Default.ParseArguments<NewOptions, BuildOptions, RunOptions, FmtOptions, LspOptions>(args)
		.MapResult(
			async (NewOptions opts) => await new New(opts).ExecuteAsync(),
			async (BuildOptions opts) => await new Build(opts).ExecuteAsync(),
			async (RunOptions opts) => await new Run(opts).ExecuteAsync(),
			async (FmtOptions opts) => await new AuraFmt(opts).ExecuteAsync(),
			async (LspOptions opts) => await new Lsp(opts).ExecuteAsync(),
			errs => Task.FromResult(1));
