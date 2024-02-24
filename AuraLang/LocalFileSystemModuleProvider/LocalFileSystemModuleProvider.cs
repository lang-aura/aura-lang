using AuraLang.ImportedModuleProvider;

namespace AuraLang.LocalFileSystemModuleProvider;

/// <summary>
/// Responsible for fetching imported Aura files from the local file system
/// </summary>
public class AuraLocalFileSystemImportedModuleProvider : IImportedModuleProvider
{
	/// <summary>
	///     Fetches an Aura module from the enclosing project
	/// </summary>
	/// <param name="moduleName">
	///     The name of the module to fetch. This name should be the same as the path used to import the
	///     module, i.e. the entire path of the module's directory after <c>src</c>, so the module located at
	///     <code>.../src/hello/world</code> would be passed in as <code>hello/world</code>
	/// </param>
	/// <returns>
	///     A list of tuples representing each file in the module, where the first item is the file's path and the second
	///     item is the file's contents
	/// </returns>
	public List<(string, string)> GetImportedModule(string moduleName)
	{
		FindProjectRoot();
		var path = $"./src/{moduleName}";
		return Directory.GetFiles(path, "*.aura")
			.Select(p =>
			{
				var contents = File.ReadAllText(p);
				return (p, contents);
			}).ToList();
	}

	/// <summary>
	///     Finds the project's root directory. This is useful as a starting point when searching for an Aura module or file
	/// </summary>
	private void FindProjectRoot() { FindProjectRootRecur("."); }

	/// <summary>
	///     Recursively searches for the project's root directory
	/// </summary>
	/// <param name="path">The current path to check if it is the project's root</param>
	/// <exception cref="Exception">
	///     Thrown if the method has reached the file system's root directory without finding the
	///     project's root directory
	/// </exception>
	private void FindProjectRootRecur(string path)
	{
		if (!File.Exists(path))
		{
			if (Directory.GetParent(path) is null) throw new Exception(); // TODO create exception
			FindProjectRootRecur(Directory.GetParent(path)!.FullName);
			return;
		}

		Directory.SetCurrentDirectory(path);
	}
}
