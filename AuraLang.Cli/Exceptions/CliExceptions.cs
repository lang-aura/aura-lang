namespace AuraLang.Cli.Exceptions;

public class TomlFileNotFoundException : Exception
{
	public TomlFileNotFoundException() : base("aura.toml file not found in current directory or any parent directories.") { }
}
