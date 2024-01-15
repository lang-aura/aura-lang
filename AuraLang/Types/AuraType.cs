using AuraLang.AST;
using AuraLang.Shared;

namespace AuraLang.Types;

public abstract class AuraType
{
	public virtual bool IsEqual(AuraType other) => IsSameType(other);
	public abstract bool IsSameType(AuraType other);
	public virtual bool IsInheritingType(AuraType other) => false;
	public bool IsSameOrInheritingType(AuraType other) => IsSameType(other) || IsInheritingType(other);
	public abstract override string ToString();

	public override bool Equals(object? obj)
	{
		if (obj is null) return false;
		if (obj is not AuraType typ) return false;
		return IsSameType(typ);
	}

	protected bool CompareParamsForEquality(List<Param> left, List<Param> right)
	{
		var filteredLeft = FilterParams(left);
		var filteredRight = FilterParams(right);
		return filteredLeft.SequenceEqual(filteredRight);
	}

	private IEnumerable<(string, AuraType, bool)> FilterParams(List<Param> parameters) =>
		parameters.Select(p => (p.Name.Value, p.ParamType.Typ, p.ParamType.Variadic));

	public override int GetHashCode() => ToString().GetHashCode();
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

	public override bool IsSameType(AuraType other) => other is Unknown;

	public override string ToString() => "unknown";
}

/// <summary>
/// Used to represent the type of an Aura statement, which has no type because statements
/// do not return a value.
/// </summary>
public class None : AuraType
{
	public override bool IsSameType(AuraType other) => other is None;

	public override string ToString() => "none";
}

/// <summary>
/// Represents an integer value
/// </summary>
public class Int : AuraType, IDefaultable
{
	public override bool IsSameType(AuraType other) => other is Int;

	public override string ToString() => "int";
	public ITypedAuraExpression Default(int line) => new IntLiteral(0, line);
}

/// <summary>
/// Represents a floating point value
/// </summary>
public class Float : AuraType, IDefaultable
{
	public override bool IsSameType(AuraType other) => other is Float;

	public override string ToString() => "float";
	public ITypedAuraExpression Default(int line) => new FloatLiteral(0.0, line);
}

/// <summary>
/// Represents a string value
/// </summary>
public class String : AuraType, IIterable, IIndexable, IRangeIndexable, IDefaultable
{
	public override bool IsSameType(AuraType other) => other is String;

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
	public override bool IsSameType(AuraType other) => other is Bool;

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
	private AuraType Kind { get; }

	public List(AuraType kind)
	{
		Kind = kind;
	}

	public override bool IsEqual(AuraType other) => other is List list && Kind.IsSameType(list.Kind);

	public override bool IsSameType(AuraType other) => other is List list;

	public AuraType GetIterType() => Kind;
	public override string ToString() => $"[]{Kind}";
	public AuraType IndexingType() => new Int();
	public AuraType GetIndexedType() => Kind;
	public AuraType GetRangeIndexedType() => new List(Kind);

	public ITypedAuraExpression Default(int line) =>
		new ListLiteral<ITypedAuraExpression>(new List<ITypedAuraExpression>(), new List(Kind), line);
}

/// <summary>
/// Represents an Aura function
/// </summary>
public class NamedFunction : AuraType, ICallable
{
	public string Name { get; }
	public Visibility Public { get; }
	private Function F { get; }

	public NamedFunction(string name, Visibility pub, Function f)
	{
		Name = name;
		Public = pub;
		F = f;
	}

	public override bool IsEqual(AuraType other) => other is NamedFunction f && Name == f.Name && F.IsSameType(f.F);

	public override bool IsSameType(AuraType other) => other is NamedFunction;

	public override string ToString()
	{
		var name = Public == Visibility.Public
			? Name.ToUpper()
			: Name.ToLower();
		var pt = string.Join(", ", F.Params
			.Select(p => $"{p.Name.Value} {p.ParamType.Typ}"));
		return $"func {name}({pt}){(F.ReturnType is not Nil ? F.ReturnType : "")}";
	}

	public string ToStringInterface()
	{
		var name = Public == Visibility.Public
			? Name.ToUpper()
			: Name.ToLower();
		var pt = string.Join(", ", F.Params
			.Select(p => $"{p.Name.Value} {p.ParamType.Typ}"));
		return $"{name}({pt}) {(F.ReturnType.IsSameType(new Nil()) ? string.Empty : F.ReturnType)}";
	}

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
public class Function : AuraType, ICallable
{
	public List<Param> Params { get; }
	public AuraType ReturnType { get; }

	public Function(List<Param> fParams, AuraType returnType)
	{
		Params = fParams;
		ReturnType = returnType;
	}

	public override bool IsEqual(AuraType other) => other is Function f && CompareParamsForEquality(Params, f.Params) &&
													ReturnType.IsSameType(f.ReturnType);

	public override bool IsSameType(AuraType other) => other is Function;

	public override string ToString()
	{
		var pt = string.Join(", ", Params
			.Select(p => $"{p.Name.Value} {p.ParamType.Typ}"));
		return $"func({pt}) {ReturnType}";
	}

	public List<Param> GetParams() => Params;
	public List<ParamType> GetParamTypes() => Params.Select(p => p.ParamType).ToList();
	public AuraType GetReturnType() => ReturnType;
	public int GetParamIndex(string name) => Params.FindIndex(p => p.Name.Value == name);
	public bool HasVariadicParam() => Params.Any(p => p.ParamType.Variadic);
}

public class Interface : AuraType, IGettable
{
	public Visibility Public { get; }
	public string Name { get; init; }
	public List<NamedFunction> Functions { get; init; }

	public Interface(string name, List<NamedFunction> functions, Visibility pub)
	{
		Name = name;
		Functions = functions;
		Public = pub;
	}

	public override bool IsEqual(AuraType other) =>
		other is Interface i && Name == i.Name && Functions.SequenceEqual(i.Functions);

	public override bool IsSameType(AuraType other) => other is Interface;

	public override bool IsInheritingType(AuraType other)
	{
		if (other is not Class c) return false;
		return c.Implementing.Contains(this);
	}

	public override string ToString() => Public == Visibility.Public ? Name.ToUpper() : Name.ToLower();

	public override bool Equals(object? obj)
	{
		if (obj is null) return false;
		if (obj is not Interface i) return false;
		return Name == i.Name &&
			   Functions == i.Functions;
	}

	public AuraType? Get(string attribute)
	{
		return Functions.First(f => f.Name == attribute);
	}

	public override int GetHashCode() => base.GetHashCode();
}

/// <summary>
/// Represents a class type in Aura. Classes have their own type signature as well as zero or more
/// methods, each of which also have their own type.
/// </summary>
public class Class : AuraType, IGettable, ICallable
{
	public Visibility Public { get; }
	public string Name { get; init; }
	public List<Param> Parameters { get; }
	public List<NamedFunction> Methods { get; }
	public List<Interface> Implementing { get; }

	public Class(string name, List<Param> parameters, List<NamedFunction> methods, List<Interface> implementing,
		Visibility pub)
	{
		Name = name;
		Parameters = parameters;
		Methods = methods;
		Implementing = implementing;
		Public = pub;
	}

	public override bool IsEqual(AuraType other) => other is Class c && Name == c.Name &&
													Parameters.SequenceEqual(c.Parameters) &&
													Methods.SequenceEqual(c.Methods);

	public override bool IsSameType(AuraType other) => other is Class;

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
			return Parameters.First(p => p.Name.Value == name).ParamType.Typ;
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

	public List<Param> GetParams() => Parameters;

	public List<ParamType> GetParamTypes() => Parameters.Select(p => p.ParamType).ToList();

	public AuraType GetReturnType() => this;

	public int GetParamIndex(string name) => Parameters.FindIndex(p => p.Name.Value == name);

	public bool HasVariadicParam() => Parameters.Any(p => p.ParamType.Variadic);
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
	public List<NamedFunction> PublicFunctions { get; init; }
	public List<Class> PublicClasses { get; init; }
	public Dictionary<string, ITypedAuraExpression> PublicVariables { get; init; }

	public Module(string name, List<NamedFunction> publicFunctions, List<Class> publicClasses,
		Dictionary<string, ITypedAuraExpression> publicVariables)
	{
		Name = name;
		PublicFunctions = publicFunctions;
		PublicClasses = publicClasses;
		PublicVariables = publicVariables;
	}

	public override bool IsEqual(AuraType other) =>
		other is Module m && Name == m.Name && PublicFunctions.SequenceEqual(m.PublicFunctions);

	public override bool IsSameType(AuraType other) => other is Module;

	public override string ToString() => "module";

	public AuraType? Get(string attribute)
	{
		try
		{
			// Check if attribute is a function
			return PublicFunctions.First(f => f.Name == attribute);
		}
		catch (InvalidOperationException)
		{
			try
			{
				// Check if attribute is a class
				return PublicClasses.First(c => c.Name == attribute);
			}
			catch (InvalidOperationException)
			{
				try
				{
					// Check if attribute is a variable
					return PublicVariables.First(v => v.Key == attribute).Value.Typ;
				}
				catch (InvalidOperationException)
				{
					return null;
				}
			}
		}
	}
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
	public AuraType Key { get; }
	public AuraType Value { get; }

	public Map(AuraType key, AuraType value)
	{
		Key = key;
		Value = value;
	}

	public override bool IsEqual(AuraType other) => other is Map m && Key.IsSameOrInheritingType(m.Key) &&
													Value.IsSameOrInheritingType(m.Value);

	public override bool IsSameType(AuraType other) => other is Map;
	public override string ToString() => $"map[{Key}]{Value}";
	public AuraType IndexingType() => Key;
	public AuraType GetIndexedType() => Value;

	public ITypedAuraExpression Default(int line) =>
		new MapLiteral<ITypedAuraExpression, ITypedAuraExpression>(
			new Dictionary<ITypedAuraExpression, ITypedAuraExpression>(), Key, Value, line);
}

public class Error : AuraType
{
	public string? Message { get; }

	public Error(string message)
	{
		Message = message;
	}

	public Error() { }

	public override bool IsSameType(AuraType other) => other is Error;
	public override string ToString() => Message ?? string.Empty;
}
