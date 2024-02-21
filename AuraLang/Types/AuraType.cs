using AuraLang.AST;
using AuraLang.Shared;
using AuraLang.Stdlib;
using AuraLang.Token;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Range = AuraLang.Location.Range;

namespace AuraLang.Types;

public abstract class AuraType
{
	public virtual bool IsEqual(AuraType other) => IsSameType(other);
	public abstract bool IsSameType(AuraType other);
	public virtual bool IsInheritingType(AuraType other) => false;
	public bool IsSameOrInheritingType(AuraType other) => IsSameType(other) || IsInheritingType(other);
	public abstract override string ToString();
	public virtual string ToAuraString() => ToString();

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
public class AuraUnknown : AuraType
{
	public string Name { get; init; }

	public AuraUnknown(string name)
	{
		Name = name;
	}

	public override bool IsSameType(AuraType other) => other is AuraUnknown;

	public override string ToString() => "unknown";
}

/// <summary>
/// Used to represent the type of an Aura statement, which has no type because statements
/// do not return a value.
/// </summary>
public class AuraNone : AuraType
{
	public override bool IsSameType(AuraType other) => other is AuraNone;

	public override string ToString() => "none";
}

/// <summary>
/// Represents an integer value
/// </summary>
public class AuraInt : AuraType, IDefaultable
{
	public override bool IsSameType(AuraType other) => other is AuraInt;

	public override string ToString() => "int";
	public ITypedAuraExpression Default(Range range)
		=> new IntLiteral(
			Int: new Tok(
				typ: TokType.IntLiteral,
				value: "0",
				range: range
			)
		);
}

/// <summary>
/// Represents a floating point value
/// </summary>
public class AuraFloat : AuraType, IDefaultable
{
	public override bool IsSameType(AuraType other) => other is AuraFloat;

	public override string ToString() => "float";
	public ITypedAuraExpression Default(Range range)
		=> new FloatLiteral(
			Float: new Tok(
				typ: TokType.Float,
				value: "0.0",
				range: range
			)
		);
}

/// <summary>
/// Represents a string value
/// </summary>
public class AuraString : AuraType, IIterable, IIndexable, IRangeIndexable, IDefaultable, IGettable, IImportableModule, ICompletable
{
	public override bool IsSameType(AuraType other) => other is AuraString;

	public AuraType GetIterType() => new AuraChar();
	public override string ToString() => "string";
	public AuraType IndexingType() => new AuraInt();
	public AuraType GetIndexedType() => new AuraChar();
	public AuraType GetRangeIndexedType() => new AuraString();
	public ITypedAuraExpression Default(Range range)
		=> new StringLiteral(
			String: new Tok(
				typ: TokType.String,
				value: string.Empty,
				range: range
			)
		);

	public AuraType? Get(string attribute)
	{
		var stringMod = AuraStdlib.GetAllModules()["aura/strings"];
		return stringMod.PublicFunctions.First(f => f.Name == attribute);
	}

	public string GetModuleName() => "strings";

	public CompletionList ProvideCompletableOptions()
	{
		// Get "strings" module's methods
		if (!AuraStdlib.TryGetModule("aura/strings", out var stringsModule)) return new CompletionList();
		var completionItems = stringsModule!.PublicFunctions.Select(f => new CompletionItem { Label = f.Name, Kind = CompletionItemKind.Function, Documentation = new MarkupContent{Value = $"```\n{f.Documentation}\n```", Kind = MarkupKind.Markdown} });
		return new CompletionList
		{
			Items = completionItems.ToArray()
		};
	}
}

/// <summary>
/// Represents a boolean value
/// </summary>
public class AuraBool : AuraType, IDefaultable
{
	public override bool IsSameType(AuraType other) => other is AuraBool;

	public override string ToString() => "bool";
	public ITypedAuraExpression Default(Range range)
		=> new BoolLiteral(
			Bool: new Tok(
				typ: TokType.Bool,
				value: "false",
				range: range
			)
		);
}

/// <summary>
/// Represents a resizable array of elements, all of which must have the same type
/// </summary>
public class AuraList : AuraType, IIterable, IIndexable, IRangeIndexable, IDefaultable, IGettable, IImportableModule
{
	/// <summary>
	/// The type of the elements in the list
	/// </summary>
	private AuraType Kind { get; }

	public AuraList(AuraType kind)
	{
		Kind = kind;
	}

	public override bool IsEqual(AuraType other) => other is AuraList list && Kind.IsSameType(list.Kind);

	public override bool IsSameType(AuraType other) => other is AuraList list;

	public AuraType GetIterType() => Kind;
	public override string ToString() => $"[]{Kind}";
	public AuraType IndexingType() => new AuraInt();
	public AuraType GetIndexedType() => Kind;
	public AuraType GetRangeIndexedType() => new AuraList(Kind);

	public ITypedAuraExpression Default(Range range) =>
		new ListLiteral<ITypedAuraExpression>(
			OpeningBracket: new Tok(
				typ: TokType.LeftBracket,
				value: "[",
				range: range
			),
			L: new List<ITypedAuraExpression>(),
			Kind: new AuraList(Kind),
			ClosingBrace: new Tok(
				typ: TokType.RightBrace,
				value: "}",
				range: range
			)
		);

	public AuraType? Get(string attribute)
	{
		var listsMod = AuraStdlib.GetAllModules()["aura/lists"];
		return listsMod.PublicFunctions.First(f => f.Name == attribute);
	}

	public string GetModuleName() => "lists";
}

/// <summary>
/// Represents an Aura function
/// </summary>
public class AuraNamedFunction : AuraType, ICallable, IDocumentable
{
	public string Name { get; }
	public Visibility Public { get; }
	private AuraFunction F { get; }

	public string Documentation
	{
		get
		{
			if (_documentation is null) return string.Empty;
			return $"{ToAuraString()}\n\n{_documentation}";
		}
	}

	private string? _documentation { get; }

	public AuraNamedFunction(string name, Visibility pub, AuraFunction f)
	{
		Name = name;
		Public = pub;
		F = f;
		_documentation = null;
	}

	public AuraNamedFunction(string name, Visibility pub, AuraFunction f, string documentation)
	{
		Name = name;
		Public = pub;
		F = f;
		_documentation = documentation;
	}

	public override bool IsEqual(AuraType other) => other is AuraNamedFunction f && Name == f.Name && F.IsSameType(f.F);

	public override bool IsSameType(AuraType other) => other is AuraNamedFunction;

	public override string ToString()
	{
		var name = Public == Visibility.Public
			? Name.ToUpper()
			: Name.ToLower();
		var @params = string.Join(", ", F.Params
			.Select(p => $"{p.Name.Value} {p.ParamType.Typ}"));
		return $"func {name}({@params}){(F.ReturnType is not AuraNil ? F.ReturnType : "")}";
	}

	public override string ToAuraString()
	{
		var pub = Public == Visibility.Public
			? "pub "
			: string.Empty;
		var @params = string.Join(", ", F.Params
			.Select(p => $"{p.Name.Value}: {p.ParamType.Typ}"));
		var returnType = F.ReturnType is not AuraNil
			? $" -> {F.ReturnType}"
			: string.Empty;
		return $"{pub}fn {Name}({@params}){returnType}";
	}

	public string ToStringInterface()
	{
		var name = Public == Visibility.Public
			? Name.ToUpper()
			: Name.ToLower();
		var @params = string.Join(", ", F.Params
			.Select(p => $"{p.Name.Value} {p.ParamType.Typ}"));
		return $"{name}({@params}) {(F.ReturnType.IsSameType(new AuraNil()) ? string.Empty : F.ReturnType)}";
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
public class AuraFunction : AuraType, ICallable
{
	public List<Param> Params { get; }
	public AuraType ReturnType { get; }

	public AuraFunction(List<Param> fParams, AuraType returnType)
	{
		Params = fParams;
		ReturnType = returnType;
	}

	public override bool IsEqual(AuraType other) => other is AuraFunction f && CompareParamsForEquality(Params, f.Params) &&
													ReturnType.IsSameType(f.ReturnType);

	public override bool IsSameType(AuraType other) => other is AuraFunction;

	public override string ToString()
	{
		var @params = string.Join(", ", Params
			.Select(p => $"{p.Name.Value} {p.ParamType.Typ}"));
		return $"func({@params}) {ReturnType}";
	}

	public List<Param> GetParams() => Params;
	public List<ParamType> GetParamTypes() => Params.Select(p => p.ParamType).ToList();
	public AuraType GetReturnType() => ReturnType;
	public int GetParamIndex(string name) => Params.FindIndex(p => p.Name.Value == name);
	public bool HasVariadicParam() => Params.Any(p => p.ParamType.Variadic);
}

public class AuraInterface : AuraType, IGettable, IDocumentable
{
	public Visibility Public { get; }
	public string Name { get; }
	public List<AuraNamedFunction> Functions { get; }
	public string Documentation { get; }

	public AuraInterface(string name, List<AuraNamedFunction> functions, Visibility pub)
	{
		Name = name;
		Functions = functions;
		Public = pub;
		Documentation = string.Empty;
	}
	
	public AuraInterface(string name, List<AuraNamedFunction> functions, Visibility pub, string documentation)
	{
		Name = name;
		Functions = functions;
		Public = pub;
		Documentation = documentation;
	}

	public override bool IsEqual(AuraType other) =>
		other is AuraInterface i && Name == i.Name && Functions.SequenceEqual(i.Functions);

	public override bool IsSameType(AuraType other) => other is AuraInterface;

	public override bool IsInheritingType(AuraType other)
	{
		if (other is not AuraClass c) return false;
		return c.Implementing.Contains(this);
	}

	public override string ToString() => Public == Visibility.Public ? Name.ToUpper() : Name.ToLower();

	public override bool Equals(object? obj)
	{
		if (obj is null) return false;
		if (obj is not AuraInterface i) return false;
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
public class AuraClass : AuraType, IGettable, ICallable, ICompletable, IDocumentable
{
	public Visibility Public { get; }
	public string Name { get; }
	public List<Param> Parameters { get; }
	public List<AuraNamedFunction> Methods { get; }
	public List<AuraInterface> Implementing { get; }
	public string Documentation { get; }

	public AuraClass(string name, List<Param> parameters, List<AuraNamedFunction> methods, List<AuraInterface> implementing,
		Visibility pub)
	{
		Name = name;
		Parameters = parameters;
		Methods = methods;
		Implementing = implementing;
		Public = pub;
		Documentation = string.Empty;
	}
	
	public AuraClass(string name, List<Param> parameters, List<AuraNamedFunction> methods, List<AuraInterface> implementing,
		Visibility pub, string documentation)
	{
		Name = name;
		Parameters = parameters;
		Methods = methods;
		Implementing = implementing;
		Public = pub;
		Documentation = documentation;
	}

	public override bool IsEqual(AuraType other) => other is AuraClass c && Name == c.Name &&
													Parameters.SequenceEqual(c.Parameters) &&
													Methods.SequenceEqual(c.Methods);

	public override bool IsSameType(AuraType other) => other is AuraClass;

	public override string ToString() => "class";

	public override string ToAuraString()
	{
		var pub = Public == Visibility.Public ? "pub " : string.Empty;
		var @params = string.Join(", ", Parameters.Select(p => $"{p.Name.Value}: {p.ParamType.Typ.ToAuraString()}"));
		return $"{pub}class {Name}({@params})";
	}

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

	public CompletionList ProvideCompletableOptions()
	{
		var completionItems = Methods.Select(m => new CompletionItem { Label = m.Name, Kind = CompletionItemKind.Function, Documentation = m.Documentation });
		return new CompletionList
		{
			Items = completionItems.ToArray()
		};
	}
}

/// <summary>
/// Represents an Aura module, which includes zero or more public functions capable of being
/// called outside of their defining module. Each Aura source file begins with a <c>mod</c> statement,
/// which establishes the module's name. Any functions declared in that source file are considered
/// part of the same module.
/// </summary>
public class AuraModule : AuraType, IGettable, ICompletable
{
	public string Name { get; }
	public List<AuraNamedFunction> PublicFunctions { get; }
	public List<AuraInterface> PublicInterfaces { get; }
	public List<AuraClass> PublicClasses { get; }
	public Dictionary<string, ITypedAuraExpression> PublicVariables { get; }

	public AuraModule(string name, List<AuraNamedFunction> publicFunctions, List<AuraInterface> publicInterfaces,
		List<AuraClass> publicClasses, Dictionary<string, ITypedAuraExpression> publicVariables)
	{
		Name = name;
		PublicFunctions = publicFunctions;
		PublicInterfaces = publicInterfaces;
		PublicClasses = publicClasses;
		PublicVariables = publicVariables;
	}

	public override bool IsEqual(AuraType other) =>
		other is AuraModule m && Name == m.Name && PublicFunctions.SequenceEqual(m.PublicFunctions);

	public override bool IsSameType(AuraType other) => other is AuraModule;

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

	public CompletionList ProvideCompletableOptions()
	{
		Console.Error.WriteLine($"getting module's completable items: {Name}");
		// Get stdlib module
		if (!AuraStdlib.TryGetModule("Name", out var module)) return new CompletionList();
		var completionItems = module!.PublicFunctions.Select(f => new CompletionItem { Label = f.Name, Kind = CompletionItemKind.Function, Documentation = f.Documentation });
		return new CompletionList { Items = completionItems.ToArray() };
	}
}

/// <summary>
/// Represents a type with no return value. This type is used for expressions that do not return a value.
/// This type differs from <see cref="AuraUnknown"/> in that <c>Nil</c> indicates the type is known to not
/// exist, whereas <c>Unknown</c> indicates that the type is not yet known.
/// </summary>
public class AuraNil : AuraType
{
	public override bool IsSameType(AuraType other) => other is AuraNil;
	public override string ToString() => "nil";
}

/// <summary>
/// Represents the parent type of all other types in Aura
/// </summary>
public class AuraAny : AuraType
{
	public override bool IsInheritingType(AuraType other) => true;
	public override bool IsSameType(AuraType other) => other is AuraAny;
	public override string ToString() => "any";
}

/// <summary>
/// Represents a single character, and is denoted in Aura programs by a single character surrounded
/// with single quotes.
/// </summary>
public class AuraChar : AuraType
{
	public override bool IsSameType(AuraType other) => other is AuraChar;
	public override string ToString() => "byte";
}

/// <summary>
/// Represents a data type containing a series of key-value pairs. All the keys must have the same
/// type and all the values must have the same type.
/// </summary>
public class AuraMap : AuraType, IIndexable, IDefaultable
{
	public AuraType Key { get; }
	public AuraType Value { get; }

	public AuraMap(AuraType key, AuraType value)
	{
		Key = key;
		Value = value;
	}

	public override bool IsEqual(AuraType other) => other is AuraMap m && Key.IsSameOrInheritingType(m.Key) &&
													Value.IsSameOrInheritingType(m.Value);

	public override bool IsSameType(AuraType other) => other is AuraMap;
	public override string ToString() => $"map[{Key}]{Value}";
	public AuraType IndexingType() => Key;
	public AuraType GetIndexedType() => Value;

	public ITypedAuraExpression Default(Range range) =>
		new MapLiteral<ITypedAuraExpression, ITypedAuraExpression>(
			Map: new Tok(
				typ: TokType.Map,
				value: "map",
				range: range
			),
			M: new Dictionary<ITypedAuraExpression, ITypedAuraExpression>(),
			KeyType: Key,
			ValueType: Value,
			ClosingBrace: new Tok(
				typ: TokType.RightBrace,
				value: "}",
				range: range
			)
		);
}

public class AuraError : AuraType, IGettable, IImportableModule, INilable
{
	public string? Message { get; }

	public AuraError(string message)
	{
		Message = message;
	}

	public AuraError() { }

	public override bool IsSameType(AuraType other) => other is AuraError;
	public override string ToString() => "error";

	public AuraType? Get(string attribute)
	{
		var errorMod = AuraStdlib.GetAllModules()["aura/errors"];
		return errorMod.PublicFunctions.First(f => f.Name == attribute);
	}

	public string GetModuleName() => "errors";
}

public class AuraStruct : AuraType, ICallable, IGettable, ICompletable, IDocumentable
{
	public Visibility Public { get; }
	public string Name { get; }
	public List<Param> Parameters { get; }
	public string Documentation { get; }

	public AuraStruct(string name, List<Param> parameters, Visibility pub)
	{
		Public = pub;
		Name = name;
		Parameters = parameters;
		Documentation = string.Empty;
	}
	
	public AuraStruct(string name, List<Param> parameters, Visibility pub, string documentation)
	{
		Public = pub;
		Name = name;
		Parameters = parameters;
		Documentation = documentation;
	}

	public override bool IsSameType(AuraType other)
	{
		if (other is not AuraStruct st) return false;
		return Parameters.Zip(st.Parameters)
			.Select(pair => pair.First.ParamType.Typ.IsSameOrInheritingType(pair.Second.ParamType.Typ))
			.Any(b => b is true);
	}

	public override string ToString() => "struct";

	public override string ToAuraString()
	{
		var @params = string.Join(", ", Parameters.Select(p => $"{p.Name.Value}: {p.ParamType.Typ.ToAuraString()}"));
		return $"struct {Name}({@params})";
	}

	public List<Param> GetParams() => Parameters;

	public List<ParamType> GetParamTypes() => Parameters.Select(p => p.ParamType).ToList();

	public AuraType GetReturnType() => this;

	public int GetParamIndex(string name) => Parameters.FindIndex(p => p.Name.Value == name);

	public bool HasVariadicParam() => Parameters.Any(p => p.ParamType.Variadic);

	public AuraType? Get(string attribute) => Parameters.First(p => p.Name.Value == attribute).ParamType.Typ;

	public CompletionList ProvideCompletableOptions()
	{
		var completionItems = Parameters.Select(p => new CompletionItem { Label = p.Name.Value, Kind = CompletionItemKind.Property });
		return new CompletionList
		{
			Items = completionItems.ToArray()
		};
	}
}

public class AuraAnonymousStruct : AuraType
{
	public Visibility Public { get; }
	public List<Param> Parameters { get; }

	public AuraAnonymousStruct(List<Param> parameters, Visibility pub)
	{
		Public = pub;
		Parameters = parameters;
	}
	public override bool IsSameType(AuraType other)
	{
		if (other is not AuraAnonymousStruct st) return false;
		return Parameters.Zip(st.Parameters)
			.Select(pair => pair.First.ParamType.Typ.IsSameOrInheritingType(pair.Second.ParamType.Typ))
			.Any(b => b is true);
	}

	public override string ToString() => "struct";
}

public class AuraResult : AuraType, IGettable, IImportableModule
{
	public AuraType Success { get; }
	public AuraError Failure { get; }

	public AuraResult(AuraType success, AuraError failure)
	{
		Success = success;
		Failure = failure;
	}

	public override bool IsSameType(AuraType other) => other is AuraResult r && r.Success.IsSameOrInheritingType(Success);

	public override string ToString() => $"struct{{\nSuccess {Success}\nFailure {Failure}\n}}";

	public AuraType? Get(string attribute)
	{
		var stringMod = AuraStdlib.GetAllModules()["aura/results"];
		return stringMod.PublicFunctions.First(f => f.Name == attribute);
	}

	public string GetModuleName() => "results";
}
