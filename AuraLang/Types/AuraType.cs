using AuraLang.Shared;

namespace AuraLang.Types;

public abstract class AuraType
{
    public abstract bool IsSameType(AuraType other);
    public virtual bool IsInheritingType(AuraType other) => false;
    public bool IsSameOrInheritingType(AuraType other) => IsSameType(other) || IsInheritingType(other);
}

/// <summary>
/// Represents a value whose type is unknown. This type should only be used prior to the type checking
/// stage of the compilation process. For example, when declaring a variable using Aura's short <c>:=</c>
/// syntax (i.e. <code>i := 5</code>), the type of <c>i</c> is unknown before it is type checked.
/// </summary>
public class Unknown : AuraType
{
    public string Name { get; init; }

    public Unknown(string name)
    {
        Name = name;
    }

    override public bool IsSameType(AuraType other)
    {
        return other is Unknown;
    }
}

/// <summary>
/// Used to represent the type of an Aura statement, which has no type because statements
/// do not return a value.
/// </summary>
public class None : AuraType
{
    override public bool IsSameType(AuraType other)
    {
        return other is None;
    }
}

/// <summary>
/// Represents an integer value
/// </summary>
public class Int : AuraType
{
    override public bool IsSameType(AuraType other)
    {
        return other is Int;
    }
}

/// <summary>
/// Represents a floating point value
/// </summary>
public class Float : AuraType
{
    override public bool IsSameType(AuraType other)
    {
        return other is Float;
    }
}


/// <summary>
/// Represents a string value
/// </summary>
public class String : AuraType
{
    override public bool IsSameType(AuraType other)
    {
        return other is String;
    }
}

/// <summary>
/// Represents a boolean value
/// </summary>
public class Bool : AuraType
{
    override public bool IsSameType(AuraType other)
    {
        return other is Bool;
    }
}

/// <summary>
/// Represents a resizable array of elements, all of which must have the same type
/// </summary>
public class List : AuraType
{
    /// <summary>
    /// The type of the elements in the list
    /// </summary>
    public AuraType Kind { get; init; }

    public List(AuraType kind)
    {
        Kind = kind;
    }

    override public bool IsSameType(AuraType other)
    {
        return other is List list && Kind.IsSameType(list.Kind);
    }
}

/// <summary>
/// Represents an Aura function
/// </summary>
public class Function : AuraType
{
    public string Name { get; init; }
    public AnonymousFunction F { get; init; }

    public Function(string name, AnonymousFunction f)
    {
        Name = name;
        F = f;
    }

    override public bool IsSameType(AuraType other)
    {
        return other is Function;
    }
}

/// <summary>
/// Represents an anonymous function in Aura, which is basically just a named function
/// without a name
/// </summary>
public class AnonymousFunction : AuraType
{
    public List<ParamType> Params { get; init; }
    public AuraType ReturnType { get; init; }

    public AnonymousFunction(List<ParamType> params_, AuraType returnType)
    {
        Params = params_;
        ReturnType = returnType;
    }

    override public bool IsSameType(AuraType other)
    {
        return other is AnonymousFunction;
    }
}

/// <summary>
/// Represents a class type in Aura. Classes have their own type signature as well as zero or more
/// methods, each of which also have their own type.
/// </summary>
public class Class : AuraType
{
    public string Name { get; init; }
    public List<string> ParamNames { get; init; }
    public List<ParamType> ParamTypes { get; init; }
    public List<Function> Methods { get; init; }

    public Class(string name, List<string> paramNames, List<ParamType> paramTypes, List<Function> methods)
    {
        Name = name;
        ParamNames = paramNames;
        ParamTypes = paramTypes;
        Methods = methods;
    }

    override public bool IsSameType(AuraType other)
    {
        return other is Class;
    }
}

/// <summary>
/// Represents an Aura module, which includes zero or more public functions capable of being
/// called outside of their defining module. Each Aura source file begins with a <c>mod</c> statement,
/// which establishes the module's name. Any functions declared in that source file are considered
/// part of the same module.
/// </summary>
public class Module : AuraType
{
    public string Name { get; init; }
    public List<Function> PublicFunctions { get; init; }

    public Module(string name, List<Function> publicFunctions)
    {
        Name = name;
        PublicFunctions = publicFunctions;
    }

    public override bool IsSameType(AuraType other)
    {
        return other is Module;
    }
}

/// <summary>
/// Represents a type with no return value. This type is used for expressions that do not return a value.
/// This t ype differs from <see cref="Unknown"/> in that <c>Nil</c> indicates the type is known to not
/// exist, whereas <c>Unknown</c> indicates that the type is not yet known.
/// </summary>
public class Nil : AuraType
{
    public override bool IsSameType(AuraType other) => other is Nil;
}

/// <summary>
/// Represents the parent type of all other types in Aura
/// </summary>
public class Any : AuraType
{
    public override bool IsInheritingType(AuraType other) => true;
    public override bool IsSameType(AuraType other) => other is Any;
}

/// <summary>
/// Represents a single character, and is denoted in Aura programs by a single character surrounded
/// with single quotes.
/// </summary>
public class Char : AuraType
{
    public override bool IsSameType(AuraType other) => other is Char;
}

/// <summary>
/// Represents a data type containing a series of key-value pairs. All the keys must have the same
/// type and all the values must have the same type.
/// </summary>
public class Map : AuraType
{
    public AuraType Key { get; init; }
    public AuraType Value { get; init; }

    public Map(AuraType key, AuraType value)
    {
        Key = key;
        Value = value;
    }

    public override bool IsSameType(AuraType other) => other is Map;
}

/// <summary>
/// Represents an ordered collection of elements. Unlike a list, the elements of a tuple do not all need to have
/// the same type.
/// </summary>
public class Tuple : AuraType
{
    public List<AuraType> ElementTypes { get; init; }

    public Tuple(List<AuraType> elementTypes)
    {
        ElementTypes = elementTypes;
    }

    public override bool IsSameType(AuraType other) => other is Tuple;
}