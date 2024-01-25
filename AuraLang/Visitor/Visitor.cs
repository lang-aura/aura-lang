using AuraLang.AST;

namespace AuraLang.Visitor;

public interface ITypedAuraStmtVisitor<T>
{
	public abstract T Visit(TypedDefer defer);
	public abstract T Visit(TypedExpressionStmt expressionStmt);
	public abstract T Visit(TypedFor for_);
	public abstract T Visit(TypedForEach forEach);
	public abstract T Visit(TypedNamedFunction namedFunction);
	public abstract T Visit(TypedLet let);
	public abstract T Visit(TypedMod mod);
	public abstract T Visit(TypedReturn return_);
	public abstract T Visit(FullyTypedClass class_);
	public abstract T Visit(TypedInterface interface_);
	public abstract T Visit(TypedWhile while_);
	public abstract T Visit(TypedImport import);
	public abstract T Visit(TypedMultipleImport multipleImport);
	public abstract T Visit(TypedComment comment);
	public abstract T Visit(TypedContinue continue_);
	public abstract T Visit(TypedBreak break_);
	public abstract T Visit(TypedYield yield);
	public abstract T Visit(PartiallyTypedFunction partiallyTypedFunction);
	public abstract T Visit(PartiallyTypedClass partiallyTypedClass);
	public abstract T Visit(TypedCheck check);
	public abstract T Visit(TypedStruct @struct);
}

public interface ITypedAuraExprVisitor<T>
{
	public abstract T Visit(TypedAssignment assignment);
	public abstract T Visit(TypedAnonymousFunction anonymousFunction);
	public abstract T Visit(TypedPlusPlusIncrement plusPlusIncrement);
	public abstract T Visit(TypedMinusMinusDecrement minusMinusDecrement);
	public abstract T Visit(TypedBinary binary);
	public abstract T Visit(TypedBlock block);
	public abstract T Visit(TypedCall call);
	public abstract T Visit(TypedGet get);
	public abstract T Visit(TypedGetIndex getIndex);
	public abstract T Visit(TypedGetIndexRange getIndexRange);
	public abstract T Visit(TypedGrouping grouping);
	public abstract T Visit(TypedIf if_);
	public abstract T Visit(TypedNil nil);
	public abstract T Visit(TypedLogical logical);
	public abstract T Visit(TypedSet set);
	public abstract T Visit(TypedThis this_);
	public abstract T Visit(TypedUnary unary);
	public abstract T Visit(TypedVariable variable);
	public abstract T Visit(TypedIs is_);
	public abstract T Visit(IntLiteral intLiteral);
	public abstract T Visit(FloatLiteral floatLiteral);
	public abstract T Visit(StringLiteral stringLiteral);
	public abstract T Visit<U>(ListLiteral<U> listLiteral) where U : IAuraAstNode;
	public abstract T Visit<TK, TV>(MapLiteral<TK, TV> mapLiteral)
		where TK : IAuraAstNode
		where TV : IAuraAstNode;
	public abstract T Visit(BoolLiteral boolLiteral);
	public abstract T Visit(CharLiteral charLiteral);
}

public interface IUntypedAuraStmtVisitor<T>
{
	public abstract T Visit(UntypedDefer defer);
	public abstract T Visit(UntypedExpressionStmt expressionStmt);
	public abstract T Visit(UntypedFor for_);
	public abstract T Visit(UntypedForEach forEach);
	public abstract T Visit(UntypedNamedFunction namedFunction);
	public abstract T Visit(UntypedLet let);
	public abstract T Visit(UntypedMod mod);
	public abstract T Visit(UntypedReturn return_);
	public abstract T Visit(UntypedClass class_);
	public abstract T Visit(UntypedInterface interface_);
	public abstract T Visit(UntypedWhile while_);
	public abstract T Visit(UntypedImport import);
	public abstract T Visit(UntypedMultipleImport multipleImport);
	public abstract T Visit(UntypedComment comment);
	public abstract T Visit(UntypedContinue continue_);
	public abstract T Visit(UntypedBreak break_);
	public abstract T Visit(UntypedYield yield);
	public abstract T Visit(UntypedNewLine newline);
	public abstract T Visit(UntypedCheck check);
	public abstract T Visit(UntypedStruct @struct);
}

public interface IUntypedAuraExprVisitor<T>
{
	public abstract T Visit(UntypedAssignment assignment);
	public abstract T Visit(UntypedAnonymousFunction anonymousFunction);
	public abstract T Visit(UntypedPlusPlusIncrement plusPlusIncrement);
	public abstract T Visit(UntypedMinusMinusDecrement minusMinusDecrement);
	public abstract T Visit(UntypedBinary binary);
	public abstract T Visit(UntypedBlock block);
	public abstract T Visit(UntypedCall call);
	public abstract T Visit(UntypedGet get);
	public abstract T Visit(UntypedGetIndex getIndex);
	public abstract T Visit(UntypedGetIndexRange getIndexRange);
	public abstract T Visit(UntypedGrouping grouping);
	public abstract T Visit(UntypedIf if_);
	public abstract T Visit(UntypedNil nil);
	public abstract T Visit(UntypedLogical logical);
	public abstract T Visit(UntypedSet set);
	public abstract T Visit(UntypedThis this_);
	public abstract T Visit(UntypedUnary unary);
	public abstract T Visit(UntypedVariable variable);
	public abstract T Visit(UntypedIs is_);
	public abstract T Visit(IntLiteral intLiteral);
	public abstract T Visit(FloatLiteral floatLiteral);
	public abstract T Visit(StringLiteral stringLiteral);
	public abstract T Visit<U>(ListLiteral<U> listLiteral) where U : IAuraAstNode;
	public abstract T Visit<TK, TV>(MapLiteral<TK, TV> mapLiteral)
		where TK : IAuraAstNode
		where TV : IAuraAstNode;
	public abstract T Visit(BoolLiteral boolLiteral);
	public abstract T Visit(CharLiteral charLiteral);
}
