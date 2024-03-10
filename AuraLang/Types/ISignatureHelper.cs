using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace AuraLang.Types;

/// <summary>
///     Represents a type that can provide signature help to an LSP client
/// </summary>
public interface ISignatureHelper
{
	/// <summary>
	///     Represents the trigger characters that this type supports
	/// </summary>
	IEnumerable<string> SupportedSignatureHelpTriggerCharacters { get; }

	/// <summary>
	///     Determines if the supplied trigger character is supported
	/// </summary>
	/// <param name="triggerCharacter">The trigger character</param>
	/// <returns>A boolean indicating if the supplied trigger character is supported</returns>
	bool IsSignatureHelpTriggerCharacterSupported(string triggerCharacter);

	/// <summary>
	///     Provides signature help based on the supplied trigger character
	/// </summary>
	/// <param name="triggerCharacter">The supplied trigger character</param>
	/// <returns>Signature help information</returns>
	SignatureHelp ProvideSignatureHelp(string triggerCharacter);
}
