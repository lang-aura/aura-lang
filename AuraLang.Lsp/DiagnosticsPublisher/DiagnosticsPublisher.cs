using AuraLang.Exceptions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;
using LspRange = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace AuraLang.Lsp.DiagnosticsPublisher;

public class AuraDiagnosticsPublisher
{
	private JsonRpc Rpc { get; }

	public AuraDiagnosticsPublisher(JsonRpc rpc)
	{
		Rpc = rpc;
	}

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

	public async Task ClearAsync(Uri uri)
	{
		var publish = new PublishDiagnosticParams { Uri = uri, Diagnostics = Array.Empty<Diagnostic>() };
		await Rpc.NotifyWithParameterObjectAsync("textDocument/publishDiagnostics", publish);
	}
}
