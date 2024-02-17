using AuraLang.ImportedFileProvider;
using AuraLang.Lsp.DocumentManager;

namespace AuraLang.Lsp.SynchronizedFileProvider;

public class AuraSynchronizedFileProvider : IImportedModuleProvider
{
	private AuraDocumentManager Documents { get; }

	public AuraSynchronizedFileProvider(AuraDocumentManager documentManager)
	{
		Documents = documentManager;
	}

	public List<(string, string)> GetImportedModule(string moduleName)
	{
		return Documents.GetModule(moduleName);
	}
}
