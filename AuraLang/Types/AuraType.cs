using AuraLang.AST;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.TypeChecker;

namespace AuraLang.Types;

public abstract class AuraType
{
    public abstract bool IsSameType(AuraType other);
    public virtual bool IsInheritingType(AuraType other) => false;
    public bool IsSameOrInheritingType(AuraType other) => IsSameType(other) || IsInheritingType(other);
    public abstract override string ToString();
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

    public override bool IsSameType(AuraType other)
    {
        return other is Unknown;
    }

    public override string ToString() => "unknown";
}

/// <summary>
/// Used to represent the type of an Aura statement, which has no type because statements
/// do not return a value.
/// </summary>
public class None : AuraType
{
    public override bool IsSameType(AuraType other)
    {
        return other is None;
    }

    public override string ToString() => "none";
}

/// <summary>
/// Represents an integer value
/// </summary>
public class Int : AuraType, IDefaultable
{
    public override bool IsSameType(AuraType other)
    {
        return other is Int;
    }

    public override string ToString() => "int";
    public ITypedAuraExpression Default(int line) => new IntLiteral(0, line);
}

/// <summary>
/// Represents a floating point value
/// </summary>
public class Float : AuraType, IDefaultable
{
    public override bool IsSameType(AuraType other)
    {
        return other is Float;
    }

    public override string ToString() => "float";
    public ITypedAuraExpression Default(int line) => new FloatLiteral(0.0, line);
}


/// <summary>
/// Represents a string value
/// </summary>
public class String : AuraType, IIterable, IIndexable, IRangeIndexable, IDefaultable
{
    public override bool IsSameType(AuraType other)
    {
        return other is String;
    }

    public AuraType GetIterType() => new Char();
    public override string ToString() => "string";
    public AuraType IndexingType() => new Int();
    public AuraType GetIndexedType() => new Char();
    public AuraType GetRangeIndexedType() => new String();
    public ITypedAuraExpression Default(int line) => new StringLiteral(string.Empty, line);
}

/// <summary>
/// Represents a boolean value
/// </summary>
public class Bool : AuraType, IDefaultable
{
    public override bool IsSameType(AuraType other)
    {
        return other is Bool;
    }

    public override string ToString() => "bool";
    public ITypedAuraExpression Default(int line) => new BoolLiteral(false, line);
}

/// <summary>
/// Represents a resizable array of elements, all of which must have the same type
/// </summary>
public class List : AuraType, IIterable, IIndexable, IRangeIndexable, IDefaultable
{
    /// <summary>
    /// The type of the elements in the list
    /// </summary>
    private AuraType Kind { get; init; }

    public List(AuraType kind)
    {
        Kind = kind;
    }

    public override bool IsSameType(AuraType other)
    {
        return other is List list && Kind.IsSameType(list.Kind);
    }

    public AuraType GetIterType() => Kind;
    public override string ToString() => $"[]{Kind}";
    public AuraType IndexingType() => new Int();
    public AuraType GetIndexedType() => Kind;
    public AuraType GetRangeIndexedType() => new List(Kind);

    public ITypedAuraExpression Default(int line) =>
        new ListLiteral(new List<IAuraAstNode>(), new List(Kind), line);
}

/// <summary>
/// Represents an Aura function
/// </summary>
public class Function : AuraType, ICallable
{
    public string Name { get; init; }
    private AnonymousFunction F { get; init; }

    public Function(string name, AnonymousFunction f)
    {
        Name = name;
        F = f;
    }

    public override bool IsSameType(AuraType other)
    {
        return other is Function;
    }
    
    public override string ToString() => "function";
    public List<Param> GetParams() => F.Params;
    public List<ParamType> GetParamTypes() => F.GetParamTypes();
    public AuraType GetReturnType() => F.ReturnType;
    public int GetParamIndex(string name) => F.GetParamIndex(name);
    public bool HasVariadicParam() => F.HasVariadicParam();
}

/// <summary>
/// Represents an anonymous function in Aura, which is basically just a named function
/// without a name
/// </summary>
public class AnonymousFunction : AuraType, ICallable
{
    public List<Param> Params { get; }
    public AuraType ReturnType { get; }

    public AnonymousFunction(List<Param> fParams, AuraType returnType)
    {
        Params = fParams;
        ReturnType = returnType;
    }

    public override bool IsSameType(AuraType other)
    {
        return other is AnonymousFunction;
    }

    public override string ToString()
    {
        var pt = Params
            .Select(p => p.ToString())
            .Aggregate("", (prev, curr) => $"{prev}, {curr}");
        return $"fn({pt}) -> {ReturnType}";
    }

    public List<Param> GetParams() => Params;
    public List<ParamType> GetParamTypes() => Params.Select(p => p.ParamType).ToList();
    public AuraType GetReturnType() => ReturnType;
    public int GetParamIndex(string name) => Params.FindIndex(p => p.Name.Value == name);
    public bool HasVariadicParam() => Params.Any(p => p.ParamType.Variadic);
}

/// <summary>
/// Represents a class type in Aura. Classes have their own type signature as well as zero or more
/// methods, each of which also have their own type.
/// </summary>
public class Class : AuraType, IGettable
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

    public override bool IsSameType(AuraType other)
    {
        return other is Class;
    }

    public override string ToString() => "class";

    /// <summary>
    /// Fetches an attribute of the class matching the provided name
    /// </summary>
    /// <param name="name">The name of the attribute to fetch</param>
    /// <returns>The class's attribute matching the provided name, if one exists, else null</returns>
    public AuraType? Get(string name)
    {
        // Check if attribute is a param
        try
        {
            return ParamNames
                .Zip(ParamTypes)
                .First(item => item.First == name)
                .Second.Typ;
        }
        catch (InvalidOperationException)
        {
            // Check if attribute is a method
            try
            {
                return Methods.First(m => m.Name == name);
            }
            catch (InvalidOperationException)
            {
                // If the attribute is neither a param nor a method, return null
                return null;
            }
        }
    }
}

/// <summary>
/// Represents an Aura module, which includes zero or more public functions capable of being
/// called outside of their defining module. Each Aura source file begins with a <c>mod</c> statement,
/// which establishes the module's name. Any functions declared in that source file are considered
/// part of the same module.
/// </summary>
public class Module : AuraType, IGettable
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

    public override string ToString() => "module";

    public AuraType? Get(string attribute) => PublicFunctions.First(f => f.Name == attribute);
}

/// <summary>
/// Represents a type with no return value. This type is used for expressions that do not return a value.
/// This type differs from <see cref="Unknown"/> in that <c>Nil</c> indicates the type is known to not
/// exist, whereas <c>Unknown</c> indicates that the type is not yet known.
/// </summary>
public class Nil : AuraType
{
    public override bool IsSameType(AuraType other) => other is Nil;
    public override string ToString() => "nil";
}

/// <summary>
/// Represents the parent type of all other types in Aura
/// </summary>
public class Any : AuraType
{
    public override bool IsInheritingType(AuraType other) => true;
    public override bool IsSameType(AuraType other) => other is Any;
    public override string ToString() => "any";
}

/// <summary>
/// Represents a single character, and is denoted in Aura programs by a single character surrounded
/// with single quotes.
/// </summary>
public class Char : AuraType
{
    public override bool IsSameType(AuraType other) => other is Char;
    public override string ToString() => "byte";
}

/// <summary>
/// Represents a data type containing a series of key-value pairs. All the keys must have the same
/// type and all the values must have the same type.
/// </summary>
public class Map : AuraType, IIndexable, IDefaultable
{
    public AuraType Key { get; init; }
    public AuraType Value { get; init; }

    public Map(AuraType key, AuraType value)
    {
        Key = key;
        Value = value;
    }

    public override bool IsSameType(AuraType other) => other is Map;
    public override string ToString() => $"map[{Key}]{Value}";
    public AuraType IndexingType() => Key;
    public AuraType GetIndexedType() => Value;

    public ITypedAuraExpression Default(int line) =>
        new MapLiteral(
            new Dictionary<IAuraAstNode, IAuraAstNode>(), Key, Value, line);
}
