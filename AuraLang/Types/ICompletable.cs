using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace AuraLang.Types;

public interface ICompletable
{
	IEnumerable<string> SupportedTriggerCharacters { get; }
	bool IsTriggerCharacterSupported(string triggerCharacter);
	CompletionList ProvideCompletableOptions(string triggerCharacter);
}
