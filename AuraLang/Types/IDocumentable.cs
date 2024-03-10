namespace AuraLang.Types;

/// <summary>
///     Represents a type that can be documented with a comment in an Aura source file
/// </summary>
public interface IDocumentable
{
	/// <summary>
	///     The type's documentation
	/// </summary>
	string Documentation { get; }
}
