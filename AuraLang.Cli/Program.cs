using AuraLang.Cli.Commands;
using AuraLang.Cli.Options;
using CommandLine;

return Parser.Default.ParseArguments<BuildOptions, RunOptions>(args)
        .MapResult(
            (BuildOptions opts) =>
            {
                var build = new Build(opts);
                return build.Execute();
            },
            (RunOptions opts) =>
            {
                var run = new Run(opts);
                return run.Execute();
            },
            errs => 1);