using AuraLang.Exceptions;
using AuraLang.Lsp.DiagnosticsPublisher;
using AuraLang.Lsp.DocumentManager;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace AuraLang.Lsp.LanguageServer;

/// <summary>
///     Responsible for communicating with an LSP client to provide an enhanced Aura development experience
/// </summary>
public class AuraLanguageServer : IDisposable
{
	/// <summary>
	///     The JSON RPC connection to communicate over
	/// </summary>
	private JsonRpc? _rpc;
	private readonly ManualResetEvent _disconnectEvent = new(false);
	private bool _isDisposed;
	private static readonly object Object = new();
	private bool Verbose { get; }

	/// <summary>
	///     Manages the Aura source files currently owned by the LSP client
	/// </summary>
	private readonly AuraDocumentManager _documents = new();

	/// <summary>
	///     Sends diagnostics to the LSP client
	/// </summary>
	private AuraDiagnosticsPublisher? DiagnosticsPublisher { get; set; }

	public AuraLanguageServer(bool verbose) { Verbose = verbose; }

	/// <summary>
	///     Initializes the Aura LSP server
	/// </summary>
	public async Task InitAsync()
	{
		_rpc = JsonRpc.Attach(
			Console.OpenStandardOutput(),
			Console.OpenStandardInput(),
			this
		);
		DiagnosticsPublisher = new AuraDiagnosticsPublisher(_rpc);
		_rpc.Disconnected += OnRpcDisconnected;
		await _rpc.Completion;
	}

	/// <summary>
	///     Responds to the LSP client's <c>initialize</c> event
	/// </summary>
	[JsonRpcMethod(Methods.InitializeName)]
	public InitializeResult Initialize(JToken arg)
	{
		lock (Object)
		{
			if (Verbose) Console.Error.WriteLine("<-- Initialize");

			var capabilities = new ServerCapabilities
			{
				TextDocumentSync =
					new TextDocumentSyncOptions
					{
						Change = TextDocumentSyncKind.Full,
						OpenClose = true,
						Save = new SaveOptions { IncludeText = true },
						WillSave = true,
						WillSaveWaitUntil = true
					},
				CompletionProvider = new CompletionOptions { TriggerCharacters = new[] { "." } },
				HoverProvider = true,
				SignatureHelpProvider = new SignatureHelpOptions { TriggerCharacters = new[] { "(" } },
				DefinitionProvider = false,
				TypeDefinitionProvider = false,
				ImplementationProvider = false,
				ReferencesProvider = false,
				DocumentHighlightProvider = false,
				DocumentSymbolProvider = false,
				CodeLensProvider = null,
				DocumentLinkProvider = null,
				DocumentFormattingProvider = true,
				DocumentRangeFormattingProvider = false,
				RenameProvider = false,
				ExecuteCommandProvider = null,
				WorkspaceSymbolProvider = false,
				SemanticTokensOptions = null
			};

			var result = new InitializeResult { Capabilities = capabilities };

			var json = JsonConvert.SerializeObject(result);
			if (Verbose) Console.Error.WriteLine("--> " + json);
			return result;
		}
	}

	/// <summary>
	///     Responds to the LSP client's <c>initialized</c> event
	/// </summary>
	[JsonRpcMethod(Methods.InitializedName)]
	public void Initialized(JToken arg)
	{
		lock (Object)
		{
			try
			{
				if (Verbose)
				{
					Console.Error.WriteLine("<-- Initialized");
					Console.Error.WriteLine(arg.ToString());
				}
			}
			catch (Exception)
			{
			}
		}
	}

	[JsonRpcMethod(Methods.TextDocumentDidOpenName)]
	public async Task DidOpenTextDocumentAsync(JToken jToken)
	{
		var @params = DeserializeJToken<DidOpenTextDocumentParams>(jToken);
		try
		{
			_documents.UpdateDocument(@params.TextDocument.Uri.LocalPath, @params.TextDocument.Text);
		}
		catch (AuraExceptionContainer e)
		{
			await DiagnosticsPublisher!.SendAsync(e, @params.TextDocument.Uri);

			return;
		}

		await DiagnosticsPublisher!.ClearAsync(@params.TextDocument.Uri);
	}

	[JsonRpcMethod(Methods.TextDocumentDidChangeName)]
	public async Task DidChangeTextDocumentAsync(JToken jToken)
	{
		var @params = DeserializeJToken<DidChangeTextDocumentParams>(jToken);
		try
		{
			_documents.UpdateDocument(@params.TextDocument.Uri.LocalPath, @params.ContentChanges.First().Text);
		}
		catch (AuraExceptionContainer e)
		{
			await DiagnosticsPublisher!.SendAsync(e, @params.TextDocument.Uri);

			return;
		}

		await DiagnosticsPublisher!.ClearAsync(@params.TextDocument.Uri);
	}

	[JsonRpcMethod(Methods.TextDocumentWillSaveName)]
	public void WillSaveTextDocument(JToken jToken)
	{
		var @params = DeserializeJToken<WillSaveTextDocumentParams>(jToken);
	}

	[JsonRpcMethod(Methods.TextDocumentWillSaveWaitUntilName)]
	public TextEdit[]? WillSaveWaitUntilTextDocument(JToken jToken)
	{
		var @params = DeserializeJToken<WillSaveTextDocumentParams>(jToken);
		return null;
	}

	[JsonRpcMethod(Methods.TextDocumentDidSaveName)]
	public async Task DidSaveTextDocumentAsync(JToken jToken)
	{
		var @params = DeserializeJToken<DidSaveTextDocumentParams>(jToken);
		try
		{
			_documents.UpdateDocument(@params.TextDocument.Uri.LocalPath, @params.Text!);
		}
		catch (AuraExceptionContainer e)
		{
			await DiagnosticsPublisher!.SendAsync(e, @params.TextDocument.Uri);

			return;
		}

		await DiagnosticsPublisher!.ClearAsync(@params.TextDocument.Uri);
	}

	[JsonRpcMethod(Methods.TextDocumentDidCloseName)]
	public void DidCloseTextDocument(JToken jToken)

	{
		var @params = DeserializeJToken<DidCloseTextDocumentParams>(jToken);
		_documents.DeleteDocument(@params.TextDocument.Uri.LocalPath);
	}

	[JsonRpcMethod(Methods.TextDocumentHoverName)]
	public Hover HoverProvider(JToken jToken)
	{
		var @params = DeserializeJToken<TextDocumentPositionParams>(jToken);
		return _documents.GetHoverText(@params);
	}

	[JsonRpcMethod(Methods.TextDocumentCompletionName)]
	public CompletionList? CompletionProvider(JToken jToken)
	{
		var @params = DeserializeJToken<CompletionParams>(jToken);
		return _documents.GetCompletionItems(@params);
	}

	[JsonRpcMethod(Methods.TextDocumentSignatureHelpName)]
	public SignatureHelp? SignatureHelpProvider(JToken jToken)
	{
		var @params = DeserializeJToken<SignatureHelpParams>(jToken);
		return _documents.GetSignatureHelp(@params);
	}

	[JsonRpcMethod(Methods.ShutdownName)]
	public JToken? ShutdownName()
	{
		lock (Object)
		{
			if (Verbose) Console.Error.WriteLine("<-- Shutdown");
			return null;
		}
	}

	[JsonRpcMethod(Methods.ExitName)]
	public void ExitName()
	{
		lock (Object)
		{
			try
			{
				if (Verbose) Console.Error.WriteLine("<-- Exit");
				Exit();
			}
			catch (Exception) { }
		}
	}

	~AuraLanguageServer() { Dispose(false); }

	public void Start() { _disconnectEvent.WaitOne(); }

	private void OnRpcDisconnected(object? sender, JsonRpcDisconnectedEventArgs e) { Exit(); }

	private void Exit() { Environment.Exit(0); }

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (_isDisposed) return;
		if (disposing) _disconnectEvent.Dispose();

		_isDisposed = true;
	}

	private T DeserializeJToken<T>(JToken jToken)
	{
		var s = JsonConvert.SerializeObject(jToken);
		var t = JsonConvert.DeserializeObject<T>(s);
		return t!;
	}
}
