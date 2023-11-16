using Tommy;

namespace AuraLang.Cli.Toml;

public class AuraToml
{
    private readonly string _path;

    public AuraToml(string path)
    {
        _path = path;
    }

    public void InitProject(string name)
    {
        var config = new TomlTable
        {
            ["Project"] =
            {
                ["Name"] = name,
                ["Version"] = "0.0.1",
                ["Description"] = string.Empty
            }
        };

        using var writer = File.CreateText($"{_path}/aura.toml");
        config.WriteTo(writer);
        writer.Flush();
    }
}
