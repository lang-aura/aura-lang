using System.Collections.Concurrent;
using AuraLang.AST;
using AuraLang.Exceptions;
using AuraLang.Location;
using AuraLang.Lsp.Document;
using AuraLang.Lsp.HoverProvider;
using AuraLang.Lsp.SynchronizedFileProvider;
using AuraLang.Parser;
using AuraLang.Scanner;
using AuraLang.Token;
using AuraLang.TypeChecker;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using Position = AuraLang.Location.Position;

namespace AuraLang.Lsp.DocumentManager;

public class AuraDocumentManager
{
	private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AuraLspDocument>> _documents = new();
	private readonly AuraHoverProvider _hoverProvider = new();

	public void UpdateDocument(string path, string contents)
	{
		var (module, file) = GetModuleAndFileNames(path);
		try
		{
			// Compile file's new contents
			var tokens = new AuraScanner(contents, path).ScanTokens().Where(tok => tok.Typ is not TokType.Newline).ToList();
			var untypedAst = new AuraParser(tokens, path).Parse();
			var typeChecker = new AuraTypeChecker(
				importedModuleProvider: new AuraSynchronizedFileProvider(this),
				filePath: path,
				projectName: "Test Project Name"
			);
			typeChecker.BuildSymbolsTable(untypedAst);
			var typedAst = typeChecker.CheckTypes(untypedAst);

			// Store file's new contents
			var lspDoc = new AuraLspDocument(contents, typedAst);
			var modDict = _documents.GetOrAdd(module, (_) => new ConcurrentDictionary<string, AuraLspDocument>());
			modDict.AddOrUpdate(file, lspDoc, (k, v) => v);
			_documents.AddOrUpdate(module, modDict, (k, dict) => dict);
			_documents[module] = modDict;
		}
		catch (AuraExceptionContainer)
		{
			throw;
		}
		catch (Exception e)
		{
			Console.Error.WriteLine($"Caught exception {e}: {e.Message}");
		}
	}

	public void DeleteDocument(string path)
	{
		var (module, file) = GetModuleAndFileNames(path);
		var modDict = _documents[module];
		modDict.TryRemove(file, out AuraLspDocument? _);
		if (!modDict.IsEmpty)
		{
			_documents[module] = modDict;
		}
		else
		{
			_documents.Remove(module, out _);
		}
	}

	public AuraLspDocument? GetDocument(string path)
	{
		var (module, file) = GetModuleAndFileNames(path);
		return _documents[module][file];
	}

	public List<(string, string)> GetModule(string module)
	{
		if (_documents.TryGetValue(module, out var value))
		{
			return value.Select(item => (item.Key, item.Value.Contents)).ToList();
		}
		return new List<(string, string)>();
	}

	public ITypedAuraStatement FindStmtByPosition(TextDocumentPositionParams hoverParams)
	{
		var position = hoverParams.Position;
		var fileContents = GetDocument(hoverParams.TextDocument.Uri.ToString());
		return _hoverProvider.FindStmtByPosition(Position.FromMicrosoftPosition(position), fileContents!.TypedAst);
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
