using AuraLang.ImportedModuleProvider;
using AuraLang.Lsp.DocumentManager;

namespace AuraLang.Lsp.SynchronizedFileProvider;

/// <summary>
///     Responsible for providing the contents of files managed by the LSP server
/// </summary>
public class AuraSynchronizedFileProvider : IImportedModuleProvider
{
	/// <summary>
	///     The Aura source files currently owned by the LSP client
	/// </summary>
	private AuraDocumentManager Documents { get; }

	public AuraSynchronizedFileProvider(AuraDocumentManager documentManager)
	{
		Documents = documentManager;
	}

	/// <summary>
	///     Fetches all files contained in the specified Aura module
	/// </summary>
	/// <param name="moduleName">The name of the Aura module</param>
	/// <returns>
	///     A list of tuples, where each tuple represents an Aura source file. The first item of each tuple is the file's
	///     path, and the second item is the file's contents
	/// </returns>
	public List<(string, string)> GetImportedModule(string moduleName)
	{
		return Documents.GetModule(moduleName);
	}
}
