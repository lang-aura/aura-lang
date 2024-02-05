using AuraLang.Lsp.LanguageServerProtocol;

class Program
{
	static async Task Main(string[] args)
	{
		await AuraLanguageServer.InitAsync();
	}
}
