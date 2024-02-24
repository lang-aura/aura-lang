using AuraLang.AST;

namespace AuraLang.Visitor;

/// <summary>
///     Visits a <see cref="ITypedAuraStmtVisitable" /> AST node
/// </summary>
/// <typeparam name="T">The value returned from the visit</typeparam>
public interface ITypedAuraStmtVisitor<out T>
{
	public T Visit(TypedDefer defer);
	public T Visit(TypedExpressionStmt expressionStmt);
	public T Visit(TypedFor @for);
	public T Visit(TypedForEach forEach);
	public T Visit(TypedNamedFunction namedFunction);
	public T Visit(TypedLet let);
	public T Visit(TypedMod mod);
	public T Visit(TypedReturn @return);
	public T Visit(FullyTypedClass @class);
	public T Visit(TypedInterface @interface);
	public T Visit(TypedWhile @while);
	public T Visit(TypedImport import);
	public T Visit(TypedMultipleImport multipleImport);
	public T Visit(TypedComment comment);
	public T Visit(TypedContinue @continue);
	public T Visit(TypedBreak @break);
	public T Visit(TypedYield yield);
	public T Visit(PartiallyTypedClass partiallyTypedClass);
	public T Visit(TypedCheck check);
	public T Visit(TypedStruct @struct);
	public T Visit(TypedFunctionSignature fnSignature);
}

/// <summary>
///     Visits a <see cref="ITypedAuraExprVisitable" /> AST node
/// </summary>
/// <typeparam name="T">The value returned from the visit</typeparam>
public interface ITypedAuraExprVisitor<out T>
{
	public T Visit(TypedAssignment assignment);
	public T Visit(TypedAnonymousFunction anonymousFunction);
	public T Visit(TypedPlusPlusIncrement plusPlusIncrement);
	public T Visit(TypedMinusMinusDecrement minusMinusDecrement);
	public T Visit(TypedBinary binary);
	public T Visit(TypedBlock block);
	public T Visit(TypedCall call);
	public T Visit(TypedGet get);
	public T Visit(TypedGetIndex getIndex);
	public T Visit(TypedGetIndexRange getIndexRange);
	public T Visit(TypedGrouping grouping);
	public T Visit(TypedIf @if);
	public T Visit(TypedNil nil);
	public T Visit(TypedLogical logical);
	public T Visit(TypedSet set);
	public T Visit(TypedThis @this);
	public T Visit(TypedUnary unary);
	public T Visit(TypedVariable variable);
	public T Visit(TypedIs @is);
	public T Visit(IntLiteral intLiteral);
	public T Visit(FloatLiteral floatLiteral);
	public T Visit(StringLiteral stringLiteral);
	public T Visit<TU>(ListLiteral<TU> listLiteral) where TU : IAuraAstNode;

	public T Visit<TK, TV>(MapLiteral<TK, TV> mapLiteral) where TK : IAuraAstNode where TV : IAuraAstNode;

	public T Visit(BoolLiteral boolLiteral);
	public T Visit(CharLiteral charLiteral);
	public T Visit(TypedAnonymousStruct anonymousStruct);
}

/// <summary>
///     Visits a <see cref="IUntypedAuraStmtVisitable" /> AST node
/// </summary>
/// <typeparam name="T">The value returned from the visit</typeparam>
public interface IUntypedAuraStmtVisitor<out T>
{
	public T Visit(UntypedDefer defer);
	public T Visit(UntypedExpressionStmt expressionStmt);
	public T Visit(UntypedFor @for);
	public T Visit(UntypedForEach forEach);
	public T Visit(UntypedNamedFunction namedFunction);
	public T Visit(UntypedLet let);
	public T Visit(UntypedMod mod);
	public T Visit(UntypedReturn @return);
	public T Visit(UntypedClass @class);
	public T Visit(UntypedInterface @interface);
	public T Visit(UntypedWhile @while);
	public T Visit(UntypedImport import);
	public T Visit(UntypedMultipleImport multipleImport);
	public T Visit(UntypedComment comment);
	public T Visit(UntypedContinue @continue);
	public T Visit(UntypedBreak @break);
	public T Visit(UntypedYield yield);
	public T Visit(UntypedNewLine newline);
	public T Visit(UntypedCheck check);
	public T Visit(UntypedStruct @struct);
	public T Visit(UntypedFunctionSignature fnSignature);
}

/// <summary>
///     Visits a <see cref="IUntypedAuraExprVisitable" /> AST node
/// </summary>
/// <typeparam name="T">The value returned from the visit</typeparam>
public interface IUntypedAuraExprVisitor<out T>
{
	public T Visit(UntypedAssignment assignment);
	public T Visit(UntypedAnonymousFunction anonymousFunction);
	public T Visit(UntypedPlusPlusIncrement plusPlusIncrement);
	public T Visit(UntypedMinusMinusDecrement minusMinusDecrement);
	public T Visit(UntypedBinary binary);
	public T Visit(UntypedBlock block);
	public T Visit(UntypedCall call);
	public T Visit(UntypedGet get);
	public T Visit(UntypedGetIndex getIndex);
	public T Visit(UntypedGetIndexRange getIndexRange);
	public T Visit(UntypedGrouping grouping);
	public T Visit(UntypedIf @if);
	public T Visit(UntypedNil nil);
	public T Visit(UntypedLogical logical);
	public T Visit(UntypedSet set);
	public T Visit(UntypedThis @this);
	public T Visit(UntypedUnary unary);
	public T Visit(UntypedVariable variable);
	public T Visit(UntypedIs @is);
	public T Visit(IntLiteral intLiteral);
	public T Visit(FloatLiteral floatLiteral);
	public T Visit(StringLiteral stringLiteral);
	public T Visit<TU>(ListLiteral<TU> listLiteral) where TU : IAuraAstNode;

	public T Visit<TK, TV>(MapLiteral<TK, TV> mapLiteral) where TK : IAuraAstNode where TV : IAuraAstNode;

	public T Visit(BoolLiteral boolLiteral);
	public T Visit(CharLiteral charLiteral);
	public T Visit(UntypedAnonymousStruct anonymousStruct);
}
