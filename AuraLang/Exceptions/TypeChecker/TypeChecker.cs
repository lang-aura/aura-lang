﻿namespace AuraLang.Exceptions.TypeChecker;

public abstract class TypeCheckerException : AuraException
{
    public TypeCheckerException(string message, int line) : base(message, line) { }
}

public class UnknownStatementTypeException : TypeCheckerException
{
    public UnknownStatementTypeException(int line) : base("Unknown statement type", line) { }
}

public class UnknownExpressionTypeException : TypeCheckerException
{
    public UnknownExpressionTypeException(int line) : base("Unknown expression type", line) { }
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
    public MismatchedUnaryOperatorAndOperandException(int line) : base("mismatched unary operator and operand", line) { }
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
    public IncorrectNumberOfArgumentsException(int line) : base("Incorrect number of arguments", line) { }
}

public class CannotGetFromNonClassException : TypeCheckerException
{
    public CannotGetFromNonClassException(int line) : base("Cannot get from non-class", line) { }
}

public class ClassAttributeDoesNotExistException : TypeCheckerException
{
    public ClassAttributeDoesNotExistException(int line) : base("Class attribute does not exist", line) { }
}