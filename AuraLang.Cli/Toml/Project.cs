namespace AuraLang.Cli.Toml;

/// <summary>
/// Represents the <c>project</c> section of an Aura TOML config file
/// </summary>
/// <param name="Name">The project's name</param>
/// <param name="Version">The project's version</param>
/// <param name="Description">A description of the project</param>
public record Project(string Name, string Version, string Description);
