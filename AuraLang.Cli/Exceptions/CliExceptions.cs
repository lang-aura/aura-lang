namespace AuraLang.Cli.Exceptions;

public class TomlFileNotFoundException : Exception
{
	public TomlFileNotFoundException() : base("aura.toml file not found in current directory or any parent directories.") { }
}

public class NewParentDirectoryMustBeEmpty : Exception
{
	public NewParentDirectoryMustBeEmpty() : base("The project's parent directory must be empty.") { }
}
