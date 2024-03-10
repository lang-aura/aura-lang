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
	/// <returns>The type of the attribute</returns>
	AuraType? Get(string attribute);
}
