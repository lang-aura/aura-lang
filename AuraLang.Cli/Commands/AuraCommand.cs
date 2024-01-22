using AuraLang.Cli.Exceptions;
using AuraLang.Cli.Options;
using AuraLang.Cli.Toml;
using AuraLang.Exceptions;

namespace AuraLang.Cli.Commands;

public abstract class AuraCommand
{
	/// <summary>
	/// Indicates whether the command's output is verbose
	/// </summary>
	protected bool Verbose { get; init; }

	/// <summary>
	/// Used to read the TOML config file located in the project's root
	/// </summary>
	private readonly AuraToml _toml = new();

	protected AuraCommand(AuraOptions opts)
	{
		Verbose = opts.Verbose ?? false;
	}

	public int Execute()
	{
		try
		{
			FindProjectRoot();
		}
		catch (TomlFileNotFoundException ex)
		{
			Console.WriteLine(ex.Message);
			return 1;
		}
		return ExecuteCommand();
	}

	protected abstract int ExecuteCommand();

	/// <summary>
	/// Traverses the project's Aura source files and calls the supplied Action on each file
	///	</summary>
	/// <param name="a">The Action to call on each Aura source file in the project. The action will accept
	/// each file's path as a parameter</param>
	protected void TraverseProject(Action<string, string> a) => TraverseProjectRecur("./src", a);

    protected void FindProjectRoot()
    {
        FindProjectRootRecur(".");
    }

    private void FindProjectRootRecur(string path)
    {
        var projName = new AuraToml(path).GetProjectName();
        if (projName is null)
        {
            if (Directory.GetParent(path) is null) throw new TomlFileNotFoundException();
            FindProjectRootRecur(Directory.GetParent(path)!.FullName);
            return;
        }

        Directory.SetCurrentDirectory(path);
    }

	/// <summary>
	/// Recursively traverses the current directory and any sub-directories, calling the supplied Action
	/// on each Aura source file
	/// </summary>
	/// <param name="path">The path of the current directory being traversed. All Aura source files located in the current
	/// directory and any sub-directories will be processed by the supplied Action</param>
	/// <param name="a">The Action to call on each Aura source file in the current directory and any sub-directories. The action will
	/// accept each file's path as a parameter</param>
	private void TraverseProjectRecur(string path, Action<string, string> a)
	{
		var paths = Directory.GetFiles(path, "*.aura");
		foreach (var p in paths)
		{
			try
			{
				var contents = File.ReadAllText(p);
				a(p, contents);
			}
			catch (AuraExceptionContainer ex)
			{
				ex.Report();
				return;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return;
			}
		}

		var dirs = Directory.GetDirectories(path);
		foreach (var dir in dirs)
		{
			TraverseProjectRecur(dir, a);
		}
	}
}
