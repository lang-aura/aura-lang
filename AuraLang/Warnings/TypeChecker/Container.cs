namespace AuraLang;

public class TypeCheckerWarningContainer : AuraWarningContainer
{
    public TypeCheckerWarningContainer(string filePath) : base(filePath) { }

    public void Add(TypeCheckerWarning w)
    {
        Warnings.Add(w);
    }
}
