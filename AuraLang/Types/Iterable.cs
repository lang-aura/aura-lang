namespace AuraLang.Types;

/// <summary>
///     Represents a type that can be iterated over
/// </summary>
public interface IIterable
{
	/// <summary>
	///     Returns the type of each element in the iterable
	/// </summary>
	/// <returns>The type of each element in the iterable</returns>
	AuraType GetIterType();
}
