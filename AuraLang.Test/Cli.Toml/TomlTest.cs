using AuraLang.Cli.Toml;

namespace AuraLang.Test.Cli.Toml;

public class TomlTest
{
	private AuraToml toml { get; set; }

	[SetUp]
	public void Setup()
	{
		toml = new AuraToml("../../../Cli.Toml");
	}

	[Test]
	public void TestToml_Parse()
	{
		// Act
		var doc = toml.Parse();
		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(doc, Is.Not.Null);
			Assert.That(doc!.Project, Is.Not.Null);
			Assert.That(doc.Project!.Name, Is.EqualTo("test"));
			Assert.That(doc.Project.Version, Is.EqualTo("0.0.1"));
			Assert.That(doc.Project.Description, Is.EqualTo(string.Empty));
		});
	}

	[Test]
	public void TestToml_GetProjectName()
	{
		// Act
		var projName = toml.GetProjectName();
		// Assert
		Assert.That(projName, Is.EqualTo("test"));
	}
}
