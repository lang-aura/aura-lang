using System.Diagnostics;
using AuraLang.Compiler;
using AuraLang.Exceptions;
using AuraLang.Parser;
using AuraLang.Scanner;
using AuraLang.TypeChecker;

namespace AuraLang.Test.Integration.Test;

public class IntegrationTest
{
    [Test]
    public async Task TestIntegration_HelloWorld()
    {
        Directory.SetCurrentDirectory("../../../Integration/Examples");
        Console.WriteLine($"Current directory = {Directory.GetCurrentDirectory()}");
        var output = await ArrangeAndAct("./src/hello_world.aura");
        MakeAssertions(output, "Hello world!\n");
    }

    private string ReadFile(string path) => File.ReadAllText(path);

    private async Task<string> ArrangeAndAct(string path)
    {
        Console.WriteLine($"Current directory = {Directory.GetCurrentDirectory()}");
        var contents = ReadFile(path);
        Console.WriteLine($"contents = {contents}");
        try
        {
            // Scan tokens
            var tokens = new AuraScanner(contents).ScanTokens();
            // Parse tokens
            var untypedAst = new AuraParser(tokens).Parse();
            // Type check AST
            var typedAst = new AuraTypeChecker(new VariableStore(), new EnclosingClassStore(), new CurrentModuleStore())
                .CheckTypes(untypedAst);
            // Compile typed AST
            var output = new AuraCompiler(typedAst, "Examples").Compile();
            // Create Go output file
            Console.WriteLine($"Current directory = {Directory.GetCurrentDirectory()}");
            await File.WriteAllTextAsync("./build/pkg/hello_world.go", output);
            // Format Go output file
            var fmt = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "go",
                    Arguments = "fmt hello_world.go",
                    WorkingDirectory = "./build/pkg"
                }
            };
            fmt.Start();
            await fmt.WaitForExitAsync();
            // Run Go file
            var run = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "go",
                    Arguments = "run hello_world.go",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = ".build/pkg"
                }
            };
            Console.WriteLine(Directory.GetCurrentDirectory());
            run.Start();
            var actual = await run.StandardOutput.ReadToEndAsync();
            await run.WaitForExitAsync();
            Console.WriteLine($"actual = {actual}");
    
            return actual;
        }
        catch (AuraExceptionContainer ex)
        {
            ex.Report();
            throw;
        }
    }

    private void MakeAssertions(string actual, string expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.Not.Empty);
            Assert.That(actual, Has.Length.EqualTo(expected.Length));
            Assert.That(actual, Is.EqualTo(expected));
        });
    }
}