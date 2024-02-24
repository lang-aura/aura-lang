namespace AuraLang.Visitor;

/// <summary>
///     Accepts a visit from an <see cref="ITypedAuraStmtVisitor{T}" /> and redirects the visitor to the correct method
///     call
/// </summary>
public interface ITypedAuraStmtVisitable
{
	T Accept<T>(ITypedAuraStmtVisitor<T> visitor);
}

/// <summary>
///     Accepts a visit from an <see cref="ITypedAuraExprVisitor{T}" /> and redirects the visitor to the correct method
///     call
/// </summary>
public interface ITypedAuraExprVisitable
{
	T Accept<T>(ITypedAuraExprVisitor<T> visitor);
}

/// <summary>
///     Accepts a visit from a <see cref="IUntypedAuraStmtVisitor{T}" /> and redirects the visitor to the correct method
///     call
/// </summary>
public interface IUntypedAuraStmtVisitable
{
	T Accept<T>(IUntypedAuraStmtVisitor<T> visitor);
}

/// <summary>
///     Accepts a visit from a <see cref="IUntypedAuraExprVisitor{T}" /> and redirects the visitor to the correct method
///     call
/// </summary>
public interface IUntypedAuraExprVisitable
{
	T Accept<T>(IUntypedAuraExprVisitor<T> visitor);
}
