using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;

namespace AuraLang.AST;

public interface IUntypedAuraAstNode : IAuraAstNode { }

/// <summary>
/// An untyped Aura expression
/// </summary>
public interface IUntypedAuraExpression : IUntypedAuraAstNode { }

/// <summary>
/// An untyped Aura statement
/// </summary>
public interface IUntypedAuraStatement : IUntypedAuraAstNode { }

// TODO Should this inherit from IUntypedAuraExpression?
public interface IUntypedAuraCallable
{
	string GetName();
}

/// <summary>
/// Represents an assignment expression in Aura, which assigns a value to a previously-declared
/// variable. An example in Aura might look like:
/// <code>x = 5</code>
/// </summary>
/// <param name="Name">The name of the variable being assigned a new value</param>
/// <param name="Value">The variable's new value</param>
public record UntypedAssignment(Tok Name, IUntypedAuraExpression Value, int Line) : IUntypedAuraExpression;

/// <summary>
/// Represents an increment operation where the value of the variable is incremented by 1.
/// </summary>
/// <param name="Name">The variable being incremented</param>
public record UntypedPlusPlusIncrement(IUntypedAuraExpression Name, int Line) : IUntypedAuraExpression;

/// <summary>
/// Represents a decrement operation where the value of the variable is decremented by 1.
/// </summary>
/// <param name="Name">The variable being decremented</param>
public record UntypedMinusMinusDecrement(IUntypedAuraExpression Name, int Line) : IUntypedAuraExpression;

/// <summary>
/// Represents a binary expression containing a left and right operand and an operator. A simple
/// binary expression might look like:
/// <code>5 + 5</code>
/// However, binary expression can become more complex, and the left and right operands of a binary
/// expression can themselves be binary expressions or any other expression.
/// The operator in a binary expression are confined to a subset of the operators available in Aura.
/// Other operators, such as the logical `and` and `or`, are available in other expression types. In
/// the case of the logical operators, they can be used in a <c>logical</c> expression.
/// </summary>
/// <param name="Left">The expression on the left side of the binary expression</param>
/// <param name="Operator">The binary expression's operator</param>
/// <param name="Right">The expression on the right side of the binary expression</param>
public record UntypedBinary(IUntypedAuraExpression Left, Tok Operator, IUntypedAuraExpression Right, int Line) : IUntypedAuraExpression;

/// <summary>
/// Represents a block, which is a series of statements wrapped in curly braces. Blocks in Aura
/// are used in many places, including function bodies, `if` expression bodies, and loop bodies.
/// Each block defines its own local scope, and users may define local variables inside a block
/// that shadow variables with the same name defined outside of the block.
///
/// Blocks themselves are expressions, and so return a value, but that value is not used everywhere
/// that blocks are used. For example, <c>while</c> and <c>for</c> loops do not return a value.
/// </summary>
/// <param name="Statements">The block's statements</param>
public record UntypedBlock(List<IUntypedAuraStatement> Statements, int Line) : IUntypedAuraExpression;

/// <summary>
/// Represents a function call
/// </summary>
/// <param name="Callee">The expression being called</param>
/// <param name="Arguments">The call's arguments. Each argument is a tuple containing an optional tag and the argument's value.
/// An argument's tag must precede the argument's value, and the two are separated by a colon. The tag must match the name
/// of one of the function's parameters. Tags can be used to specify arguments in a different order than the function's parameters
/// were defined. For example, the stdlib's <c>printf</c> function could be called with tags like so:
/// <code>printf(a: 5, format: "%d\n")</code></param>
public record UntypedCall
	(IUntypedAuraCallable Callee, List<(Tok?, IUntypedAuraExpression)> Arguments, int Line) : IUntypedAuraExpression,
		IUntypedAuraCallable
{
	public string GetName() => Callee.GetName();
}

/// <summary>
/// Represents an Aura expression that fetches an attribute from a compound object, such as a
/// module or class. The syntax of a <c>get</c> expression is <code>object.attribute</code> An
/// example might look like:
/// <code>greeter.name</code>
/// where <c>greeter</c> is a class and <c>name</c> is an attribute of that class.
/// </summary>
/// <param name="Obj">The compound object being queried. This compound object should contain the attribute being fetched via
/// the <see cref="Name"/> parameter</param>
/// <param name="Name">The attribute being fetched</param>
public record UntypedGet(IUntypedAuraExpression Obj, Tok Name, int Line) : IUntypedAuraExpression, IUntypedAuraCallable
{
	public string GetName() => Name.Value;
}

/// <summary>
/// Represents an Aura expression that fetches a single item from an indexable data type
/// </summary>
/// <param name="Obj">The compound object being queried</param>
/// <param name="Index">The index being fetched</param>
public record UntypedGetIndex(IUntypedAuraExpression Obj, IUntypedAuraExpression Index, int Line) : IUntypedAuraExpression;

/// <summary>
/// Represents an Aura expression that fetches a range of items from an indexable data type
/// </summary>
/// <param name="Obj">The compound object being queried</param>
/// <param name="Lower">The lower bound of the range. This value is inclusive.</param>
/// <param name="Upper">The upper bound of the range. This value is exclusive.</param>
public record UntypedGetIndexRange(IUntypedAuraExpression Obj, IUntypedAuraExpression Lower, IUntypedAuraExpression Upper, int Line) : IUntypedAuraExpression;

/// <summary>
/// Represents one or more Aura expressions grouped together with parentheses. A simple
/// grouping expression would look like:
/// <code>(5 + 5)</code>
/// </summary>
/// <param name="Expr">The grouped expression</param>
public record UntypedGrouping(IUntypedAuraExpression Expr, int Line) : IUntypedAuraExpression;

/// <summary>
/// Represents an <c>if</c> expression, which consists of at least two components -
/// the condition and one or more conditional branches of execution. A simple <c>if</c>
/// expression might look like this:
/// <code>
/// if true {
///     io.println("true")
/// } else {
///     io.println("false")
/// }
/// </code>
/// The expression's condition must be a valid Aura expression and does not need to be surrounded
/// by parentheses.
/// </summary>
/// <param name="Condition">The condition that will be evaluated first when entering the <c>if</c> expression. If the condition evaluates
/// to true, the <see cref="Then"/> branch will be executed.</param>
/// <param name="Then">The branch that will be executed if the <see cref="Condition"/> evaluates to true</param>
/// <param name="Else">The branch that will be executed if the <see cref="Condition"/> evalutes to false</param>
public record UntypedIf(IUntypedAuraExpression Condition, UntypedBlock Then, IUntypedAuraExpression? Else, int Line) : IUntypedAuraExpression;

/// <summary>
/// Represents Aura's <c>nil</c> keyword.
/// </summary>
public record UntypedNil(int Line) : IUntypedAuraExpression;

/// <summary>
/// Represents a logical expression, which is any binary expression that evaluates
/// to a boolean value
/// </summary>
/// <param name="Left">The expression on the left side of the expression</param>
/// <param name="Operator">The logical expression's operator</param>
/// <param name="Right">The expression on the right side of the expression</param>
public record UntypedLogical(IUntypedAuraExpression Left, Tok Operator, IUntypedAuraExpression Right, int Line) : IUntypedAuraExpression;

/// <summary>
/// Represents a set expression, which is a binary expression whose operator must be an equal sign,
/// and consisting of a <c>get</c> expression on the left hand side of the expression and an expression
/// on the right hand side whose result will be assigned to the left hand side operand. A <c>set</c>
/// expression is similar to an assignment expression except the expression on the left hand side must be
/// a <c>get</c> expression on a compound object.
///
/// A simple <c>set</c> expression would look like:
/// <code>greeter.name = "Bob"</code>
/// </summary>
/// <param name="Obj">The compound object whose attribute is getting a new value</param>
/// <param name="Name">The name of the attribute being assigned a new value</param>
/// <param name="Value">The new value</param>
public record UntypedSet(IUntypedAuraExpression Obj, Tok Name, IUntypedAuraExpression Value, int Line) : IUntypedAuraExpression;

/// <summary>
/// Represents the <c>this</c> keyword when it's used inside of a class's declaration body
/// </summary>
/// <param name="Keyword">The <c>this</c> keyword</param>
public record UntypedThis(Tok Keyword, int Line) : IUntypedAuraExpression;

/// <summary>
/// Represents a unary expression, which consists of an operator and an operand.
/// A simple unary expression in Aura looks like:
/// <code>!true</code>
/// </summary>
/// <param name="Operator">The expression's operator, must be one of <c>!</c>, <c>-</c></param>
/// <param name="Right">The expression's type is determined by the <see cref="Operator"/>. If the operator is
/// <c>!</c>, the expression must be a boolean value, and if the operator is <c>-</c>, the expression must
/// be either an integer or a float.</param>
public record UntypedUnary(Tok Operator, IUntypedAuraExpression Right, int Line) : IUntypedAuraExpression;

/// <summary>
/// Represents an Aura variable. At the parsing stage of the compilation process, a variable is any token
/// that doesn't match an Aura reserved keyword or reserved token.
/// </summary>
/// <param name="Name">The variable's name</param>
public record UntypedVariable(Tok Name, int Line) : IUntypedAuraExpression, IUntypedAuraCallable
{
	public string GetName() => Name.Value;
}

/// <summary>
/// Represents an <c>is</c> expression, which determines if the supplied expression matches the expected type
/// </summary>
/// <param name="expr">The expression whose type is being tested</param>
/// <param name="expected">The expected type that the expression's type is compared against</param>
public record UntypedIs(IUntypedAuraExpression Expr, Tok Expected, int Line) : IUntypedAuraExpression;

/// <summary>
/// Represents a <c>defer</c> statement that is responsible for deferring the supplied function call
/// until the end of the enclosing function's execution.
/// </summary>
/// <param name="Call">The call expression to be deferred until the end of the enclosing function scope</param>
public record UntypedDefer(IUntypedAuraCallable Call, int Line) : IUntypedAuraStatement;

/// <summary>
/// Represents any expression used in a context where a statement is expected. In these situations,
/// the expression's return value is ignored.
/// </summary>
/// <param name="Expression">The expression that was present in a context where a statement was expected</param>
public record UntypedExpressionStmt(IUntypedAuraExpression Expression, int Line) : IUntypedAuraStatement;

/// <summary>
/// Represents an Aura <c>for</c> loop, which has the same structure as a C <c>for</c>
/// loop, except that the parentheses are not required around the loop's signature. For example,
/// a <c>for</c> loop in Aura would look like:
/// <code>
/// for i := 0; i < 10; i++ {
///     io.printf("%d\b", i)
/// }
/// </code>
/// </summary>
/// <param name="Initializer">Used to initialize a variable that will be available in the loop's body</param>
/// <param name="Condition">The condition that will be evaluated after each iteration. If the condition evaluates to false, the loop
/// will exit</param>
/// <param name="Body">Collection of statements that will be executed on each iteration</param>
public record UntypedFor(IUntypedAuraStatement? Initializer, IUntypedAuraExpression? Condition, IUntypedAuraExpression? Increment, List<IUntypedAuraStatement> Body, int Line) : IUntypedAuraStatement;

/// <summary>
/// Represents a simplified <c>for</c> loop that supports iterating through an
/// iterable data type. The syntax for a <c>foreach</c> loop looks like this:
/// <code>for <c>var_name</c> in <c>iterable</c> { ... }</code>
/// </summary>
/// <param name="EachName">The name of the variable that will be available in the loop's body and will represent each successive item
/// in the iterable</param>
/// <param name="Iterable">The collection being iterated over</param>
/// <param name="Body">Collection of statements that will be executed on each iteration</param>
public record UntypedForEach(Tok EachName, IUntypedAuraExpression Iterable, List<IUntypedAuraStatement> Body, int Line) : IUntypedAuraStatement;

/// <summary>
/// Represents a named function declaration. The syntax for declaring a named function looks like:
/// <code>fn <c>function_name</c>(<c>param_name</c>: <c>param_type</c>[,...]_ -> <c>return_type</c> { ... }</code>
/// </summary>
/// <param name="Name">The function's name</param>
/// <param name="Params">The function's parameters</param>
/// <param name="Body">The function's body</param>
/// <param name="ReturnType">The function's return type. This struct stores it as a token instead of a type because it hasn't
/// been type checked yet.</param>
/// <param name="Public">Indicates if the function is public or private</param>
public record UntypedNamedFunction(Tok Name, List<Param> Params, UntypedBlock Body, Tok? ReturnType, Visibility Public, int Line) : IUntypedAuraStatement, IUntypedFunction
{
	public List<Param> GetParams() => Params;
	public List<ParamType> GetParamTypes() => Params.Select(p => p.ParamType).ToList();
}

/// <summary>
/// Represents an anonymous function, which can be declared the same way as a named function,
/// just without including the function's name.
/// </summary>
/// <param name="Params">The function's parameters</param>
/// <param name="Body">The function's body</param>
/// <param name="ReturnType">The function's return type. This struct stores it as a token instead of a type because it hasn't
/// /// been type checked yet.</param>
public record UntypedAnonymousFunction(List<Param> Params, UntypedBlock Body, Tok? ReturnType, int Line) : IUntypedAuraExpression, IUntypedFunction
{
	public List<Param> GetParams() => Params;
	public List<ParamType> GetParamTypes() => Params.Select(p => p.ParamType).ToList();
}

/// <summary>
/// Represents a <c>let</c> expression that declares a new variable and optionally assigns it an initial value.
/// Variable declarations in Aura can be written one of two ways. The full <c>let</c>-style declaration looks like:
/// <code>let i: int = 5</code>
/// where the variable's type annotation is required. For a shorter syntax, one can write:
/// <code>i := 5</code>
/// </summary>
/// <param name="Name">The name of the newly-declared variable</param>
/// <param name="NameTyp">The variable's type, if it was declared with an explicit type annotation. If not, the value of this field will
/// be <see cref="Unknown"/></param>
/// <param name="Mutable">Indicates if the variable is mutable or not</param>
/// <param name="Initializer">The initializer expression whose result will be assigned to the new variable. This expression may be omitted.</param>
public record UntypedLet(Tok Name, AuraType? NameTyp, bool Mutable, IUntypedAuraExpression? Initializer, int Line) : IUntypedAuraStatement;

/// <summary>
/// Represents the current source file's module declaration. It should appear at the top of the file and have
/// the format:
/// <code>mod <c>mod_name</c></code>
/// </summary>
/// <param name="Value">The module's name</param>
public record UntypedMod(Tok Value, int Line) : IUntypedAuraStatement;

/// <summary>
/// Represents a <c>return</c> statement, which can be either explicit (i.e. <code>return 5</code>) or, in specific
/// circumstances, implicit.
/// </summary>
/// <param name="Value">The value to be returned</param>
public record UntypedReturn(IUntypedAuraExpression? Value, int Line) : IUntypedAuraStatement;

/// <summary>
/// Represents a class declaration, which follows the syntax:
/// <code>class <c>class_name</c>(<c>param</c>: <c>param_type</c>[,...]) { ... }</code>
/// </summary>
/// <param name="Name">The class's name</param>
/// <param name="Params">The class's parameters</param>
/// <param name="Body">The class's body</param>
/// <param name="Public">Indicates if the class is public or not</param>
public record UntypedClass(Tok Name, List<Param> Params, List<IUntypedAuraStatement> Body, Visibility Public, List<Tok> Implementing, int Line) : IUntypedAuraStatement, IUntypedFunction
{
	public List<Param> GetParams() => Params;
	public List<ParamType> GetParamTypes() => Params.Select(p => p.ParamType).ToList();
	public List<UntypedNamedFunction> Methods => Body
		.Where(item => item is not UntypedComment)
		.Select(m => (UntypedNamedFunction)m)
		.ToList();
}

/// <summary>
/// Represents an interface declaration, which follows the syntax:
/// <code>interface <c>interface_name</c> { ... } </code>
/// </summary>
/// <param name="Name">The interface's name</param>
/// <param name="Methods">The interface's methods</param>
/// <param name="Public">Indicates if the interface is public or not</param>
public record UntypedInterface
	(Tok Name, List<NamedFunction> Methods, Visibility Public, int Line) : IUntypedAuraStatement;

/// <summary>
/// Represents a <c>while</c> loop, which follows this syntax:
/// <code>while true { ... }</code>
/// where its body is executed until the loop's condition evaluates to false.
/// </summary>
/// <param name="Condition">The condition to be evaluated on each iteration of the loop. The loop will exit when the condition
/// evaluates to false.</param>
/// <param name="Body">Collection of statements executed once per iteration</param>
public record UntypedWhile(IUntypedAuraExpression Condition, List<IUntypedAuraStatement> Body, int Line) : IUntypedAuraStatement;

/// <summary>
/// Represents an <c>import</c> statement
/// </summary>
/// <param name="Package">The name of the package being imported</param>
/// <param name="Alias">Will contain a value if the import has an alias</param>
public record UntypedImport(Tok Package, Tok? Alias, int Line) : IUntypedAuraStatement;

public record UntypedMultipleImport(List<UntypedImport> Packages, int Line) : IUntypedAuraStatement;

/// <summary>
/// Represents an Aura comment, which can be declared in two different ways. Beginning a comment with
/// <c>//</c> will declare a single-line comment that will last until the next <c>\n</c> character. Beginning
/// a comment with <c>/*</c> will declare a comment that will last until the closing <c>*/</c>, which may be
/// on the same line or a future line.
/// </summary>
/// <param name="Text">The comment's text</param>
public record UntypedComment(Tok Text, int Line) : IUntypedAuraStatement;

/// <summary>
/// Represents a <c>continue</c> statement that is used as a control flow construct inside a loop. The
/// <c>continue</c> keyword will advance execution to the next iteration in the loop.
/// </summary>
public record UntypedContinue(int Line) : IUntypedAuraStatement;

/// <summary>
/// Represents a <c>break</c> statement that is used as a control flow construct inside a loop. The <c>break</c>
/// keyword will immediately break out of the enclosing loop.
/// </summary>
public record UntypedBreak(int Line) : IUntypedAuraStatement;

/// <summary>
/// Represents a <c>yield</c> statement that is used to return a value from an <c>if</c> expression or block without
/// returning from the enclosing function.
/// </summary>
/// <param name="Value">The value to be yielded from the enclosing scope</param>
public record UntypedYield(IUntypedAuraExpression Value, int Line) : IUntypedAuraStatement;

public record UntypedNewLine(int Line) : IUntypedAuraStatement;
