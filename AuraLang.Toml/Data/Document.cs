using Tomlet.Attributes;

namespace AuraLang.Toml.Data;

/// <summary>
/// Represents the entire Aura TOML config file
/// </summary>
public record Document
{
    [TomlProperty("project")]
    public Project Project { get; set; }
}
