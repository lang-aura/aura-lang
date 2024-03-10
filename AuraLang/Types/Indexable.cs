namespace AuraLang.Types;

/// <summary>
///     Represents a type that can be indexed
/// </summary>
public interface IIndexable
{
	/// <summary>
	///     Represents the type that can be used as the indexing value
	/// </summary>
	/// <returns>The type that can be used as the indexing value</returns>
	AuraType IndexingType();

	/// <summary>
	///     Represents the type that will be returned by the indexing operation
	/// </summary>
	/// <returns>The type that will be returned by the indexing operation</returns>
	AuraType GetIndexedType();
}

/// <summary>
///     Represents a type that can be ranged indexed
/// </summary>
public interface IRangeIndexable
{
	/// <summary>
	///     Represents the type that can be used as the indexing values
	/// </summary>
	/// <returns>The type that can be used as the indexing value</returns>
	AuraType IndexingType();

	/// <summary>
	///     Represents the type that will be returned by the range indexing operation
	/// </summary>
	/// <returns>The type that will be returned by the indexing operation</returns>
	AuraType GetRangeIndexedType();
}
