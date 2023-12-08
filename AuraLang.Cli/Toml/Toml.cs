using Tomlet;

namespace AuraLang.Cli.Toml;

public class AuraToml
{
	/// <summary>
	/// The path to the TOML config file
	/// </summary>
	private readonly string _path;

	public AuraToml(string path)
	{
		_path = path;
	}

	public AuraToml()
	{
		_path = ".";
	}

	/// <summary>
	/// Initializes the project's TOML config file with default values
	/// </summary>
	/// <param name="name">The project's name</param>
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
		File.WriteAllText($"{_path}/aura.toml", tomlDoc);
	}

	/// <summary>
	/// Parses the project's TOML config file
	/// </summary>
	/// <returns>A <c>Document</c> representing the TOML config file</returns>
	public Document Parse()
	{
		var s = File.ReadAllText($"{_path}/aura.toml");
		return TomletMain.To<Document>(s);
	}

	/// <summary>
	/// Fetches the project's name from the TOML config file
	/// </summary>
	/// <returns>The project's name</returns>
	public string GetProjectName()
	{
		var doc = Parse();
		return doc.Project.Name;
	}
}
