namespace AuraLang.Visitor;

public interface ITypedAuraStmtVisitable
{
	T Accept<T>(ITypedAuraStmtVisitor<T> visitor);
}

public interface ITypedAuraExprVisitable
{
	T Accept<T>(ITypedAuraExprVisitor<T> visitor);
}

public interface IUntypedAuraStmtVisitable
{
	T Accept<T>(IUntypedAuraStmtVisitor<T> visitor);
}

public interface IUntypedAuraExprVisitable
{
	T Accept<T>(IUntypedAuraExprVisitor<T> visitor);
}
