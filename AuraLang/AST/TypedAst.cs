using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using AuraLang.Visitor;
using Range = AuraLang.Location.Range;

namespace AuraLang.AST;

/// <summary>
///     Represents a typed AST node
/// </summary>
public interface ITypedAuraAstNode : IAuraAstNode
{
	/// <summary>
	///     The AST node's type
	/// </summary>
	AuraType Typ { get; }

	/// <summary>
	///     Recursively collects all the <see cref="IHoverable" /> nodes contained in the current node. This method must be
	///     implemented by all typed AST nodes, even if the node is not itself <c>IHoverable</c>, because it may still contain
	///     <c>IHoverable</c> nodes that need to be captured by the LSP server's Hover Provider.
	/// </summary>
	/// <returns>A list of all <see cref="IHoverable" /> nodes contained in the current node</returns>
	IEnumerable<IHoverable> ExtractHoverables() { return new List<IHoverable>(); }
}

/// <summary>
///     Represents a typed Aura expression
/// </summary>
public interface ITypedAuraExpression : ITypedAuraAstNode, ITypedAuraExprVisitable
{
}

/// <summary>
///     Represents a typed Aura statement
/// </summary>
public interface ITypedAuraStatement : ITypedAuraAstNode, ITypedAuraStmtVisitable
{
}

/// <summary>
///     Represents an Aura node that can be called (i.e. by suffixing the node with a pair of parentheses and zero or more
///     arguments
/// </summary>
public interface ITypedAuraCallable : ITypedAuraAstNode
{
	string GetName();
}

/// <summary>
///     Represents an AST node that returns useful information when hovered over in an IDE, such as VS Code
/// </summary>
public interface IHoverable : IAuraAstNode
{
	/// <summary>
	///     The text to be shown to the user when this AST node is hovered over
	/// </summary>
	string HoverText { get; }

	/// <summary>
	///     The range in which the user must hover over to see the <see cref="HoverText" />. This range may differ from the
	///     node's <see cref="Range" /> if the user must hover over a specific part of the node to see the hover pop-up.
	/// </summary>
	Range HoverableRange { get; }
}

/// <summary>
///     Represents a typed assignment expression.
/// </summary>
/// <param name="Name">The variable's name</param>
/// <param name="Value">the variable's new value</param>
public record TypedAssignment(Tok Name, ITypedAuraExpression Value, AuraType Typ) : ITypedAuraExpression, IHoverable
{
	public string HoverText => $"let {Name.Value}: {Value.Typ}";

	public Range HoverableRange => Name.Range;

	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public Range Range => new(Name.Range.Start, Value.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable> { this };
		hoverables.AddRange(Value.ExtractHoverables());
		return hoverables;
	}
}

/// <summary>
///     Represents an increment operation where the value of the variable is incremented by 1.
/// </summary>
/// <param name="Name">The value being incremented</param>
/// <param name="PlusPlus">
///     A token representing the <c>++</c> suffix operator, which is used to determine the node's
///     ending position in the Aura source file
/// </param>
public record TypedPlusPlusIncrement(ITypedAuraExpression Name, Tok PlusPlus, AuraType Typ) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public Range Range => new(Name.Range.Start, PlusPlus.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables() { return Name.ExtractHoverables(); }
}

/// <summary>
///     Represents a decrement operation where the value of the variable is decremented by 1.
/// </summary>
/// <param name="Name">The value being decremented</param>
/// <param name="MinusMinus">
///     A token representing the <c>--</c> suffix operator, which is used to determine the node's
///     ending position in the Aura source file
/// </param>
public record TypedMinusMinusDecrement(ITypedAuraExpression Name, Tok MinusMinus, AuraType Typ) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public Range Range => new(Name.Range.Start, MinusMinus.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables() { return Name.ExtractHoverables(); }
}

/// <summary>
///     Represents a typed binary expression. Both the left and right expressions must have the same type.
/// </summary>
/// <param name="Left">The expression on the left side of the binary expression</param>
/// <param name="Operator">The binary expression's operator</param>
/// <param name="Right">The expression on the right side of the binary expression</param>
public record TypedBinary(ITypedAuraExpression Left, Tok Operator, ITypedAuraExpression Right, AuraType Typ)
	: ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public Range Range => new(Left.Range.Start, Right.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		hoverables.AddRange(Left.ExtractHoverables());
		hoverables.AddRange(Right.ExtractHoverables());
		return hoverables;
	}
}

/// <summary>
///     Represents a typed block expression
/// </summary>
/// <param name="OpeningBrace">
///     A token representing the block's opening brace, which is used to determine the node's
///     starting position in the Aura source file
/// </param>
/// <param name="Statements">A collection of statements</param>
/// <param name="ClosingBrace">
///     A token representing the block's closing brace, which is used to determine the node's
///     ending position in the Aura source file
/// </param>
public record TypedBlock(Tok OpeningBrace, List<ITypedAuraStatement> Statements, Tok ClosingBrace, AuraType Typ)
	: ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public Range Range => new(OpeningBrace.Range.Start, ClosingBrace.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		foreach (var stmt in Statements) hoverables.AddRange(stmt.ExtractHoverables());

		return hoverables;
	}
}

/// <summary>
///     Represents a typed call expression
/// </summary>
/// <param name="Callee">The callee expression</param>
/// <param name="Arguments">The call's arguments</param>
/// <param name="ClosingParen">
///     A token representing the call's closing parenthesis, which is used to determine the node's
///     ending position in the Aura source file
/// </param>
/// <param name="FnTyp">
///     The original declaration of the function being called. This parameter must implement the <see cref="ICallable" />
///     interface instead of being of type <see cref="AuraNamedFunction" /> because other declarations can also be called
///     by this type of node, such
///     as a class, which is instantiated by calling the class's name like a function call
/// </param>
public record TypedCall
	(ITypedAuraCallable Callee, List<ITypedAuraExpression> Arguments, Tok ClosingParen, ICallable FnTyp)
	: ITypedAuraExpression, IHoverable
{
	public string HoverText
	{
		get
		{
			if (FnTyp.Documentation != string.Empty) return FnTyp.Documentation;

			return FnTyp.ToAuraString();
		}
	}

	public Range HoverableRange => Callee.Range;

	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public Range Range => new(Callee.Range.Start, ClosingParen.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable> { this };
		foreach (var argument in Arguments) hoverables.AddRange(argument.ExtractHoverables());

		return hoverables;
	}

	public AuraType Typ => FnTyp.GetReturnType();
}

/// <summary>
///     Represents a typed get expression
/// </summary>
/// <param name="Obj">
///     The compound object being queried. It will have an attribute matching the <see cref="Name" />
///     parameter
/// </param>
/// <param name="Name">The name of the attribute to get</param>
public record TypedGet(ITypedAuraExpression Obj, Tok Name, AuraType Typ) : ITypedAuraExpression, ITypedAuraCallable
{
	public string GetName() { return Name.Value; }

	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public Range Range => new(Obj.Range.Start, Name.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		hoverables.AddRange(Obj.ExtractHoverables());
		return hoverables;
	}
}

/// <summary>
///     Represents a fully typed expression that fetched an individual item from an indexable data type
/// </summary>
/// <param name="Obj">The collection object being queried.</param>
/// <param name="Index">The index in the collection to fetch</param>
/// <param name="ClosingBracket">
///     A token representing a right bracket, which is used to determine the node's ending
///     position in the Aura source file
/// </param>
public record TypedGetIndex(ITypedAuraExpression Obj, ITypedAuraExpression Index, Tok ClosingBracket, AuraType Typ)
	: ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public Range Range => new(Obj.Range.Start, ClosingBracket.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		hoverables.AddRange(Obj.ExtractHoverables());
		hoverables.AddRange(Index.ExtractHoverables());
		return hoverables;
	}
}

/// <summary>
///     Represents a fully typed expression that fetches a range of items from an indexable data type
/// </summary>
/// <param name="Obj">The collection object being queried</param>
/// <param name="Lower">The lower bound of the range being fetched. This bound is inclusive.</param>
/// <param name="Upper">The upper bound of the range being fetched. This bound is exclusive.</param>
/// <param name="ClosingBracket">
///     A token representing a right bracket, which is used to determine the node's ending
///     position in the Aura source file
/// </param>
public record TypedGetIndexRange
	(ITypedAuraExpression Obj, ITypedAuraExpression Lower, ITypedAuraExpression Upper, Tok ClosingBracket, AuraType Typ)
	: ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public Range Range => new(Obj.Range.Start, ClosingBracket.Range.End);

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
///     Represents a typed <c>if</c> expression
/// </summary>
/// <param name="If">
///     A token representing the <c>if</c> keyword, which is used to determine the node's starting position
///     in the Aura source file
/// </param>
/// <param name="Condition">The condition that will determine which branch is executed</param>
/// <param name="Then">The branch that will be executed if the <see cref="Condition" /> evaluates to true</param>
/// <param name="Else">The branch that will be executed if the <see cref="Condition" /> evalutes to false</param>
public record TypedIf(Tok If, ITypedAuraExpression Condition, TypedBlock Then, ITypedAuraExpression? Else, AuraType Typ)
	: ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public Range Range => new(If.Range.Start, Else is not null ? Else.Range.End : Then.Range.End);

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
///     Represents a type-checked logical expression
/// </summary>
/// <param name="Left">The expression on the left side of the logical expression</param>
/// <param name="Operator">The logical expression's operator</param>
/// <param name="Right">The expression on the right side of the logical expression</param>
public record TypedLogical(ITypedAuraExpression Left, Tok Operator, ITypedAuraExpression Right, AuraType Typ)
	: ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public Range Range => new(Left.Range.Start, Right.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		hoverables.AddRange(Left.ExtractHoverables());
		hoverables.AddRange(Right.ExtractHoverables());
		return hoverables;
	}
}

/// <summary>
///     Represents a valid type-checked <c>set</c> expression
/// </summary>
/// <param name="Obj">The compound object whose attribute is being assigned a new value</param>
/// <param name="Name">The name of the attribute that is being assigned a new value</param>
/// <param name="Value">The new value</param>
public record TypedSet(ITypedAuraExpression Obj, Tok Name, ITypedAuraExpression Value, AuraType Typ)
	: ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public Range Range => new(Obj.Range.Start, Value.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		hoverables.AddRange(Obj.ExtractHoverables());
		hoverables.AddRange(Value.ExtractHoverables());
		return hoverables;
	}
}

/// <summary>
///     Represents a type-checked <c>this</c> token
/// </summary>
/// <param name="This">A token representing the <c>this</c> keyword</param>
public record TypedThis(Tok This, AuraType Typ) : ITypedAuraExpression, IHoverable
{
	public string HoverText => $"{Typ}";

	public Range HoverableRange => This.Range;

	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public Range Range => This.Range;

	public IEnumerable<IHoverable> ExtractHoverables() { return new List<IHoverable> { this }; }
}

/// <summary>
///     Represents a type-checked unary expression
/// </summary>
/// <param name="Operator">The unary expression's operator. Will be one of <c>!</c> or <c>-</c></param>
/// <param name="Right">The expression onto which the operator will be applied</param>
public record TypedUnary(Tok Operator, ITypedAuraExpression Right, AuraType Typ) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public Range Range => new(Operator.Range.Start, Right.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables() { return Right.ExtractHoverables(); }
}

/// <summary>
///     Represents a type-checked variable
/// </summary>
/// <param name="Name">The variable's name</param>
public record TypedVariable(Tok Name, AuraType Typ) : ITypedAuraExpression, ITypedAuraCallable, IHoverable
{
	public string HoverText => $"{Typ}";

	public Range HoverableRange => Name.Range;

	public string GetName() { return Name.Value; }

	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public Range Range => Name.Range;

	public IEnumerable<IHoverable> ExtractHoverables() { return new List<IHoverable> { this }; }
}

/// <summary>
///     Represents a type-checked <c>is</c> expression
/// </summary>
/// <param name="Expr">The expression whose type will be checked against the expected type</param>
/// <param name="Expected">The expected type</param>
public record TypedIs(ITypedAuraExpression Expr, TypedInterfacePlaceholder Expected) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	/// <summary>
	///     An <c>is</c> expression always returns a boolean indicating if the <c>expr</c> matches the <c>expected</c> type
	/// </summary>
	public AuraType Typ => new AuraBool();

	public Range Range =>
		new(
			Expr.Range.Start,
			Expected.Range.End
		);

	public IEnumerable<IHoverable> ExtractHoverables() { return Expr.ExtractHoverables(); }
}

/// <summary>
///     Represents a type-checked grouping expression
/// </summary>
/// <param name="OpeningParen">
///     A token representing the grouping's opening parenthesis, which determines the node's
///     starting position in the Aura source file
/// </param>
/// <param name="Expr">The expression contained in the grouping expression</param>
/// <param name="ClosingParen">
///     A token representing the grouping's closing parenthesis, which determines the node's
///     ending position in the Aura source file
/// </param>
public record TypedGrouping(Tok OpeningParen, ITypedAuraExpression Expr, Tok ClosingParen, AuraType Typ)
	: ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public Range Range => new(OpeningParen.Range.Start, ClosingParen.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		hoverables.AddRange(Expr.ExtractHoverables());
		return hoverables;
	}
}

/// <summary>
///     Represents a type-checked <c>defer</c> statement
/// </summary>
/// <param name="Defer">
///     A token representing the <c>defer</c> keyword, which is used to determine the node's starting
///     position in the Aura source file
/// </param>
/// <param name="Call">The call expression to be deferred until the end of the enclosing function's scope</param>
public record TypedDefer(Tok Defer, TypedCall Call) : ITypedAuraStatement, IHoverable
{
	public string HoverText =>
		"Used to defer execution of a single function call until just before exiting the enclosing scope";

	public Range HoverableRange => Defer.Range;

	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNone();

	public Range Range => new(Defer.Range.Start, Call.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable> { this };
		hoverables.AddRange(Call.ExtractHoverables());
		return hoverables;
	}
}

/// <summary>
///     Represents a type-checked expression statement
/// </summary>
/// <param name="Expression">The enclosed expression</param>
public record TypedExpressionStmt(ITypedAuraExpression Expression) : ITypedAuraStatement
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNone();
	public Range Range => Expression.Range;

	public IEnumerable<IHoverable> ExtractHoverables() { return Expression.ExtractHoverables(); }
}

/// <summary>
///     Represents a valid type-checked <c>for</c> loop
/// </summary>
/// <param name="For">
///     A token representing the <c>for</c> keyword, which is used to determine the node's starting
///     position in the Aura source file
/// </param>
/// <param name="Initializer">
///     The loop's initializer. The variable initialized here will be available inside the loop's
///     body.
/// </param>
/// <param name="Condition">The loop's condition. The loop will exit when the condition evaluates to false.</param>
/// <param name="Increment">The increment expression that is executed at the end of each iteration</param>
/// <param name="Body">Collection of statements that will be executed once per iteration.</param>
/// <param name="ClosingBrace">
///     A token representing the closing brace, which determines the node's ending position in
///     the Aura source file
/// </param>
public record TypedFor
(
	Tok For,
	ITypedAuraStatement? Initializer,
	ITypedAuraExpression? Condition,
	ITypedAuraExpression? Increment,
	List<ITypedAuraStatement> Body,
	Tok ClosingBrace
) : ITypedAuraStatement
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNone();

	public Range Range => new(For.Range.Start, ClosingBrace.Range.End);

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
///     Represents a valid type-checked <c>foreach</c> loop
/// </summary>
/// <param name="ForEach">
///     A token representing the <c>foreach</c> keyword, which determines the node's starting position
///     in the Aura source file
/// </param>
/// <param name="EachName">Represents the current item in the collection being iterated over</param>
/// <param name="Iterable">The collection being iterated over</param>
/// <param name="Body">Collection of statements that will be executed once per iteration</param>
/// <param name="ClosingBrace">
///     A token representing the closing brace, which determines the node's closing position in
///     the Aura source file
/// </param>
public record TypedForEach
	(Tok ForEach, Tok EachName, ITypedAuraExpression Iterable, List<ITypedAuraStatement> Body, Tok ClosingBrace)
	: ITypedAuraStatement
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNone();

	public Range Range => new(ForEach.Range.Start, ClosingBrace.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		hoverables.AddRange(Iterable.ExtractHoverables());
		hoverables.AddRange(Body.OfType<IHoverable>());
		return hoverables;
	}
}

/// <summary>
///     Represents a valid type-checked named function declaration
/// </summary>
/// <param name="Fn">
///     A token representing the <c>fn</c> keyword, which determines the node's starting position in the
///     Aura source file
/// </param>
/// <param name="Name">The function's name</param>
/// <param name="Params">The function's parameters</param>
/// <param name="Body">The function's body</param>
/// <param name="ReturnType">The function's return type</param>
/// <param name="Documentation">
///     The optional documentation comment appearing on the line above the named function in the
///     Aura source file
/// </param>
public record TypedNamedFunction
(
	Tok Fn,
	Tok Name,
	List<Param> Params,
	TypedBlock Body,
	AuraType ReturnType,
	Visibility Public,
	string? Documentation
)
	: ITypedAuraStatement, ITypedFunction, IHoverable
{
	public string HoverText
	{
		get
		{
			var pub = Public == Visibility.Public ? "pub " : string.Empty;
			var @params = string.Join(", ", Params.Select(p => $"{p.Name.Value}: {p.ParamType.Typ.ToAuraString()}"));
			var returnType = ReturnType is not AuraNil ? $" -> {ReturnType}" : string.Empty;
			var doc = Documentation is not null ? $"\n\n{Documentation}" : string.Empty;
			return $"{pub}fn {Name.Value}({@params}){returnType}{doc}";
		}
	}

	public Range HoverableRange => Name.Range;

	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ =>
		new AuraNamedFunction(
			Name.Value,
			Public,
			new AuraFunction(Params, ReturnType),
			Documentation
		);

	public Range Range => new(Fn.Range.Start, Body.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable> { this };
		foreach (var param in Params) hoverables.Add(param);

		hoverables.AddRange(Body.ExtractHoverables());
		return hoverables;
	}

	public List<Param> GetParams() { return Params; }

	public List<ParamType> GetParamTypes() { return Params.Select(param => param.ParamType).ToList(); }

	/// <summary>
	///     Gets the type of the declared function. This differs from the <c>Typ</c> field because this function
	///     type is not returned by the declaration itself. Instead, this method returns the type of the function
	///     that was declared.
	/// </summary>
	/// <returns>The type of the declared function</returns>
	public AuraType GetFunctionType()
	{
		return new AuraNamedFunction(
			Name.Value,
			Public,
			new AuraFunction(Params, ReturnType)
		);
	}
}

/// <summary>
///     Represents a valid type-checked anonymous function
/// </summary>
/// <param name="Fn">
///     A token representing the <c>fn</c> keyword, which determines the node's starting position in the
///     Aura source file
/// </param>
/// <param name="Params">The anonymous function's parameters</param>
/// <param name="Body">The anonymous function's body</param>
/// <param name="ReturnType">The anonymous function's return type</param>
public record TypedAnonymousFunction(Tok Fn, List<Param> Params, TypedBlock Body, AuraType ReturnType)
	: ITypedAuraExpression, ITypedFunction, IHoverable
{
	public string HoverText =>
		$"fn({string.Join(", ", Params)}){(ReturnType is not AuraNil ? $" -> {ReturnType}" : string.Empty)}";

	public Range HoverableRange => Fn.Range;

	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraFunction(Params, ReturnType);

	public Range Range => new(Fn.Range.Start, Body.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable> { this };
		hoverables.AddRange(Body.ExtractHoverables());
		return hoverables;
	}

	public List<Param> GetParams() { return Params; }

	public List<ParamType> GetParamTypes() { return Params.Select(param => param.ParamType).ToList(); }
}

/// <summary>
///     Represents a valid type-checked variable declaration
/// </summary>
/// <param name="Let">
///     A token representing the <c>let</c> keyword, which determines the node's starting position in the
///     Aura source file
/// </param>
/// <param name="Names">The name(s) of the newly-declared variable(s), along with a boolean value indicating if they are mutable or not</param>
/// <param name="TypeAnnotation">Indicates whether the variables were declared with a type annotation</param>
/// <param name="Initializer">The initializer expression. This can be omitted.</param>
public record TypedLet(Tok? Let, List<(bool, Tok)> Names, bool TypeAnnotation, ITypedAuraExpression? Initializer)
	: ITypedAuraStatement, IHoverable
{
	public string HoverText
	{
		get
		{
			var names = string.Join(", ", Names.Select(n => n.Item2.Value));
			var typ = Initializer is not null ? $": {Initializer.Typ.ToAuraString()}" : string.Empty;
			return $"let {names}{typ}";
		}
	}

	public Range HoverableRange => new(Names.First().Item2.Range.Start, Names.Last().Item2.Range.End);

	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNone();

	public Range Range =>
		new(
			Let is not null ? Let.Value.Range.Start : Names.First().Item2.Range.Start,
			Initializer is not null ? Initializer.Range.End : Names.Last().Item2.Range.End
		);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable> { this };
		if (Initializer is not null) hoverables.AddRange(Initializer.ExtractHoverables());

		return hoverables;
	}
}

/// <summary>
///     Represents a type-checked module declaration
/// </summary>
/// <param name="Mod">
///     A token representing the <c>mod</c> keyword, which determines the node's starting position in the
///     Aura source file
/// </param>
/// <param name="Value">The module's name</param>
public record TypedMod(Tok Mod, Tok Value) : ITypedAuraStatement, IHoverable
{
	public string HoverText => $"mod {Value.Value}";
	public Range HoverableRange => new(Mod.Range.Start, Value.Range.End);

	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNone();

	public Range Range => new(Mod.Range.Start, Value.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables() { return new List<IHoverable> { this }; }
}

/// <summary>
///     Represents a valid type-checked <c>return</c> statement
/// </summary>
/// <param name="Return">
///     A token representing the <c>return</c> keyword, which determines the node's starting position
///     in the Aura source file
/// </param>
/// <param name="Value">The value to return</param>
public record TypedReturn(Tok Return, ITypedAuraExpression? Value) : ITypedAuraStatement
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNone();

	public Range Range => new(Return.Range.Start, Value is not null ? Value.Range.End : Return.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		if (Value is not null) hoverables.AddRange(Value.ExtractHoverables());

		return hoverables;
	}
}

/// <summary>
///     Represents a valid type-checked interface declaration
/// </summary>
/// <param name="Interface">
///     A token representing the <c>interface</c> keyword, which helps determine the node's starting
///     position in the Aura source file
/// </param>
/// <param name="Name">The interface's declaration</param>
/// <param name="Methods">The interface's methods</param>
/// <param name="Public">Indicates whether the class is declared as public</param>
/// <param name="ClosingBrace">
///     A token representing the interface's closing brace, which determines the node's ending
///     position in the Aura source file
/// </param>
/// <param name="Documentation">
///     The optional documentation comment appearing on the line above the interface in the Aura
///     source file
/// </param>
public record TypedInterface
(
	Tok Interface,
	Tok Name,
	List<TypedFunctionSignature> Methods,
	Visibility Public,
	Tok ClosingBrace,
	string? Documentation
) : ITypedAuraStatement, IHoverable
{
	public string HoverText
	{
		get
		{
			var pub = Public == Visibility.Public ? "pub " : string.Empty;
			var methods = Methods.Count > 0
				? $"\n\nMethods:\n{string.Join("\n", Methods.Select(m => m.HoverText))}"
				: string.Empty;
			var doc = Documentation is not null ? $"\n\n{Documentation}" : string.Empty;
			return $"{pub}interface {Name.Value}{methods}{doc}";
		}
	}

	public Range HoverableRange => Name.Range;

	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNone();

	public Range Range =>
		new(
			Public == Visibility.Public
				? Interface.Range.Start with { Character = Interface.Range.Start.Character - 4 }
				: Interface.Range.Start,
			ClosingBrace.Range.End
		);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable> { this };
		foreach (var method in Methods) hoverables.AddRange(method.ExtractHoverables());

		return hoverables;
	}
}

/// <summary>
///     Represents a valid interface placeholder
/// </summary>
/// <param name="InterfaceValue">A token representing a specific interface type</param>
/// <param name="Typ">The interface type</param>
public record TypedInterfacePlaceholder(Tok InterfaceValue, AuraType Typ) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }
	public Range Range => InterfaceValue.Range;
}

/// <summary>
///     Represents a function signature. This node is used to represent the function signatures that appear inside the body
///     of an Aura interface.
/// </summary>
/// <param name="Visibility">
///     A token representing the function's visibility (public or private), which helps determine the
///     node's starting position in the Aura source file
/// </param>
/// <param name="Fn">
///     A token representing the <c>fn</c> keyword, which helps determine the node's starting position in
///     the Aura source file when the <c>pub</c> keyword is omitted
/// </param>
/// <param name="Name">The function's name</param>
/// <param name="Params">the function's parameters</param>
/// <param name="ClosingParen">
///     The function's closing parenthesis, which helps determine the node's ending position in
///     the Aura source file when a return type is omitted
/// </param>
/// <param name="ReturnType">The function's return type</param>
/// <param name="Documentation">
///     The optional documentation comment that appears on the line directly above the function
///     signature in the Aura source file
/// </param>
public record TypedFunctionSignature
(
	Tok? Visibility,
	Tok Fn,
	Tok Name,
	List<Param> Params,
	Tok ClosingParen,
	AuraType ReturnType,
	string? Documentation
)
	: ITypedAuraStatement, IHoverable
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public Range Range => new(Visibility?.Range.Start ?? Fn.Range.Start, ClosingParen.Range.End);
	public Range HoverableRange => Name.Range;

	public AuraType Typ =>
		new AuraNamedFunction(
			Name.Value,
			Visibility is not null ? Shared.Visibility.Public : Shared.Visibility.Private,
			new AuraFunction(Params, ReturnType),
			Documentation
		);

	public string HoverText => ((AuraNamedFunction)Typ).Documentation;

	public IEnumerable<IHoverable> ExtractHoverables() { return new List<IHoverable> { this }; }
}

/// <summary>
///     Represents a type-checked Aura struct
/// </summary>
/// <param name="Struct">
///     A token representing the <c>struct</c> keyword, which determines the node's starting position
///     in the Aura source file
/// </param>
/// <param name="Name">The struct's name</param>
/// <param name="Params">The struct's parameters</param>
/// <param name="ClosingParen">
///     A token representing the struct's closing parenthesis, which determines the node's ending
///     position in the Aura source file
/// </param>
/// <param name="Documentation">
///     The optional documentation comment that appears on the line directly above the struct in
///     the Aura source file
/// </param>
public record TypedStruct(Tok Struct, Tok Name, List<Param> Params, Tok ClosingParen, string? Documentation)
	: ITypedAuraStatement, ITypedFunction, ITypedAuraCallable, IHoverable
{
	public string HoverText
	{
		get
		{
			var @params = string.Join(", ", Params.Select(p => $"{p.Name.Value}: {p.ParamType.Typ.ToAuraString()}"));
			var doc = Documentation is not null ? $"\n\n{Documentation}" : string.Empty;
			return $"struct {Name.Value} ({@params}){doc}";
		}
	}

	public Range HoverableRange => Name.Range;

	public string GetName() { return Name.Value; }

	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNone();

	public Range Range => new(Struct.Range.Start, ClosingParen.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable> { this };
		foreach (var param in Params) hoverables.Add(param);

		return hoverables;
	}

	public List<Param> GetParams() { return Params; }

	public List<ParamType> GetParamTypes() { return Params.Select(p => p.ParamType).ToList(); }
}

/// <summary>
///     Represents a type-checked anonymous struct
/// </summary>
/// <param name="Struct">
///     A token representing the <c>struct</c> keyword, which determines the node's starting position
///     in the Aura source file
/// </param>
/// <param name="Params">The struct's parameters</param>
/// <param name="Values">The supplied values, which have been type checked against the anonymous struct's parameters</param>
/// <param name="ClosingParen">
///     The anonymous struct's closing parenthesis, which determines the node's ending position
///     in the Aura source file
/// </param>
public record TypedAnonymousStruct(Tok Struct, List<Param> Params, List<ITypedAuraExpression> Values, Tok ClosingParen)
	: ITypedAuraExpression, ITypedFunction, IHoverable
{
	public string HoverText => $"struct ({string.Join(", ", Params.Select(p => p.Name.Value))})";

	public Range HoverableRange => Struct.Range;

	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraAnonymousStruct(Params, Visibility.Private);

	public Range Range => new(Struct.Range.Start, ClosingParen.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables() { return new List<IHoverable> { this }; }

	public List<Param> GetParams() { return Params; }

	public List<ParamType> GetParamTypes() { return Params.Select(p => p.ParamType).ToList(); }
}

/// <summary>
///     Represents a valid type-checked class declaration
/// </summary>
/// <param name="Class">
///     A token representing the <c>class</c> keyword, which determines the node's starting position
///     in the Aura source file
/// </param>
/// <param name="Name">The class's name</param>
/// <param name="Params">The class's parameters</param>
/// <param name="Methods">The class's methods</param>
/// <param name="Public">Indicates whether the class is declared as public</param>
/// <param name="Implementing">A list of Aura interfaces implemented by this class</param>
/// <param name="ClosingBrace">
///     A token representing the class's closing brace, which determines the node's ending
///     position in the Aura source file
/// </param>
/// <param name="Documentation">
///     The optional documentation comment appearing on the line directly above the class in
///     the Aura source file
/// </param>
public record FullyTypedClass
(
	Tok Class,
	Tok Name,
	List<Param> Params,
	List<TypedNamedFunction> Methods,
	Visibility Public,
	List<AuraInterface> Implementing,
	Tok ClosingBrace,
	string? Documentation
) : ITypedAuraStatement, ITypedFunction, ITypedAuraCallable, IHoverable
{
	public string HoverText
	{
		get
		{
			var pub = Public == Visibility.Public ? "pub " : string.Empty;
			var @params = string.Join(", ", Params.Select(p => $"{p.Name.Value}: {p.ParamType.Typ}"));
			var implementing = Implementing.Count > 0
				? $"\n\nImplementing:\n{string.Join("\n\n", Implementing)}\n\n"
				: string.Empty;
			var methods = Methods.Count > 0
				? $"\n\nMethods:\n{string.Join("\n\n", Methods.Select(m => m.HoverText))}"
				: string.Empty;
			var doc = Documentation is not null ? $"\n\n{Documentation}" : string.Empty;
			return $"{pub}class {Name.Value}({@params}){implementing}{methods}{doc}";
		}
	}

	public Range HoverableRange => Name.Range;

	public string GetName() { return Name.Value; }

	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNone();

	public Range Range => new(Class.Range.Start, ClosingBrace.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable> { this };
		foreach (var param in Params) hoverables.Add(param);

		foreach (var method in Methods) hoverables.AddRange(method.ExtractHoverables());

		return hoverables;
	}

	public List<Param> GetParams() { return Params; }

	public List<ParamType> GetParamTypes() { return Params.Select(param => param.ParamType).ToList(); }
}

/// <summary>
///     Represents a valid type-checked <c>while</c> loop
/// </summary>
/// <param name="While">
///     A token representing the <c>while</c> keyword, which determines the node's starting position
///     in the Aura source file
/// </param>
/// <param name="Condition">The loop's condition. The loop will exit when the condition evaluates to false</param>
/// <param name="Body">Collection of statements executed once per iteration</param>
/// <param name="ClosingBrace">
///     A token representing the loop's closing brace, which determines the node's ending position
///     in the Aura source file
/// </param>
public record TypedWhile(Tok While, ITypedAuraExpression Condition, List<ITypedAuraStatement> Body, Tok ClosingBrace)
	: ITypedAuraStatement
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNone();

	public Range Range => new(While.Range.Start, ClosingBrace.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables()
	{
		var hoverables = new List<IHoverable>();
		hoverables.AddRange(Condition.ExtractHoverables());
		hoverables.AddRange(Body.OfType<IHoverable>());
		return hoverables;
	}
}

/// <summary>
///     Represents a valid type-checked <c>import</c> statement
/// </summary>
/// <param name="Import">
///     A token representing the <c>import</c> keyword, which determines the node's starting position
///     in the Aura source file
/// </param>
/// <param name="Package">The name of the package being imported</param>
/// <param name="Alias">Will have a value if the package is being imported under an alias</param>
public record TypedImport(Tok Import, Tok Package, Tok? Alias) : ITypedAuraStatement, IHoverable
{
	public string HoverText => $"{Package.Value}{(Alias is not null ? $" (as {Alias.Value.Value})" : string.Empty)}";

	public Range HoverableRange =>
		new(Package.Range.Start, Alias is not null ? Alias.Value.Range.End : Package.Range.End);

	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNone();

	public Range Range => new(Import.Range.Start, Alias is not null ? Alias.Value.Range.End : Package.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables() { return new List<IHoverable> { this }; }
}

/// <summary>
///     Represents a type-checked multiple import statement
/// </summary>
/// <param name="Import">
///     A token representing the <c>import</c> keyword, which determines the node's starting position
///     in the Aura source file
/// </param>
/// <param name="Packages">The packages to be imported</param>
/// <param name="ClosingParen">
///     A token representing the closing parenthesis, which determines the node's ending position
///     in the Aura source file
/// </param>
public record TypedMultipleImport(Tok Import, List<TypedImport> Packages, Tok ClosingParen) : ITypedAuraStatement
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNone();

	public Range Range => new(Import.Range.Start, ClosingParen.Range.End);
}

/// <summary>
///     Represents a type-checked comment
/// </summary>
/// <param name="Text">The text of the comment</param>
public record TypedComment(Tok Text) : ITypedAuraStatement
{
	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNone();
	public Range Range => Text.Range;
}

/// <summary>
///     Represents a type-checked <c>continue</c> statement
/// </summary>
/// <param name="Continue">A token representing the <c>continue</c> keyword</param>
public record TypedContinue(Tok Continue) : ITypedAuraStatement, IHoverable
{
	public string HoverText => "Immediately advances execution to the enclosing loop's next iteration";

	public Range HoverableRange => Continue.Range;

	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNone();
	public Range Range => Continue.Range;

	public IEnumerable<IHoverable> ExtractHoverables() { return new List<IHoverable> { this }; }
}

/// <summary>
///     Represents a type-checked <c>break</c> statement
/// </summary>
/// <param name="Break">A token representing the <c>break</c> keyword</param>
public record TypedBreak(Tok Break) : ITypedAuraStatement, IHoverable
{
	public string HoverText => "Immediately stops the enclosing loop's execution";

	public Range HoverableRange => Break.Range;

	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNone();
	public Range Range => Break.Range;

	public IEnumerable<IHoverable> ExtractHoverables() { return new List<IHoverable> { this }; }
}

/// <summary>
///     Represents a type-checked <c>nil</c> keyword
/// </summary>
/// <param name="Nil">a token representing the <c>nil</c> keyword</param>
public record TypedNil(Tok Nil) : ITypedAuraExpression
{
	public T Accept<T>(ITypedAuraExprVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNil();
	public Range Range => Nil.Range;
}

/// <summary>
///     Represents a type-checked <c>yield</c> statement
/// </summary>
/// <param name="Yield">
///     A token representing the <c>yield</c> keyword, which determines the node's starting position
///     in the Aura source file
/// </param>
/// <param name="Value">The value to be yielded from the enclosing expression</param>
public record TypedYield(Tok Yield, ITypedAuraExpression Value) : ITypedAuraStatement, IHoverable
{
	public string HoverText =>
		"Used inside an `if` or block expression to return a value without returning the value from the enclosing function context";

	public Range HoverableRange => Yield.Range;

	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNone();

	public Range Range => new(Yield.Range.Start, Value.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables() { return new List<IHoverable> { this }; }
}

/// <summary>
///     Represents a type-checked <c>check</c> statement
/// </summary>
/// <param name="Check">
///     A token representing the <c>check</c> keyword, which determines the node's starting position
///     in the Aura source file
/// </param>
/// <param name="Call">The call to be checked</param>
public record TypedCheck(Tok Check, TypedCall Call) : ITypedAuraStatement, IHoverable
{
	public string HoverText =>
		"Used to simplify error handling on a function call whose return type is a `Result`. The enclosing function must also return a `Result`";

	public Range HoverableRange => Check.Range;

	public T Accept<T>(ITypedAuraStmtVisitor<T> visitor) { return visitor.Visit(this); }

	public AuraType Typ => new AuraNone();

	public Range Range => new(Check.Range.Start, Call.Range.End);

	public IEnumerable<IHoverable> ExtractHoverables() { return new List<IHoverable> { this }; }
}
