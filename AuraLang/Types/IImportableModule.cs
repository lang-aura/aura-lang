namespace AuraLang.Types;

/// <summary>
///     Represents a type that has a standard library module that can be imported
/// </summary>
public interface IImportableModule
{
	/// <summary>
	///     Fetches the stdlib module's name
	/// </summary>
	/// <returns>The stdlib module's name</returns>
	string GetModuleName();
}
