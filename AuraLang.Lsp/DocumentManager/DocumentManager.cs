using System.Collections.Concurrent;

namespace AuraLang.Lsp.DocumentManager;

public class AuraDocumentManager
{
	private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _documents = new();

	public void UpdateDocument(string path, string contents)
	{
		var (module, file) = GetModuleAndFileNames(path);
		var modDict = _documents.GetOrAdd(module, (_) => new ConcurrentDictionary<string, string>());
		modDict.AddOrUpdate(file, contents, (k, v) => v);
		_documents.AddOrUpdate(module, modDict, (k, dict) => dict);
		_documents[module] = modDict;
		foreach (var pair in _documents)
		{
			Console.Error.WriteLine($"Module: {pair.Key}");
			foreach (var files in pair.Value)
			{
				Console.Error.WriteLine($"File '{files.Key}' with content: {files.Value}");
			}
		}
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
