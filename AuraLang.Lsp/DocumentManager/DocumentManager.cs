using System.Collections.Concurrent;
using AuraLang.AST;
using AuraLang.Exceptions;
using AuraLang.Exceptions.Parser;
using AuraLang.Exceptions.Scanner;
using AuraLang.Exceptions.TypeChecker;
using AuraLang.Lsp.Document;
using AuraLang.Lsp.Service.CompletionProvider;
using AuraLang.Lsp.Service.HoverProvider;
using AuraLang.Lsp.Service.SignatureHelpProvider;
using AuraLang.Lsp.SynchronizedFileProvider;
using AuraLang.Parser;
using AuraLang.Scanner;
using AuraLang.Symbol;
using AuraLang.Token;
using AuraLang.TypeChecker;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Position = AuraLang.Location.Position;

namespace AuraLang.Lsp.DocumentManager;

/// <summary>
///     Responsible for managing the Aura source files currently owned by the LSP client
/// </summary>
public class AuraDocumentManager
{
	/// <summary>
	///     The Aura source files currently owned by the LSP client. The keys are module names and each value is a dictionary
	///     mapping the name of each file in the module to the file's contents and typed AST
	/// </summary>
	private readonly ConcurrentDictionary<string, (ConcurrentDictionary<string, AuraLspDocument>, IGlobalSymbolsTable)>
		_documents = new();

	/// <summary>
	///     Responsible for providing hover text
	/// </summary>
	private readonly AuraHoverProvider _hoverProvider = new();

	/// <summary>
	///     Responsible for providing completion options
	/// </summary>
	private readonly AuraCompletionProvider _completionProvider = new();

	/// <summary>
	///     Responsible for providing signature help
	/// </summary>
	private readonly AuraSignatureHelpProvider _signatureHelpProvider = new();

	/// <summary>
	///     Updates an existing document
	/// </summary>
	/// <param name="path">The path of the Aura source file being updated</param>
	/// <param name="contents">The new contents of the Aura source file</param>
	public void UpdateDocument(string path, string contents)
	{
		var (module, file) = GetModuleAndFileNames(path);
		try
		{
			var symbolsTable = new GlobalSymbolsTable();
			var (typedAst, auraEx) = CompileFile(
				path,
				contents,
				symbolsTable
			);

			// Store file's new contents
			var lspDoc = new AuraLspDocument(contents, typedAst);
			var modDict = _documents.GetOrAdd(
				module,
				_ => (new ConcurrentDictionary<string, AuraLspDocument>(), symbolsTable)
			);
			modDict.Item1.AddOrUpdate(
				file,
				lspDoc,
				(_, _) => lspDoc
			);
			modDict.Item2 = symbolsTable;
			_documents.AddOrUpdate(
				module,
				modDict,
				(_, _) => modDict
			);

			if (auraEx is not null) throw auraEx;
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

	/// <summary>
	///     Deletes an existing document
	/// </summary>
	/// <param name="path">The path of the Aura source file to delete</param>
	public void DeleteDocument(string path)
	{
		var (module, file) = GetModuleAndFileNames(path);
		var modDict = _documents[module];
		modDict.Item1.TryRemove(file, out _);
		if (!modDict.Item1.IsEmpty)
			_documents[module] = modDict;
		else
			_documents.Remove(module, out _);
	}

	/// <summary>
	///     Fetches an existing document
	/// </summary>
	/// <param name="path">The path of the Aura source file to fetch</param>
	/// <returns>A document representing the Aura source file located at the supplied path</returns>
	private AuraLspDocument? GetDocument(string path)
	{
		var (module, file) = GetModuleAndFileNames(path);
		if (_documents.TryGetValue(module, out var mod))
			if (mod.Item1.TryGetValue(file, out var doc))
				return doc;

		return null;
	}

	/// <summary>
	///     Fetches all existing Aura source files located in the specified module
	/// </summary>
	/// <param name="module">The module in question</param>
	/// <returns>All Aura source files contained in the specified module</returns>
	public List<(string, string)> GetModule(string module)
	{
		if (_documents.TryGetValue(module, out var value))
			return value.Item1.Select(item => (item.Key, item.Value.Contents)).ToList();
		return new List<(string, string)>();
	}

	/// <summary>
	///     Fetches hover text to display in the LSP client
	/// </summary>
	/// <param name="hoverParams">The hover parameters provided by the LSP client</param>
	/// <returns>A typed AST node that will provide the necessary hover text</returns>
	public Hover GetHoverText(TextDocumentPositionParams hoverParams)
	{
		var position = hoverParams.Position;
		var fileContents = GetDocument(hoverParams.TextDocument.Uri.ToString());
		if (fileContents is null) return new Hover();
		return _hoverProvider.GetHoverText(Position.FromMicrosoftPosition(position), fileContents.TypedAst);
	}

	/// <summary>
	///     Fetches completion items to display in the LSP client
	/// </summary>
	/// <param name="completionParams">The completion parameters provided by the LSP client</param>
	/// <returns>A list of completion items</returns>
	public CompletionList? GetCompletionItems(CompletionParams completionParams)
	{
		var position = completionParams.Position;
		var fileContents = GetDocument(completionParams.TextDocument.Uri.ToString());
		return _completionProvider.ComputeCompletionOptions(
			Position.FromMicrosoftPosition(position),
			completionParams.Context!.TriggerCharacter!,
			fileContents!.TypedAst
		);
	}

	/// <summary>
	///     Fetches signature help for a function to display in the LSP client
	/// </summary>
	/// <param name="signatureHelpParams">The signature help parameters provided by the LSP client</param>
	/// <returns>Signature help information</returns>
	public SignatureHelp? GetSignatureHelp(SignatureHelpParams signatureHelpParams)
	{
		var position = signatureHelpParams.Position;
		var fileContents = GetDocument(signatureHelpParams.TextDocument.Uri.ToString());
		return _signatureHelpProvider.ComputeSignatureHelp(
			Position.FromMicrosoftPosition(position),
			signatureHelpParams.Context!.TriggerCharacter!,
			fileContents!.TypedAst
		);
	}

	/// <summary>
	///     Parses a file path (which may be either absolute or relative) and returns the Aura module and source file names
	/// </summary>
	/// <param name="path">The path of the Aura source file</param>
	/// <returns>The module and source file names</returns>
	private (string, string) GetModuleAndFileNames(string path)
	{
		var startIndex = path.IndexOf("src");
		var moduleName = path[(startIndex + 4)..];
		var module = Path.GetDirectoryName(moduleName);
		var name = Path.GetFileName(moduleName);
		module = module is null || module == string.Empty
			? "src"
			: module;
		return (module, name);
	}

	private (List<ITypedAuraStatement>, AuraExceptionContainer?) CompileFile(
		string path,
		string contents,
		IGlobalSymbolsTable symbolsTable
	)
	{
		var (tokens, scannerEx) = Scan(path, contents);
		if (scannerEx is not null)
		{
			var (uAst, _) = Parse(path, tokens);
			var (tAst, _) = TypeCheck(
				path,
				uAst,
				symbolsTable
			);
			return (tAst, scannerEx);
		}

		var (untypedAst, parserEx) = Parse(path, tokens);
		if (parserEx is not null)
		{
			var (tyAst, _) = TypeCheck(
				path,
				untypedAst,
				symbolsTable
			);
			return (tyAst, parserEx);
		}

		return TypeCheck(
			path,
			untypedAst,
			symbolsTable
		);
	}

	private (List<Tok>, AuraExceptionContainer?) Scan(string path, string contents)
	{
		try
		{
			var tokens = new AuraScanner(contents, path)
				.ScanTokens()
				.Where(tok => tok.Typ is not TokType.Newline)
				.ToList();
			return (tokens, null);
		}
		catch (ScannerExceptionContainer e)
		{
			return (e.Valid ?? new List<Tok>(), e);
		}
	}

	private (List<IUntypedAuraStatement>, AuraExceptionContainer?) Parse(string path, List<Tok> tokens)
	{
		try
		{
			var untypedAst = new AuraParser(tokens, path).Parse();
			return (untypedAst, null);
		}
		catch (ParserExceptionContainer e)
		{
			return (e.Valid ?? new List<IUntypedAuraStatement>(), e);
		}
	}

	private (List<ITypedAuraStatement>, AuraExceptionContainer?) TypeCheck(
		string path,
		List<IUntypedAuraStatement> untypedAst,
		IGlobalSymbolsTable symbolsTable
	)
	{
		try
		{
			var typeChecker = new AuraTypeChecker(
				symbolsTable,
				new AuraSynchronizedFileProvider(this),
				path,
				"Test Project Name"
			);
			typeChecker.BuildSymbolsTable(untypedAst);
			var typedAst = typeChecker.CheckTypes(untypedAst);
			return (typedAst, null);
		}
		catch (TypeCheckerExceptionContainer e)
		{
			return (e.Valid ?? new List<ITypedAuraStatement>(), e);
		}
	}
}
