using AuraLang.Types;

namespace AuraLang.Exceptions.TypeChecker;

public abstract class TypeCheckerException : AuraException
{
	protected TypeCheckerException(string message, int line) : base(message, line) { }
}

public class UnknownStatementTypeException : TypeCheckerException
{
	public UnknownStatementTypeException(int line) : base("Unknown statement type", line) { }
}

public class UnknownExpressionTypeException : TypeCheckerException
{
	public UnknownExpressionTypeException(int line) : base("Unknown expression type", line)
	{
	}
}

public class UnexpectedTypeException : TypeCheckerException
{
	public UnexpectedTypeException(int line) : base("Unexpected type", line) { }
}

public class ExpectIterableException : TypeCheckerException
{
	public ExpectIterableException(int line) : base("Expect iterable", line) { }
}

public class TypeMismatchException : TypeCheckerException
{
	public TypeMismatchException(int line) : base("Type mismatch", line) { }
}

public class MismatchedUnaryOperatorAndOperandException : TypeCheckerException
{
	public MismatchedUnaryOperatorAndOperandException(string unaryOperator, AuraType operandType, int line)
		: base($"Mismatched unary operator and operand. Operator `{unaryOperator}` not valid with type {operandType}.", line) { }
}

public class ExpectIndexableException : TypeCheckerException
{
	public ExpectIndexableException(int line) : base("Expect indexable", line) { }
}

public class ExpectRangeIndexableException : TypeCheckerException
{
	public ExpectRangeIndexableException(int line) : base("Expect range indexable", line) { }
}

public class IncorrectNumberOfArgumentsException : TypeCheckerException
{
	public IncorrectNumberOfArgumentsException(int have, int want, int line)
		: base($"Incorrect number of arguments. Have {have}, but want {want}.", line) { }
}

public class CannotGetFromNonClassException : TypeCheckerException
{
	public CannotGetFromNonClassException(string varName, AuraType varType, string attributeName, int line)
		: base($"Cannot get attribute from non-class. Trying to get attribute `{attributeName}` from `{varName}`, which has type `{varType}`.", line) { }
}

public class ClassAttributeDoesNotExistException : TypeCheckerException
{
	public ClassAttributeDoesNotExistException(string className, string attributeName, int line)
		: base($"Attribute `{attributeName}` does not exist on class `{className}`.", line) { }
}

public class InvalidUseOfYieldKeywordException : TypeCheckerException
{
	public InvalidUseOfYieldKeywordException(int line) : base("Invalid use of yield keyword", line) { }
}

public class InvalidUseOfBreakKeywordException : TypeCheckerException
{
	public InvalidUseOfBreakKeywordException(int line) : base("Invalid use of break keyword", line) { }
}

public class InvalidUseOfContinueKeywordException : TypeCheckerException
{
	public InvalidUseOfContinueKeywordException(int line) : base("Invalid use of continue keyword", line) { }
}

public class CannotMixNamedAndUnnamedArgumentsException : TypeCheckerException
{
	public CannotMixNamedAndUnnamedArgumentsException(string functionName, int line)
		: base($"Cannot mix named and unnamed arguments for function `{functionName}`.", line) { }
}

public class MustSpecifyValueForArgumentWithoutDefaultValueException : TypeCheckerException
{
	public MustSpecifyValueForArgumentWithoutDefaultValueException(string functionName, string argument, int line)
		: base($"Argument `{argument}` in call to `{functionName}` does not have a default value specified, so a value must be specified.", line) { }
}

public class MustSpecifyInitialValueForNonDefaultableTypeException : TypeCheckerException
{
	public MustSpecifyInitialValueForNonDefaultableTypeException(AuraType typ, int line)
		: base($"The type `{typ}` does not have a default value specified, so an initial value must be provided.", line) { }
}

public class UnknownVariableException : TypeCheckerException
{
	public UnknownVariableException(string varName, int line)
		: base($"Unknown variable `{varName}`.", line) { }
}

public class CannotImplementNonInterfaceException : TypeCheckerException
{
	public CannotImplementNonInterfaceException(string name, int line)
		: base($"`{name}` is not an interface, so it cannot be implemented.", line) { }
}

public class MissingInterfaceMethodException : TypeCheckerException
{
	public MissingInterfaceMethodException(string interfaceName, string missingMethod, int line)
		: base($"All implementors of `{interfaceName}` must implement the method `{missingMethod}`.", line) { }
}

public class CannotSetOnNonClassException : TypeCheckerException
{
	public CannotSetOnNonClassException(int line) : base("Cannot set on non-class", line) { }
}
