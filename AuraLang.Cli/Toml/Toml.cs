using System.Text.Json.Serialization;
using Newtonsoft.Json;
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

    public TomlTable Parse()
    {
        using var reader = File.OpenText($"{_path}/aura.toml");
        return TOML.Parse(reader);
    }

    public string GetProjectName()
    {
        var table = Parse();
        return table["project"]["name"].AsString.Value;
    }
}
