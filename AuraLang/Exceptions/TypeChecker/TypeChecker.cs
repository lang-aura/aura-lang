using AuraLang.Types;
using Range = AuraLang.Location.Range;

namespace AuraLang.Exceptions.TypeChecker;

/// <summary>
///     Represents an error encountered by the type checker
/// </summary>
public abstract class TypeCheckerException : AuraException
{
    protected TypeCheckerException(string message, params Range[] range) : base(message, range) { }
}

/// <summary>
///     Thrown when an unexpected type is encountered
/// </summary>
public class UnexpectedTypeException : TypeCheckerException
{
    public UnexpectedTypeException(AuraType expected, AuraType found, Range range) : base(
        $"Unexpected type. Expected {expected}, but found {found}",
        range
    ) { }
}

/// <summary>
///     Thrown when an iterable is expected, but another node type is encountered
/// </summary>
public class ExpectIterableException : TypeCheckerException
{
    public ExpectIterableException(AuraType found, Range range) : base(
        $"Expected iterable type, but found {found}",
        range
    ) { }
}

/// <summary>
///     Thrown when the encountered type does not match the expected type
/// </summary>
public class TypeMismatchException : TypeCheckerException
{
    public TypeMismatchException(AuraType expected, AuraType found, Range range) : base(
        $"Type mismatch. Expected {expected}, but found {found}",
        range
    ) { }
}

/// <summary>
///     Thrown when the operator in a unary expression is not valid for the supplied operand, i.e. <code>!5</code> or
///     <code>-true</code>
/// </summary>
public class MismatchedUnaryOperatorAndOperandException : TypeCheckerException
{
    public MismatchedUnaryOperatorAndOperandException(string unaryOperator, AuraType operandType, Range range)
        : base(
            $"Mismatched unary operator and operand. Operator `{unaryOperator}` not valid with type {operandType}.",
            range
        ) { }
}

/// <summary>
///     Thrown when a suffix index operator is applied to a non-indexable AST node
/// </summary>
public class ExpectIndexableException : TypeCheckerException
{
    public ExpectIndexableException(AuraType found, Range range) : base(
        $"Expected indexable type, but found {found}",
        range
    ) { }
}

/// <summary>
///     Thrown when a suffix range indexable operator is applied to a non-indexable AST node
/// </summary>
public class ExpectRangeIndexableException : TypeCheckerException
{
    public ExpectRangeIndexableException(AuraType found, Range range) : base(
        $"Expected range indexable type, but found {found}",
        range
    ) { }
}

/// <summary>
///     Thrown when the number of arguments supplied does not match the expected number of parameters
/// </summary>
public class IncorrectNumberOfArgumentsException : TypeCheckerException
{
    public IncorrectNumberOfArgumentsException(int have, int want, Range range)
        : base($"Incorrect number of arguments. Have {have}, but want {want}.", range) { }
}

public class TooManyArgumentsException : TypeCheckerException
{
    public TooManyArgumentsException(int have, int want, Range[] range) : base(
        $"Incorrect number of arguments. Have {have}, but want {want}.",
        range
    ) { }
}

/// <summary>
///     Thrown when the object of a <c>get</c> expression is not a valid get-able type
/// </summary>
public class CannotGetFromNonClassException : TypeCheckerException
{
    public CannotGetFromNonClassException(
        string varName,
        AuraType varType,
        string attributeName,
        Range range
    )
        : base(
            $"Cannot get attribute from non-class. Trying to get attribute `{attributeName}` from `{varName}`, which has type `{varType}`.",
            range
        ) { }
}

/// <summary>
///     Thrown when the accessed attribute in a <c>get</c> expression does not exist on the supplied object, i.e.
///     <code>obj.name</code> where <c>name</c> does not exist on the <c>obj</c> object
/// </summary>
public class ClassAttributeDoesNotExistException : TypeCheckerException
{
    public ClassAttributeDoesNotExistException(string className, string attributeName, Range range)
        : base($"Attribute `{attributeName}` does not exist on class `{className}`.", range) { }
}

/// <summary>
///     Thrown when the <c>yield</c> keyword is used in an invalid context. The <c>yield</c> keyword may only be used
///     inside <c>if</c> expression and <c>block</c> expressions
/// </summary>
public class InvalidUseOfYieldKeywordException : TypeCheckerException
{
    public InvalidUseOfYieldKeywordException(Range range) : base(
        "Invalid use of yield keyword. The yield keyword may only be used inside of an if expression or block to return a value from the enclosing context",
        range
    ) { }
}

/// <summary>
///     Thrown when the <c>break</c> keyword is used in an invalid context. The <c>break</c> keyword may only be used
///     inside loops
/// </summary>
public class InvalidUseOfBreakKeywordException : TypeCheckerException
{
    public InvalidUseOfBreakKeywordException(Range range) : base(
        "Invalid use of `break` keyword. The `break` keyword may only be used inside of a loop to break out of the loop's execution",
        range
    ) { }
}

/// <summary>
///     Thrown when the <c>continue</c> keyword is used in an invalid context. The <c>continue</c> keyword may only be used
///     inside loops
/// </summary>
public class InvalidUseOfContinueKeywordException : TypeCheckerException
{
    public InvalidUseOfContinueKeywordException(Range range) : base(
        "Invalid use of `continue` keyword. The `continue` keyword may only be used inside of a loop to immediately advance to the loop's next iteration",
        range
    ) { }
}

/// <summary>
///     Thrown when a function call utilizes both named and unnamed arguments, which is not valid in Aura. i.e.
///     <code>f(v1, name: v2, v3)</code> is invalid. All arguments must be either named or unnamed
/// </summary>
public class CannotMixNamedAndUnnamedArgumentsException : TypeCheckerException
{
    public CannotMixNamedAndUnnamedArgumentsException(string functionName, Range range)
        : base($"Mixing named and unnamed arguments for function `{functionName}` is not permitted.", range) { }
}

/// <summary>
///     Thrown when no argument is passed in corresponding to a parameter that was defined without a default value
/// </summary>
public class MustSpecifyValueForArgumentWithoutDefaultValueException : TypeCheckerException
{
    public MustSpecifyValueForArgumentWithoutDefaultValueException(string functionName, string argument, Range range)
        : base(
            $"Argument `{argument}` in call to `{functionName}` does not have a default value specified, so a value must be specified.",
            range
        ) { }
}

/// <summary>
///     Thrown when a new variable is defined without an initializer, but its type does not specify a default value
/// </summary>
public class MustSpecifyInitialValueForNonDefaultableTypeException : TypeCheckerException
{
    public MustSpecifyInitialValueForNonDefaultableTypeException(AuraType typ, Range range)
        : base(
            $"The type `{typ}` does not have a default value specified, so an initial value must be provided.",
            range
        ) { }
}

/// <summary>
///     Thrown when a variable is used that was not previously defined
/// </summary>
public class UnknownVariableException : TypeCheckerException
{
    public UnknownVariableException(string varName, Range range)
        : base($"Unknown variable `{varName}`.", range) { }
}

/// <summary>
///     Thrown when a class attempts to implement a non-interface
/// </summary>
public class CannotImplementNonInterfaceException : TypeCheckerException
{
    public CannotImplementNonInterfaceException(string name, Range range)
        : base($"`{name}` is not an interface, so it cannot be implemented.", range) { }
}

/// <summary>
///     Thrown when a class implements an interface, but does not implement all required methods
/// </summary>
public class MissingInterfaceMethodException : TypeCheckerException
{
    public MissingInterfaceMethodException(string interfaceName, string missingMethod, Range range)
        : base($"All implementors of `{interfaceName}` must implement the method `{missingMethod}`.", range) { }
}

/// <summary>
///     Thrown when the object of a <c>set</c> expression does not have a type of class
/// </summary>
public class CannotSetOnNonClassException : TypeCheckerException
{
    public CannotSetOnNonClassException(AuraType typ, Range range) : base(
        $"Trying to set a value on type {typ}, which is not permitted. ",
        range
    ) { }
}

/// <summary>
///     Thrown when a <c>++</c> suffix operator is applied to a non-number
/// </summary>
public class CannotIncrementNonNumberException : TypeCheckerException
{
    public CannotIncrementNonNumberException(AuraType found, Range range) : base(
        $"Cannot increment non-number type. Expected either int or float, but found {found}",
        range
    ) { }
}

/// <summary>
///     Thrown when a <c>--</c> suffix operator is applied to a non-number
/// </summary>
public class CannotDecrementNonNumberException : TypeCheckerException
{
    public CannotDecrementNonNumberException(AuraType found, Range range) : base(
        $"Cannot decrement non-number. Expected either int or float, but found {found}",
        range
    ) { }
}

/// <summary>
///     Thrown when a directory contains more than one Aura module
/// </summary>
public class DirectoryCannotContainMultipleModulesException : TypeCheckerException
{
    public DirectoryCannotContainMultipleModulesException(List<string> found, Range range) : base(
        $"Directory cannot contain multiple modules. Expected only one module name, but found [{string.Join(", ", found)}]",
        range
    ) { }
}

/// <summary>
///     Thrown when the <c>check</c> keyword is used in an invalid context. The <c>check</c> keyword may only be used
///     before a function call that returns a <see cref="AuraResult" /> value
/// </summary>
public class InvalidUseOfCheckKeywordException : TypeCheckerException
{
    public InvalidUseOfCheckKeywordException(Range range) : base(
        "Invalid use of `check` keyword. The `check` keyword may only be used with function calls whose return type is `Result`",
        range
    ) { }
}

/// <summary>
///     Thrown when a <c>let</c> statement that contains multiple variable names mixes type annotations, i.e.
///     <code>let i: int, f, s: string = ...</code>. All variables in this situation must either have a type annotation or
///     not
/// </summary>
public class CannotMixTypeAnnotationsException : TypeCheckerException
{
    public CannotMixTypeAnnotationsException(Range range) : base(
        "Cannot mix type annotations in `let` statement",
        range
    ) { }
}