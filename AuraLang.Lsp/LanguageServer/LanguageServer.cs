using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;

namespace AuraLang.Lsp.LanguageServerProtocol;

public static class AuraLanguageServer
{
	public static async Task InitAsync()
	{
		// var server = await LanguageServer.From(options =>
		// {
		// 	options
		// 		.WithInput(Console.OpenStandardInput())
		// 		.WithOutput(Console.OpenStandardOutput())
		// 		// .ConfigureLogging(
		// 		// 	x => x
		// 		// 		.AddLanguageProtocolLogging()
		// 		// 		.SetMinimumLevel(LogLevel.Debug)
		// 		// )
		// 		// .WithLoggerFactory(new LoggerFactory())
		// 		// .WithServices(ConfigureServices)
		// 		// .WithHandler<AuraTextDocumentSyncHandler>()
		// 		.OnInitialize(async (server, request, token) =>
		// 		{
		// 			server.Log("Server initialize.");
		// 			await Task.CompletedTask.ConfigureAwait(false);
		// 		})
        //         .OnInitialized(async (server, request, r, c) =>
        //         {
        //             Console.WriteLine("initialized");
        //             await Task.CompletedTask.ConfigureAwait(false);
        //         })
		// 		.OnStarted(async (server, token) =>
		// 		{
		// 			server.Log("started");
		// 			await Task.CompletedTask.ConfigureAwait(false);
		// 		});
		// });

        // await server.WaitForExit;
		var server = await LanguageServer.From(options =>
                options
                    .WithInput(Console.OpenStandardInput())
                    .WithOutput(Console.OpenStandardOutput())
                    .WithLoggerFactory(new LoggerFactory())
                    .AddDefaultLoggingProvider()
                    .WithMinimumLogLevel(LogLevel.Trace)
                    //.WithServices(ConfigureServices)
                );

        await server.WaitForExit;
	}

	// static void ConfigureServices(IServiceCollection services)
	// {
	// 	services.AddSingleton<AuraDocumentManager>();
	// }
}
