using AuraLang.Types;

namespace AuraLang.TypeChecker;

/// <summary>
/// The Type Checker's representation of a local variable in the Aura source code
/// </summary>
/// <param name="Name">The variable's name</param>
/// <param name="Kind">The variable's type</param>
/// <param name="Scope">The scope where the variable was declared</param>
/// <param name="Module">The module where the variable was declared</param>
public record struct Local(string Name, AuraType Kind, int Scope, string Module);
