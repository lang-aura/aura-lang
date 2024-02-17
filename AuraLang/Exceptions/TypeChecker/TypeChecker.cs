using AuraLang.Types;
using Range = AuraLang.Location.Range;

namespace AuraLang.Exceptions.TypeChecker;

public abstract class TypeCheckerException : AuraException
{
	protected TypeCheckerException(string message, Range range) : base(message, range) { }
}

public class UnknownStatementTypeException : TypeCheckerException
{
	public UnknownStatementTypeException(Range range) : base("Unknown statement type", range) { }
}

public class UnknownExpressionTypeException : TypeCheckerException
{
	public UnknownExpressionTypeException(Range range) : base("Unknown expression type", range)
	{
	}
}

public class UnexpectedTypeException : TypeCheckerException
{
	public UnexpectedTypeException(AuraType expected, AuraType found, Range range) : base($"Unexpected type. Expected {expected}, but found {found}", range) { }
}

public class ExpectIterableException : TypeCheckerException
{
	public ExpectIterableException(AuraType found, Range range) : base($"Expected iterable type, but found {found}", range) { }
}

public class TypeMismatchException : TypeCheckerException
{
	public TypeMismatchException(AuraType expected, AuraType found, Range range) : base($"Type mismatch. Expected {expected}, but found {found}", range) { }
}

public class MismatchedUnaryOperatorAndOperandException : TypeCheckerException
{
	public MismatchedUnaryOperatorAndOperandException(string unaryOperator, AuraType operandType, Range range)
		: base($"Mismatched unary operator and operand. Operator `{unaryOperator}` not valid with type {operandType}.", range) { }
}

public class ExpectIndexableException : TypeCheckerException
{
	public ExpectIndexableException(AuraType found, Range range) : base($"Expected indexable type, but found {found}", range) { }
}

public class ExpectRangeIndexableException : TypeCheckerException
{
	public ExpectRangeIndexableException(AuraType found, Range range) : base($"Expected range indexable type, but found {found}", range) { }
}

public class IncorrectNumberOfArgumentsException : TypeCheckerException
{
	public IncorrectNumberOfArgumentsException(int have, int want, Range range)
		: base($"Incorrect number of arguments. Have {have}, but want {want}.", range) { }
}

public class CannotGetFromNonClassException : TypeCheckerException
{
	public CannotGetFromNonClassException(string varName, AuraType varType, string attributeName, Range range)
		: base($"Cannot get attribute from non-class. Trying to get attribute `{attributeName}` from `{varName}`, which has type `{varType}`.", range) { }
}

public class ClassAttributeDoesNotExistException : TypeCheckerException
{
	public ClassAttributeDoesNotExistException(string className, string attributeName, Range range)
		: base($"Attribute `{attributeName}` does not exist on class `{className}`.", range) { }
}

public class InvalidUseOfYieldKeywordException : TypeCheckerException
{
	public InvalidUseOfYieldKeywordException(Range range) : base("Invalid use of yield keyword. The yield keyword may only be used inside of an if expression or block to return a value from the enclosing context", range) { }
}

public class InvalidUseOfBreakKeywordException : TypeCheckerException
{
	public InvalidUseOfBreakKeywordException(Range range) : base("Invalid use of `break` keyword. The `break` keyword may only be used inside of a loop to break out of the loop's execution", range) { }
}

public class InvalidUseOfContinueKeywordException : TypeCheckerException
{
	public InvalidUseOfContinueKeywordException(Range range) : base("Invalid use of `continue` keyword. The `continue` keyword may only be used inside of a loop to immediately advance to the loop's next iteration", range) { }
}

public class CannotMixNamedAndUnnamedArgumentsException : TypeCheckerException
{
	public CannotMixNamedAndUnnamedArgumentsException(string functionName, Range range)
		: base($"Mixing named and unnamed arguments for function `{functionName}` is not permitted.", range) { }
}

public class MustSpecifyValueForArgumentWithoutDefaultValueException : TypeCheckerException
{
	public MustSpecifyValueForArgumentWithoutDefaultValueException(string functionName, string argument, Range range)
		: base($"Argument `{argument}` in call to `{functionName}` does not have a default value specified, so a value must be specified.", range) { }
}

public class MustSpecifyInitialValueForNonDefaultableTypeException : TypeCheckerException
{
	public MustSpecifyInitialValueForNonDefaultableTypeException(AuraType typ, Range range)
		: base($"The type `{typ}` does not have a default value specified, so an initial value must be provided.", range) { }
}

public class UnknownVariableException : TypeCheckerException
{
	public UnknownVariableException(string varName, Range range)
		: base($"Unknown variable `{varName}`.", range) { }
}

public class CannotImplementNonInterfaceException : TypeCheckerException
{
	public CannotImplementNonInterfaceException(string name, Range range)
		: base($"`{name}` is not an interface, so it cannot be implemented.", range) { }
}

public class MissingInterfaceMethodException : TypeCheckerException
{
	public MissingInterfaceMethodException(string interfaceName, string missingMethod, Range range)
		: base($"All implementors of `{interfaceName}` must implement the method `{missingMethod}`.", range) { }
}

public class CannotSetOnNonClassException : TypeCheckerException
{
	public CannotSetOnNonClassException(AuraType typ, Range range) : base($"Trying to set a value on type {typ}, which is not permitted. ", range) { }
}

public class CannotIncrementNonNumberException : TypeCheckerException
{
	public CannotIncrementNonNumberException(AuraType found, Range range) : base($"Cannot increment non-number type. Expected either int or float, but found {found}", range) { }
}

public class CannotDecrementNonNumberException : TypeCheckerException
{
	public CannotDecrementNonNumberException(AuraType found, Range range) : base($"Cannot decrement non-number. Expected either int or float, but found {found}", range) { }
}

public class DirectoryCannotContainMultipleModulesException : TypeCheckerException
{
	public DirectoryCannotContainMultipleModulesException(List<string> found, Range range) : base($"Directory cannot contain multiple modules. Expected only one module name, but found [{string.Join(", ", found)}]", range) { }
}

public class InvalidUseOfCheckKeywordException : TypeCheckerException
{
	public InvalidUseOfCheckKeywordException(Range range) : base("Invalid use of `check` keyword. The `check` keyword may only be used with function calls whose return type is `Result`", range) { }
}

public class CannotMixTypeAnnotationsException : TypeCheckerException
{
	public CannotMixTypeAnnotationsException(Range range) : base("Cannot mix type annotations in `let` statement", range) { }
}
