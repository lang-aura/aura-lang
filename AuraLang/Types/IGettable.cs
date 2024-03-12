namespace AuraLang.Types;

/// <summary>
///     Represents a type that can be used as the object in a <c>get</c> expression
/// </summary>
public interface IGettable
{
	/// <summary>
	///     Gets the type identified by the supplied attribute name
	/// </summary>
	/// <param name="attribute">The name of the attribute to get from this type</param>
	/// <returns>The type of the attribute, if it exists; else null</returns>
	AuraType? Get(string attribute);

	/// <summary>
	///     Gets the type identified by the supplied attribute name only if the identified type has public visibility
	/// </summary>
	/// <param name="attribute">The name of the attribute to get from this type</param>
	/// <returns>The type of the attribute, if it exists and has public visibility; else null</returns>
	AuraType? GetPublic(string attribute) { return Get(attribute); }
}
