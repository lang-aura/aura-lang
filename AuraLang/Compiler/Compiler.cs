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

public class AuraCompiler : ITypedAuraStmtVisitor<string>, ITypedAuraExprVisitor<string>
{
	/// <summary>
	/// The typed Aura AST that will be compiled to Go
	/// </summary>
	private readonly List<ITypedAuraStatement> _typedAst;

	/// <summary>
	/// Aura allows implicit returns in certain situations, and the behavior of the return statement differs depending on the situaiton and whether its implicit
	/// or explicit. Because of that, the compiler keeps track of any enclosing types, which it refers to when compiling a return statement. The enclosing types
	/// that the compiler is interested in are `if` expressions, blocks, functions, and classes.
	/// </summary>
	private readonly Stack<ITypedAuraAstNode> _enclosingType = new();

	/// <summary>
	/// The compiler keeps track of variables declared in the Aura typed AST, but it doesn't need to keep track of all available information about these variables.
	/// Instead, it needs to know if the variables were declared as public or not. This is because public functions, classes, etc. in Aura are declared with the
	/// `pub` keyword, but in Go they are denoted with a leading capital letter. Therefore, the compiler refers to this field to determine if the variable's name
	/// should be in title case in the outputted Go file.
	/// </summary>
	private Dictionary<string, Visibility> _declaredVariables = new();

	/// <summary>
	/// Is used by the compiler as a buffer to organize the Go output file before producing the final Go string
	/// </summary>
	private readonly GoDocument _goDocument = new();

	/// <summary>
	/// Contains all exceptions thrown during the compilation process. 
	/// </summary>
	private readonly CompilerExceptionContainer _exContainer;

	private string ProjectName { get; }
	private readonly CompiledOutputWriter _outputWriter;
	private readonly AuraModule _prelude;
	private Stack<TypedNamedFunction> _enclosingFunctionDeclarationStore;

	public AuraCompiler(List<ITypedAuraStatement> typedAst, string projectName,
		CompiledOutputWriter outputWriter, Stack<TypedNamedFunction> enclosingFunctionDeclarationStore, string filePath)
	{
		_typedAst = typedAst;
		ProjectName = projectName;
		_outputWriter = outputWriter;
		_exContainer = new CompilerExceptionContainer(filePath);
		_prelude = new AuraPrelude().GetPrelude();
		_enclosingFunctionDeclarationStore = enclosingFunctionDeclarationStore;
	}

	public string Compile()
	{
		foreach (var node in _typedAst)
		{
			try
			{
				var s = Statement(node);
				_goDocument.WriteStmt(s, node.Line, node);
			}
			catch (CompilerException ex)
			{
				_exContainer.Add(ex);
			}
		}

		if (!_exContainer.IsEmpty()) throw _exContainer;
		return _goDocument.Assemble();
	}

	private string Statement(ITypedAuraStatement stmt) => stmt.Accept(this);

	private string Expression(ITypedAuraExpression expr) => expr.Accept(this);

	public string Visit(TypedDefer defer)
	{
		var call = Visit(defer.Call);
		return $"defer {call}";
	}

	public string Visit(TypedExpressionStmt es) => Expression(es.Expression);

	public string Visit(TypedFor for_)
	{
		return InNewEnclosingType(() =>
		{
			var init = for_.Initializer is not null ? Statement(for_.Initializer) : string.Empty;
			var cond = for_.Condition is not null ? Expression(for_.Condition) : string.Empty;
			var inc = for_.Increment is not null ? $"{Expression(for_.Increment)} " : string.Empty;
			var body = CompileLoopBody(for_.Body);
			// The compiler will always compile an Aura `for` loop to a Go `for` loop without the increment part of the `for` loop's signature. The increment is instead
			// added to the end of the loop's body. The loop's execution will remain the same, so it doesn't seem worth it to extract it from the body and put it back
			// into the loop's signature.
			return body != string.Empty ? $"for {init}; {cond}; {inc}{{{body}\n}}" : $"for {init}; {cond}; {{}}";
		}, for_);
	}

	public string Visit(TypedForEach foreach_)
	{
		return InNewEnclosingType(() =>
		{
			var iter = Expression(foreach_.Iterable);
			var body = CompileLoopBody(foreach_.Body);
			return body != string.Empty
				? $"for _, {foreach_.EachName.Value} := range {iter} {{{body}\n}}"
				: $"for _, {foreach_.EachName.Value} := range {iter} {{}}";
		}, foreach_);
	}

	public string Visit(TypedNamedFunction f)
	{
		_enclosingFunctionDeclarationStore.Push(f);
		var s = InNewEnclosingType(() =>
		{
			_declaredVariables[f.Name.Value] = f.Public;
			var funcName = f.Public is Visibility.Public ? f.Name.Value.ToUpper() : f.Name.Value.ToLower();
			var compiledParams = CompileParams(f.Params, ",");
			// Compile return type
			string returnValue = string.Empty;
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
		}, f);
		_enclosingFunctionDeclarationStore.Pop();
		return s;
	}

	public string Visit(TypedAnonymousFunction f)
	{
		return InNewEnclosingType(() =>
		{
			var compiledParams = CompileParams(f.Params, ",");
			var returnValue = f.ReturnType is not AuraNil ? $" {AuraTypeToGoType(f.ReturnType)}" : string.Empty;
			var body = Expression(f.Body);
			return $"func({compiledParams}){returnValue} {body}";
		}, f);
	}

	public string Visit(TypedLet let)
	{
		if (let.Names.Count > 1) return LetStmtMultipleNames(let);
		// Since Go's `if` statements and blocks are not expressions like they are in Aura, the compiler will first declare the variable without an initializer,
		// and then convert the `return` statement in the block or `if` expression into an assignment where the value that would be returned is instead assigned
		// to the declared variable.
		switch (let.Initializer)
		{
			case TypedBlock block:
				var decl = $"var {let.Names[0].Value} {AuraTypeToGoType(let.Typ)}";
				var body = ParseReturnableBody(block.Statements, let.Names[0].Value);
				return $"{decl}\n{{{body}\n}}";
			case TypedIf if_:
				var varName = let.Names[0].Value;
				var decl_ = $"var {varName} {AuraTypeToGoType(let.Initializer.Typ)}";
				var init = Expression(if_.Condition);
				var then = ParseReturnableBody(if_.Then.Statements, varName);
				// The `else` block can either be a block or another `if` expression
				var else_ = if_.Else switch
				{
					TypedIf @if => Visit(@if),
					TypedBlock b => ParseReturnableBody(b.Statements, varName),
					_ => string.Empty
				};
				return $"{decl_}\nif {init} {{{then}\n}} else {{{else_}\n}}";
			case TypedIs is_:
				var typedIs = Expression(is_);
				return $"_, {let.Names[0].Value} := {typedIs}";
			default:
				var value = let.Initializer is not null ? Expression(let.Initializer) : string.Empty;
				// We check to see if we are inside a `for` loop because Aura and Go differ in whether a a long variable initialization is allowed
				// as the initializer of a `for` loop. Go only allows the short syntax (i.e. `x := 0`), whereas Aura allows both styles of variable
				// declaration. Therefore, if the user has entered the full `let`-style syntax (i.e. `let x: int = 0`) inside the signature of a `for`
				// loop, it must be compiled to the short syntax in the final Go file.
				if (let.TypeAnnotation)
				{
					var b = _enclosingType.TryPeek(out var for_);
					if (!b || for_ is not TypedFor)
					{
						return $"var {let.Names[0].Value} {AuraTypeToGoType(let.Initializer!.Typ)} = {value}";
					}
				}

				return $"{let.Names[0].Value} := {value}";
		}
	}

	private string LetStmtMultipleNames(TypedLet let)
	{
		var names = string.Join(", ", let.Names.Select(n => n.Value));
		return $"{names} := {Expression(let.Initializer!)}";
	}

	public string Visit(TypedMod mod) => $"package {mod.Value.Value}";

	public string Visit(TypedReturn r)
	{
		if (r.Value is null) return "return";

		if (_enclosingFunctionDeclarationStore.Count > 0 &&
			_enclosingFunctionDeclarationStore.Peek().ReturnType is AuraResult res)
		{
			if (r.Value.Typ.IsSameOrInheritingType(res.Success)) return $"return {res}{{\nSuccess: {Expression(r.Value)},\n}}";
			return $"return {res}{{\nFailure: {Expression(r.Value)},\n}}";
		}

		if (r.Value.Typ is AuraAnonymousStruct)
		{
			var values = string.Join(", ", ((TypedAnonymousStruct)r.Value).Values.Select(Expression));
			return $"return {values}";
		}
		else return $"return {Expression(r.Value)}";
	}

	public string Visit(FullyTypedClass c)
	{
		return InNewEnclosingType(() =>
		{
			_declaredVariables[c.Name.Value] = c.Public;

			var className = c.Public == Visibility.Public ? c.Name.Value.ToUpper() : c.Name.Value.ToLower();
			var compiledParams = CompileParams(c.Params, "\n");

			var compiledMethods = c.Methods.Select(m =>
				{
					var params_ = CompileParams(m.Params, ",");
					var body = Expression(m.Body);
					var returnType = m.ReturnType is AuraNil ? string.Empty : $" {AuraTypeToGoType(m.ReturnType)}";
					// To make the handling of `this` expressions a little easier for the compiler, all method receivers in the outputted Go code have an identifier of `this`
					return $"func (this {className}) {m.Name.Value}({params_}){returnType} {body}";
				})
				.Aggregate(string.Empty, (prev, curr) => $"{prev}\n\n{curr}");

			return compiledParams != string.Empty
				? $"type {className} struct {{\n{compiledParams}\n}}\n\n{compiledMethods}"
				: $"type {className} struct {{}}\n\n{compiledMethods}";
		}, c);
	}

	public string Visit(TypedWhile w)
	{
		var cond = Expression(w.Condition);
		var body = CompileLoopBody(w.Body);
		return body != string.Empty
			? $"for {cond} {{{body}\n}}"
			: $"for {cond} {{}}";
	}

	public string Visit(TypedMultipleImport i)
	{
		var multipleImports = i.Packages.Select(CompileImportStmt);
		return $"import (\n\t{string.Join("\n\t", multipleImports)}\n)";
	}

	public string Visit(TypedImport i) => $"import {CompileImportStmt(i)}";

	private string CompileImportStmt(TypedImport i)
	{
		if (IsStdlibImportName(i.Package.Value))
		{
			var name = ExtractStdlibPkgName(i.Package.Value);
			return BuildStdlibPkgName(name);
		}
		if (i.Package.Value.Contains("prelude")) return $"prelude \"{i.Package.Value}\"";

		var compiledModule = new AuraModuleCompiler($"src/{i.Package.Value}", ProjectName).CompileModule();
		foreach (var (path, output) in compiledModule)
		{
			// Write output to `build` directory
			var dirName = Path.GetDirectoryName(path)!.Replace("src/", "");
			_outputWriter.CreateDirectory(dirName);
			_outputWriter.WriteOutput(dirName, Path.GetFileNameWithoutExtension(path), output);
		}

		return i.Alias is null
			? $"\"{ProjectName}/{i.Package.Value}\""
			: $"{i.Alias.Value.Value} \"{ProjectName}/{i.Package.Value}\"";
	}

	public string Visit(TypedComment com) => com.Text.Value;

	public string Visit(TypedContinue _) => "continue";

	public string Visit(TypedBreak _) => "break";

	public string Visit(TypedInterface i)
	{
		return InNewEnclosingType(() =>
		{
			var interfaceName = i.Public == Visibility.Public ? i.Name.Value.ToUpper() : i.Name.Value.ToLower();
			var methods = i.Methods.Select(m => m.ToStringInterface());

			return methods.Any()
				? $"type {interfaceName} interface {{\n{string.Join("\n", methods)}\n}}"
				: $"type {interfaceName} interface {{}}";
		}, i);
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
			compiledStmts.WriteString(s, stmt.Line, stmt);
		}

		return compiledStmts.String() == string.Empty ? "{}" : $"{{\n{compiledStmts.String()}\n}}";
	}

	public string Visit(TypedCall c)
	{
		if (c.Callee is TypedGet) return CallExpr_GetCallee(c);
		if (c.Callee is TypedVariable cv && cv.Typ is AuraClass) return CallExpr_Class(c);
		if (c.Callee is TypedVariable sv && sv.Typ is AuraStruct) return CallExpr_Struct(c);
		var callee = Expression((ITypedAuraExpression)c.Callee);
		var compiledParams = c.Arguments.Select(Expression);
		return $"{callee}({string.Join(", ", compiledParams)})";
	}

	private string CallExpr_Class(TypedCall c)
	{
		var v = c.Callee as TypedVariable;
		var class_ = v!.Typ as AuraClass;

		var params_ = string.Join("\n", class_!.GetParams()
			.Zip(c.Arguments)
			.Select(pair => $"{pair.First.Name.Value}: {Expression(pair.Second)},"));
		return
			$"{(class_.Public is Visibility.Public ? class_.Name.ToUpper() : class_.Name.ToLower())}{{\n{params_}\n}}";
	}

	private string CallExpr_Struct(TypedCall c)
	{
		var v = c.Callee as TypedVariable;
		var @struct = v!.Typ as AuraStruct;

		var params_ = string.Join("\n", @struct!.GetParams()
			.Zip(c.Arguments)
			.Select(pair => $"{pair.First.Name.Value}: {Expression(pair.Second)},"));

		return $"{@struct.Name.ToLower()}{{\n{params_}\n}}";
	}

	private string CallExpr_GetCallee(TypedCall c)
	{
		var get = c.Callee as TypedGet;
		var obj = Expression(get!.Obj);

		string callee;
		if (IsStdlibPkgType(get.Obj.Typ))
		{
			_goDocument.WriteStmt(
				s: Visit(
					i: new TypedImport(
						Import: new Tok(
							typ: TokType.Import,
							value: "import",
							line: 1
						),
						Package: new Tok(
							typ: TokType.Identifier,
							value: $"aura/{AuraTypeToString(get.Obj.Typ)}",
							line: 1
						),
						Alias: new Tok(
							typ: TokType.Identifier,
							value: AuraTypeToString(get.Obj.Typ),
							line: 1
						),
						Line: 1
					)
				),
				line: 1,
				typ: new TypedImport(
					Import: new Tok(
						typ: TokType.Import,
						value: "import",
						line: 1
					),
					Package: new Tok(
						typ: TokType.Identifier,
						value: $"aura/{AuraTypeToString(get.Obj.Typ)}",
						line: 1
					),
					Alias: new Tok(
						typ: TokType.Identifier,
						value: AuraTypeToString(get.Obj.Typ),
						line: 1
					),
					Line: 1
				)
			);
			callee = $"{AuraTypeToString(get.Obj.Typ)}.{ConvertSnakeCaseToCamelCase(get.Name.Value)}";
		}
		else
		{
			callee = get!.Obj.Typ is AuraModule m
				? IsStdlibPkg(m.Name)
					? $"{obj}.{ConvertSnakeCaseToCamelCase(get.Name.Value)}"
					: $"{obj}.{get.Name.Value.ToUpper()}"
				: $"{obj}.{get.Name.Value}";
		}

		if (get.Obj.Typ is not AuraModule && get.Obj.Typ is not AuraClass && get.Obj.Typ is not AuraInterface) c.Arguments.Insert(0, get!.Obj);

		var compiledParams = c.Arguments.Select(Expression);
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

	public string Visit(TypedIf if_)
	{
		var cond = Expression(if_.Condition);
		var then = Expression(if_.Then);
		var else_ = if_.Else is not null ? $" else {Expression(if_.Else)}" : string.Empty;
		return if_.Condition is TypedIs
			? $"if _, ok := {cond}; ok {then}{else_}"
			: $"if {cond} {then}{else_}";
	}

	public string Visit(StringLiteral literal) => $"\"{literal.Value}\"";

	public string Visit(CharLiteral literal) => $"'{literal.Value}'";

	public string Visit(IntLiteral literal) => $"{literal.Value}";

	public string Visit(FloatLiteral literal) =>
		string.Format(CultureInfo.InvariantCulture, "{0:0.0}", literal.Value);

	public string Visit(BoolLiteral literal) => literal.Value ? "true" : "false";

	public string Visit<U>(ListLiteral<U> literal) where U : IAuraAstNode
	{
		var items = literal.Value
			.Select(item => Expression((ITypedAuraExpression)item));
		return $"{AuraTypeToGoType(literal.Typ)}{{{string.Join(", ", items)}}}";
	}

	public string Visit(TypedNil nil) => "nil";

	public string Visit<TK, TV>(MapLiteral<TK, TV> literal)
		where TK : IAuraAstNode
		where TV : IAuraAstNode
	{
		var items = literal.Value.Select(pair =>
		{
			var keyExpr = Expression((ITypedAuraExpression)pair.Key);
			var valueExpr = Expression((ITypedAuraExpression)pair.Value);
			return $"{keyExpr}: {valueExpr}";
		});
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
		else
		{
			return v.Name.Value;
		}
	}

	public string Visit(TypedIs is_)
	{
		var expr = Expression(is_.Expr);
		return $"{expr}.({is_.Expected.Name})";
	}

	public string Visit(TypedYield y)
	{
		var value = y.Value is ILiteral lit
			? lit.ToString()
			: y.Value.ToString();
		return $"x = {value}";
	}

	private string AuraTypeToGoType(AuraType typ) => typ.ToString();

	private string CompileLoopBody(List<ITypedAuraStatement> body)
	{
		return body.Any()
			? body
				.Select(Statement)
				.Aggregate(string.Empty, (prev, curr) => $"{prev}\n{curr}")
			: string.Empty;
	}

	private string CompileParams(List<Param> params_, string sep)
	{
		return string.Join(sep, params_
			.Select(p => $"{p.Name.Value} {AuraTypeToGoType(p.ParamType.Typ)}"));
	}

	private string CompileArgs(List<ITypedAuraExpression> args)
	{
		return args
			.Select(Expression)
			.Aggregate(string.Empty, (prev, curr) => $"{prev}, {curr}");
	}

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

	private bool IsStdlibPkgType(AuraType typ)
	{
		return typ switch
		{
			AuraString or AuraList or AuraError or AuraResult => true,
			_ => false
		};
	}

	private string AuraTypeToString(AuraType typ)
	{
		return typ switch
		{
			AuraString => "strings",
			AuraList => "lists",
			AuraError => "errors",
			AuraResult => "results",
			_ => string.Empty
		};
	}

	private bool IsStdlibPkg(string pkg)
	{
		return pkg switch
		{
			"io" or "strings" or "lists" or "errors" => true,
			_ => false
		};
	}

	private bool IsStdlibImportName(string pkg)
	{
		return pkg switch
		{
			"aura/io" or "aura/strings" or "aura/lists" or "aura/errors" or "aura/results" => true,
			_ => false
		};
	}

	private string ExtractStdlibPkgName(string pkg) => pkg.Split('/').Last();

	private string BuildStdlibPkgImportStmt(string pkg) => $"import {BuildStdlibPkgName(pkg)}";

	private string BuildStdlibPkgName(string pkg) => $"{pkg} \"{ProjectName}/stdlib/{pkg}\"";

	private string LogicalOperatorToGoOperator(Tok op)
	{
		return op.Typ switch
		{
			TokType.And => "&&",
			TokType.Or => "||",
			_ => op.Value
		};
	}

	private string InNewEnclosingType(Func<string> f, ITypedAuraAstNode node)
	{
		_enclosingType.Push(node);
		var s = f();
		_enclosingType.Pop();
		return s;
	}

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

			if (s[i] == '_' && i < s.Length - 1)
			{
				camelCase.Append(char.ToUpper(s[i + 1]));
				i++;
				continue;
			}

			camelCase.Append(s[i]);
		}

		return camelCase.ToString();
	}

	private bool IsVariableInPrelude(string name)
	{
		if (_prelude.PublicVariables.ContainsKey(name)) return true;
		if (_prelude.PublicClasses.Where(c => c.Name == name).Any()) return true;
		if (_prelude.PublicFunctions.Where(f => f.Name == name).Any()) return true;
		return false;
	}

	private string AddPreludePrefix(string name)
	{
		var typedImport = new TypedImport(
			Import: new Tok(
				typ: TokType.Import,
				value: "import",
				line: 1
			),
			Package: new Tok(
				typ: TokType.Identifier,
				value: $"{ProjectName}/prelude",
				line: 1
			),
			Alias: new Tok(
				typ: TokType.Identifier,
				value: "prelude",
				line: 1
			),
			Line: 1
		);
		var preludeImport = Visit(typedImport);
		_goDocument.WriteStmt(preludeImport, 1, typedImport);
		return $"prelude.{ConvertSnakeCaseToCamelCase(name)}";
	}

	public string Visit(PartiallyTypedFunction partiallyTypedFunction)
	{
		throw new NotImplementedException();
	}

	public string Visit(PartiallyTypedClass partiallyTypedClass)
	{
		throw new NotImplementedException();
	}

	public string Visit(TypedCheck check) => $"e := {Visit(check.Call)}\nif e.Failure != nil {{\nreturn e\n}}";

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

	private string GoTypeDefaultValue(AuraType typ)
	{
		return typ switch
		{
			AuraString => "\"\"",
			AuraInt => "0",
			AuraFloat => "0.0",
			_ => string.Empty
		};
	}
}
