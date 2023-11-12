namespace AuraLang.Exceptions.Compiler;

public abstract class CompilerException : AuraException
{
    public CompilerException(string message, int line) : base(message, line) { }
}

public class UnknownStatementException : CompilerException
{
    public UnknownStatementException(int line) : base("Unknown statement", line) { }
}

public class UnknownExpressionException : CompilerException
{
    public UnknownExpressionException(int line) : base("Unknown expression", line) { }
}
