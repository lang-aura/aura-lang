using System.Collections.Concurrent;
using AuraLang.Exceptions;
using AuraLang.Lsp.SynchronizedFileProvider;
using AuraLang.Parser;
using AuraLang.Scanner;
using AuraLang.Token;
using AuraLang.TypeChecker;

namespace AuraLang.Lsp.DocumentManager;

public class AuraDocumentManager
{
	private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _documents = new();

	public void UpdateDocument(string path, string contents)
	{
		var (module, file) = GetModuleAndFileNames(path);
		// Store file's new contents
		var modDict = _documents.GetOrAdd(module, (_) => new ConcurrentDictionary<string, string>());
		modDict.AddOrUpdate(file, contents, (k, v) => v);
		_documents.AddOrUpdate(module, modDict, (k, dict) => dict);
		_documents[module] = modDict;
		// Compile file's new contents
		Console.Error.WriteLine("Type checking document");
		try
		{
			var tokens = new AuraScanner(contents, path).ScanTokens().Where(tok => tok.Typ is not TokType.Newline).ToList();
			var untypedAst = new AuraParser(tokens, path).Parse();
			var typeChecker = new AuraTypeChecker(
				importedModuleProvider: new AuraSynchronizedFileProvider(this),
				filePath: path,
				projectName: "Test Project Name"
			);
			typeChecker.BuildSymbolsTable(untypedAst);
			var typedAst = typeChecker.CheckTypes(untypedAst);
		}
		catch (AuraExceptionContainer e)
		{
			foreach (var ex in e.Exs)
			{
				Console.Error.WriteLine($"Caught Aura container exception: {ex.Message}");
			}
			return;
		}
		catch (Exception e)
		{
			Console.Error.WriteLine($"Caught exception {e}: {e.Message}");
			return;
		}
		Console.Error.WriteLine("File type checked successfully!");
	}

	public void DeleteDocument(string path)
	{
		var (module, file) = GetModuleAndFileNames(path);
		var modDict = _documents[module];
		modDict.TryRemove(file, out string? _);
		if (!modDict.IsEmpty)
		{
			_documents[module] = modDict;
		}
		else
		{
			_documents.Remove(module, out var _);
		}
	}

	public string? GetDocument(string path)
	{
		var (module, file) = GetModuleAndFileNames(path);
		return _documents[module][file];
	}

	public List<(string, string)> GetModule(string module)
	{
		if (_documents.TryGetValue(module, out var value))
		{
			return value.Select(item => (item.Key, item.Value)).ToList();
		}
		return new List<(string, string)>();
	}

	private (string, string) GetModuleAndFileNames(string path)
	{
		var startIndex = path.IndexOf("src");
		var moduleName = path[(startIndex + 4)..];
		var module = Path.GetDirectoryName(moduleName);
		var name = Path.GetFileName(moduleName);
		module = (module is null || module == string.Empty)
			? "src"
			: module;
		return (module, name);
	}
}
