using Tomlet.Attributes;

namespace AuraLang.Toml.Data;

/// <summary>
/// Represents the <c>project</c> section of an Aura TOML config file
/// </summary>
public record Project
{
    [TomlProperty("name")]
    public string Name { get; set; }
    [TomlProperty("version")]
    public string Version { get; set; }
    [TomlProperty("description")]
    public string Description { get; set; }
}
