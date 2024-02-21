using AuraLang.AST;
using AuraLang.Location;
using AuraLang.Visitor;

namespace AuraLang.Lsp.RangeFinder;

public class AuraRangeFinder : ITypedAuraStmtVisitor<ITypedAuraAstNode?>, ITypedAuraExprVisitor<ITypedAuraAstNode?>
{
	private Position Position { get; }
	private IEnumerable<ITypedAuraStatement> TypedAst { get; }

	public AuraRangeFinder(Position position, IEnumerable<ITypedAuraStatement> typedAst)
	{
		Position = position;
		TypedAst = typedAst;
	}

	public ITypedAuraAstNode? FindImmediatelyPrecedingNode()
	{
		foreach (var node in TypedAst)
		{
			var stmt = Statement(node);
			if (stmt is not null) return stmt;
		}

		return null;
	}

	private ITypedAuraAstNode? Statement(ITypedAuraStatement stmt) => stmt.Accept(this);

	private ITypedAuraAstNode? Expression(ITypedAuraExpression expr) => expr.Accept(this);

	public ITypedAuraAstNode? Visit(TypedAssignment assignment) => Expression(assignment.Value);

	public ITypedAuraAstNode? Visit(TypedAnonymousFunction anonymousFunction) =>
		anonymousFunction.Range.End == Position ? anonymousFunction : null;

	public ITypedAuraAstNode? Visit(TypedPlusPlusIncrement plusPlusIncrement) =>
		plusPlusIncrement.Range.End == Position ? plusPlusIncrement : null;

	public ITypedAuraAstNode? Visit(TypedMinusMinusDecrement minusMinusDecrement) =>
		minusMinusDecrement.Range.End == Position ? minusMinusDecrement : null;

	public ITypedAuraAstNode? Visit(TypedBinary binary) => binary.Range.End == Position ? binary : null;

	public ITypedAuraAstNode? Visit(TypedBlock block) => block.Range.End == Position ? block : null;

	public ITypedAuraAstNode? Visit(TypedCall call) => call.Range.End == Position ? call : null;

	public ITypedAuraAstNode? Visit(TypedGet get) => get.Range.End == Position ? get : null;

	public ITypedAuraAstNode? Visit(TypedGetIndex getIndex) => getIndex.Range.End == Position ? getIndex : null;

	public ITypedAuraAstNode? Visit(TypedGetIndexRange getIndexRange) =>
		getIndexRange.Range.End == Position ? getIndexRange : null;

	public ITypedAuraAstNode? Visit(TypedGrouping grouping) => grouping.Range.End == Position ? grouping : null;

	public ITypedAuraAstNode? Visit(TypedIf @if) => @if.Range.End == Position ? @if : null;

	public ITypedAuraAstNode? Visit(TypedNil nil) => nil.Range.End == Position ? nil : null;

	public ITypedAuraAstNode? Visit(TypedLogical logical) => logical.Range.End == Position ? logical : null;

	public ITypedAuraAstNode? Visit(TypedSet set) => set.Range.End == Position ? set : null;

	public ITypedAuraAstNode? Visit(TypedThis @this) => @this.Range.End == Position ? @this : null;

	public ITypedAuraAstNode? Visit(TypedUnary unary) => unary.Range.End == Position ? unary : null;

	public ITypedAuraAstNode? Visit(TypedVariable variable)
	{
		return variable.Range.End == Position ? variable : null;
	}

	public ITypedAuraAstNode? Visit(TypedIs @is) => @is.Range.End == Position ? @is : null;

	public ITypedAuraAstNode? Visit(IntLiteral intLiteral) => intLiteral.Range.End == Position ? intLiteral : null;

	public ITypedAuraAstNode? Visit(FloatLiteral floatLiteral) =>
		floatLiteral.Range.End == Position ? floatLiteral : null;

	public ITypedAuraAstNode? Visit(StringLiteral stringLiteral)
	{
		return stringLiteral.Range.End == Position ? stringLiteral : null;
	}


	public ITypedAuraAstNode? Visit(BoolLiteral boolLiteral) =>
		boolLiteral.Range.End == Position ? boolLiteral : null;

	public ITypedAuraAstNode? Visit(CharLiteral charLiteral) =>
		charLiteral.Range.End == Position ? charLiteral : null;

	public ITypedAuraAstNode? Visit(TypedAnonymousStruct anonymousStruct) =>
		anonymousStruct.Range.End == Position ? anonymousStruct : null;

	public ITypedAuraAstNode? Visit<TK, TV>(MapLiteral<TK, TV> mapLiteral)
		where TK : IAuraAstNode
		where TV : IAuraAstNode => mapLiteral.Range.End == Position ? mapLiteral : null;

	public ITypedAuraAstNode? Visit<T>(ListLiteral<T> listLiteral) where T : IAuraAstNode =>
		listLiteral.Range.End == Position ? listLiteral : null;

	public ITypedAuraAstNode? Visit(TypedDefer defer) => Expression(defer.Call);

	public ITypedAuraAstNode? Visit(TypedExpressionStmt expressionStmt)
	{
		return Expression(expressionStmt.Expression);
	}

	public ITypedAuraAstNode? Visit(TypedFor @for) => @for.Range.End == Position ? @for : null;

	public ITypedAuraAstNode? Visit(TypedForEach @foreach) => @foreach.Range.End == Position ? @foreach : null;

	public ITypedAuraAstNode? Visit(TypedNamedFunction namedFunction)
	{
		if (namedFunction.Range.End == Position) return namedFunction;
		foreach (var stmt in namedFunction.Body.Statements)
		{
			var result = Statement(stmt);
			if (result is not null) return result;
		}

		return null;
	}

	public ITypedAuraAstNode? Visit(TypedLet let)
	{
		if (let.Initializer is null) return let.Range.End == Position ? let : null;
		return Expression(let.Initializer);
	}

	public ITypedAuraAstNode? Visit(TypedMod mod) => mod.Range.End == Position ? mod : null;

	public ITypedAuraAstNode? Visit(TypedReturn @return)
	{
		if (@return.Value is null) return @return.Range.End == Position ? @return : null;
		return Expression(@return.Value);
	}

	public ITypedAuraAstNode? Visit(FullyTypedClass fullyTypedClass)
	{
		if (fullyTypedClass.Range.End == Position) return fullyTypedClass;
		foreach (var f in fullyTypedClass.Methods)
		{
			var stmt = Statement(f);
			if (stmt is not null) return stmt;
		}

		return null;
	}


	public ITypedAuraAstNode? Visit(TypedInterface @interface) =>
		@interface.Range.End == Position ? @interface : null;

	public ITypedAuraAstNode? Visit(TypedWhile @while) => @while.Range.End == Position ? @while : null;

	public ITypedAuraAstNode? Visit(TypedImport import) => import.Range.End == Position ? import : null;

	public ITypedAuraAstNode? Visit(TypedMultipleImport multipleImport) =>
		multipleImport.Range.End == Position ? multipleImport : null;

	public ITypedAuraAstNode? Visit(TypedComment comment) => comment.Range.End == Position ? comment : null;

	public ITypedAuraAstNode? Visit(TypedContinue @continue) => @continue.Range.End == Position ? @continue : null;

	public ITypedAuraAstNode? Visit(TypedBreak @break) => @break.Range.End == Position ? @break : null;

	public ITypedAuraAstNode? Visit(TypedYield yield) => yield.Range.End == Position ? yield : null;

	public ITypedAuraAstNode? Visit(PartiallyTypedFunction partiallyTypedFunction) => null;

	public ITypedAuraAstNode? Visit(PartiallyTypedClass partiallyTypedClass) => null;

	public ITypedAuraAstNode? Visit(TypedCheck check) => check.Range.End == Position ? check : null;

	public ITypedAuraAstNode? Visit(TypedStruct @struct) => @struct.Range.End == Position ? @struct : null;
}
