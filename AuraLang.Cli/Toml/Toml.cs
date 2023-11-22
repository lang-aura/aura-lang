using Tomlet;

namespace AuraLang.Cli.Toml;

public class AuraToml
{
	private readonly string _path;

	public AuraToml(string path)
	{
		_path = path;
	}

	public AuraToml()
	{
		_path = ".";
	}

	public void InitProject(string name)
	{
		var doc = new Document
		{
			Project = new Project
			{
				Name = name,
				Version = "0.0.1",
				Description = string.Empty
			}
		};

		var tomlDoc = TomletMain.TomlStringFrom(doc);
		using var writer = File.CreateText($"{_path}/aura.toml");
		writer.Write(tomlDoc);
		writer.Flush();
	}

	public Document Parse()
	{
		var s = File.ReadAllText($"{_path}/aura.toml");
		return TomletMain.To<Document>(s);
	}

	public string GetProjectName()
	{
		var doc = Parse();
		return doc.Project.Name;
	}
}
