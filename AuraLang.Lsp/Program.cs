using AuraLang.Lsp.LanguageServer;

class Program
{
	static async Task Main(string[] args)
	{
		await new AuraLanguageServer(true).InitAsync();
	}
}
