using AuraLang.Exceptions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;
using LspRange = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace AuraLang.Lsp.DiagnosticsPublisher;

/// <summary>
///     Responsible for publishing diagnostics to the LSP client
/// </summary>
public class AuraDiagnosticsPublisher
{
	/// <summary>
	///     The JSON RPC connection used to transmit diagnostics
	/// </summary>
	private JsonRpc Rpc { get; }

	public AuraDiagnosticsPublisher(JsonRpc rpc)
	{
		Rpc = rpc;
	}

	/// <summary>
	///     Sends diagnostics to the LSP client corresponding to the supplied exception and Aura source file URI
	/// </summary>
	/// <param name="ex">
	///     The <see cref="AuraException" /> encountered during the compilation process. The specific details of
	///     the diagnostic will be extracted from this exception
	/// </param>
	/// <param name="uri">The path of the Aura source file where the error was encountered</param>
	public async Task SendAsync(AuraException ex, Uri uri)
	{
		var diagnostic = new Diagnostic
		{
			Code = "Warning",
			Message = ex.Message,
			Severity = DiagnosticSeverity.Error,
			Range = new LspRange
			{
				Start = new Position { Line = ex.Range.Start.Line - 1, Character = ex.Range.Start.Character },
				End = new Position { Line = ex.Range.End.Line - 1, Character = ex.Range.End.Character }

			}
		};
		var publish = new PublishDiagnosticParams { Uri = uri, Diagnostics = new[] { diagnostic } };

		// Send 'textDocument/publishDiagnostics' notification to the client
		await Rpc.NotifyWithParameterObjectAsync("textDocument/publishDiagnostics", publish);
	}

	/// <summary>
	///     Clears any existing diagnostics
	/// </summary>
	/// <param name="uri">The path of the Aura source file where the diagnostics will be cleared</param>
	public async Task ClearAsync(Uri uri)
	{
		var publish = new PublishDiagnosticParams { Uri = uri, Diagnostics = Array.Empty<Diagnostic>() };
		await Rpc.NotifyWithParameterObjectAsync("textDocument/publishDiagnostics", publish);
	}
}
