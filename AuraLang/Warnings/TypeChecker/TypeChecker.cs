namespace AuraLang;

public class TypeCheckerWarning : AuraWarning
{
	protected TypeCheckerWarning(string message, int line) : base(message, line) { }
}

public class UnusedVariableWarning : TypeCheckerWarning
{
	public UnusedVariableWarning(string varName, int line) : base($"Variable {varName} not used", line) { }
}
