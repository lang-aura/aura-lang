using AuraLang.ModuleCompiler;

namespace AuraLang.ProjectCompiler;

public class AuraProjectCompiler
{
	private string ProjectName { get; }

	public AuraProjectCompiler(string projectName)
	{
		ProjectName = projectName;
	}

	public List<(string, string)> CompileProject()
	{
		return GetAllProjectDirectories()
			.Select(dir => new AuraModuleCompiler(dir, ProjectName).CompileModule())
			.Aggregate(new List<(string, string)>(), (acc, item) =>
			{
				acc.AddRange(item);
				return acc;
			});
	}

	private List<string> GetAllProjectDirectories() => GetAllProjectDirectoriesRecur("./src");

	private List<string> GetAllProjectDirectoriesRecur(string path)
	{
		var dirs = Directory.GetDirectories(path);
		if (dirs.Length == 0) return new List<string> { path };
		var subDirs = dirs
			.Select(GetAllProjectDirectoriesRecur)
			.Aggregate(new List<string>(), (acc, item) =>
			{
				acc.AddRange(item);
				return acc;
			});

		var paths = new List<string> { path };
		paths.AddRange(subDirs);
		return paths;
	}
}