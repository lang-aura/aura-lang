namespace AuraLang.ImportedModuleProvider;

/// <summary>
///     Responsible for fetching Aura modules that are imported into an Aura source file via an <c>import</c> statement.
///     The reason this action has been
///     abstracted into its own interface is because these imported Aura modules are fetched in different ways depending on
///     the execution context. When
///     running a CLI command, imported Aura modules can be fetched from the local file system. However, the LSP server may
///     be storing Aura modules that
///     have been synchronized from the client, and it would prefer to provide these synchronized files instead of reading
///     them from the local file system.
/// </summary>
public interface IImportedModuleProvider
{
    /// <summary>
    ///     Fetches the Aura source file identified by the module and file name
    /// </summary>
    /// <param name="moduleName">The name of the imported Aura module</param>
    /// <returns>
    ///     A list of tuples, with each tuple containing the source file's name as its first item and the source
    ///     file's contents as its second item
    /// </returns>
    List<(string, string)> GetImportedModule(string moduleName);
}