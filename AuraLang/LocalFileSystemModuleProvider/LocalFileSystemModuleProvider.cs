using AuraLang.ImportedFileProvider;

namespace AuraLang.Cli.LocalFileSystemModuleProvider;

/// <summary>
/// Responsible for fetching imported Aura files from the local file system
/// </summary>
public class AuraLocalFileSystemImportedModuleProvider : IImportedModuleProvider
{
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

	protected void FindProjectRoot() => FindProjectRootRecur(".");

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
