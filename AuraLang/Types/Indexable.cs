namespace AuraLang.Types;

public interface IIndexable
{
	AuraType IndexingType();
	AuraType GetIndexedType();
}

public interface IRangeIndexable
{
	AuraType IndexingType();
	AuraType GetRangeIndexedType();
}

