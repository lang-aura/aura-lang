using AuraLang.Types;

namespace AuraLang.TypeChecker;

/// <summary>
/// The Type Checker's representation of a local variable in the Aura source code
/// </summary>
/// <param name="Name">The variable's name</param>
/// <param name="Kind">The variable's type</param>
/// <param name="Scope">The scope where the variable was declared</param>
/// <param name="Defining">The defining scope where the variable was declared -- should
/// be either a class or a module. If null, the local variable was defined in the current
/// module, but outside of a class</param>
public record struct Local(string Name, AuraType Kind, int Scope, string? Defining);
