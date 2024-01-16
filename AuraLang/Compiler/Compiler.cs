using System.Globalization;
using System.Text;
using AuraLang.AST;
using AuraLang.Exceptions.Compiler;
using AuraLang.ModuleCompiler;
using AuraLang.Prelude;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using AuraString = AuraLang.Types.String;

namespace AuraLang.Compiler;

public class AuraCompiler
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
	private readonly Module _prelude;

	public AuraCompiler(List<ITypedAuraStatement> typedAst, string projectName,
		CompiledOutputWriter outputWriter, string filePath)
	{
		_typedAst = typedAst;
		ProjectName = projectName;
		_outputWriter = outputWriter;
		_exContainer = new CompilerExceptionContainer(filePath);
		_prelude = new AuraPrelude().GetPrelude();
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

	private string Statement(ITypedAuraStatement stmt)
	{
		return stmt switch
		{
			TypedDefer d => DeferStmt(d),
			TypedExpressionStmt es => ExpressionStmt(es),
			TypedFor for_ => ForStmt(for_),
			TypedForEach foreach_ => ForEachStmt(foreach_),
			TypedNamedFunction f => NamedFunctionStmt(f),
			TypedLet let => LetStmt(let),
			TypedMod m => ModStmt(m),
			TypedReturn r => ReturnStmt(r),
			FullyTypedClass c => ClassStmt(c),
			TypedWhile w => WhileStmt(w),
			TypedImport i => ImportStmt(i),
			TypedMultipleImport mi => MultipleImportStmt(mi),
			TypedComment c => CommentStmt(c),
			TypedContinue c => ContinueStmt(c),
			TypedBreak b => BreakStmt(b),
			TypedInterface i => InterfaceStmt(i),
			_ => throw new UnknownStatementException(stmt, stmt.Line)
		};
	}

	private string Expression(ITypedAuraExpression expr)
	{
		return expr switch
		{
			TypedAssignment a => AssignmentExpr(a),
			TypedBinary b => BinaryExpr(b),
			TypedBlock b => BlockExpr(b),
			TypedCall c => CallExpr(c),
			TypedGet g => GetExpr(g),
			TypedGetIndex g => GetIndexExpr(g),
			TypedGetIndexRange r => GetIndexRangeExpr(r),
			TypedGrouping g => GroupingExpr(g),
			TypedIf i => IfExpr(i),
			StringLiteral s => StringLiteralExpr(s),
			CharLiteral c => CharLiteralExpr(c),
			IntLiteral i => IntLiteralExpr(i),
			FloatLiteral f => FloatLiteralExpr(f),
			BoolLiteral b => BoolLiteralExpr(b),
			ListLiteral<ITypedAuraExpression> l => ListLiteralExpr(l),
			TypedNil n => NilLiteralExpr(n),
			MapLiteral<ITypedAuraExpression, ITypedAuraExpression> m => MapLiteralExpr(m),
			TypedLogical l => LogicalExpr(l),
			TypedSet s => SetExpr(s),
			TypedThis t => ThisExpr(t),
			TypedUnary u => UnaryExpr(u),
			TypedVariable v => VariableExpr(v),
			TypedAnonymousFunction f => AnonymousFunctionExpr(f),
			TypedIs is_ => IsExpr(is_),
			TypedPlusPlusIncrement ppi => IncrementExpr(ppi),
			TypedMinusMinusDecrement mmd => DecrementExpr(mmd),
			_ => throw new UnknownExpressionException(expr, expr.Line)
		};
	}

	private string DeferStmt(TypedDefer defer)
	{
		var call = CallExpr(defer.Call);
		return $"defer {call}";
	}

	private string ExpressionStmt(TypedExpressionStmt es) => Expression(es.Expression);

	private string ForStmt(TypedFor for_)
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

	private string ForEachStmt(TypedForEach foreach_)
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

	private string NamedFunctionStmt(TypedNamedFunction f)
	{
		return InNewEnclosingType(() =>
		{
			_declaredVariables[f.Name.Value] = f.Public;
			var funcName = f.Public is Visibility.Public ? f.Name.Value.ToUpper() : f.Name.Value.ToLower();
			var compiledParams = CompileParams(f.Params, ",");
			var returnValue = f.ReturnType is not Nil ? $" {AuraTypeToGoType(f.ReturnType)}" : string.Empty;
			var body = Expression(f.Body);
			return $"func {funcName}({compiledParams}){returnValue} {body}";
		}, f);
	}

	private string AnonymousFunctionExpr(TypedAnonymousFunction f)
	{
		return InNewEnclosingType(() =>
		{
			var compiledParams = CompileParams(f.Params, ",");
			var returnValue = f.ReturnType is not Nil ? $" {AuraTypeToGoType(f.ReturnType)}" : string.Empty;
			var body = Expression(f.Body);
			return $"func({compiledParams}){returnValue} {body}";
		}, f);
	}

	private string LetStmt(TypedLet let)
	{
		// Since Go's `if` statements and blocks are not expressions like they are in Aura, the compiler will first declare the variable without an initializer,
		// and then convert the `return` statement in the block or `if` expression into an assignment where the value that would be returned is instead assigned
		// to the declared variable.
		switch (let.Initializer)
		{
			case TypedBlock block:
				var decl = $"var {let.Name.Value} {AuraTypeToGoType(let.Typ)}";
				var body = ParseReturnableBody(block.Statements, let.Name.Value);
				return $"{decl}\n{{{body}\n}}";
			case TypedIf if_:
				var varName = let.Name.Value;
				var decl_ = $"var {varName} {AuraTypeToGoType(let.Initializer.Typ)}";
				var init = Expression(if_.Condition);
				var then = ParseReturnableBody(if_.Then.Statements, varName);
				// The `else` block can either be a block or another `if` expression
				var else_ = if_.Else switch
				{
					TypedIf @if => IfExpr(@if),
					TypedBlock b => ParseReturnableBody(b.Statements, varName),
					_ => string.Empty
				};
				return $"{decl_}\nif {init} {{{then}\n}} else {{{else_}\n}}";
			case TypedIs is_:
				var typedIs = Expression(is_);
				return $"_, {let.Name.Value} := {typedIs}";
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
						return $"var {let.Name.Value} {AuraTypeToGoType(let.Initializer!.Typ)} = {value}";
					}
				}

				return $"{let.Name.Value} := {value}";
		}
	}

	private string ModStmt(TypedMod mod)
	{
		return $"package {mod.Value.Value}";
	}

	private string ReturnStmt(TypedReturn r)
	{
		return r.Value is not null
			? $"return {Expression(r.Value)}"
			: "return";
	}

	private string ClassStmt(FullyTypedClass c)
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
					var returnType = m.ReturnType is Nil ? string.Empty : $" {AuraTypeToGoType(m.ReturnType)}";
					// To make the handling of `this` expressions a little easier for the compiler, all method receivers in the outputted Go code have an identifier of `this`
					return $"func (this {className}) {m.Name.Value}({params_}){returnType} {body}";
				})
				.Aggregate(string.Empty, (prev, curr) => $"{prev}\n\n{curr}");

			return compiledParams != string.Empty
				? $"type {className} struct {{\n{compiledParams}\n}}\n\n{compiledMethods}"
				: $"type {className} struct {{}}\n\n{compiledMethods}";
		}, c);
	}

	private string WhileStmt(TypedWhile w)
	{
		var cond = Expression(w.Condition);
		var body = CompileLoopBody(w.Body);
		return body != string.Empty
			? $"for {cond} {{{body}\n}}"
			: $"for {cond} {{}}";
	}

	private string MultipleImportStmt(TypedMultipleImport i)
	{
		var multipleImports = i.Packages.Select(CompileImportStmt);
		return $"import (\n\t{string.Join("\n\t", multipleImports)}\n)";
	}

	private string ImportStmt(TypedImport i) => $"import {CompileImportStmt(i)}";

	private string CompileImportStmt(TypedImport i)
	{
		if (IsStdlibImportName(i.Package.Value))
		{
			var name = ExtractStdlibPkgName(i.Package.Value);
			return BuildStdlibPkgName(name);
		}
		if (i.Package.Value.Contains("prelude")) return $"prelude \"{i.Package.Value}\"";

		try
		{
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
		catch (Exception)
		{
			throw new DirectoryCannotContainMultipleModulesException(1);
		}

	}

	private string CommentStmt(TypedComment com) => com.Text.Value;

	private string ContinueStmt(TypedContinue _) => "continue";

	private string BreakStmt(TypedBreak _) => "break";

	private string InterfaceStmt(TypedInterface i)
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

	private string AssignmentExpr(TypedAssignment assign)
	{
		var value = Expression(assign.Value);
		return $"{assign.Name.Value} = {value}";
	}

	private string IncrementExpr(TypedPlusPlusIncrement inc)
	{
		var name = Expression(inc.Name);
		return $"{name}++";
	}

	private string DecrementExpr(TypedMinusMinusDecrement dec)
	{
		var name = Expression(dec.Name);
		return $"{name}--";
	}

	private string BinaryExpr(TypedBinary b)
	{
		var left = Expression(b.Left);
		var right = Expression(b.Right);
		return $"{left} {b.Operator.Value} {right}";
	}

	private string BlockExpr(TypedBlock b)
	{
		var compiledStmts = new AuraStringBuilder();
		foreach (var stmt in b.Statements)
		{
			var s = Statement(stmt);
			compiledStmts.WriteString(s, stmt.Line, stmt);
		}

		return compiledStmts.String() == string.Empty ? "{}" : $"{{\n{compiledStmts.String()}\n}}";
	}

	private string CallExpr(TypedCall c)
	{
		if (c.Callee is TypedGet) return CallExpr_GetCallee(c);
		if (c.Callee is TypedVariable v && v.Typ is Class) return CallExpr_Class(c);
		var callee = Expression((ITypedAuraExpression)c.Callee);
		var compiledParams = c.Arguments.Select(Expression);
		return $"{callee}({string.Join(", ", compiledParams)})";
	}

	private string CallExpr_Class(TypedCall c)
	{
		var v = c.Callee as TypedVariable;
		var class_ = v!.Typ as Class;

		var params_ = string.Join("\n", class_!.GetParams()
			.Zip(c.Arguments)
			.Select(pair => $"{pair.First.Name.Value}: {Expression(pair.Second)},"));
		return
			$"{(class_.Public is Visibility.Public ? class_.Name.ToUpper() : class_.Name.ToLower())}{{\n{params_}\n}}";
	}

	private string CallExpr_GetCallee(TypedCall c)
	{
		var get = c.Callee as TypedGet;
		var obj = Expression(get!.Obj);

		string callee;
		if (IsStdlibPkgType(get.Obj.Typ))
		{
			_goDocument.WriteStmt(ImportStmt(new TypedImport(
				Package: new Tok(
					Typ: TokType.Identifier,
					Value: "aura/errors",
					Line: 1
				),
				Alias: new Tok(
					Typ: TokType.Identifier,
					Value: "errors",
					Line: 1
				),
				Line: 1
			)),
			1,
			new TypedImport(
				Package: new Tok(
					Typ: TokType.Identifier,
					Value: "aura/errors",
					Line: 1
				),
				Alias: new Tok(
					Typ: TokType.Identifier,
					Value: "errors",
					Line: 1
				),
				Line: 1
			));
			callee = $"{AuraTypeToString(get.Obj.Typ)}.{ConvertSnakeCaseToCamelCase(get.Name.Value)}";
		}
		else
		{
			callee = get!.Obj.Typ is Module m
				? IsStdlibPkg(m.Name)
					? $"{obj}.{ConvertSnakeCaseToCamelCase(get.Name.Value)}"
					: $"{obj}.{get.Name.Value.ToUpper()}"
				: $"{obj}.{get.Name.Value}";
		}
		
		if (get.Obj.Typ is not Module && get.Obj.Typ is not Class && get.Obj.Typ is not Interface) c.Arguments.Insert(0, get!.Obj);

		var compiledParams = c.Arguments.Select(Expression);
		return $"{callee}({string.Join(", ", compiledParams)})";
	}

	private string GetExpr(TypedGet get)
	{
		var objExpr = Expression(get.Obj);
		return $"{objExpr}.{get.Name.Value}";
	}

	private string GetIndexExpr(TypedGetIndex getIndex)
	{
		var objExpr = Expression(getIndex.Obj);
		var indexExpr = Expression(getIndex.Index);
		return $"{objExpr}[{indexExpr}]";
	}

	private string GetIndexRangeExpr(TypedGetIndexRange getIndexRange)
	{
		var objExpr = Expression(getIndexRange.Obj);
		var lowerExpr = Expression(getIndexRange.Lower);
		var upperExpr = Expression(getIndexRange.Upper);
		return $"{objExpr}[{lowerExpr}:{upperExpr}]";
	}

	private string GroupingExpr(TypedGrouping g)
	{
		var expr = Expression(g.Expr);
		return $"({expr})";
	}

	private string IfExpr(TypedIf if_)
	{
		var cond = Expression(if_.Condition);
		var then = Expression(if_.Then);
		var else_ = if_.Else is not null ? $" else {Expression(if_.Else)}" : string.Empty;
		return if_.Condition is TypedIs
			? $"if _, ok := {cond}; ok {then}{else_}"
			: $"if {cond} {then}{else_}";
	}

	private string StringLiteralExpr(StringLiteral literal) => $"\"{literal.Value}\"";

	private string CharLiteralExpr(CharLiteral literal) => $"'{literal.Value}'";

	private string IntLiteralExpr(IntLiteral literal) => $"{literal.Value}";

	private string FloatLiteralExpr(FloatLiteral literal) =>
		string.Format(CultureInfo.InvariantCulture, "{0:0.##}", literal.Value);

	private string BoolLiteralExpr(BoolLiteral literal) => (bool)literal.Value ? "true" : "false";

	private string ListLiteralExpr(ListLiteral<ITypedAuraExpression> literal)
	{
		var items = literal.Value
			.Select(item => Expression((ITypedAuraExpression)item));
		return $"{AuraTypeToGoType(literal.Typ)}{{{string.Join(", ", items)}}}";
	}

	private string NilLiteralExpr(TypedNil nil) => "nil";

	private string MapLiteralExpr(MapLiteral<ITypedAuraExpression, ITypedAuraExpression> literal)
	{
		var items = literal.Value.Select(pair =>
		{
			var keyExpr = Expression(pair.Key);
			var valueExpr = Expression(pair.Value);
			return $"{keyExpr}: {valueExpr}";
		});
		return items.Any()
			? $"{AuraTypeToGoType(literal.Typ)}{{\n{string.Join(", ", items)}\n}}"
			: $"{AuraTypeToGoType(literal.Typ)}{{}}";
	}

	private string LogicalExpr(TypedLogical logical)
	{
		var left = Expression(logical.Left);
		var right = Expression(logical.Right);
		return $"{left} {LogicalOperatorToGoOperator(logical.Operator)} {right}";
	}

	private string SetExpr(TypedSet set)
	{
		var obj = Expression(set.Obj);
		var value = Expression(set.Value);
		return $"{obj}.{set.Name.Value} = {value}";
	}

	private string ThisExpr(TypedThis _)
	{
		return "this";
	}

	private string UnaryExpr(TypedUnary unary)
	{
		var expr = Expression(unary.Right);
		return $"{unary.Operator.Value}{expr}";
	}

	private string VariableExpr(TypedVariable v)
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

	private string IsExpr(TypedIs is_)
	{
		var expr = Expression(is_.Expr);
		return $"{expr}.({is_.Expected.Name})";
	}

	private string YieldStmt(TypedYield y, string decl) => $"{decl} = {y.Value}";

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
			AuraString or List or Error => true,
			_ => false
		};
	}

	private string AuraTypeToString(AuraType typ)
	{
		return typ switch
		{
			AuraString => "string",
			List => "lists",
			Error => "errors",
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
			"aura/io" or "aura/strings" or "aura/lists" or "aura/errors" => true,
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
			Package: new Tok(
				Typ: TokType.Identifier,
				Value: $"{ProjectName}/prelude",
				Line: 1
			),
			Alias: new Tok(
				Typ: TokType.Identifier,
				Value: "prelude",
				Line: 1
			),
			Line: 1
		);
		var preludeImport = ImportStmt(typedImport);
		_goDocument.WriteStmt(preludeImport, 1, typedImport);
		return $"prelude.{ConvertSnakeCaseToCamelCase(name)}";
	}
}
