using Tomlet.Attributes;

namespace AuraLang.Cli.Toml;

/// <summary>
/// Represents the entire Aura TOML config file
/// </summary>
public record Document
{
	[TomlProperty("project")]
	[TomlDoNotInlineObject]
	public Project? Project { get; init; }
}
