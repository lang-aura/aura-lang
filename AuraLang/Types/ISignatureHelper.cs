using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace AuraLang.Types;

public interface ISignatureHelper
{
	IEnumerable<string> SupportedSignatureHelpTriggerCharacters { get; }
	bool IsSignatureHelpTriggerCharacterSupported(string triggerCharacter);
	SignatureHelp ProvideSignatureHelp(string triggerCharacter);
}
