namespace AuraLang.Cli.Toml;

/// <summary>
/// Represents the entire Aura TOML config file
/// </summary>
/// <param name="project">The <c>Project</c> section of the config file</param>
public record Document(Project project);
