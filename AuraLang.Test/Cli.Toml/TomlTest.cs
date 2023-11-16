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
            Assert.That(doc["project"], Is.Not.Null);
            Assert.That(doc["project"]["name"].AsString.Value, Is.EqualTo("test"));
            Assert.That(doc["project"]["version"].AsString.Value, Is.EqualTo("0.0.1"));
            Assert.That(doc["project"]["description"].AsString.Value, Is.EqualTo(string.Empty));
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