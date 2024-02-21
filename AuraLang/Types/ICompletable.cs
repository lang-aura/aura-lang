using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace AuraLang.Types;

public interface ICompletable
{
	CompletionList ProvideCompletableOptions();
}
