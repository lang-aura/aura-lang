using AuraLang.ModuleCompiler;

namespace AuraLang.ProjectCompiler;

/// <summary>
///     Responsible for compiling an entire Aura project
/// </summary>
public class AuraProjectCompiler
{
    /// <summary>
    ///     The project's name
    /// </summary>
	private string ProjectName { get; }

    public AuraProjectCompiler(string projectName) { ProjectName = projectName; }

    /// <summary>
    ///     Compiles an Aura project
    /// </summary>
    /// <returns>
    ///     A list of all the Aura source files, where each item in the list contains the file's path and the valid Go
    ///     string representing the file's contents
    /// </returns>
    public List<(string, string)> CompileProject()
    {
        return GetAllProjectDirectories()
            .Select(dir => new AuraModuleCompiler(dir, ProjectName).CompileModule())
            .Aggregate(
                new List<(string, string)>(),
                (acc, item) =>
                {
                    acc.AddRange(item);
                    return acc;
                }
            );
    }

    /// <summary>
    ///     Fetches all directories in the Aura project that contain at least one Aura source file
    /// </summary>
    /// <returns>A list of directory paths</returns>
    private List<string> GetAllProjectDirectories() { return GetAllProjectDirectoriesRecur("./src"); }

    /// <summary>
    ///     Recursively finds all directories in the Aura project that contain at least one Aura source file
    /// </summary>
    /// <param name="path">The current path to search</param>
    /// <returns>A list of directory paths</returns>
    private List<string> GetAllProjectDirectoriesRecur(string path)
    {
        var dirs = Directory.GetDirectories(path);
        if (dirs.Length == 0) return new List<string> { path };
        var subDirs = dirs
            .Select(GetAllProjectDirectoriesRecur)
            .Aggregate(
                new List<string>(),
                (acc, item) =>
                {
                    acc.AddRange(item);
                    return acc;
                }
            );

        var paths = new List<string> { path };
        paths.AddRange(subDirs);
        return paths;
    }
}