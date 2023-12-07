namespace AuraLang.TypeChecker;

public class LocalModuleReader
{
	public virtual string[] GetModuleSourcePaths(string path) => Directory.GetFiles(path, "*aura");
	public virtual string Read(string path) => File.ReadAllText(path);
}

