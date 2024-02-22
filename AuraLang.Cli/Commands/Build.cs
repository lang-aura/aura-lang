using System.Diagnostics;
using AuraLang.Cli.Options;
using AuraLang.Cli.Toml;
using AuraLang.Exceptions;
using AuraLang.ProjectCompiler;

namespace AuraLang.Cli.Commands;

public class Build : AuraCommand
{
	private bool WarningsAsErrors { get; }

	public Build(BuildOptions opts) : base(opts)
	{
		WarningsAsErrors = opts.WarningsAsErrors;
	}

	/// <summary>
	/// Used to read the TOML config file located in the project's root
	/// </summary>
	private readonly AuraToml _toml = new();

	/// <summary>
	/// Builds the entire Aura project
	/// </summary>
	/// <returns>An integer status indicating if the process succeeded</returns>
	protected override async Task<int> ExecuteCommandAsync()
	{
		// Before building the project, clear out all Go files from the `build` directory. This will prevent issues arising
		// from, for example, old Go files previously built from Aura source files that have since been deleted.
		ResetBuildDirectory();

		try
		{
			var projCompiler = new AuraProjectCompiler(_toml.GetProjectName()!);
			var compiledOutput = projCompiler.CompileProject();
			foreach (var output in compiledOutput)
			{
				WriteGoOutputFile(output.Item1, output.Item2);
			}
		}
		catch (AuraExceptionContainer ex)
		{
			Console.WriteLine(ex.Report());
			return 1;
		}
		catch (AuraWarningContainer w)
		{
			w.Report();
			if (WarningsAsErrors) return 1;
		}

		// Build Go binary executable
		Directory.SetCurrentDirectory("./build/pkg");
		FormatGoProject();
		var build = new Process { StartInfo = new ProcessStartInfo { FileName = "go", Arguments = "build" } };
		build.Start();
		await build.WaitForExitAsync();

		return 0;
	}

	private void WriteGoOutputFile(string auraPath, string content)
	{
		var goPath = auraPath.Replace("src/", string.Empty);
		goPath = Path.ChangeExtension(goPath, "go");
		File.WriteAllText($"./build/pkg/{goPath}", content);
	}

	/// <summary>
	/// Formats the compiled Go project with the `go fmt` tool
	/// </summary>
	private void FormatGoProject()
	{
		var cmd = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "go",
				Arguments = "fmt",
				RedirectStandardOutput = true
			}
		};
		cmd.Start();
		cmd.WaitForExit();
	}

	/// <summary>
	/// Resets the project's `build` directory by deleting any Go source files and the binary executable, if it exists. This is done first by `aura build` to start each build with a clean
	/// `build` directory, which can avoid issues where, for example, old Go files remain in the `build` directory after their corresponding Aura source file has been deleted.
	/// </summary>
	private void ResetBuildDirectory()
	{
		// Delete all Go files in `build` directory
		var paths = Directory.GetFiles("./build/pkg", "*.go");
		foreach (var path in paths)
		{
			File.Delete(path);
		}

		// Delete any sub-directories containing Go source files
		var dirs = Directory.GetDirectories("./build/pkg").Where(p => p.Split("/")[^1] != "stdlib" && p.Split("/")[^1] != "prelude");
		foreach (var dir in dirs)
		{
			Directory.Delete(dir, true);
		}

		// Delete executable file
		var projName = _toml.GetProjectName();
		File.Delete($"./build/pkg/{projName}");
	}
}
