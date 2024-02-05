// using AuraLang.Lsp.DocumentManager;
// using MediatR;
// using OmniSharp.Extensions.LanguageServer.Protocol;
// using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
// using OmniSharp.Extensions.LanguageServer.Protocol.Document;
// using OmniSharp.Extensions.LanguageServer.Protocol.Models;
// using OmniSharp.Extensions.LanguageServer.Protocol.Server;
// using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

// namespace AuraLang.Lsp.TextDocumentSyncHandler;

// public class AuraTextDocumentSyncHandler : TextDocumentSyncHandlerBase
// {
// 	private readonly ILanguageServerConfiguration _configuration;
//     private readonly AuraDocumentManager _documentManager;
// 	public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;
// 	private readonly TextDocumentSelector _documentSelector = new(
//         new TextDocumentFilter()
//         {
//             Pattern = "**/*.aura"
//         }
//     );
	
// 	public AuraTextDocumentSyncHandler(ILanguageServerConfiguration configuration, AuraDocumentManager documentManager)
//     {
//         _configuration = configuration;
//         _documentManager = documentManager;
//     }
	
// 	public TextDocumentChangeRegistrationOptions GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
// 	{
// 		return new TextDocumentChangeRegistrationOptions()
//         {
//             DocumentSelector = _documentSelector,
//             SyncKind = Change
//         };
// 	}

// 	public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
// 	{
// 		return new TextDocumentAttributes(uri, "text");
// 	}

// 	public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
// 	{
// 		var documentPath = request.TextDocument.Uri.ToString();
//         var text = request.ContentChanges.FirstOrDefault()?.Text;
// 		if (text is null) return Unit.Task;

//         _documentManager.UpdateDocument(documentPath, text);

// 		Console.WriteLine($"Updated buffer for document: {documentPath}\n{text}");

//         return Unit.Task;
// 	}

// 	public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
// 	{
// 		_documentManager.UpdateDocument(request.TextDocument.Uri.ToString(), request.TextDocument.Text);
//         return Unit.Task;
// 	}

// 	public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
// 	{
// 		_documentManager.DeleteDocument(request.TextDocument.Uri.ToString());
// 		return Unit.Task;
// 	}

// 	public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
// 	{
// 		_documentManager.UpdateDocument(request.TextDocument.Uri.ToString(), request.TextDocument.ToString());
// 		return Unit.Task;
// 	}

// 	protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
// 	{
// 		return new TextDocumentSyncRegistrationOptions();
// 	}
// }
