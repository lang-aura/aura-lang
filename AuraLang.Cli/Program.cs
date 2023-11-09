using AuraLang.Cli.Commands;
using AuraLang.Cli.Options;
using CommandLine;

return Parser.Default.ParseArguments<BuildOptions, RunOptions>(args)
        .MapResult(
            (BuildOptions opts) => Build.ExecuteBuild(opts),
            (RunOptions opts) => Run.ExecuteRun(opts),
            errs => 1);