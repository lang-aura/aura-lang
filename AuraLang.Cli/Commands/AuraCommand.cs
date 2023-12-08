using AuraLang.Cli.Options;

namespace AuraLang.Cli.Commands;

public abstract class AuraCommand
{
	/// <summary>
	/// Indicates whether the command's output is verbose
	/// </summary>
	protected bool Verbose { get; init; }

	protected AuraCommand(AuraOptions opts)
	{
		Verbose = opts.Verbose ?? false;
	}
	public abstract int Execute();

	/// <summary>
	/// Traverses the project's Aura source files and calls the supplied Action on each file
	///	</summary>
	/// <param name="a">The Action to call on each Aura source file in the project. The action will accept
	/// each file's path as a parameter</param>
	protected void TraverseProject(Action<string> a)
	{
		TraverseProjectRecur("./src", a);
	}

	/// <summary>
	/// Recursively traverses the current directory and any sub-directories, calling the supplied Action
	/// on each Aura source file
	/// </summary>
	/// <param name="path">The path of the current directory being traversed. All Aura source files located in the current
	/// directory and any sub-directories will be processed by the supplied Action</param>
	/// <param name="a">The Action to call on each Aura source file in the current directory and any sub-directories. The action will
	/// accept each file's path as a parameter</param>
	private void TraverseProjectRecur(string path, Action<string> a)
	{
		var paths = Directory.GetFiles(path, "*.aura");
		foreach (var p in paths)
		{
			a(p);
		}

		var dirs = Directory.GetDirectories(path);
		foreach (var dir in dirs)
		{
			TraverseProjectRecur(dir, a);
		}
	}
}
