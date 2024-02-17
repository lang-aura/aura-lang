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
	public UnexpectedTypeException(Range range) : base("Unexpected type", range) { }
}

public class ExpectIterableException : TypeCheckerException
{
	public ExpectIterableException(Range range) : base("Expect iterable", range) { }
}

public class TypeMismatchException : TypeCheckerException
{
	public TypeMismatchException(Range range) : base("Type mismatch", range) { }
}

public class MismatchedUnaryOperatorAndOperandException : TypeCheckerException
{
	public MismatchedUnaryOperatorAndOperandException(string unaryOperator, AuraType operandType, Range range)
		: base($"Mismatched unary operator and operand. Operator `{unaryOperator}` not valid with type {operandType}.", range) { }
}

public class ExpectIndexableException : TypeCheckerException
{
	public ExpectIndexableException(Range range) : base("Expect indexable", range) { }
}

public class ExpectRangeIndexableException : TypeCheckerException
{
	public ExpectRangeIndexableException(Range range) : base("Expect range indexable", range) { }
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
	public InvalidUseOfYieldKeywordException(Range range) : base("Invalid use of yield keyword", range) { }
}

public class InvalidUseOfBreakKeywordException : TypeCheckerException
{
	public InvalidUseOfBreakKeywordException(Range range) : base("Invalid use of break keyword", range) { }
}

public class InvalidUseOfContinueKeywordException : TypeCheckerException
{
	public InvalidUseOfContinueKeywordException(Range range) : base("Invalid use of continue keyword", range) { }
}

public class CannotMixNamedAndUnnamedArgumentsException : TypeCheckerException
{
	public CannotMixNamedAndUnnamedArgumentsException(string functionName, Range range)
		: base($"Cannot mix named and unnamed arguments for function `{functionName}`.", range) { }
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
	public CannotSetOnNonClassException(Range range) : base("Cannot set on non-class", range) { }
}

public class CannotIncrementNonNumberException : TypeCheckerException
{
	public CannotIncrementNonNumberException(Range range) : base("Cannot increment non-number", range) { }
}

public class CannotDecrementNonNumberException : TypeCheckerException
{
	public CannotDecrementNonNumberException(Range range) : base("Cannot decrement non-number", range) { }
}

public class DirectoryCannotContainMultipleModulesException : TypeCheckerException
{
	public DirectoryCannotContainMultipleModulesException(Range range) : base("Directory cannot contain multiple modules", range) { }
}

public class InvalidUseOfCheckKeywordException : TypeCheckerException
{
	public InvalidUseOfCheckKeywordException(Range range) : base("Invalid use of `check` keyword", range) { }
}

public class CannotMixTypeAnnotationsException : TypeCheckerException
{
	public CannotMixTypeAnnotationsException(Range range) : base("Cannot mix type annotations", range) { }
}
