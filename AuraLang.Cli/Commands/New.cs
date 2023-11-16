using System.Diagnostics;
using AuraLang.Cli.Options;
using AuraLang.Cli.Toml;

namespace AuraLang.Cli.Commands;

public class New
{
    private NewOptions Opts { get; }
    
    public New(NewOptions opts)
    {
        Opts = opts;
    }

    public int Execute()
    {
	    var projPath = $"{Opts.Path}/{Opts.Name}";
	    Directory.CreateDirectory(projPath);
	    Directory.CreateDirectory($"{projPath}/src");
	    File.WriteAllText($"{projPath}/src/{Opts.Name}.aura", "mod main\n\nimport aura/io\n\nfn main() {\n\tio.println(\"Hello world!\")\n}\n");
	    Directory.CreateDirectory($"{projPath}/test");
	    Directory.CreateDirectory($"{projPath}/build");
	    Directory.CreateDirectory($"{projPath}/build/pkg");
	    Directory.CreateDirectory($"{projPath}/build/pkg/stdlib");
	    var homeDir = Environment.GetEnvironmentVariable("HOME");

	    var cp = new Process
	    {
		    StartInfo = new ProcessStartInfo
		    {
			    FileName = "cp",
			    Arguments = $"-R \"{homeDir}/.aura/stdlib/\" \"{projPath}/build/pkg/stdlib\""
		    }
	    };
	    cp.Start();
	    cp.WaitForExit();
	    
	    File.WriteAllText($"{projPath}/README.md", string.Empty);
	    File.WriteAllText($"{projPath}/aura.toml", string.Empty);
	    new AuraToml(projPath).InitProject(Opts.Name);
	    
	    Directory.SetCurrentDirectory($"./{projPath}/build/pkg");

	    var modInit = new Process
	    {
			StartInfo = new ProcessStartInfo
			{
				FileName = "go",
				Arguments = $"mod init {Opts.Name}"
			}
	    };
	    modInit.Start();
	    modInit.WaitForExit();

	    return 0;
    }
}
