using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using AuraLang.Visitor;
using Range = AuraLang.Location.Range;

namespace AuraLang.AST;

public interface ITypedAuraAstNode : IAuraAstNode
{
	AuraType Typ { get; }
	IEnumerable<IHoverable> ExtractHoverables() => new List<IHoverable>();
}

public interface ITypedAuraExpression : ITypedAuraAstNode, ITypedAuraExprVisitable { }

public interface ITypedAuraStatement : ITypedAuraAstNode, ITypedAuraStmtVisitable { }

public interface ITypedAuraCallable : ITypedAuraAstNode
{
	string GetName();
}

public interface IHoverable : IAuraAstNode
{
	string HoverText { get; }
	Range HoverableRange { get; }
}

/// <summary>
/// Represents a typed assignment expression.
/// </summary>
/// <param name="Name">The variable's name</param>
/// <param name="Value">the variable's new value</param>
public record TypedAssignment(Tok Name, ITypedAuraExpression Value, AuraType Typ) : ITypedAuraExpression, IHoverable
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public Range Range => new(
		start: Name.Range.Start,
		end: Value.Range.End
	);
	public string HoverText => $"```let {Name}: {Value.Typ}```";
	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable> { this };
		hoverables.AddRange(Value.ExtractHoverables());
		return hoverables;
	}

	public Range HoverableRange => Name.Range;
}

/// <summary>
/// Represents an increment operation where the value of the variable is incremented by 1.
/// </summary>
/// <param name="Name">The variable being incremented</param>
public record TypedPlusPlusIncrement(ITypedAuraExpression Name, Tok PlusPlus, AuraType Typ) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public Range Range => new(
		start: Name.Range.Start,
		end: PlusPlus.Range.End
	);

	public IEnumerable<IHoverable> ExtractHoverables() => Name.ExtractHoverables();
}

/// <summary>
/// Represents a decrement operation where the value of the variable is decremented by 1.
/// </summary>
/// <param name="Name">The variable being decremented</param>
public record TypedMinusMinusDecrement(ITypedAuraExpression Name, Tok MinusMinus, AuraType Typ) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public Range Range => new(
		start: Name.Range.Start,
		end: MinusMinus.Range.End
	);
	public IEnumerable<IHoverable> ExtractHoverables() => Name.ExtractHoverables();
}

/// <summary>
/// Represents a typed binary expression. Both the left and right expressions must have the same type.
/// </summary>
/// <param name="Left">The expression on the left side of the binary expression</param>
/// <param name="Operator">The binary expression's operator</param>
/// <param name="Right">The expression on the right side of the binary expression</param>
public record TypedBinary(ITypedAuraExpression Left, Tok Operator, ITypedAuraExpression Right, AuraType Typ) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public Range Range => new(
		start: Left.Range.Start,
		end: Right.Range.End
	);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		hoverables.AddRange(Left.ExtractHoverables());
		hoverables.AddRange(Right.ExtractHoverables());
		return hoverables;
	}
}

/// <summary>
/// Represents a typed block expression
/// </summary>
/// <param name="Statements">A collection of statements</param>
public record TypedBlock(Tok OpeningBrace, List<ITypedAuraStatement> Statements, Tok ClosingBrace, AuraType Typ) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public Range Range => new(
		start: OpeningBrace.Range.Start,
		end: ClosingBrace.Range.End
	);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		foreach (var stmt in Statements)
		{
			hoverables.AddRange(stmt.ExtractHoverables());
		}

		return hoverables;
	}
}

/// <summary>
/// Represents a typed call expression
/// </summary>
/// <param name="Callee">The callee expressions</param>
/// <param name="Arguments">The call's arguments</param>
public record TypedCall(ITypedAuraCallable Callee, List<ITypedAuraExpression> Arguments, Tok ClosingParen, ICallable FnTyp) : ITypedAuraExpression, IHoverable
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public Range Range => new(
		start: Callee.Range.Start,
		end: ClosingParen.Range.End
	);
	public string HoverText => $"```{FnTyp}```";

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable> { this };
		foreach (var argument in Arguments)
		{
			hoverables.AddRange(argument.ExtractHoverables());
		}
		return hoverables;
	}

	public Range HoverableRange => Callee.Range;

	public AuraType Typ => FnTyp.GetReturnType();
}

/// <summary>
/// Represents a typed get expression
/// </summary>
/// <param name="Obj">The compound object being queried. It will have an attribute matching the <see cref="Name"/> parameter</param>
/// <param name="Name">The name of the attribute to get</param>
public record TypedGet(ITypedAuraExpression Obj, Tok Name, AuraType Typ) : ITypedAuraExpression, ITypedAuraCallable
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public string GetName() => Name.Value;
	public Range Range => new(
		start: Obj.Range.Start,
		end: Name.Range.End
	);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		hoverables.AddRange(Obj.ExtractHoverables());
		return hoverables;
	}
}

/// <summary>
/// Represents a fully typed expression that fetched an individual item from an indexable data type
/// </summary>
/// <param name="Obj">The collection object being queried.</param>
/// <param name="Index">The index in the collection to fetch</param>
public record TypedGetIndex(ITypedAuraExpression Obj, ITypedAuraExpression Index, Tok ClosingBracket, AuraType Typ) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public Range Range => new(
		start: Obj.Range.Start,
		end: ClosingBracket.Range.End
	);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		hoverables.AddRange(Obj.ExtractHoverables());
		hoverables.AddRange(Index.ExtractHoverables());
		return hoverables;
	}
}

/// <summary>
/// Represents a fully typed expression that fetches a range of items from an indexable data type
/// </summary>
/// <param name="Obj">The collection object being queried</param>
/// <param name="Lower">The lower bound of the range being fetched. This bound is inclusive.</param>
/// <param name="Upper">The upper bound of the range being fetched. This bound is exclusive.</param>
public record TypedGetIndexRange(ITypedAuraExpression Obj, ITypedAuraExpression Lower, ITypedAuraExpression Upper, Tok ClosingBracket, AuraType Typ) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public Range Range => new(
		start: Obj.Range.Start,
		end: ClosingBracket.Range.End
	);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		hoverables.AddRange(Obj.ExtractHoverables());
		hoverables.AddRange(Lower.ExtractHoverables());
		hoverables.AddRange(Upper.ExtractHoverables());
		return hoverables;
	}
}

/// <summary>
/// Represents a typed <c>if</c> expression
/// </summary>
/// <param name="Condition">The condition that will determine which branch is executed</param>
/// <param name="Then">The branch that will be executed if the <see cref="Condition"/> evaluates to true</param>
/// <param name="Else">The branch that will be executed if the <see cref="Condition"/> evalutes to false</param>
public record TypedIf(Tok If, ITypedAuraExpression Condition, TypedBlock Then, ITypedAuraExpression? Else, AuraType Typ) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public Range Range => new(
		start: If.Range.Start,
		end: Else is not null ? Else.Range.End : Then.Range.End
	);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		hoverables.AddRange(Condition.ExtractHoverables());
		hoverables.AddRange(Then.Statements.OfType<IHoverable>());
		if (Else is TypedBlock b) hoverables.AddRange(b.Statements.OfType<IHoverable>());
		return hoverables;
	}
}

/// <summary>
/// Represents a type-checked logical expression
/// </summary>
/// <param name="Left">The expression on the left side of the logical expression</param>
/// <param name="Operator">The logical expression's operator</param>
/// <param name="Right">The expression on the right side of the logical expression</param>
public record TypedLogical(ITypedAuraExpression Left, Tok Operator, ITypedAuraExpression Right, AuraType Typ) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public Range Range => new(
		start: Left.Range.Start,
		end: Right.Range.End
	);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		hoverables.AddRange(Left.ExtractHoverables());
		hoverables.AddRange(Right.ExtractHoverables());
		return hoverables;
	}
}

/// <summary>
/// Represents a valid type-checked <c>set</c> expression
/// </summary>
/// <param name="Obj">The compound object whose attribute is being assigned a new value</param>
/// <param name="Name">The name of the attribute that is being assigned a new value</param>
/// <param name="Value">The new value</param>
public record TypedSet(ITypedAuraExpression Obj, Tok Name, ITypedAuraExpression Value, AuraType Typ) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public Range Range => new(
		start: Obj.Range.Start,
		end: Value.Range.End
	);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		hoverables.AddRange(Obj.ExtractHoverables());
		hoverables.AddRange(Value.ExtractHoverables());
		return hoverables;
	}
}

/// <summary>
/// Represents a type-checked <c>this</c> token
/// </summary>
/// <param name="This">The <c>this</c> token</param>
public record TypedThis(Tok This, AuraType Typ) : ITypedAuraExpression, IHoverable
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public Range Range => This.Range;
	public string HoverText => $"```{Typ}```";
	public IEnumerable<IHoverable> ExtractHoverables() => new List<IHoverable> { this };
	public Range HoverableRange => This.Range;
}

/// <summary>
/// Represents a type-checked unary expression
/// </summary>
/// <param name="Operator">The unary expression's operator. Will be one of <c>!</c> or <c>-</c></param>
/// <param name="Right">The expression onto which the operator will be applied</param>
public record TypedUnary(Tok Operator, ITypedAuraExpression Right, AuraType Typ) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public Range Range => new(
		start: Operator.Range.Start,
		end: Right.Range.End
	);

	public IEnumerable<IHoverable> ExtractHoverables() => Right.ExtractHoverables();
}

/// <summary>
/// Represents a type-checked variable
/// </summary>
/// <param name="Name">The variable's name</param>
public record TypedVariable(Tok Name, AuraType Typ) : ITypedAuraExpression, ITypedAuraCallable, IHoverable
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public string GetName() => Name.Value;
	public Range Range => Name.Range;
	public string HoverText => $"```{Typ}```";
	public IEnumerable<IHoverable> ExtractHoverables() => new List<IHoverable> { this };
	public Range HoverableRange => Name.Range;
}

/// <summary>
/// Represents a type-checked <c>is</c> expression
/// </summary>
/// <param name="Expr">The expression whose type will be checked against the expected type</param>
/// <param name="Expected">The expected type</param>
public record TypedIs(ITypedAuraExpression Expr, AuraInterface Expected) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	/// <summary>
	/// An <c>is</c> expression always returns a boolean indicating if the <c>expr</c> matches the <c>expected</c> type
	/// </summary>
	public AuraType Typ => new AuraBool();
	public Range Range => new(
		start: Expr.Range.Start,
		end: new Location.Position() // TODO add range to AuraInterface
	);

	public IEnumerable<IHoverable> ExtractHoverables() => Expr.ExtractHoverables();
}

/// <summary>
/// Represents a type-checked grouping expression
/// </summary>
/// <param name="Expr">The expression contained in the grouping expression</param>
public record TypedGrouping(Tok OpeningParen, ITypedAuraExpression Expr, Tok ClosingParen, AuraType Typ) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public Range Range => new(
		start: OpeningParen.Range.Start,
		end: ClosingParen.Range.End
	);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		hoverables.AddRange(Expr.ExtractHoverables());
		return hoverables;
	}
}

/// <summary>
/// Represents a type-checked <c>defer</c> statement
/// </summary>
/// <param name="Call">The call expression to be deferred until the end of the enclosing function's scope</param>
public record TypedDefer(Tok Defer, TypedCall Call) : ITypedAuraStatement, IHoverable
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNone();
	public Range Range => new(
		start: Defer.Range.Start,
		end: Call.Range.End
	);
	public string HoverText => "Used to defer execution of a single function call until just before exiting the enclosing scope";

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable> { this };
		hoverables.AddRange(Call.ExtractHoverables());
		return hoverables;
	}

	public Range HoverableRange => Defer.Range;
}

/// <summary>
/// Represents a type-checked expression statement
/// </summary>
/// <param name="Expression">The enclosed expression</param>
public record TypedExpressionStmt(ITypedAuraExpression Expression) : ITypedAuraStatement
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNone();
	public Range Range => Expression.Range;
	public IEnumerable<IHoverable> ExtractHoverables() => Expression.ExtractHoverables();
}

/// <summary>
/// Represents a valid type-checked <c>for</c> loop
/// </summary>
/// <param name="Initializer">The loop's initializer. The variable initialized here will be available inside the loop's body.</param>
/// <param name="Condition">The loop's condition. The loop will exit when the condition evaluates to false.</param>
/// <param name="Body">Collection of statements that will be executed once per iteration.</param>
public record TypedFor(Tok For, ITypedAuraStatement? Initializer, ITypedAuraExpression? Condition, ITypedAuraExpression? Increment,
	List<ITypedAuraStatement> Body, Tok ClosingBrace) : ITypedAuraStatement
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNone();
	public Range Range => new(
		start: For.Range.Start,
		end: ClosingBrace.Range.End
	);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		if (Initializer is not null) hoverables.AddRange(Initializer.ExtractHoverables());
		if (Condition is not null) hoverables.AddRange(Condition.ExtractHoverables());
		if (Increment is not null) hoverables.AddRange(Increment.ExtractHoverables());
		hoverables.AddRange(Body.OfType<IHoverable>());
		return hoverables;
	}
}

/// <summary>
/// Represents a valid type-checked  <c>foreach</c> loop
/// </summary>
/// <param name="EachName">Represents the current item in the collection being iterated over</param>
/// <param name="Iterable">The collection being iterated over</param>
/// <param name="Body">Collection of statements that will be executed once per iteration</param>
public record TypedForEach
	(Tok ForEach, Tok EachName, ITypedAuraExpression Iterable, List<ITypedAuraStatement> Body, Tok ClosingBrace) : ITypedAuraStatement
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNone();
	public Range Range => new(
		start: ForEach.Range.Start,
		end: ClosingBrace.Range.End
	);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		hoverables.AddRange(Iterable.ExtractHoverables());
		hoverables.AddRange(Body.OfType<IHoverable>());
		return hoverables;
	}
}

/// <summary>
/// Represents a valid type-checked named function declaration
/// </summary>
/// <param name="Name">The function's name</param>
/// <param name="Params">The function's parameters</param>
/// <param name="Body">The function's body</param>
/// <param name="ReturnType">The function's return type</param>
public record TypedNamedFunction(Tok Fn, Tok Name, List<Param> Params, TypedBlock Body, AuraType ReturnType,
	Visibility Public) : ITypedAuraStatement, ITypedFunction, IHoverable
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNamedFunction(Name.Value, Public, new AuraFunction(Params, ReturnType));
	public List<Param> GetParams() => Params;
	public List<ParamType> GetParamTypes() => Params.Select(param => param.ParamType).ToList();
	/// <summary>
	/// Gets the type of the declared function. This differs from the <c>Typ</c> field because this function
	/// type is not returned by the declaration itself. Instead, this method returns the type of the function
	/// that was declared.
	/// </summary>
	/// <returns>The type of the declared function</returns>
	public AuraType GetFunctionType() => new AuraNamedFunction(Name.Value, Public, new AuraFunction(Params, ReturnType));
	public Range Range => new(
		start: Fn.Range.Start,
		end: Body.Range.End
	);
	public string HoverText => $"```{(Public == Visibility.Public ? "pub " : string.Empty)}fn {Name.Value}({string.Join(", ", Params)}){(ReturnType is not AuraNil ? $" -> {ReturnType}" : string.Empty)}```";

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable> { this };
		hoverables.AddRange(Body.ExtractHoverables());
		return hoverables;
	}

	public Range HoverableRange => Name.Range;
}

/// <summary>
/// Represents a valid type-checked anonymous function
/// </summary>
/// <param name="Params">The anonymous function's parameters</param>
/// <param name="Body">The anonymous function's body</param>
/// <param name="ReturnType">The anonymous function's return type</param>
public record TypedAnonymousFunction(Tok Fn, List<Param> Params, TypedBlock Body, AuraType ReturnType) : ITypedAuraExpression, ITypedFunction, IHoverable
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraFunction(Params, ReturnType);
	public List<Param> GetParams() => Params;
	public List<ParamType> GetParamTypes() => Params.Select(param => param.ParamType).ToList();
	public Range Range => new(
		start: Fn.Range.Start,
		end: Body.Range.End
	);
	public string HoverText => $"```fn({string.Join(", ", Params)}){(ReturnType is not AuraNil ? $" -> {ReturnType}" : string.Empty)}```";

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable> { this };
		hoverables.AddRange(Body.ExtractHoverables());
		return hoverables;
	}

	public Range HoverableRange => Fn.Range;
}

/// <summary>
/// Represents a valid type-checked variable declaration
/// </summary>
/// <param name="Names">The name(s) of the newly-declared variable(s)</param>
/// <param name="TypeAnnotation">Indicates whether the variables were declared with a type annotation</param>
/// <param name="Mutable">Indicates whether the variable was declared as mutable</param>
/// <param name="Initializer">The initializer expression. This can be omitted.</param>
public record TypedLet(Tok? Let, List<Tok> Names, bool TypeAnnotation, bool Mutable, ITypedAuraExpression? Initializer) : ITypedAuraStatement, IHoverable
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNone();
	public Range Range => new(
		start: Let is not null ? Let.Value.Range.Start : Names.First().Range.Start,
		end: Initializer is not null ? Initializer.Range.End : Names.Last().Range.End
	);
	public string HoverText => $"`let {string.Join(", ", Names.Select(n => n.Value))}`";

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable> { this };
		if (Initializer is not null) hoverables.AddRange(Initializer.ExtractHoverables());
		return hoverables;
	}

	public Range HoverableRange => new(
		start: Names.First().Range.Start,
		end: Names.Last().Range.End);
}

/// <summary>
/// Represents a type-checked module declaration
/// </summary>
/// <param name="Value">The module's name</param>
public record TypedMod(Tok Mod, Tok Value) : ITypedAuraStatement
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNone();
	public Range Range => new(
		start: Mod.Range.Start,
		end: Value.Range.End
	);
}

/// <summary>
/// Represents a valid type-checked <c>return</c> statement
/// </summary>
/// <param name="Value">The value to return</param>
public record TypedReturn(Tok Return, ITypedAuraExpression? Value) : ITypedAuraStatement
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNone();
	public Range Range => new(
		start: Return.Range.Start,
		end: Value is not null ? Value.Range.End : Return.Range.End
	);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		if (Value is not null) hoverables.AddRange(Value.ExtractHoverables());
		return hoverables;
	}
}

/// <summary>
/// Represents a valid type-checked interface declaration
/// </summary>
/// <param name="Name">The interface's declaration</param>
/// <param name="Methods">The interface's methods</param>
/// <param name="Public">Indicates whether the class is declared as public</param>
public record TypedInterface
	(Tok Interface, Tok Name, List<AuraNamedFunction> Methods, Visibility Public, Tok ClosingBrace) : ITypedAuraStatement, IHoverable
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNone();
	public Range Range => new(
		start: Public == Visibility.Public
			? Interface.Range.Start with { Character = Interface.Range.Start.Character - 4 }
			: Interface.Range.Start,
		end: ClosingBrace.Range.End
	);
	public string HoverText => $"```{(Public == Visibility.Public ? "pub " : string.Empty)}interface {Name.Value}\n\nMethods:\n\n{string.Join("\n\n", Methods)}```";

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		return new List<IHoverable> { this }; // TODO Add methods
	}

	public Range HoverableRange => Name.Range;
}

public record TypedStruct(Tok Struct, Tok Name, List<Param> Params, Tok ClosingParen) : ITypedAuraStatement, ITypedFunction, ITypedAuraCallable, IHoverable
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNone();
	public string GetName() => Name.Value;
	public List<Param> GetParams() => Params;
	public List<ParamType> GetParamTypes() => Params.Select(p => p.ParamType).ToList();
	public Range Range => new(
		start: Struct.Range.Start,
		end: ClosingParen.Range.End
	);
	public string HoverText => $"```struct {Name.Value} ({string.Join(", ", Params.Select(p => p.Name.Value))})```";

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		return new List<IHoverable> { this };
	}

	public Range HoverableRange => Name.Range;
}

public record TypedAnonymousStruct(Tok Struct, List<Param> Params, List<ITypedAuraExpression> Values, Tok ClosingParen) : ITypedAuraExpression, ITypedFunction, IHoverable
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraAnonymousStruct(
		parameters: Params,
		pub: Visibility.Private
	);
	public List<Param> GetParams() => Params;
	public List<ParamType> GetParamTypes() => Params.Select(p => p.ParamType).ToList();
	public Range Range => new(
		start: Struct.Range.Start,
		end: ClosingParen.Range.End
	);
	public string HoverText => $"```struct ({string.Join(", ", Params.Select(p => p.Name.Value))})```";

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		return new List<IHoverable> { this };
	}

	public Range HoverableRange => Struct.Range;
}

/// <summary>
/// Represents a valid type-checked class declaration
/// </summary>
/// <param name="Name">The class's name</param>
/// <param name="Params">The class's parameters</param>
/// <param name="Methods">The class's methods</param>
/// <param name="Public">Indicates whether the class is declared as public</param>
public record FullyTypedClass(Tok Class, Tok Name, List<Param> Params, List<TypedNamedFunction> Methods, Visibility Public, List<AuraInterface> Implementing,
	Tok ClosingBrace) : ITypedAuraStatement, ITypedFunction, ITypedAuraCallable, IHoverable
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNone();
	public List<Param> GetParams() => Params;
	public List<ParamType> GetParamTypes() => Params.Select(param => param.ParamType).ToList();
	public string GetName() => Name.Value;
	public Range Range => new(
		start: Class.Range.Start,
		end: ClosingBrace.Range.End
	);
	public string HoverText => $"```{(Public == Visibility.Public ? "pub " : string.Empty)}class {Name.Value}({string.Join(", ", Params.Select(p => p.Name.Value))})\n\n{(Implementing.Count > 0 ? $"Implementing:\n\n{string.Join("\n\n", Implementing)}\n\n" : string.Empty)}Methods:\n\n{string.Join("\n\n", Methods)}```";

	public IEnumerable<IHoverable> ExtractHoverables() => new List<IHoverable> { this };

	public Range HoverableRange => Name.Range;
}

/// <summary>
/// Represents a valid type-checked <c>while</c> loop
/// </summary>
/// <param name="Condition">The loop's condition. The loop will exit when the condition evaluates to false</param>
/// <param name="Body">Collection of statements executed once per iteration</param>
public record TypedWhile(Tok While, ITypedAuraExpression Condition, List<ITypedAuraStatement> Body, Tok ClosingBrace) : ITypedAuraStatement
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNone();
	public Range Range => new(
		start: While.Range.Start,
		end: ClosingBrace.Range.End
	);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		hoverables.AddRange(Condition.ExtractHoverables());
		hoverables.AddRange(Body.OfType<IHoverable>());
		return hoverables;
	}
}

/// <summary>
/// Represents a valid type-checked <c>import</c> statement
/// </summary>
/// <param name="Package">The name of the package being imported</param>
/// <param name="Alias">Will have a value if the package is being imported under an alias</param>
public record TypedImport(Tok Import, Tok Package, Tok? Alias) : ITypedAuraStatement
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNone();
	public Range Range => new(
		start: Import.Range.Start,
		end: Alias is not null ? Alias.Value.Range.End : Package.Range.End
	);
}

public record TypedMultipleImport(Tok Import, List<TypedImport> Packages, Tok ClosingParen) : ITypedAuraStatement
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNone();
	public Range Range => new(
		start: Import.Range.Start,
		end: ClosingParen.Range.End
	);
}

/// <summary>
/// Represents a type-checked comment
/// </summary>
/// <param name="Text">The text of the comment</param>
public record TypedComment(Tok Text) : ITypedAuraStatement
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNone();
	public Range Range => Text.Range;
}

/// <summary>
/// Represents a type-checked <c>continue</c> statement
/// </summary>
public record TypedContinue(Tok Continue) : ITypedAuraStatement, IHoverable
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNone();
	public Range Range => Continue.Range;
	public string HoverText => "Immediately advances execution to the enclosing loop's next iteration";
	public IEnumerable<IHoverable> ExtractHoverables() => new List<IHoverable> { this };
	public Range HoverableRange => Continue.Range;
}

/// <summary>
/// Represents a type-checked <c>break</c> statement
/// </summary>
public record TypedBreak(Tok Break) : ITypedAuraStatement, IHoverable
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNone();
	public Range Range => Break.Range;
	public string HoverText => "Immediately stops the enclosing loop's execution";
	public IEnumerable<IHoverable> ExtractHoverables() => new List<IHoverable> { this };
	public Range HoverableRange => Break.Range;
}

/// <summary>
/// Represents a type-checked <c>nil</c> keyword
/// </summary>
public record TypedNil(Tok Nil) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNil();
	public Range Range => Nil.Range;
}

/// <summary>
/// Represents a type-checked <c>yield</c> statement
/// </summary>
/// <param name="Value">The value to be yielded from the enclosing expression</param>
public record TypedYield(Tok Yield, ITypedAuraExpression Value) : ITypedAuraStatement, IHoverable
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNone();
	public Range Range => new(
		start: Yield.Range.Start,
		end: Value.Range.End
	);
	public string HoverText => "Used inside an `if` or block expression to return a value without returning the value from the enclosing function context";
	public IEnumerable<IHoverable> ExtractHoverables() => new List<IHoverable> { this };

	public Range HoverableRange => Yield.Range;
}

public record TypedCheck(Tok Check, TypedCall Call) : ITypedAuraStatement, IHoverable
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) => visitor.Visit(this);
	public AuraType Typ => new AuraNone();
	public Range Range => new(
		start: Check.Range.Start,
		end: Call.Range.End
	);
	public string HoverText => "Used to simplify error handling on a function call whose return type is a `Result`. The enclosing function must also return a `Result`";
	public IEnumerable<IHoverable> ExtractHoverables() => new List<IHoverable> { this };
	public Range HoverableRange => Check.Range;
}
