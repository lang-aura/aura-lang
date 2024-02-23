namespace AuraLang.Compiler;

/// <summary>
///     Responsible for writing compiled Aura code to its corresponding Go output file in the <c>build</c> directory
/// </summary>
public class CompiledOutputWriter
{
	/// <summary>
	///     Creates a new directory in the <c>build</c> directory
	/// </summary>
	/// <param name="dirName">The new directory's name</param>
	public virtual void CreateDirectory(string dirName) => Directory.CreateDirectory($"build/pkg/{dirName}");

	/// <summary>
	///     Writes compiled Aura code to the appropriate Go file in the <c>build</c> directory
	/// </summary>
	/// <param name="dirName">The directory name where the Go output file will be saved</param>
	/// <param name="fileName">The name of the file to be created (without any file extension)</param>
	/// <param name="content">The compiled Aura code</param>
	public virtual void WriteOutput(string dirName, string fileName, string content) => File.WriteAllText($"build/pkg/{dirName}/{fileName}.go", content);
}

