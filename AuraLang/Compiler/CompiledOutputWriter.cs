namespace AuraLang.Compiler;

public class CompiledOutputWriter
{
	public virtual void CreateDirectory(string dirName) => Directory.CreateDirectory($"build/pkg/{dirName}");

	public virtual void WriteOutput(string dirName, string fileName, string content) => File.WriteAllText($"build/pkg/{dirName}/{fileName}.go", content);
}

