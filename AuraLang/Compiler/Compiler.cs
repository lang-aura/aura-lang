using System.Globalization;
using System.Text;
using AuraLang.AST;
using AuraLang.Exceptions.Compiler;
using AuraLang.ModuleCompiler;
using AuraLang.Prelude;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using AuraLang.Visitor;

namespace AuraLang.Compiler;

/// <summary>
///     Responsible for compiling a typed Abstract Syntax Tree to valid Go output
/// </summary>
public class AuraCompiler : ITypedAuraStmtVisitor<string>, ITypedAuraExprVisitor<string>
{
	/// <summary>
	///     The typed Aura AST that will be compiled to Go
	/// </summary>
	private readonly List<ITypedAuraStatement> _typedAst;

	/// <summary>
	///     Aura allows implicit returns in certain situations, and the behavior of the return statement differs depending on
	///     the situation and whether its implicit or explicit. Because of that, the compiler keeps track of any enclosing
	///     types, which it refers to when compiling a return statement. The enclosing types
	///     that the compiler is interested in are `if` expressions, blocks, functions, and classes.
	/// </summary>
	private readonly Stack<ITypedAuraAstNode> _enclosingType = new();

	/// <summary>
	///     The compiler keeps track of variables declared in the Aura typed AST, but it doesn't need to keep track of all
	///     available information about these variables. Instead, it needs to know if the variables were declared as public or
	///     not. This is because public functions, classes, etc. in Aura are declared with the <c>pub</c> keyword, but in Go
	///     they are denoted with a leading capital letter. Therefore, the compiler refers to this field to determine if the
	///     variable's name should be in title case in the outputted Go file.
	/// </summary>
	private readonly Dictionary<string, Visibility> _declaredVariables = new();

	/// <summary>
	///     Is used by the compiler as a buffer to organize the Go output file before producing the final Go string
	/// </summary>
	private readonly GoDocument _goDocument = new();

	/// <summary>
	///     Contains all exceptions thrown during the compilation process.
	/// </summary>
	private readonly CompilerExceptionContainer _exContainer;

	/// <summary>
	///     The name of the Aura project where the Aura source file is located
	/// </summary>
	private string ProjectName { get; }

	/// <summary>
	///     Writes the compiled output to the project's <c>build</c> directory
	/// </summary>
	private readonly CompiledOutputWriter _outputWriter;

	/// <summary>
	///     The prelude contains exported methods, etc. that can be used in an Aura source file without explicitly importing
	///     them
	/// </summary>
	private readonly AuraModule _prelude;

	/// <summary>
	///     Used to keep track of whether the code being compiled is located inside a function, which may impact the behavior
	///     of the compiler when encountering certain AST nodes
	/// </summary>
	private readonly Stack<TypedNamedFunction> _enclosingFunctionDeclarationStore;

	/// <summary>
	///     The path of the Aura source file being compiled
	/// </summary>
	private string FilePath { get; }

	public AuraCompiler(
		List<ITypedAuraStatement> typedAst,
		string projectName,
		CompiledOutputWriter outputWriter,
		Stack<TypedNamedFunction> enclosingFunctionDeclarationStore,
		string filePath
	)
	{
		_typedAst = typedAst;
		ProjectName = projectName;
		_outputWriter = outputWriter;
		_exContainer = new CompilerExceptionContainer(filePath);
		_prelude = new AuraPrelude().GetPrelude();
		_enclosingFunctionDeclarationStore = enclosingFunctionDeclarationStore;
		FilePath = filePath;
	}

	/// <summary>
	///     Compiles the supplied AST to valid Go code
	/// </summary>
	/// <returns>A string of valid Go code</returns>
	/// <exception cref="CompilerExceptionContainer">
	///     Thrown when the compiler encounters an error during the compilation
	///     process. An exception container is used because the compiler may report more than one error
	/// </exception>
	public string Compile()
	{
		foreach (var node in _typedAst)
		{
			try
			{
				var s = Statement(node);
				_goDocument.WriteStmt(
					s,
					node.Range.Start.Line,
					node
				);
			}
			catch (CompilerException ex)
			{
				_exContainer.Add(ex);
			}
		}

		if (!_exContainer.IsEmpty())
		{
			throw _exContainer;
		}

		return _goDocument.Assemble();
	}

	/// <summary>
	///     Compiles an AST statement
	/// </summary>
	/// <param name="stmt">The statement to compile</param>
	/// <returns>A valid Go string</returns>
	private string Statement(ITypedAuraStatement stmt)
	{
		return stmt.Accept(this);
	}

	/// <summary>
	///     Compiles an AST expression
	/// </summary>
	/// <param name="expr">The expression to compile</param>
	/// <returns>A valid Go string</returns>
	private string Expression(ITypedAuraExpression expr)
	{
		return expr.Accept(this);
	}

	public string Visit(TypedDefer defer)
	{
		var call = Visit(defer.Call);
		return $"defer {call}";
	}

	public string Visit(TypedExpressionStmt es)
	{
		return Expression(es.Expression);
	}

	public string Visit(TypedFor @for)
	{
		return InNewEnclosingType(
			() =>
			{
				var init = @for.Initializer is not null ? Statement(@for.Initializer) : string.Empty;
				var cond = @for.Condition is not null ? Expression(@for.Condition) : string.Empty;
				var inc = @for.Increment is not null ? $"{Expression(@for.Increment)} " : string.Empty;
				var body = CompileLoopBody(@for.Body);
				return body != string.Empty ? $"for {init}; {cond}; {inc}{{{body}\n}}" : $"for {init}; {cond}; {{}}";
			},
			@for
		);
	}

	public string Visit(TypedForEach @foreach)
	{
		return InNewEnclosingType(
			() =>
			{
				var iter = Expression(@foreach.Iterable);
				var body = CompileLoopBody(@foreach.Body);
				return body != string.Empty
					? $"for _, {@foreach.EachName.Value} := range {iter} {{{body}\n}}"
					: $"for _, {@foreach.EachName.Value} := range {iter} {{}}";
			},
			@foreach
		);
	}

	public string Visit(TypedNamedFunction f)
	{
		_enclosingFunctionDeclarationStore.Push(f);
		var s = InNewEnclosingType(
			() =>
			{
				_declaredVariables[f.Name.Value] = f.Public;
				var funcName = f.Public is Visibility.Public ? f.Name.Value.ToUpper() : f.Name.Value.ToLower();
				var compiledParams = CompileParams(f.Params, ",");
				// Compile return type
				var returnValue = string.Empty;
				if (f.ReturnType is AuraAnonymousStruct st)
				{
					var items = string.Join(", ", st.Parameters.Select(p => p.ParamType.Typ));
					returnValue = $"({items})";
				}
				else if (f.ReturnType is not AuraNil)
				{
					returnValue = AuraTypeToGoType(f.ReturnType);
				}

				var body = Expression(f.Body);
				return $"func {funcName}({compiledParams}){returnValue} {body}";
			},
			f
		);
		_enclosingFunctionDeclarationStore.Pop();
		return s;
	}

	public string Visit(TypedAnonymousFunction f)
	{
		return InNewEnclosingType(
			() =>
			{
				var compiledParams = CompileParams(f.Params, ",");
				var returnValue = f.ReturnType is not AuraNil ? $" {AuraTypeToGoType(f.ReturnType)}" : string.Empty;
				var body = Expression(f.Body);
				return $"func({compiledParams}){returnValue} {body}";
			},
			f
		);
	}

	public string Visit(TypedLet let)
	{
		if (let.Names.Count > 1)
		{
			return LetStmtMultipleNames(let);
		}

		// Since Go's `if` statements and blocks are not expressions like they are in Aura, the compiler will first declare the variable without an initializer,
		// and then convert the `yield` statement in the block or `if` expression into an assignment where the value that would be returned is instead assigned
		// to the declared variable.
		switch (let.Initializer)
		{
			case TypedBlock block:
				var decl = $"var {let.Names[0].Value} {AuraTypeToGoType(let.Typ)}";
				var body = ParseReturnableBody(block.Statements, let.Names[0].Value);
				return $"{decl}\n{{{body}\n}}";
			case TypedIf iff:
				var varName = let.Names[0].Value;
				var decll = $"var {varName} {AuraTypeToGoType(let.Initializer.Typ)}";
				var init = Expression(iff.Condition);
				var then = ParseReturnableBody(iff.Then.Statements, varName);
				// The `else` block can either be a block or another `if` expression
				var @else = iff.Else switch
				{
					TypedIf @if => Visit(@if),
					TypedBlock b => ParseReturnableBody(b.Statements, varName),
					_ => string.Empty
				};
				return $"{decll}\nif {init} {{{then}\n}} else {{{@else}\n}}";
			case TypedIs @is:
				var typedIs = Expression(@is);
				return $"_, {let.Names[0].Value} := {typedIs}";
			default:
				var value = let.Initializer is not null ? Expression(let.Initializer) : string.Empty;
				// We check to see if we are inside a `for` loop because Aura and Go differ in whether a a long variable initialization is allowed
				// as the initializer of a `for` loop. Go only allows the short syntax (i.e. `x := 0`), whereas Aura allows both styles of variable
				// declaration. Therefore, if the user has entered the full `let`-style syntax (i.e. `let x: int = 0`) inside the signature of a `for`
				// loop, it must be compiled to the short syntax in the final Go file.
				if (let.TypeAnnotation)
				{
					var b = _enclosingType.TryPeek(out var @for);
					if (!b ||
						@for is not TypedFor)
					{
						return $"var {let.Names[0].Value} {AuraTypeToGoType(let.Initializer!.Typ)} = {value}";
					}
				}

				return $"{let.Names[0].Value} := {value}";
		}
	}

	/// <summary>
	///     Compiles a short let statement when assigning to multiple variables
	/// </summary>
	/// <param name="let">The let statement that contains multiple variables</param>
	/// <returns>A valid Go string representing the let statement</returns>
	private string LetStmtMultipleNames(TypedLet let)
	{
		var names = string.Join(", ", let.Names.Select(n => n.Value));
		return $"{names} := {Expression(let.Initializer!)}";
	}

	public string Visit(TypedMod mod)
	{
		return $"package {mod.Value.Value}";
	}

	public string Visit(TypedReturn r)
	{
		if (r.Value is null)
		{
			return "return";
		}

		if (_enclosingFunctionDeclarationStore.Count > 0 &&
			_enclosingFunctionDeclarationStore.Peek().ReturnType is AuraResult res)
		{
			if (r.Value.Typ.IsSameOrInheritingType(res.Success))
			{
				return $"return {res}{{\nSuccess: {Expression(r.Value)},\n}}";
			}

			return $"return {res}{{\nFailure: {Expression(r.Value)},\n}}";
		}

		if (r.Value.Typ is AuraAnonymousStruct)
		{
			var values = string.Join(", ", ((TypedAnonymousStruct)r.Value).Values.Select(Expression));
			return $"return {values}";
		}

		return $"return {Expression(r.Value)}";
	}

	public string Visit(FullyTypedClass c)
	{
		return InNewEnclosingType(
			() =>
			{
				_declaredVariables[c.Name.Value] = c.Public;

				var className = c.Public == Visibility.Public ? c.Name.Value.ToUpper() : c.Name.Value.ToLower();
				var compiledParams = CompileParams(c.Params, "\n");

				var compiledMethods = c
					.Methods.Select(
						m =>
						{
							var @params = CompileParams(m.Params, ",");
							var body = Expression(m.Body);
							var returnType = m.ReturnType is AuraNil
								? string.Empty
								: $" {AuraTypeToGoType(m.ReturnType)}";
							// To make the handling of `this` expressions a little easier for the compiler, all method receivers in the outputted Go code have an identifier of `this`
							return $"func (this {className}) {m.Name.Value}({@params}){returnType} {body}";
						}
					)
					.Aggregate(string.Empty, (prev, curr) => $"{prev}\n\n{curr}");

				return compiledParams != string.Empty
					? $"type {className} struct {{\n{compiledParams}\n}}\n\n{compiledMethods}"
					: $"type {className} struct {{}}\n\n{compiledMethods}";
			},
			c
		);
	}

	public string Visit(TypedWhile w)
	{
		var cond = Expression(w.Condition);
		var body = CompileLoopBody(w.Body);
		return body != string.Empty ? $"for {cond} {{{body}\n}}" : $"for {cond} {{}}";
	}

	public string Visit(TypedMultipleImport i)
	{
		var multipleImports = i.Packages.Select(CompileImportStmt);
		return $"import (\n\t{string.Join("\n\t", multipleImports)}\n)";
	}

	public string Visit(TypedImport i)
	{
		return $"import {CompileImportStmt(i)}";
	}

	/// <summary>
	///     Compiles an import statement
	/// </summary>
	/// <param name="i">The import statement to compile</param>
	/// <returns>A valid Go string representing the supplied import statement</returns>
	private string CompileImportStmt(TypedImport i)
	{
		if (IsStdlibImportName(i.Package.Value))
		{
			var name = ExtractStdlibPkgName(i.Package.Value);
			return BuildStdlibPkgName(name);
		}

		if (i.Package.Value.Contains("prelude"))
		{
			return $"prelude \"{i.Package.Value}\"";
		}

		var compiledModule = new AuraModuleCompiler($"src/{i.Package.Value}", ProjectName).CompileModule();
		foreach (var (path, output) in compiledModule)
		{
			// Write output to `build` directory
			var dirName = Path.GetDirectoryName(path)!.Replace("src/", "");
			_outputWriter.CreateDirectory(dirName);
			_outputWriter.WriteOutput(
				dirName,
				Path.GetFileNameWithoutExtension(path),
				output
			);
		}

		return i.Alias is null
			? $"\"{ProjectName}/{i.Package.Value}\""
			: $"{i.Alias.Value.Value} \"{ProjectName}/{i.Package.Value}\"";
	}

	public string Visit(TypedComment com)
	{
		return com.Text.Value;
	}

	public string Visit(TypedContinue _)
	{
		return "continue";
	}

	public string Visit(TypedBreak _)
	{
		return "break";
	}

	public string Visit(TypedInterface i)
	{
		return InNewEnclosingType(
			() =>
			{
				var interfaceName = i.Public == Visibility.Public ? i.Name.Value.ToUpper() : i.Name.Value.ToLower();
				var methods = i.Methods.Select(Visit).ToList();

				return methods.Any()
					? $"type {interfaceName} interface {{\n{string.Join("\n", methods)}\n}}"
					: $"type {interfaceName} interface {{}}";
			},
			i
		);
	}

	public string Visit(TypedFunctionSignature fnSignature)
	{
		var name = fnSignature.Visibility is not null
			? fnSignature.Name.Value.ToUpper()
			: fnSignature.Name.Value.ToLower();
		var @params = string.Join(", ", fnSignature.Params.Select(p => $"{p.Name.Value} {p.ParamType.Typ}"));
		return
			$"{name}({@params}) {(fnSignature.ReturnType.IsSameType(new AuraNil()) ? string.Empty : fnSignature.ReturnType)}";
	}

	public string Visit(TypedAssignment assign)
	{
		var value = Expression(assign.Value);
		return $"{assign.Name.Value} = {value}";
	}

	public string Visit(TypedPlusPlusIncrement inc)
	{
		var name = Expression(inc.Name);
		return $"{name}++";
	}

	public string Visit(TypedMinusMinusDecrement dec)
	{
		var name = Expression(dec.Name);
		return $"{name}--";
	}

	public string Visit(TypedBinary b)
	{
		var left = Expression(b.Left);
		var right = Expression(b.Right);
		return $"{left} {b.Operator.Value} {right}";
	}

	public string Visit(TypedBlock b)
	{
		var compiledStmts = new AuraStringBuilder();
		foreach (var stmt in b.Statements)
		{
			var s = Statement(stmt);
			compiledStmts.WriteString(
				s,
				stmt.Range.Start.Line,
				stmt
			);
		}

		return compiledStmts.String() == string.Empty ? "{}" : $"{{\n{compiledStmts.String()}\n}}";
	}

	public string Visit(TypedCall c)
	{
		switch (c.Callee)
		{
			case TypedGet: return CallExpr_GetCallee(c);
			case TypedVariable { Typ: AuraClass }: return CallExpr_Class(c);
			case TypedVariable { Typ: AuraStruct }: return CallExpr_Struct(c);
			default:
				var callee = Expression((ITypedAuraExpression)c.Callee);
				var compiledParams = c.Arguments.Select(Expression);
				return $"{callee}({string.Join(", ", compiledParams)})";
		}
	}

	/// <summary>
	///     Compiles a call expression when the callee is a class
	/// </summary>
	/// <param name="c">The call expression</param>
	/// <returns>A valid Go string</returns>
	private string CallExpr_Class(TypedCall c)
	{
		var v = c.Callee as TypedVariable;
		var @class = v!.Typ as AuraClass;

		var @params = string.Join(
			"\n",
			@class!.GetParams().Zip(c.Arguments).Select(pair => $"{pair.First.Name.Value}: {Expression(pair.Second)},")
		);
		return
			$"{(@class.Public is Visibility.Public ? @class.Name.ToUpper() : @class.Name.ToLower())}{{\n{@params}\n}}";
	}

	/// <summary>
	///     Compiles a call expression when the callee is a struct
	/// </summary>
	/// <param name="c">The call expression</param>
	/// <returns>A valid Go string</returns>
	private string CallExpr_Struct(TypedCall c)
	{
		var v = c.Callee as TypedVariable;
		var @struct = v!.Typ as AuraStruct;

		var @params = string.Join(
			"\n",
			@struct!.GetParams().Zip(c.Arguments).Select(pair => $"{pair.First.Name.Value}: {Expression(pair.Second)},")
		);

		return $"{@struct.Name}{{\n{@params}\n}}";
	}

	/// <summary>
	///     Compiles a call expression when the callee is a get expression
	/// </summary>
	/// <param name="c">The call expression</param>
	/// <returns>A valid Go string</returns>
	private string CallExpr_GetCallee(TypedCall c)
	{
		var get = c.Callee as TypedGet;
		var obj = Expression(get!.Obj);

		string callee;
		if (IsStdlibPkgType(get.Obj.Typ))
		{
			_goDocument.WriteStmt(
				Visit(
					new TypedImport(
						new Tok(TokType.Import, "import"),
						new Tok(TokType.Identifier, $"aura/{AuraTypeToString(get.Obj.Typ)}"),
						new Tok(TokType.Identifier, AuraTypeToString(get.Obj.Typ))
					)
				),
				1,
				new TypedImport(
					new Tok(TokType.Import, "import"),
					new Tok(TokType.Identifier, $"aura/{AuraTypeToString(get.Obj.Typ)}"),
					new Tok(TokType.Identifier, AuraTypeToString(get.Obj.Typ))
				)
			);
			callee = $"{AuraTypeToString(get.Obj.Typ)}.{ConvertSnakeCaseToCamelCase(get.Name.Value)}";
		}
		else
		{
			callee = get.Obj.Typ is AuraModule m
				? IsStdlibPkg(m.Name)
					? $"{obj}.{ConvertSnakeCaseToCamelCase(get.Name.Value)}"
					: $"{obj}.{get.Name.Value.ToUpper()}"
				: $"{obj}.{get.Name.Value}";
		}

		if (get.Obj.Typ is not AuraModule &&
			get.Obj.Typ is not AuraClass &&
			get.Obj.Typ is not AuraInterface)
		{
			c.Arguments.Insert(0, get.Obj);
		}

		var compiledParams = c.Arguments.Select(Expression).ToList();
		if (new List<string> { "read_file", "read_lines", "write_file" }.Contains(get.Name.Value))
		{
			// The path is surrounded by double-quotes, which will mess with the check for absolute paths, so trim them off
			var p = compiledParams[0].Trim('"');
			if (!Path.IsPathRooted(p))
				compiledParams[0] = "\"" + Path.GetDirectoryName(FilePath) + "/" + c.Arguments[0] + "\"";
		}

		return $"{callee}({string.Join(", ", compiledParams)})";
	}

	public string Visit(TypedGet get)
	{
		var objExpr = Expression(get.Obj);
		return $"{objExpr}.{get.Name.Value}";
	}

	public string Visit(TypedGetIndex getIndex)
	{
		var objExpr = Expression(getIndex.Obj);
		var indexExpr = Expression(getIndex.Index);
		return $"{objExpr}[{indexExpr}]";
	}

	public string Visit(TypedGetIndexRange getIndexRange)
	{
		var objExpr = Expression(getIndexRange.Obj);
		var lowerExpr = Expression(getIndexRange.Lower);
		var upperExpr = Expression(getIndexRange.Upper);
		return $"{objExpr}[{lowerExpr}:{upperExpr}]";
	}

	public string Visit(TypedGrouping g)
	{
		var expr = Expression(g.Expr);
		return $"({expr})";
	}

	public string Visit(TypedIf @if)
	{
		var cond = Expression(@if.Condition);
		var then = Expression(@if.Then);
		var @else = @if.Else is not null ? $" else {Expression(@if.Else)}" : string.Empty;
		return @if.Condition is TypedIs ? $"if _, ok := {cond}; ok {then}{@else}" : $"if {cond} {then}{@else}";
	}

	public string Visit(StringLiteral literal)
	{
		return $"\"{literal.Value}\"";
	}

	public string Visit(CharLiteral literal)
	{
		return $"'{literal.Value}'";
	}

	public string Visit(IntLiteral literal)
	{
		return $"{literal.Value}";
	}

	public string Visit(FloatLiteral literal)
	{
		return string.Format(
			CultureInfo.InvariantCulture,
			"{0:0.0}",
			literal.Value
		);
	}

	public string Visit(BoolLiteral literal)
	{
		return literal.Value ? "true" : "false";
	}

	public string Visit<TU>(ListLiteral<TU> literal) where TU : IAuraAstNode
	{
		var items = literal.Value.Select(item => Expression((ITypedAuraExpression)item));
		return $"{AuraTypeToGoType(literal.Typ)}{{{string.Join(", ", items)}}}";
	}

	public string Visit(TypedNil nil)
	{
		return "nil";
	}

	public string Visit<TK, TV>(MapLiteral<TK, TV> literal) where TK : IAuraAstNode where TV : IAuraAstNode
	{
		var items = literal.Value.Select(
			pair =>
			{
				var keyExpr = Expression((ITypedAuraExpression)pair.Key);
				var valueExpr = Expression((ITypedAuraExpression)pair.Value);
				return $"{keyExpr}: {valueExpr},";
			}
			)
			.ToList();
		return items.Any()
			? $"{AuraTypeToGoType(literal.Typ)}{{\n{string.Join(", ", items)}\n}}"
			: $"{AuraTypeToGoType(literal.Typ)}{{}}";
	}

	public string Visit(TypedLogical logical)
	{
		var left = Expression(logical.Left);
		var right = Expression(logical.Right);
		return $"{left} {LogicalOperatorToGoOperator(logical.Operator)} {right}";
	}

	public string Visit(TypedSet set)
	{
		var obj = Expression(set.Obj);
		var value = Expression(set.Value);
		return $"{obj}.{set.Name.Value} = {value}";
	}

	public string Visit(TypedThis _)
	{
		return "this";
	}

	public string Visit(TypedUnary unary)
	{
		var expr = Expression(unary.Right);
		return $"{unary.Operator.Value}{expr}";
	}

	public string Visit(TypedVariable v)
	{
		if (IsVariableInPrelude(v.Name.Value))
		{
			return AddPreludePrefix(v.Name.Value);
		}

		return v.Name.Value;
	}

	public string Visit(TypedIs @is)
	{
		var expr = Expression(@is.Expr);
		var expectedInterface = Visit(@is.Expected);
		return $"interface{{}}({expr}).({expectedInterface})";
	}

	public string Visit(TypedInterfacePlaceholder ip)
	{
		return ((AuraInterface)ip.Typ).Public == Visibility.Public
			? ip.InterfaceValue.Value.ToUpper()
			: ip.InterfaceValue.Value.ToLower();
	}

	public string Visit(TypedYield y)
	{
		var value = y.Value is ILiteral lit ? lit.ToString() : y.Value.ToString();
		return $"x = {value}";
	}

	/// <summary>
	///     Converts an Aura type to its corresponding Go string representation
	/// </summary>
	/// <param name="typ">The Aura type to convert to a Go type</param>
	/// <returns>The Aura type's corresponding Go type</returns>
	private string AuraTypeToGoType(AuraType typ)
	{
		return typ.ToString();
	}

	/// <summary>
	///     Compiles the body of a loop statement
	/// </summary>
	/// <param name="body">A list of statements comprising the loop's body</param>
	/// <returns>A valid Go string</returns>
	private string CompileLoopBody(List<ITypedAuraStatement> body)
	{
		return body.Any()
			? body.Select(Statement).Aggregate(string.Empty, (prev, curr) => $"{prev}\n{curr}")
			: string.Empty;
	}

	/// <summary>
	///     Compiles parameters
	/// </summary>
	/// <param name="params">The parameters to compile</param>
	/// <param name="sep">The separator between each parameters</param>
	/// <returns>A valid Go string</returns>
	private string CompileParams(List<Param> @params, string sep)
	{
		return string.Join(sep, @params.Select(p => $"{p.Name.Value} {AuraTypeToGoType(p.ParamType.Typ)}"));
	}

	/// <summary>
	///     Compiles a return-able body. Because Go does not allow <c>if</c> statements or blocks to return a value, Aura's
	///     workaround
	///     is to first declare a variable without an initializer, and then convert <c>yield</c> statements to an assignment.
	///     For example:
	///     <code>
	///  let i = {
	/// 		yield 5
	///  }
	///  </code>
	///     is compiled to
	///     <code>
	///  var i int
	///  {
	/// 		i = 5
	///  }
	///  </code>
	/// </summary>
	/// <param name="stmts">The body's statements</param>
	/// <param name="decl">The original declaration without an initializer</param>
	/// <returns>A valid Go string</returns>
	private string ParseReturnableBody(List<ITypedAuraStatement> stmts, string decl)
	{
		var body = new StringBuilder();

		foreach (var stmt in stmts)
		{
			switch (stmt)
			{
				case TypedReturn r:
					var returnVal = r.Value is not null ? Expression(r.Value) : string.Empty;
					body.Append($"\nreturn {returnVal}");
					break;
				case TypedYield y:
					var yieldVal = Expression(y.Value);
					body.Append($"\n{decl} = {yieldVal}");
					break;
				default:
					var s = Statement(stmt);
					body.Append($"\n{s}");
					break;
			}
		}

		return body.ToString();
	}

	/// <summary>
	///     Determines if the supplied type refers to a standard library module
	/// </summary>
	/// <param name="typ">An Aura type</param>
	/// <returns>A boolean value indicating if the supplied Aura type refers to a stdlib module</returns>
	private bool IsStdlibPkgType(AuraType typ)
	{
		return typ switch
		{
			AuraString or AuraList or AuraError or AuraResult or AuraMap => true,
			_ => false
		};
	}

	/// <summary>
	///     Converts an Aura type to a string
	/// </summary>
	/// <param name="typ">An Aura type</param>
	/// <returns>A string representation of the Aura type</returns>
	private string AuraTypeToString(AuraType typ)
	{
		return typ switch
		{
			AuraString => "strings",
			AuraList => "lists",
			AuraError => "errors",
			AuraResult => "results",
			AuraMap => "maps",
			_ => string.Empty
		};
	}

	/// <summary>
	///     Determines if the supplied module name refers to a stdlib module
	/// </summary>
	/// <param name="pkg">A module name</param>
	/// <returns>A boolean value indicating if the supplied module name refers to a stdlib module</returns>
	private bool IsStdlibPkg(string pkg)
	{
		return pkg switch
		{
			"io" or "strings" or "lists" or "errors" or "maps" => true,
			_ => false
		};
	}

	/// <summary>
	///     Determines if the supplied module name refers to a stdlib module import name
	/// </summary>
	/// <param name="pkg">A module name</param>
	/// <returns>A boolean value indicating if the supplied module name refers to a stdlib module import name</returns>
	private bool IsStdlibImportName(string pkg)
	{
		return pkg switch
		{
			"aura/io" or "aura/strings" or "aura/lists" or "aura/errors" or "aura/results" or "aura/maps" => true,
			_ => false
		};
	}

	/// <summary>
	///     Extracts the stdlib module name from an import path
	/// </summary>
	/// <param name="pkg">An import path</param>
	/// <returns>The stdlib module name extracted from the import path</returns>
	private string ExtractStdlibPkgName(string pkg)
	{
		return pkg.Split('/').Last();
	}

	/// <summary>
	///     Constructs a complete stdlib module import path
	/// </summary>
	/// <param name="pkg">The stdlib module's name</param>
	/// <returns>A complete stdlib module import path</returns>
	private string BuildStdlibPkgName(string pkg)
	{
		return $"{pkg} \"{ProjectName}/stdlib/{pkg}\"";
	}

	/// <summary>
	///     Converts an Aura logical operator to a Go logical operator
	/// </summary>
	/// <param name="op">An Aura logical operator</param>
	/// <returns>A valid Go string</returns>
	private string LogicalOperatorToGoOperator(Tok op)
	{
		return op.Typ switch
		{
			TokType.And => "&&",
			TokType.Or => "||",
			_ => op.Value
		};
	}

	/// <summary>
	///     Executes the supplied function within the context of an enclosing AST node
	/// </summary>
	/// <param name="f">The function to execute</param>
	/// <param name="node">The enclosing AST node</param>
	/// <returns>A valid Go string</returns>
	private string InNewEnclosingType(Func<string> f, ITypedAuraAstNode node)
	{
		_enclosingType.Push(node);
		var s = f();
		_enclosingType.Pop();
		return s;
	}

	/// <summary>
	///     Converts a snake case variable name to camel case
	/// </summary>
	/// <param name="s">The variable name in snake case format</param>
	/// <returns>A valid Go string</returns>
	private string ConvertSnakeCaseToCamelCase(string s)
	{
		var camelCase = new StringBuilder();
		for (var i = 0; i < s.Length; i++)
		{
			if (i == 0)
			{
				camelCase.Append(char.ToUpper(s[0]));
				continue;
			}

			if (s[i] == '_' &&
				i < s.Length - 1)
			{
				camelCase.Append(char.ToUpper(s[i + 1]));
				i++;
				continue;
			}

			camelCase.Append(s[i]);
		}

		return camelCase.ToString();
	}

	/// <summary>
	///     Determines if a variable is defined in Aura's prelude
	/// </summary>
	/// <param name="name">A variable name</param>
	/// <returns>A boolean indicating if the variable is defined in the prelude</returns>
	private bool IsVariableInPrelude(string name)
	{
		if (_prelude.PublicVariables.ContainsKey(name))
		{
			return true;
		}

		if (_prelude.PublicClasses.Any(c => c.Name == name))
		{
			return true;
		}

		return _prelude.PublicFunctions.Any(f => f.Name == name);
	}

	/// <summary>
	///     Adds an import statement for the <c>prelude</c> module and appends the <c>prelude</c> prefix to the variable's name
	/// </summary>
	/// <param name="name">A variable's name</param>
	/// <returns>A valid Go string</returns>
	private string AddPreludePrefix(string name)
	{
		var typedImport = new TypedImport(
			new Tok(TokType.Import, "import"),
			new Tok(TokType.Identifier, $"{ProjectName}/prelude"),
			new Tok(TokType.Identifier, "prelude")
		);
		var preludeImport = Visit(typedImport);
		_goDocument.WriteStmt(
			preludeImport,
			1,
			typedImport
		);
		return $"prelude.{ConvertSnakeCaseToCamelCase(name)}";
	}

	public string Visit(PartiallyTypedClass partiallyTypedClass)
	{
		throw new NotImplementedException();
	}

	public string Visit(TypedCheck check)
	{
		return $"e := {Visit(check.Call)}\nif e.Failure != nil {{\nreturn e\n}}";
	}

	public string Visit(TypedStruct @struct)
	{
		var @params = CompileParams(@struct.Params, "\n");
		return @params != string.Empty
			? $"type {@struct.Name.Value} struct {{\n{@params}\n}}"
			: $"type {@struct.Name.Value} struct {{}}";
	}

	public string Visit(TypedAnonymousStruct anonymousStruct)
	{
		var items = string.Join(", ", anonymousStruct.Params.Select(p => p.Name.Value));
		return $"{items}";
	}
}
