using AuraLang.AST;
using AuraLang.Shared;
using AuraLang.Stdlib;
using AuraLang.Token;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Range = AuraLang.Location.Range;

namespace AuraLang.Types;

public abstract class AuraType
{
	public virtual bool IsEqual(AuraType other)
	{
		return IsSameType(other);
	}

	/// <summary>
	///     Determines if the current type and the supplied type are the same type. In the case of a compound type, such as
	///     <see cref="AuraList" /> or <see cref="AuraMap" />, this method will return true only if the supplied type is the
	///     same compound type and all contained types also share a type
	/// </summary>
	/// <param name="other">The supplied type that will be compared to this type</param>
	/// <returns>A boolean indicating if the two types are the same</returns>
	public abstract bool IsSameType(AuraType other);

	/// <summary>
	///     Determines if the supplied type inherits from this type. This method will only return true if the supplied type is
	///     a sub-type that inherits from this type. In the case when the supplied type is the same as this type, this method
	///     will return false
	/// </summary>
	/// <param name="other">The supplied type that will be compared to this type</param>
	/// <returns>A boolean indicating if the supplied type inherits from this type</returns>
	public virtual bool IsInheritingType(AuraType other)
	{
		return false;
	}

	/// <summary>
	///     Determines if the supplied type is either the same as this type or inherits from it.
	/// </summary>
	/// <param name="other">The supplied type which will be compared to this type</param>
	/// <returns>A boolean indicating if the supplied type is the same or inherits from this type</returns>
	public bool IsSameOrInheritingType(AuraType other)
	{
		return IsSameType(other) || IsInheritingType(other);
	}

	public abstract override string ToString();

	public virtual string ToAuraString()
	{
		return ToString();
	}

	public virtual string ToType()
	{
		return ToAuraString();
	}

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

	private IEnumerable<(string, AuraType, bool)> FilterParams(List<Param> parameters)
	{
		return parameters.Select(p => (p.Name.Value, p.ParamType.Typ, p.ParamType.Variadic));
	}

	public override int GetHashCode()
	{
		return ToString().GetHashCode();
	}
}

/// <summary>
///     Represents a value whose type is unknown. This type should only be used prior to the type checking
///     stage of the compilation process. For example, when declaring a variable using Aura's short <c>:=</c>
///     syntax (i.e. <code>i := 5</code>), the type of <c>i</c> is unknown before it is type checked.
/// </summary>
public class AuraUnknown : AuraType
{
	/// <summary>
	///     The name of the unknown type. Since <c>Unknown</c> is primarily used for user-defined types before they've been
	///     type checked or variables defined with type inference, this field will contain the name of the variable whose type
	///     is still unknown
	/// </summary>
	public string Name { get; }

	public AuraUnknown(string name)
	{
		Name = name;
	}

	public override bool IsSameType(AuraType other)
	{
		return other is AuraUnknown;
	}

	public override string ToString()
	{
		return "unknown";
	}
}

/// <summary>
///     Used to represent the type of an Aura statement, which has no type because statements do not return a value.
/// </summary>
public class AuraNone : AuraType
{
	public override bool IsSameType(AuraType other)
	{
		return other is AuraNone;
	}

	public override string ToString()
	{
		return "none";
	}
}

/// <summary>
///     Represents an integer value
/// </summary>
public class AuraInt : AuraType, IDefaultable
{
	public override bool IsSameType(AuraType other)
	{
		return other is AuraInt;
	}

	public override string ToString()
	{
		return "int";
	}

	public ITypedAuraExpression Default(Range range)
	{
		return new IntLiteral(
			new Tok(
				TokType.IntLiteral,
				"0",
				range
			)
		);
	}
}

/// <summary>
///     Represents a floating point value
/// </summary>
public class AuraFloat : AuraType, IDefaultable
{
	public override bool IsSameType(AuraType other)
	{
		return other is AuraFloat;
	}

	public override string ToString()
	{
		return "float";
	}

	public ITypedAuraExpression Default(Range range)
	{
		return new FloatLiteral(
			new Tok(
				TokType.Float,
				"0.0",
				range
			)
		);
	}
}

/// <summary>
///     Represents a string value
/// </summary>
public class AuraString : AuraType, IIterable, IIndexable, IRangeIndexable, IDefaultable, IGettable, IImportableModule,
	ICompletable
{
	public override bool IsSameType(AuraType other)
	{
		return other is AuraString;
	}

	public AuraType GetIterType()
	{
		return new AuraChar();
	}

	public override string ToString()
	{
		return "string";
	}

	public AuraType IndexingType()
	{
		return new AuraInt();
	}

	public AuraType GetIndexedType()
	{
		return new AuraChar();
	}

	public AuraType GetRangeIndexedType()
	{
		return new AuraString();
	}

	public ITypedAuraExpression Default(Range range)
	{
		return new StringLiteral(
			new Tok(
				TokType.String,
				string.Empty,
				range
			)
		);
	}

	public AuraType? Get(string attribute)
	{
		var stringMod = AuraStdlib.GetAllModules()["aura/strings"];
		return stringMod.PublicFunctions.First(f => f.Name == attribute);
	}

	public string GetModuleName()
	{
		return "strings";
	}

	public IEnumerable<string> SupportedTriggerCharacters => new List<string> { "." };

	public bool IsTriggerCharacterSupported(string triggerCharacter)
	{
		return SupportedTriggerCharacters.Contains(triggerCharacter);
	}

	public CompletionList ProvideCompletableOptions(string triggerCharacter)
	{
		switch (triggerCharacter)
		{
			case ".":
				// Get "strings" module's methods
				if (!AuraStdlib.TryGetModule("aura/strings", out var stringsModule)) return new CompletionList();

				var completionItems = stringsModule!.PublicFunctions.Select(
					f => new CompletionItem
					{
						Label = f.Name,
						Kind = CompletionItemKind.Function,
						Documentation =
							new MarkupContent { Value = $"```\n{f.Documentation}\n```", Kind = MarkupKind.Markdown }
					}
				);
				return new CompletionList { Items = completionItems.ToArray() };
			default:
				return new CompletionList();
		}
	}
}

/// <summary>
///     Represents a boolean value
/// </summary>
public class AuraBool : AuraType, IDefaultable
{
	public override bool IsSameType(AuraType other)
	{
		return other is AuraBool;
	}

	public override string ToString()
	{
		return "bool";
	}

	public ITypedAuraExpression Default(Range range)
	{
		return new BoolLiteral(
			new Tok(
				TokType.Bool,
				"false",
				range
			)
		);
	}
}

/// <summary>
///     Represents a resizable array of elements, all of which must have the same type
/// </summary>
public class AuraList : AuraType, IIterable, IIndexable, IRangeIndexable, IDefaultable, IGettable, IImportableModule
{
	/// <summary>
	///     The type of the elements in the list
	/// </summary>
	private AuraType Kind { get; }

	public AuraList(AuraType kind)
	{
		Kind = kind;
	}

	public override bool IsEqual(AuraType other)
	{
		return other is AuraList list && Kind.IsSameType(list.Kind);
	}

	public override bool IsSameType(AuraType other)
	{
		return other is AuraList;
	}

	public AuraType GetIterType()
	{
		return Kind;
	}

	public override string ToString()
	{
		return $"[]{Kind}";
	}

	public AuraType IndexingType()
	{
		return new AuraInt();
	}

	public AuraType GetIndexedType()
	{
		return Kind;
	}

	public AuraType GetRangeIndexedType()
	{
		return new AuraList(Kind);
	}

	public ITypedAuraExpression Default(Range range)
	{
		return new ListLiteral<ITypedAuraExpression>(
			new Tok(
				TokType.LeftBracket,
				"[",
				range
			),
			new List<ITypedAuraExpression>(),
			new AuraList(Kind),
			new Tok(
				TokType.RightBrace,
				"}",
				range
			)
		);
	}

	public AuraType? Get(string attribute)
	{
		var listsMod = AuraStdlib.GetAllModules()["aura/lists"];
		return listsMod.PublicFunctions.First(f => f.Name == attribute);
	}

	public string GetModuleName()
	{
		return "lists";
	}
}

/// <summary>
///     Represents an Aura function
/// </summary>
public class AuraNamedFunction : AuraType, ICallable, IDocumentable, ISignatureHelper
{
	/// <summary>
	///     The function's name
	/// </summary>
	public string Name { get; }

	/// <summary>
	///     The function's visibility
	/// </summary>
	public Visibility Public { get; }

	public AuraFunction F { get; }

	public string Documentation
	{
		get
		{
			if (_documentation is null) return string.Empty;

			return $"{ToAuraString()}\n\n{_documentation}";
		}
	}

	private string? _documentation { get; }

	public AuraNamedFunction(
		string name,
		Visibility pub,
		AuraFunction f
	)
	{
		Name = name;
		Public = pub;
		F = f;
		_documentation = null;
	}

	public AuraNamedFunction(
		string name,
		Visibility pub,
		AuraFunction f,
		string? documentation
	)
	{
		Name = name;
		Public = pub;
		F = f;
		_documentation = documentation;
	}

	public override bool IsEqual(AuraType other)
	{
		return other is AuraNamedFunction f && Name == f.Name && F.IsEqual(f.F);
	}

	public override bool IsSameType(AuraType other)
	{
		return other is AuraNamedFunction;
	}

	public override string ToString()
	{
		var name = Public == Visibility.Public ? Name.ToUpper() : Name.ToLower();
		var @params = string.Join(", ", F.Params.Select(p => $"{p.Name.Value} {p.ParamType.Typ}"));
		return $"func {name}({@params}){(F.ReturnType is not AuraNil ? F.ReturnType : "")}";
	}

	public override string ToAuraString()
	{
		var pub = Public == Visibility.Public ? "pub " : string.Empty;
		var @params = string.Join(", ", F.Params.Select(p => $"{p.Name.Value}: {p.ParamType.Typ}"));
		var returnType = F.ReturnType is not AuraNil ? $" -> {F.ReturnType}" : string.Empty;
		return $"{pub}fn {Name}({@params}){returnType}";
	}

	public List<Param> GetParams()
	{
		return F.Params;
	}

	public List<ParamType> GetParamTypes()
	{
		return F.GetParamTypes();
	}

	public AuraType GetReturnType()
	{
		return F.ReturnType;
	}

	public int GetParamIndex(string name)
	{
		return F.GetParamIndex(name);
	}

	public bool HasVariadicParam()
	{
		return F.HasVariadicParam();
	}

	public IEnumerable<string> SupportedSignatureHelpTriggerCharacters => new List<string> { "(" };

	public bool IsSignatureHelpTriggerCharacterSupported(string triggerCharacter)
	{
		return SupportedSignatureHelpTriggerCharacters.Contains(triggerCharacter);
	}

	public SignatureHelp ProvideSignatureHelp(string triggerCharacter)
	{
		var @params = F.Params.Select(
			p => new ParameterInformation
			{
				Label = p.Name.Value,
				Documentation = new MarkupContent
				{
					Kind = MarkupKind.Markdown,
					Value = $"```\n{p.Name.Value}: {p.ParamType.Typ.ToAuraString()}\n```"
				}
			}
		);
		return new SignatureHelp
		{
			ActiveParameter = 0,
			ActiveSignature = 0,
			Signatures = new[]
			{
				new SignatureInformation
				{
					Label = ToAuraString(),
					Documentation =
						new MarkupContent { Kind = MarkupKind.Markdown, Value = $"```\n{_documentation}\n```" },
					Parameters = @params.ToArray()
				}
			}
		};
	}
}

/// <summary>
///     Represents an anonymous function in Aura, which is basically just a named function
///     without a name
/// </summary>
public class AuraFunction : AuraType, ICallable
{
	/// <summary>
	///     The function's parameters
	/// </summary>
	public List<Param> Params { get; }

	/// <summary>
	///     The function's return type
	/// </summary>
	public AuraType ReturnType { get; }

	public string Documentation => string.Empty;

	public AuraFunction(List<Param> fParams, AuraType returnType)
	{
		Params = fParams;
		ReturnType = returnType;
	}

	public override bool IsEqual(AuraType other)
	{
		return other is AuraFunction f && CompareParamsForEquality(Params, f.Params) &&
			   ReturnType.IsSameType(f.ReturnType);
	}

	public override bool IsSameType(AuraType other)
	{
		return other is AuraFunction;
	}

	public override string ToString()
	{
		var @params = string.Join(", ", Params.Select(p => $"{p.Name.Value} {p.ParamType.Typ}"));
		return $"func({@params}) {ReturnType}";
	}

	public List<Param> GetParams()
	{
		return Params;
	}

	public List<ParamType> GetParamTypes()
	{
		return Params.Select(p => p.ParamType).ToList();
	}

	public AuraType GetReturnType()
	{
		return ReturnType;
	}

	public int GetParamIndex(string name)
	{
		return Params.FindIndex(p => p.Name.Value == name);
	}

	public bool HasVariadicParam()
	{
		return Params.Any(p => p.ParamType.Variadic);
	}
}

/// <summary>
///     Represents an interface in Aura, which can be implemented by Aura classes
/// </summary>
public class AuraInterface : AuraType, IGettable, IDocumentable
{
	/// <summary>
	///     The interface's visibility
	/// </summary>
	public Visibility Public { get; }

	/// <summary>
	///     The interface's name
	/// </summary>
	public string Name { get; }

	/// <summary>
	///     The interface's functions. When a class implements this interface, the class must provide an implementation for
	///     each of the interface's functions
	/// </summary>
	public List<AuraNamedFunction> Functions { get; }

	public string Documentation { get; }

	public AuraInterface(
		string name,
		List<AuraNamedFunction> functions,
		Visibility pub
	)
	{
		Name = name;
		Functions = functions;
		Public = pub;
		Documentation = string.Empty;
	}

	public AuraInterface(
		string name,
		List<AuraNamedFunction> functions,
		Visibility pub,
		string documentation
	)
	{
		Name = name;
		Functions = functions;
		Public = pub;
		Documentation = documentation;
	}

	public override bool IsEqual(AuraType other)
	{
		return other is AuraInterface i && Name == i.Name && Functions.SequenceEqual(i.Functions);
	}

	public override bool IsSameType(AuraType other)
	{
		return other is AuraInterface;
	}

	public override bool IsInheritingType(AuraType other)
	{
		if (other is not AuraClass c) return false;

		return c.Implementing.Contains(this);
	}

	public override string ToString()
	{
		return Public == Visibility.Public ? Name.ToUpper() : Name.ToLower();
	}

	public override bool Equals(object? obj)
	{
		if (obj is null) return false;

		if (obj is not AuraInterface i) return false;

		return Name == i.Name && Functions == i.Functions;
	}

	public AuraType? Get(string attribute)
	{
		return Functions.First(f => f.Name == attribute);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}

/// <summary>
///     Represents a class type in Aura. Classes have their own type signature as well as zero or more
///     methods, each of which also have their own type.
/// </summary>
public class AuraClass : AuraType, IGettable, ICallable, ICompletable, IDocumentable
{
	/// <summary>
	///     The class's visibility
	/// </summary>
	public Visibility Public { get; }

	/// <summary>
	///     The class's name
	/// </summary>
	public string Name { get; }

	/// <summary>
	///     The class's parameters
	/// </summary>
	public List<Param> Parameters { get; }

	/// <summary>
	///     The class's methods
	/// </summary>
	public List<AuraNamedFunction> Methods { get; }

	/// <summary>
	///     A list of zero or more interfaces implemented by the class
	/// </summary>
	public List<AuraInterface> Implementing { get; }

	public string Documentation { get; }

	public AuraClass(
		string name,
		List<Param> parameters,
		List<AuraNamedFunction> methods,
		List<AuraInterface> implementing,
		Visibility pub
	)
	{
		Name = name;
		Parameters = parameters;
		Methods = methods;
		Implementing = implementing;
		Public = pub;
		Documentation = string.Empty;
	}

	public AuraClass(
		string name,
		List<Param> parameters,
		List<AuraNamedFunction> methods,
		List<AuraInterface> implementing,
		Visibility pub,
		string documentation
	)
	{
		Name = name;
		Parameters = parameters;
		Methods = methods;
		Implementing = implementing;
		Public = pub;
		Documentation = documentation;
	}

	public override bool IsEqual(AuraType other)
	{
		return other is AuraClass c && Name == c.Name && Parameters.SequenceEqual(c.Parameters) &&
			   Methods.SequenceEqual(c.Methods);
	}

	public override bool IsSameType(AuraType other)
	{
		return other is AuraClass;
	}

	public override string ToString()
	{
		return "class";
	}

	public override string ToAuraString()
	{
		var pub = Public == Visibility.Public ? "pub " : string.Empty;
		var @params = string.Join(", ", Parameters.Select(p => $"{p.Name.Value}: {p.ParamType.Typ.ToAuraString()}"));
		return $"{pub}class {Name}({@params})";
	}

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

	public (Visibility, AuraType)? GetWithVisibility(string name)
	{
		// Check if attribute is a param
		try
		{
			var param = Parameters.First(p => p.Name.Value == name).ParamType.Typ;
			return (Visibility.Public, param);
		}
		catch (InvalidOperationException)
		{
			// Check if attribute is a method
			try
			{
				var m = Methods.First(m => m.Name == name);
				return (m.Public, m);
			}
			catch (InvalidOperationException)
			{
				// If the attribute is neither a param nor a method, return null
				return null;
			}
		}
	}

	public List<Param> GetParams()
	{
		return Parameters;
	}

	public List<ParamType> GetParamTypes()
	{
		return Parameters.Select(p => p.ParamType).ToList();
	}

	public AuraType GetReturnType()
	{
		return this;
	}

	public int GetParamIndex(string name)
	{
		return Parameters.FindIndex(p => p.Name.Value == name);
	}

	public bool HasVariadicParam()
	{
		return Parameters.Any(p => p.ParamType.Variadic);
	}

	public IEnumerable<string> SupportedTriggerCharacters => new List<string> { "." };

	public bool IsTriggerCharacterSupported(string triggerCharacter)
	{
		return SupportedTriggerCharacters.Contains(triggerCharacter);
	}

	public CompletionList ProvideCompletableOptions(string triggerCharacter)
	{
		switch (triggerCharacter)
		{
			case ".":
				var completionItems = Methods
					.Where(m => m.Public == Visibility.Public)
					.Select(
					m => new CompletionItem
					{
						Label = m.Name,
						Kind = CompletionItemKind.Function,
						Documentation = new MarkupContent
						{
							Kind = MarkupKind.Markdown,
							Value = $"```\n{m.Documentation}\n```"
						}
					}
				);
				return new CompletionList { Items = completionItems.ToArray() };
			default:
				return new CompletionList();
		}
	}
}

/// <summary>
///     Represents an Aura module, which includes zero or more public functions capable of being
///     called outside of their defining module. Each Aura source file begins with a <c>mod</c> statement,
///     which establishes the module's name. Any functions declared in that source file are considered
///     part of the same module.
/// </summary>
public class AuraModule : AuraType, IGettable, ICompletable
{
	public string Name { get; }
	public List<AuraNamedFunction> PublicFunctions { get; }
	public List<AuraInterface> PublicInterfaces { get; }
	public List<AuraClass> PublicClasses { get; }
	public Dictionary<string, ITypedAuraExpression> PublicVariables { get; }

	public AuraModule(
		string name,
		List<AuraNamedFunction> publicFunctions,
		List<AuraInterface> publicInterfaces,
		List<AuraClass> publicClasses,
		Dictionary<string, ITypedAuraExpression> publicVariables
	)
	{
		Name = name;
		PublicFunctions = publicFunctions;
		PublicInterfaces = publicInterfaces;
		PublicClasses = publicClasses;
		PublicVariables = publicVariables;
	}

	public override bool IsEqual(AuraType other)
	{
		return other is AuraModule m && Name == m.Name && PublicFunctions.SequenceEqual(m.PublicFunctions);
	}

	public override bool IsSameType(AuraType other)
	{
		return other is AuraModule;
	}

	public override string ToString()
	{
		return "module";
	}

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

	public IEnumerable<string> SupportedTriggerCharacters => new List<string> { "." };

	public bool IsTriggerCharacterSupported(string triggerCharacter)
	{
		return SupportedTriggerCharacters.Contains(triggerCharacter);
	}

	public CompletionList ProvideCompletableOptions(string triggerCharacter)
	{
		switch (triggerCharacter)
		{
			case ".":
				// Get stdlib module
				if (!AuraStdlib.TryGetModule("Name", out var module)) return new CompletionList();

				var completionItems = module!.PublicFunctions.Select(
					f => new CompletionItem
					{
						Label = f.Name,
						Kind = CompletionItemKind.Function,
						Documentation = new MarkupContent
						{
							Kind = MarkupKind.Markdown,
							Value = $"```\n{f.Documentation}\n```"
						}
					}
				);
				return new CompletionList { Items = completionItems.ToArray() };
			default:
				return new CompletionList();
		}
	}
}

/// <summary>
///     Represents a type with no return value. This type is used for expressions that do not return a value.
///     This type differs from <see cref="AuraUnknown" /> in that <c>Nil</c> indicates the type is known to not
///     exist, whereas <c>Unknown</c> indicates that the type is not yet known.
/// </summary>
public class AuraNil : AuraType
{
	public override bool IsSameType(AuraType other)
	{
		return other is AuraNil;
	}

	public override string ToString()
	{
		return "nil";
	}
}

/// <summary>
///     Represents the parent type of all other types in Aura
/// </summary>
public class AuraAny : AuraType
{
	public override bool IsInheritingType(AuraType other)
	{
		return true;
	}

	public override bool IsSameType(AuraType other)
	{
		return other is AuraAny;
	}

	public override string ToString()
	{
		return "any";
	}
}

/// <summary>
///     Represents a single character, and is denoted in Aura programs by a single character surrounded
///     with single quotes.
/// </summary>
public class AuraChar : AuraType
{
	public override bool IsSameType(AuraType other)
	{
		return other is AuraChar;
	}

	public override string ToString()
	{
		return "byte";
	}

	public override string ToAuraString()
	{
		return "char";
	}

	public override string ToType()
	{
		return "byte";
	}
}

/// <summary>
///     Represents a data type containing a series of key-value pairs. All the keys must have the same
///     type and all the values must have the same type.
/// </summary>
public class AuraMap : AuraType, IIndexable, IDefaultable, IGettable, IImportableModule
{
	public AuraType Key { get; }
	public AuraType Value { get; }

	public AuraMap(AuraType key, AuraType value)
	{
		Key = key;
		Value = value;
	}

	public override bool IsEqual(AuraType other)
	{
		return other is AuraMap m && Key.IsSameOrInheritingType(m.Key) && Value.IsSameOrInheritingType(m.Value);
	}

	public override bool IsSameType(AuraType other)
	{
		return other is AuraMap;
	}

	public override string ToString()
	{
		return $"map[{Key.ToType()}]{Value}";
	}

	public AuraType IndexingType()
	{
		return Key;
	}

	public AuraType GetIndexedType()
	{
		return Value;
	}

	public ITypedAuraExpression Default(Range range)
	{
		return new MapLiteral<ITypedAuraExpression, ITypedAuraExpression>(
			new Tok(
				TokType.Map,
				"map",
				range
			),
			new Dictionary<ITypedAuraExpression, ITypedAuraExpression>(),
			Key,
			Value,
			new Tok(
				TokType.RightBrace,
				"}",
				range
			)
		);
	}

	public AuraType? Get(string attribute)
	{
		var stringMod = AuraStdlib.GetAllModules()["aura/maps"];
		return stringMod.PublicFunctions.First(f => f.Name == attribute);
	}

	public string GetModuleName()
	{
		return "maps";
	}
}

/// <summary>
///     Represents an error encountered during execution
/// </summary>
public class AuraError : AuraType, IGettable, IImportableModule, INilable
{
	/// <summary>
	///     The error's message
	/// </summary>
	public string? Message;

	public AuraError(string message)
	{
		Message = message;
	}

	public AuraError()
	{
	}

	public override bool IsSameType(AuraType other)
	{
		return other is AuraError;
	}

	public override string ToString()
	{
		return "error";
	}

	public AuraType? Get(string attribute)
	{
		var errorMod = AuraStdlib.GetAllModules()["aura/errors"];
		return errorMod.PublicFunctions.First(f => f.Name == attribute);
	}

	public string GetModuleName()
	{
		return "errors";
	}
}

/// <summary>
///     Represents an Aura struct, which is similar to a class but does not contain any methods
/// </summary>
public class AuraStruct : AuraType, ICallable, IGettable, ICompletable, IDocumentable
{
	/// <summary>
	///     The struct's visibility
	/// </summary>
	public Visibility Public { get; }

	/// <summary>
	///     The struct's name
	/// </summary>
	public string Name { get; }

	/// <summary>
	///     The struct's parameters
	/// </summary>
	public List<Param> Parameters { get; }

	public string Documentation { get; }

	public AuraStruct(
		string name,
		List<Param> parameters,
		Visibility pub
	)
	{
		Public = pub;
		Name = name;
		Parameters = parameters;
		Documentation = string.Empty;
	}

	public AuraStruct(
		string name,
		List<Param> parameters,
		Visibility pub,
		string documentation
	)
	{
		Public = pub;
		Name = name;
		Parameters = parameters;
		Documentation = documentation;
	}

	public override bool IsSameType(AuraType other)
	{
		if (other is not AuraStruct st) return false;

		return Parameters
			.Zip(st.Parameters)
			.Select(pair => pair.First.ParamType.Typ.IsSameOrInheritingType(pair.Second.ParamType.Typ))
			.Any(b => b);
	}

	public override string ToString()
	{
		return "struct";
	}

	public override string ToAuraString()
	{
		var @params = string.Join(", ", Parameters.Select(p => $"{p.Name.Value}: {p.ParamType.Typ.ToAuraString()}"));
		return $"struct {Name}({@params})";
	}

	public override string ToType()
	{
		return Name;
	}

	public List<Param> GetParams()
	{
		return Parameters;
	}

	public List<ParamType> GetParamTypes()
	{
		return Parameters.Select(p => p.ParamType).ToList();
	}

	public AuraType GetReturnType()
	{
		return this;
	}

	public int GetParamIndex(string name)
	{
		return Parameters.FindIndex(p => p.Name.Value == name);
	}

	public bool HasVariadicParam()
	{
		return Parameters.Any(p => p.ParamType.Variadic);
	}

	public AuraType? Get(string attribute)
	{
		return Parameters.First(p => p.Name.Value == attribute).ParamType.Typ;
	}

	public IEnumerable<string> SupportedTriggerCharacters => new List<string> { "." };

	public bool IsTriggerCharacterSupported(string triggerCharacter)
	{
		return SupportedTriggerCharacters.Contains(triggerCharacter);
	}

	public CompletionList ProvideCompletableOptions(string triggerCharacter)
	{
		switch (triggerCharacter)
		{
			case ".":
				var completionItems = Parameters.Select(
					p => new CompletionItem { Label = p.Name.Value, Kind = CompletionItemKind.Property }
				);
				return new CompletionList { Items = completionItems.ToArray() };
			default:
				return new CompletionList();
		}
	}
}

/// <summary>
///     Represents an anonymous struct, which is instantiated without a name and must be stored in a variable in order to
///     be referenced again
/// </summary>
public class AuraAnonymousStruct : AuraType
{
	/// <summary>
	///     The struct's visibility
	/// </summary>
	public Visibility Public { get; }

	/// <summary>
	///     The struct's parameters
	/// </summary>
	public List<Param> Parameters { get; }

	public AuraAnonymousStruct(List<Param> parameters, Visibility pub)
	{
		Public = pub;
		Parameters = parameters;
	}

	public override bool IsSameType(AuraType other)
	{
		if (other is not AuraAnonymousStruct st) return false;

		return Parameters
			.Zip(st.Parameters)
			.Select(pair => pair.First.ParamType.Typ.IsSameOrInheritingType(pair.Second.ParamType.Typ))
			.Any(b => b);
	}

	public override string ToString()
	{
		return "struct";
	}
}

/// <summary>
///     Represents a data structure that contains either a success or failure value
/// </summary>
public class AuraResult : AuraType, IGettable, IImportableModule
{
	/// <summary>
	///     The result's success type
	/// </summary>
	public AuraType Success { get; }

	/// <summary>
	///     The result's failure type
	/// </summary>
	public AuraError Failure { get; }

	public AuraResult(AuraType success, AuraError failure)
	{
		Success = success;
		Failure = failure;
	}

	public override bool IsSameType(AuraType other)
	{
		return other is AuraResult r && r.Success.IsSameOrInheritingType(Success);
	}

	public override string ToString()
	{
		return $"struct{{\nSuccess {Success}\nFailure {Failure}\n}}";
	}

	public AuraType? Get(string attribute)
	{
		var stringMod = AuraStdlib.GetAllModules()["aura/results"];
		return stringMod.PublicFunctions.First(f => f.Name == attribute);
	}

	public string GetModuleName()
	{
		return "results";
	}
}
