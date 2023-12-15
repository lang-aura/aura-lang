namespace AuraLang.Exceptions.TypeChecker;

public class TypeCheckerExceptionContainer : AuraExceptionContainer
{
	public void Add(TypeCheckerException ex)
	{
		Exs.Add(ex);
	}
}

public abstract class TypeCheckerException : AuraException
{
	protected TypeCheckerException(string message, string filePath, int line) : base(message, filePath, line) { }
}

public class UnknownStatementTypeException : TypeCheckerException
{
	public UnknownStatementTypeException(string filePath, int line) : base("Unknown statement type", filePath, line) { }
}

public class UnknownExpressionTypeException : TypeCheckerException
{
	public UnknownExpressionTypeException(string filePath, int line) : base("Unknown expression type", filePath, line)
	{
	}
}

public class UnexpectedTypeException : TypeCheckerException
{
	public UnexpectedTypeException(string filePath, int line) : base("Unexpected type", filePath, line) { }
}

public class ExpectIterableException : TypeCheckerException
{
	public ExpectIterableException(string filePath, int line) : base("Expect iterable", filePath, line) { }
}

public class TypeMismatchException : TypeCheckerException
{
	public TypeMismatchException(string filePath, int line) : base("Type mismatch", filePath, line) { }
}

public class MismatchedUnaryOperatorAndOperandException : TypeCheckerException
{
	public MismatchedUnaryOperatorAndOperandException(string filePath, int line) : base(
		"mismatched unary operator and operand", filePath, line)
	{
	}
}

public class ExpectIndexableException : TypeCheckerException
{
	public ExpectIndexableException(string filePath, int line) : base("Expect indexable", filePath, line) { }
}

public class ExpectRangeIndexableException : TypeCheckerException
{
	public ExpectRangeIndexableException(string filePath, int line) : base("Expect range indexable", filePath, line) { }
}

public class IncorrectNumberOfArgumentsException : TypeCheckerException
{
	public IncorrectNumberOfArgumentsException(string filePath, int line) : base("Incorrect number of arguments",
		filePath, line)
	{
	}
}

public class CannotGetFromNonClassException : TypeCheckerException
{
	public CannotGetFromNonClassException(string filePath, int line) : base("Cannot get from non-class", filePath, line)
	{
	}
}

public class ClassAttributeDoesNotExistException : TypeCheckerException
{
	public ClassAttributeDoesNotExistException(string filePath, int line) : base("Class attribute does not exist",
		filePath, line)
	{
	}
}

public class InvalidUseOfYieldKeywordException : TypeCheckerException
{
	public InvalidUseOfYieldKeywordException(string filePath, int line) : base("Invalid use of yield keyword", filePath,
		line)
	{
	}
}

public class InvalidUseOfBreakKeywordException : TypeCheckerException
{
	public InvalidUseOfBreakKeywordException(string filePath, int line) : base("Invalid use of break keyword", filePath,
		line)
	{
	}
}

public class InvalidUseOfContinueKeywordException : TypeCheckerException
{
	public InvalidUseOfContinueKeywordException(string filePath, int line) : base("Invalid use of continue keyword",
		filePath, line)
	{
	}
}

public class CannotMixNamedAndUnnamedArgumentsException : TypeCheckerException
{
	public CannotMixNamedAndUnnamedArgumentsException(string filePath, int line) : base(
		"Cannot mix named and unnamed arguments", filePath, line)
	{
	}
}

public class MustSpecifyValueForArgumentWithoutDefaultValueException : TypeCheckerException
{
	public MustSpecifyValueForArgumentWithoutDefaultValueException(string filePath, int line) : base(
		"Must specify value for argument without default value", filePath, line)
	{
	}
}

public class MustSpecifyInitialValueForNonDefaultableTypeException : TypeCheckerException
{
	public MustSpecifyInitialValueForNonDefaultableTypeException(string filePath, int line) : base(
		"Must specify initial value for non-defaultable type exception", filePath, line)
	{
	}
}

public class UnknownVariableException : TypeCheckerException
{
	public UnknownVariableException(string filePath, int line) : base("Unknown variable", filePath, line) { }
}

public class CannotImplementNonInterfaceException : TypeCheckerException
{
	public CannotImplementNonInterfaceException(string filePath, int line) : base("Cannot implement non-interface",
		filePath, line)
	{
	}
}

public class MissingInterfaceMethodException : TypeCheckerException
{
	public MissingInterfaceMethodException(string filePath, int line) : base("Missing interface method", filePath, line)
	{
	}
}

public class CannotSetOnNonClassException : TypeCheckerException
{
	public CannotSetOnNonClassException(string filePath, int line) : base("Cannot set on non-class", filePath, line) { }
}
