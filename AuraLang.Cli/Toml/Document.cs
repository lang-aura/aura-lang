namespace AuraLang.Cli.Toml;

/// <summary>
/// Represents the entire Aura TOML config file
/// </summary>
/// <param name="Project">The <c>Project</c> section of the config file</param>
public record Document(Project Project);
