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
			if (stmt is not null)
			{
				return stmt;
			}
		}

		return null;
	}

	private ITypedAuraAstNode? Statement(ITypedAuraStatement stmt)
	{
		return stmt.Accept(this);
	}

	private ITypedAuraAstNode? Expression(ITypedAuraExpression expr)
	{
		return expr.Accept(this);
	}

	public ITypedAuraAstNode? Visit(TypedAssignment assignment)
	{
		return Expression(assignment.Value);
	}

	public ITypedAuraAstNode? Visit(TypedAnonymousFunction anonymousFunction)
	{
		return anonymousFunction.Range.End == Position ? anonymousFunction : null;
	}

	public ITypedAuraAstNode? Visit(TypedPlusPlusIncrement plusPlusIncrement)
	{
		return plusPlusIncrement.Range.End == Position ? plusPlusIncrement : null;
	}

	public ITypedAuraAstNode? Visit(TypedMinusMinusDecrement minusMinusDecrement)
	{
		return minusMinusDecrement.Range.End == Position ? minusMinusDecrement : null;
	}

	public ITypedAuraAstNode? Visit(TypedBinary binary)
	{
		return binary.Range.End == Position ? binary : null;
	}

	public ITypedAuraAstNode? Visit(TypedBlock block)
	{
		return block.Range.End == Position ? block : null;
	}

	public ITypedAuraAstNode? Visit(TypedCall call)
	{
		return call.Range.End == Position ? call : null;
	}

	public ITypedAuraAstNode? Visit(TypedGet get)
	{
		return get.Range.End == Position ? get : null;
	}

	public ITypedAuraAstNode? Visit(TypedGetIndex getIndex)
	{
		return getIndex.Range.End == Position ? getIndex : null;
	}

	public ITypedAuraAstNode? Visit(TypedGetIndexRange getIndexRange)
	{
		return getIndexRange.Range.End == Position ? getIndexRange : null;
	}

	public ITypedAuraAstNode? Visit(TypedGrouping grouping)
	{
		return grouping.Range.End == Position ? grouping : null;
	}

	public ITypedAuraAstNode? Visit(TypedIf @if)
	{
		return @if.Range.End == Position ? @if : null;
	}

	public ITypedAuraAstNode? Visit(TypedNil nil)
	{
		return nil.Range.End == Position ? nil : null;
	}

	public ITypedAuraAstNode? Visit(TypedLogical logical)
	{
		return logical.Range.End == Position ? logical : null;
	}

	public ITypedAuraAstNode? Visit(TypedSet set)
	{
		return set.Range.End == Position ? set : null;
	}

	public ITypedAuraAstNode? Visit(TypedThis @this)
	{
		return @this.Range.End == Position ? @this : null;
	}

	public ITypedAuraAstNode? Visit(TypedUnary unary)
	{
		return unary.Range.End == Position ? unary : null;
	}

	public ITypedAuraAstNode? Visit(TypedVariable variable)
	{
		return variable.Range.End == Position ? variable : null;
	}

	public ITypedAuraAstNode? Visit(TypedIs @is)
	{
		return @is.Range.End == Position ? @is : null;
	}

	public ITypedAuraAstNode? Visit(IntLiteral intLiteral)
	{
		return intLiteral.Range.End == Position ? intLiteral : null;
	}

	public ITypedAuraAstNode? Visit(FloatLiteral floatLiteral)
	{
		return floatLiteral.Range.End == Position ? floatLiteral : null;
	}

	public ITypedAuraAstNode? Visit(StringLiteral stringLiteral)
	{
		return stringLiteral.Range.End == Position ? stringLiteral : null;
	}


	public ITypedAuraAstNode? Visit(BoolLiteral boolLiteral)
	{
		return boolLiteral.Range.End == Position ? boolLiteral : null;
	}

	public ITypedAuraAstNode? Visit(CharLiteral charLiteral)
	{
		return charLiteral.Range.End == Position ? charLiteral : null;
	}

	public ITypedAuraAstNode? Visit(TypedAnonymousStruct anonymousStruct)
	{
		return anonymousStruct.Range.End == Position ? anonymousStruct : null;
	}

	public ITypedAuraAstNode? Visit<TK, TV>(MapLiteral<TK, TV> mapLiteral)
		where TK : IAuraAstNode where TV : IAuraAstNode
	{
		return mapLiteral.Range.End == Position ? mapLiteral : null;
	}

	public ITypedAuraAstNode? Visit<T>(ListLiteral<T> listLiteral) where T : IAuraAstNode
	{
		return listLiteral.Range.End == Position ? listLiteral : null;
	}

	public ITypedAuraAstNode? Visit(TypedDefer defer)
	{
		return Expression(defer.Call);
	}

	public ITypedAuraAstNode? Visit(TypedExpressionStmt expressionStmt)
	{
		return Expression(expressionStmt.Expression);
	}

	public ITypedAuraAstNode? Visit(TypedFor @for)
	{
		return @for.Range.End == Position ? @for : null;
	}

	public ITypedAuraAstNode? Visit(TypedForEach @foreach)
	{
		return @foreach.Range.End == Position ? @foreach : null;
	}

	public ITypedAuraAstNode? Visit(TypedNamedFunction namedFunction)
	{
		if (namedFunction.Range.End == Position)
		{
			return namedFunction;
		}

		foreach (var stmt in namedFunction.Body.Statements)
		{
			var result = Statement(stmt);
			if (result is not null)
			{
				return result;
			}
		}

		return null;
	}

	public ITypedAuraAstNode? Visit(TypedLet let)
	{
		if (let.Initializer is null)
		{
			return let.Range.End == Position ? let : null;
		}

		return Expression(let.Initializer);
	}

	public ITypedAuraAstNode? Visit(TypedMod mod)
	{
		return mod.Range.End == Position ? mod : null;
	}

	public ITypedAuraAstNode? Visit(TypedReturn @return)
	{
		if (@return.Value is null)
		{
			return @return.Range.End == Position ? @return : null;
		}

		return Expression(@return.Value);
	}

	public ITypedAuraAstNode? Visit(FullyTypedClass fullyTypedClass)
	{
		if (fullyTypedClass.Range.End == Position)
		{
			return fullyTypedClass;
		}

		foreach (var f in fullyTypedClass.Methods)
		{
			var stmt = Statement(f);
			if (stmt is not null)
			{
				return stmt;
			}
		}

		return null;
	}


	public ITypedAuraAstNode? Visit(TypedInterface @interface)
	{
		return @interface.Range.End == Position ? @interface : null;
	}

	public ITypedAuraAstNode? Visit(TypedFunctionSignature fnSignature)
	{
		return fnSignature.Range.End == Position ? fnSignature : null;
	}

	public ITypedAuraAstNode? Visit(TypedWhile @while)
	{
		return @while.Range.End == Position ? @while : null;
	}

	public ITypedAuraAstNode? Visit(TypedImport import)
	{
		return import.Range.End == Position ? import : null;
	}

	public ITypedAuraAstNode? Visit(TypedMultipleImport multipleImport)
	{
		return multipleImport.Range.End == Position ? multipleImport : null;
	}

	public ITypedAuraAstNode? Visit(TypedComment comment)
	{
		return comment.Range.End == Position ? comment : null;
	}

	public ITypedAuraAstNode? Visit(TypedContinue @continue)
	{
		return @continue.Range.End == Position ? @continue : null;
	}

	public ITypedAuraAstNode? Visit(TypedBreak @break)
	{
		return @break.Range.End == Position ? @break : null;
	}

	public ITypedAuraAstNode? Visit(TypedYield yield)
	{
		return yield.Range.End == Position ? yield : null;
	}

	public ITypedAuraAstNode? Visit(PartiallyTypedFunction partiallyTypedFunction)
	{
		return null;
	}

	public ITypedAuraAstNode? Visit(PartiallyTypedClass partiallyTypedClass)
	{
		return null;
	}

	public ITypedAuraAstNode? Visit(TypedCheck check)
	{
		return check.Range.End == Position ? check : null;
	}

	public ITypedAuraAstNode? Visit(TypedStruct @struct)
	{
		return @struct.Range.End == Position ? @struct : null;
	}
}
