using AuraLang.Cli.Options;
using AuraLang.Lsp.LanguageServerProtocol;

namespace AuraLang.Cli.Commands;

public class Lsp : AuraCommand
{
	public Lsp(LspOptions opts) : base(opts) { }

	public override int Execute() => ExecuteCommandAsync().Result;

	protected override int ExecuteCommand()
	{
		throw new NotImplementedException();
	}

	protected async Task<int> ExecuteCommandAsync()
	{
		//logger.LogSuccinct("Starting new LSP server...");
		var server = new AuraLanguageServer(true);
		await server.InitAsync();
		return 0;
	}
}
