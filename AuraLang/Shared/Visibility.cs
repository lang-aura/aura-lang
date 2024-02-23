namespace AuraLang.Shared;

/// <summary>
///     Defines an exportable type's visibility
/// </summary>
public enum Visibility
{
	/// <summary>
	///     Indicates that an exportable type may be used outside of its defining module
	/// </summary>
	Public,

	/// <summary>
	///     Indicates that an exportable type may not be used outside of its defining module. Instances of exportable types
	///     with a private visibility modifier are not exported outside their module
	/// </summary>
	Private
}
