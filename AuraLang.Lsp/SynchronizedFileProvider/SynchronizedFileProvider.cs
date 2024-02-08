using AuraLang.ImportedFileProvider;
using AuraLang.Lsp.DocumentManager;

namespace AuraLang.Lsp.SynchronizedFileProvider;

public class AuraSynchronizedFileProvider : IImportedModuleProvider
{
	private AuraDocumentManager _documents { get; }

	public AuraSynchronizedFileProvider(AuraDocumentManager documentManager)
	{
		_documents = documentManager;
	}

	public List<(string, string)> GetImportedModule(string moduleName)
	{
		return _documents.GetModule(moduleName);
	}
}
