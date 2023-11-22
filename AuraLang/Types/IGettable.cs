namespace AuraLang.Types;

public interface IGettable
{
	AuraType? Get(string attribute);
}
