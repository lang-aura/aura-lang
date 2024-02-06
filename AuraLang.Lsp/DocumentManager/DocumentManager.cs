using System.Collections.Concurrent;

namespace AuraLang.Lsp.DocumentManager;

public class AuraDocumentManager
{
	private readonly ConcurrentDictionary<string, string> _documents = new();

	public void UpdateDocument(string path, string contents) => _documents.AddOrUpdate(path, contents, (k, v) => contents);

	public void DeleteDocument(string path) => _documents.Remove(path, out var _);

	public string? GetDocument(string path) => _documents.TryGetValue(path, out var document) ? document : null;
}
