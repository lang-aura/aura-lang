using Tomlet.Attributes;

namespace AuraLang.Cli.Toml;

/// <summary>
/// Represents the entire Aura TOML config file
/// </summary>
public record Document
{
    [TomlProperty("project")]
    public Project Project { get; set; }
}
