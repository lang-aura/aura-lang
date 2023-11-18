using System.Diagnostics;
using AuraLang.AST;
using AuraLang.Compiler;
using AuraLang.Exceptions;
using AuraLang.Parser;
using AuraLang.Scanner;
using AuraLang.TypeChecker;

namespace AuraLang.Test.Integration.Test;

public class IntegrationTest
{
    private const string BasePath = "../../../Integration/Examples";
    
    [Test]
    public async Task TestIntegration_HelloWorld()
    {
        var output = await ArrangeAndAct($"{BasePath}/src/hello_world.aura");
        MakeAssertions(output, "Hello world!\n");
    }

    [Test]
    public async Task TestIntegration_Yield()
    {
        var output = await ArrangeAndAct($"{BasePath}/src/yield.aura");
        MakeAssertions(output, "0\n");
    }

    private string ReadFile(string path) => File.ReadAllText(path);

    private async Task<string> ArrangeAndAct(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        Console.WriteLine($"{fileName}");
        
        var contents = ReadFile(path);
        try
        {
            // Scan tokens
            var tokens = new AuraScanner(contents).ScanTokens();
            // Parse tokens
            var untypedAst = new AuraParser(tokens).Parse();
            // Type check AST
            var typedAst = new AuraTypeChecker(new VariableStore(), new EnclosingClassStore(), new CurrentModuleStore(), new EnclosingNodeStore<UntypedAuraExpression>(), new EnclosingNodeStore<UntypedAuraStatement>())
                .CheckTypes(untypedAst);
            // Compile typed AST
            var output = new AuraCompiler(typedAst, "Examples").Compile();
            // Create Go output file
            await File.WriteAllTextAsync($"{BasePath}/build/pkg/{fileName}.go", output);
            // Format Go output file
            var fmt = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "go",
                    Arguments = $"fmt {fileName}.go",
                    WorkingDirectory = $"{BasePath}/build/pkg",
                    UseShellExecute = false
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
                    Arguments = $"run {fileName}.go",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = $"{BasePath}/build/pkg"
                }
            };
            run.Start();
            var actual = await run.StandardOutput.ReadToEndAsync();
            await run.WaitForExitAsync();
    
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