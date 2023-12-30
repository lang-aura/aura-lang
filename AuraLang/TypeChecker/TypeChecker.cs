using AuraLang.AST;
using AuraLang.Exceptions.TypeChecker;
using AuraLang.Parser;
using AuraLang.Scanner;
using AuraLang.Shared;
using AuraLang.Stdlib;
using AuraLang.Token;
using AuraLang.Types;
using AuraChar = AuraLang.Types.Char;
using AuraString = AuraLang.Types.String;

namespace AuraLang.TypeChecker;

public class AuraTypeChecker
{
	private readonly IVariableStore _variableStore;
	private int _scope = 1;
	private readonly IEnclosingClassStore _enclosingClassStore;
	private readonly AuraStdlib _stdlib = new();
	private readonly TypeCheckerExceptionContainer _exContainer;
	private readonly EnclosingNodeStore<IUntypedAuraExpression> _enclosingExpressionStore;
	private readonly EnclosingNodeStore<IUntypedAuraStatement> _enclosingStatementStore;
	private readonly LocalModuleReader _localModuleReader;
	private string FilePath { get; }

	public AuraTypeChecker(IVariableStore variableStore, IEnclosingClassStore enclosingClassStore,
		EnclosingNodeStore<IUntypedAuraExpression> enclosingExpressionStore,
		EnclosingNodeStore<IUntypedAuraStatement> enclosingStatementStore, LocalModuleReader localModuleReader,
		string filePath)
	{
		_variableStore = variableStore;
		_enclosingClassStore = enclosingClassStore;
		_enclosingExpressionStore = enclosingExpressionStore;
		_enclosingStatementStore = enclosingStatementStore;
		_localModuleReader = localModuleReader;
		FilePath = filePath;
		_exContainer = new TypeCheckerExceptionContainer(filePath);
	}

	public List<ITypedAuraStatement> CheckTypes(List<IUntypedAuraStatement> untypedAst)
	{
		var typedAst = new List<ITypedAuraStatement>();

		foreach (var stmt in untypedAst)
		{
			try
			{
				var typedStmt = Statement(stmt);
				typedAst.Add(typedStmt);
			}
			catch (TypeCheckerException ex)
			{
				_exContainer.Add(ex);
			}
		}

		// On the first pass of the Type Checker, some nodes are only partially type checked. On this second pass,
		// the type checking process is finished for those partially typed nodes.
		for (var i = 0; i < typedAst.Count; i++)
		{
			var f = typedAst[i] as PartiallyTypedFunction;
			if (f is not null)
			{
				try
				{
					var typedF = FinishFunctionStmt(f);
					typedAst[i] = typedF;
				}
				catch (TypeCheckerException ex)
				{
					_exContainer.Add(ex);
				}
			}
		}

		if (!_exContainer.IsEmpty()) throw _exContainer;
		return typedAst;
	}

	private ITypedAuraStatement Statement(IUntypedAuraStatement stmt)
	{
		return stmt switch
		{
			UntypedDefer defer => DeferStmt(defer),
			UntypedExpressionStmt expressionStmt => ExpressionStmt(expressionStmt),
			UntypedFor for_ => ForStmt(for_),
			UntypedForEach foreach_ => ForEachStmt(foreach_),
			UntypedNamedFunction f => PartialFunctionStmt(f, null),
			UntypedLet let => LetStmt(let),
			UntypedMod mod => ModStmt(mod),
			UntypedReturn return_ => ReturnStmt(return_),
			UntypedClass class_ => ClassStmt(class_),
			UntypedWhile while_ => WhileStmt(while_),
			UntypedImport import_ => ImportStmt(import_),
			UntypedComment comment => CommentStmt(comment),
			UntypedContinue continue_ => ContinueStmt(continue_),
			UntypedBreak break_ => BreakStmt(break_),
			UntypedYield yield => YieldStmt(yield),
			UntypedInterface i => InterfaceStmt(i),
			_ => throw new UnknownStatementTypeException(stmt.Line)
		};
	}

	private ITypedAuraExpression Expression(IUntypedAuraExpression expr)
	{
		return expr switch
		{
			UntypedAssignment assignment => AssignmentExpr(assignment),
			UntypedBinary binary => BinaryExpr(binary),
			UntypedBlock block => BlockExpr(block),
			UntypedCall call => CallExpr(call),
			UntypedGet get => GetExpr(get),
			UntypedGetIndex getIndex => GetIndexExpr(getIndex),
			UntypedGetIndexRange getIndexRange => GetIndexRangeExpr(getIndexRange),
			UntypedGrouping grouping => GroupingExpr(grouping),
			UntypedIf if_ => IfExpr(if_),
			IntLiteral i => IntLiteralExpr(i),
			FloatLiteral f => FloatLiteralExpr(f),
			StringLiteral s => StringLiteralExpr(s),
			ListLiteral<IUntypedAuraExpression> l => ListLiteralExpr(l),
			MapLiteral<IUntypedAuraExpression, IUntypedAuraExpression> m => MapLiteralExpr(m),
			BoolLiteral b => BoolLiteralExpr(b),
			UntypedNil n => NilExpr(n),
			CharLiteral c => CharLiteralExpr(c),
			UntypedLogical logical => LogicalExpr(logical),
			UntypedSet set => SetExpr(set),
			UntypedThis this_ => ThisExpr(this_),
			UntypedUnary unary => UnaryExpr(unary),
			UntypedVariable variable => VariableExpr(variable),
			UntypedAnonymousFunction f => AnonymousFunctionExpr(f),
			UntypedIs is_ => IsExpr(is_),
			UntypedPlusPlusIncrement ppi => PlusPlusIncrementExpr(ppi),
			UntypedMinusMinusDecrement mmd => MinusMinusDecrementExpr(mmd),
			_ => throw new UnknownExpressionTypeException(expr.Line)
		};
	}

	/// <summary>
	/// Type checks an expression and ensures that it matches an expected type
	/// </summary>
	/// <param name="expr">The expression to type check</param>
	/// <param name="expected">The expected type</param>
	/// <returns>The typed expression, as long as it matches the expected type</returns>
	/// <exception cref="UnexpectedTypeException">Thrown if the typed expression doesn't match
	/// the expected type</exception>
	private ITypedAuraExpression ExpressionAndConfirm(IUntypedAuraExpression expr, AuraType expected)
	{
		var typedExpr = Expression(expr);
		if (!expected.IsSameOrInheritingType(typedExpr.Typ)) throw new UnexpectedTypeException(expr.Line);
		return typedExpr;
	}

	/// <summary>
	/// Finds a variable in the symbols table, and confirms that it matches an expected type.
	/// </summary>
	/// <param name="varName">The variable's name</param>
	/// <param name="modName">The name of the variable's defining scope</param>
	/// <param name="expected">The variable's expected type</param>
	/// <param name="line">The line in the Aura source file where the variable usage appears</param>
	private T FindAndConfirm<T>(string varName, string? modName, T expected, int line) where T : AuraType
	{
		var local = _variableStore.Find(varName, modName) ?? throw new UnknownVariableException(varName, line);
		if (!expected.IsSameOrInheritingType(local.Kind)) throw new UnexpectedTypeException(line);
		return (T)local.Kind;
	}

	private Local FindOrThrow(string varName, string? modName, int line)
	{
		var local = _variableStore.Find(varName, modName) ?? throw new UnknownVariableException(varName, line);
		return local!;
	}

	/// <summary>
	/// Type checks a defer statement
	/// </summary>
	/// <param name="defer">The defer statement to type check</param>
	/// <returns>A valid, type checked defer statement</returns>
	private TypedDefer DeferStmt(UntypedDefer defer)
	{
		return _enclosingStatementStore.WithEnclosing(() =>
		{
			var typedCall = CallExpr((UntypedCall)defer.Call);
			return new TypedDefer(typedCall, defer.Line);
		}, defer);
	}

	/// <summary>
	/// Type checks an expression statement
	/// </summary>
	/// <param name="exprStmt">The expression statement to type check</param>
	/// <returns>A valid, type checked expression statement</returns>
	private TypedExpressionStmt ExpressionStmt(UntypedExpressionStmt exprStmt) =>
		new(Expression(exprStmt.Expression), exprStmt.Line);

	/// <summary>
	/// Type checks a for loop
	/// </summary>
	/// <param name="forStmt">The for loop to be type checked</param>
	/// <returns>A valid, type checked for loop</returns>
	private TypedFor ForStmt(UntypedFor forStmt)
	{
		return _enclosingStatementStore.WithEnclosing(() =>
		{
			return InNewScope(() =>
			{
				var typedInit = forStmt.Initializer is not null ? Statement(forStmt.Initializer) : null;
				var typedCond = forStmt.Condition is not null
					? ExpressionAndConfirm(forStmt.Condition, new Bool())
					: null;
				var typedInc = forStmt.Increment is not null
					? Expression(forStmt.Increment)
					: null;
				var typedBody = NonReturnableBody(forStmt.Body);
				return new TypedFor(typedInit, typedCond, typedInc, typedBody, forStmt.Line);
			});
		}, forStmt);
	}

	/// <summary>
	/// Type checks a for each loop
	/// </summary>
	/// <param name="forEachStmt">The for each loop to be type checked</param>
	/// <returns>A valid, type checked for each loop</returns>
	/// <exception cref="ExpectIterableException">Thrown if the value being iterated over does not implement
	/// the IIterable interface</exception>
	private TypedForEach ForEachStmt(UntypedForEach forEachStmt)
	{
		return _enclosingStatementStore.WithEnclosing(() =>
		{
			return InNewScope(() =>
			{
				// Type check iterable
				var iter = Expression(forEachStmt.Iterable);
				if (iter.Typ is not IIterable typedIter) throw new ExpectIterableException(forEachStmt.Line);
				// Add current element variable to list of local variables
				_variableStore.Add(new Local(forEachStmt.EachName.Value, typedIter.GetIterType(), _scope, null));
				// Type check body
				var typedBody = NonReturnableBody(forEachStmt.Body);
				return new TypedForEach(forEachStmt.EachName, iter, typedBody, forEachStmt.Line);
			});
		}, forEachStmt);
	}

	/// <summary>
	/// Type checks a named function declaration
	/// </summary>
	/// <param name="f">The named function to type check</param>
	/// <param name="modName">The module name where the function is declared</param>
	/// <returns>A valid, type checked named function declaration</returns>
	/// <exception cref="TypeMismatchException">Thrown if the function's body doesn't return
	/// the same type as specified in the function's signature</exception>
	private TypedNamedFunction NamedFunctionStmt(UntypedNamedFunction f, string modName)
	{
		return _enclosingStatementStore.WithEnclosing(() =>
		{
			return InNewScope(() =>
			{
				var typedParams = TypeCheckParams(f.Params);
				// Add parameters as local variables
				foreach (var param in typedParams)
				{
					_variableStore.Add(new Local(
						param.Name.Value,
						param.ParamType.Typ,
						_scope,
						null));
				}

				var typedBody = BlockExpr(f.Body);
				// Ensure the function's body returns the type specified in its signature
				var returnType = TypeCheckReturnTypeTok(f.ReturnType);
				if (!returnType.IsSameOrInheritingType(typedBody.Typ))
					throw new TypeMismatchException(f.Line);
				// Add function as local variable
				_variableStore.Add(new Local(
					f.Name.Value,
					new NamedFunction(
						f.Name.Value,
						f.Public,
						new Function(
							TypeCheckParams(f.Params),
							returnType)
					),
					_scope,
					null));
				return new TypedNamedFunction(f.Name, typedParams.ToList(), typedBody, returnType, f.Public, f.Line);
			});
		}, f);
	}

	/// <summary>
	/// Type checks an anonymous function declaration
	/// </summary>
	/// <param name="f">The anonymous function to type check</param>
	/// <returns>A valid, type checked anonymous function declaration</returns>
	/// <exception cref="TypeMismatchException">Thrown if the anonymous function's body returns a type different
	/// than the one specified in the function's signature</exception>
	private TypedAnonymousFunction AnonymousFunctionExpr(UntypedAnonymousFunction f)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			return InNewScope(() =>
			{
				var typedParams = TypeCheckParams(f.Params);
				// Add the function's parameters as local variables
				foreach (var param in typedParams)
				{
					_variableStore.Add(new Local(
						param.Name.Value,
						param.ParamType.Typ,
						_scope,
						null));
				}

				var typedBody = BlockExpr(f.Body);
				// Ensure the function's body returns the type specified in its signature
				var returnType = TypeCheckReturnTypeTok(f.ReturnType);
				if (!returnType.IsSameOrInheritingType(typedBody.Typ))
					throw new TypeMismatchException(f.Line);

				return new TypedAnonymousFunction(typedParams, typedBody, returnType, f.Line);
			});
		}, f);
	}

	private PartiallyTypedFunction PartialFunctionStmt(UntypedNamedFunction f, string? modName)
	{
		var typedParams = TypeCheckParams(f.Params);
		var returnType = TypeCheckReturnTypeTok(f.ReturnType);
		// Add function as local
		_variableStore.Add(new Local(
			f.Name.Value,
			new NamedFunction(f.Name.Value, f.Public, new Function(typedParams, returnType)),
			_scope,
			null));

		return new PartiallyTypedFunction(
			f.Name,
			f.Params,
			f.Body,
			f.ReturnType is not null ? TypeTokenToType(f.ReturnType.Value) : new Nil(),
			f.Public,
			f.Line);
	}

	private TypedNamedFunction FinishFunctionStmt(PartiallyTypedFunction f)
	{
		var typedParams = TypeCheckParams(f.Params);
		// Add parameters as local variables
		foreach (var param in f.Params)
		{
			var paramTyp = param.ParamType.Typ;
			if (param.ParamType.Variadic) paramTyp = new List(paramTyp);
			_variableStore.Add(new Local(
				param.Name.Value,
				paramTyp,
				_scope + 1,
				null));
		}

		var typedBody = BlockExpr(f.Body);
		// Ensure the function's body returns the same type specified in its signature
		if (!f.ReturnType.IsSameOrInheritingType(typedBody.Typ)) throw new TypeMismatchException(f.Line);

		return new TypedNamedFunction(f.Name, typedParams, typedBody, f.ReturnType, f.Public, f.Line);
	}

	/// <summary>
	/// Type checks a let statement
	/// </summary>
	/// <param name="let">The let statement to type check</param>
	/// <returns>A valid, type checked let statement</returns>
	private TypedLet LetStmt(UntypedLet let)
	{
		return _enclosingStatementStore.WithEnclosing(() =>
		{
			if (let.NameTyp is null) return ShortLetStmt(let);
			var nameTyp = let.NameTyp;
			// Type check initializer
			var defaultable = nameTyp as IDefaultable;
			if (let.Initializer is null && defaultable is null)
				throw new MustSpecifyInitialValueForNonDefaultableTypeException(nameTyp, let.Line);
			var typedInit = let.Initializer is not null
				? ExpressionAndConfirm(let.Initializer, nameTyp)
				: defaultable!.Default(let.Line);
			// Add new variable to list of locals
			_variableStore.Add(new Local(
				let.Name.Value,
				typedInit?.Typ ?? new Nil(),
				_scope,
				null));

			return new TypedLet(let.Name, true, let.Mutable, typedInit, let.Line);
		}, let);
	}

	/// <summary>
	/// Type checks a short let statement
	/// </summary>
	/// <param name="let">The short let statement to type check</param>
	/// <returns>A valid, type checked short let statement</returns>
	private TypedLet ShortLetStmt(UntypedLet let)
	{
		return _enclosingStatementStore.WithEnclosing(() =>
		{
			// Type check initializer
			var typedInit = let.Initializer is not null ? Expression(let.Initializer) : null;
			// Add new variable to list of locals
			_variableStore.Add(new Local(
				let.Name.Value,
				typedInit?.Typ ?? new Nil(),
				_scope,
				null));

			return new TypedLet(let.Name, false, let.Mutable, typedInit, let.Line);
		}, let);
	}

	/// <summary>
	/// Type checks a mod statement, and saves the typed mod as the current mod
	/// </summary>
	/// <param name="mod">The mod statement to be type checked</param>
	/// <returns>A valid, type checked mod statement</returns>
	private TypedMod ModStmt(UntypedMod mod)
	{
		var m = new TypedMod(mod.Value, mod.Line);
		return m;
	}

	/// <summary>
	/// Type checks a return statement
	/// </summary>
	/// <param name="r">The return statement to type check</param>
	/// <returns>A valid, type checked return statement</returns>
	private TypedReturn ReturnStmt(UntypedReturn r)
	{
		return _enclosingStatementStore.WithEnclosing(() =>
		{
			var typedVal = r.Value is not null ? Expression(r.Value) : null;
			return new TypedReturn(typedVal, r.Line);
		}, r);
	}

	/// <summary>
	/// Type checks a class declaration
	/// </summary>
	/// <param name="class_">The class declaration to type check</param>
	/// <returns>A valid, type checked class declaration</returns>
	private FullyTypedClass ClassStmt(UntypedClass class_)
	{
		return _enclosingStatementStore.WithEnclosing(() =>
		{
			var typedParams = class_.Params.Select(p =>
			{
				var typedDefaultValue = p.ParamType.DefaultValue is not null
					? (ILiteral)Expression(p.ParamType.DefaultValue)
					: null;
				var paramTyp = p.ParamType.Typ;
				return new Param(p.Name, new ParamType(paramTyp, p.ParamType.Variadic, typedDefaultValue));
			});

			var partiallyTypedMethods = class_.Methods.Select(m => PartialFunctionStmt(m, "main")).ToList();
			var methodTypes = partiallyTypedMethods
				.Select(method =>
				{
					var typedMethodParams = method.Params.Select(p =>
					{
						var typedMethodDefaultValue = p.ParamType.DefaultValue is not null
							? (ILiteral)Expression(p.ParamType.DefaultValue)
							: null;
						var methodParamType = p.ParamType.Typ;
						return new Param(p.Name,
							new ParamType(methodParamType, p.ParamType.Variadic, typedMethodDefaultValue));
					});
					return new NamedFunction(
						method.Name.Value,
						method.Public,
						new Function(typedMethodParams.ToList(), method.ReturnType));
				})
				.ToList();

			// Get type of implementing interface
			var implements = class_.Implementing.Any()
				? class_.Implementing.Select(impl =>
				{
					var local = FindOrThrow(impl.Value, null, class_.Line);
					var i = local.Kind as Interface ??
							throw new CannotImplementNonInterfaceException(impl.Value, class_.Line);
					return i;
				})
				: new List<Interface>();

			// Add typed class to list of locals
			_variableStore.Add(new Local(
				class_.Name.Value,
				new Class(class_.Name.Value, typedParams.ToList(), methodTypes, implements.ToList(), class_.Public),
				_scope,
				null));

			// Store the partially typed class as the current enclosing class
			var partiallyTypedClass = new PartiallyTypedClass(
				class_.Name,
				class_.Params,
				partiallyTypedMethods,
				class_.Public,
				new Class(class_.Name.Value, typedParams.ToList(), methodTypes, implements.ToList(), class_.Public),
				class_.Line);
			_enclosingClassStore.Push(partiallyTypedClass);
			// Finish type checking the class's methods
			var typedMethods = partiallyTypedClass.Methods
				.Select(FinishFunctionStmt)
				.ToList();

			// If the class implements any interfaces, ensure that it contains all required methods
			if (implements.Any())
			{
				var valid = implements.Select(impl =>
					{
						return impl.Functions.Select(f =>
							{
								return typedMethods
									.Where(m => m.Public == Visibility.Public)
									.Select(tm => tm.GetFunctionType())
									.Contains(f);
							})
							.All(b => b);
					})
					.All(b => b);
				if (!valid) throw new MissingInterfaceMethodException(string.Empty, string.Empty, class_.Line); // TODO
			}

			_enclosingClassStore.Pop();
			return new FullyTypedClass(class_.Name, typedParams.ToList(), typedMethods, class_.Public,
				implements.ToList(), class_.Line);
		}, class_);
	}

	/// <summary>
	/// Type checks a while loop
	/// </summary>
	/// <param name="while_">The while loop to be type checked</param>
	/// <returns>A valid, type checked while loop</returns>
	private TypedWhile WhileStmt(UntypedWhile while_)
	{
		return _enclosingStatementStore.WithEnclosing(() =>
		{
			return InNewScope(() =>
			{
				var typedCond = ExpressionAndConfirm(while_.Condition, new Bool());
				var typedBody = NonReturnableBody(while_.Body);
				return new TypedWhile(typedCond, typedBody, while_.Line);
			});
		}, while_);
	}

	private TypedImport ImportStmt(UntypedImport import_)
	{
		// First, check if the module being imported is built-in
		if (!_stdlib.TryGetModule(import_.Package.Value, out var module))
		{
			// Read all Aura source files in the specified directory
			var exportedTypes = _localModuleReader.GetModuleSourcePaths($"src/{import_.Package.Value}")
				.Select(f =>
				{
					// Read the file's contents
					var contents = _localModuleReader.Read(f);
					// Scan file
					var tokens = new AuraScanner(contents, FilePath).ScanTokens().Where(tok => tok.Typ is not TokType.Newline).ToList();
					// Parse file
					var untypedAst = new AuraParser(tokens, FilePath).Parse();
					// Type check file
					var typedAst = CheckTypes(untypedAst);
					// Extract public methods and classes
					var methods = typedAst
						.Where(node => node.Typ is NamedFunction)
						.Select(node => (node.Typ as NamedFunction)!);
					var classes = typedAst
						.Where(node => node.Typ is Class)
						.Select(node => (node.Typ as Class)!);
					var variables = typedAst
						.Where(node => node is TypedLet)
						.Select(node => (node as TypedLet)!);
					return (methods, classes, variables);
				})
				.Aggregate((a, b) => (a.methods.Concat(b.methods), a.classes.Concat(b.classes),
					a.variables.Concat(b.variables)));
			var importedModule = new Module(
				import_.Package.Value,
				exportedTypes.methods.ToList(),
				exportedTypes.classes.ToList(),
				exportedTypes.variables.ToDictionary(v => v.Name.Value, v => v.Initializer!)
			);
			// Add module to list of local variables
			var modName = import_.Alias is not null
				? import_.Alias.Value.Value
				: import_.Package.Value.Split("/")[^1];
			_variableStore.Add(new Local(
				modName,
				importedModule,
				_scope,
				null
			));
			// Add local module's exported types to current scope
			foreach (var f in importedModule.PublicFunctions)
			{
				_variableStore.Add(new Local(
					f.Name,
					f,
					_scope,
					modName));
			}

			foreach (var c in importedModule.PublicClasses)
			{
				_variableStore.Add(new Local(
					c.Name,
					c,
					_scope,
					modName));
			}

			foreach (var v in importedModule.PublicVariables)
			{
				_variableStore.Add(new Local(
					v.Key,
					v.Value.Typ,
					_scope,
					modName));
			}

			return new TypedImport(import_.Package, import_.Alias, import_.Line);
		}
		else
		{
			// Add module to list of local variables
			_variableStore.Add(new Local(
				"io",
				module!,
				_scope,
				null));
			// Add local module's public functions to current scope
			foreach (var f in module!.PublicFunctions)
			{
				_variableStore.Add(new Local(
					f.Name,
					f,
					_scope,
					null));
			}

			return new TypedImport(import_.Package, import_.Alias, import_.Line);
		}
	}

	/// <summary>
	/// Type checks a comment. This method is basically a no-op, since comments don't have a type, nor do they
	/// contain any typed information.
	/// </summary>
	/// <param name="comment">The comment to type check</param>
	/// <returns>A valid, type checked comment</returns>
	private TypedComment CommentStmt(UntypedComment comment) => new(comment.Text, comment.Line);

	/// <summary>
	/// Type checks a continue statement. This method is basically a no-op, since continue statements don't
	/// have a type.
	/// </summary>
	/// <param name="continue_">The continue statement to type check</param>
	/// <returns>A valid, type checked continue statement</returns>
	private TypedContinue ContinueStmt(UntypedContinue continue_)
	{
		var enclosingStmt = _enclosingStatementStore.Peek();
		if (enclosingStmt is not UntypedWhile && enclosingStmt is not UntypedFor && enclosingStmt is not UntypedForEach)
			throw new InvalidUseOfContinueKeywordException(continue_.Line);
		return new TypedContinue(continue_.Line);
	}

	/// <summary>
	/// Type checks a break statement. This method is basically a no-op, since break statements don't
	/// have a type.
	/// </summary>
	/// <param name="b">The break statement to type check</param>
	/// <returns>A valid, type checked break statement</returns>
	private TypedBreak BreakStmt(UntypedBreak b)
	{
		var enclosingStmt = _enclosingStatementStore.Peek();
		if (enclosingStmt is not UntypedWhile && enclosingStmt is not UntypedFor && enclosingStmt is not UntypedForEach)
			throw new InvalidUseOfBreakKeywordException(b.Line);
		return new TypedBreak(b.Line);
	}

	/// <summary>
	/// Type checks a yield statement. The <c>yield</c> keyword can only be used inside of an <c>if</c> expression or
	/// a block. All other uses of the <c>yield</c> keyword will result in an exception.
	/// </summary>
	/// <param name="y">The yield statement to type check</param>
	/// <returns>A valid, type checked yield statement</returns>
	/// <exception cref="InvalidUseOfYieldKeywordException">Thrown if the yield statement is not used inside of an <c>if</c>
	/// expression or a block</exception>
	private TypedYield YieldStmt(UntypedYield y)
	{
		var enclosingExpr = _enclosingExpressionStore.Peek();
		if (enclosingExpr is not UntypedIf && enclosingExpr is not UntypedBlock)
			throw new InvalidUseOfYieldKeywordException(y.Line);

		var value = Expression(y.Value);
		return new TypedYield(value, y.Line);
	}

	/// <summary>
	/// Type checks an interface declaration
	/// </summary>
	/// <param name="i">The interface declaration to type check</param>
	/// <returns>A valid, type checked interface</returns>
	private TypedInterface InterfaceStmt(UntypedInterface i)
	{
		return _enclosingStatementStore.WithEnclosing(() =>
		{
			// Add interface to list of local variables
			_variableStore.Add(new Local(
				i.Name.Value,
				new Interface(
					i.Name.Value,
					i.Methods,
					i.Public),
				_scope,
				null));

			return new TypedInterface(i.Name, i.Methods, i.Public, i.Line);
		}, i);
	}

	/// <summary>
	/// Type checks an assignment expression
	/// </summary>
	/// <param name="assignment">The assignment expression to type check</param>
	/// <returns>A valid, type checked assignment expression</returns>
	private TypedAssignment AssignmentExpr(UntypedAssignment assignment)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			// Fetch the variable being assigned to
			var v = FindOrThrow(assignment.Name.Value, null, assignment.Line);
			// Ensure that the new value and the variable have the same type
			var typedExpr = ExpressionAndConfirm(assignment.Value, v.Kind);
			return new TypedAssignment(assignment.Name, typedExpr, typedExpr.Typ, assignment.Line);
		}, assignment);
	}

	private TypedPlusPlusIncrement PlusPlusIncrementExpr(UntypedPlusPlusIncrement inc)
	{
		var name = Expression(inc.Name);
		// Ensure that expression has type of either int or float
		if (name.Typ is not Int && name.Typ is not Float) throw new CannotIncrementNonNumberException(inc.Line);

		return new TypedPlusPlusIncrement(name, name.Typ, inc.Line);
	}

	private TypedMinusMinusDecrement MinusMinusDecrementExpr(UntypedMinusMinusDecrement dec)
	{
		var name = Expression(dec.Name);
		// Ensure that expression has type of either int or float
		if (name.Typ is not Int && name.Typ is not Float) throw new CannotDecrementNonNumberException(dec.Line);

		return new TypedMinusMinusDecrement(name, name.Typ, dec.Line);
	}

	/// <summary>
	/// Type checks a binary expression
	/// </summary>
	/// <param name="binary">The binary expression to type check</param>
	/// <returns>A valid, type checked binary expression</returns>
	private TypedBinary BinaryExpr(UntypedBinary binary)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			var typedLeft = Expression(binary.Left);
			// The right-hand expression must have the same type as the left-hand expression
			var typedRight = ExpressionAndConfirm(binary.Right, typedLeft.Typ);
			return new TypedBinary(typedLeft, binary.Operator, typedRight, typedLeft.Typ, binary.Line);
		}, binary);
	}

	/// <summary>
	/// Type checks a block expression
	/// </summary>
	/// <param name="block">The block expression to type check</param>
	/// <returns>A valid, type checked block expression</returns>
	private TypedBlock BlockExpr(UntypedBlock block)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			return InNewScope(() =>
			{
				var typedStmts = new List<ITypedAuraStatement>();
				foreach (var stmt in block.Statements)
				{
					typedStmts.Add(Statement(stmt));
				}

				// The block's type is the type of its last statement
				AuraType blockTyp = new Nil();
				if (typedStmts.Any())
				{
					var lastStmt = typedStmts.Last();
					blockTyp = lastStmt switch
					{
						TypedReturn r => r.Value is not null ? r.Value.Typ : new Nil(),
						TypedYield y => y.Value.Typ,
						_ => lastStmt.Typ is not None ? lastStmt.Typ : new Nil()
					};
				}

				return new TypedBlock(typedStmts.ToList(), blockTyp, block.Line);
			});
		}, block);
	}

	/// <summary>
	/// Type checks a call expression
	/// </summary>
	/// <param name="call">The call expression to type check</param>
	/// <returns>A valid, type checked call expression</returns>
	/// <exception cref="IncorrectNumberOfArgumentsException">Thrown if the number of arguments provided does
	/// not match the expected number of parameters</exception>
	private TypedCall CallExpr(UntypedCall call)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			var f = FindOrThrow(call.Callee.GetName(), null, call.Line);
			var funcDeclaration = f.Kind as ICallable;
			// Type check arguments
			if (call.Arguments.Any())
			{
				var named = call.Arguments.All(arg => arg.Item1 is not null);
				var positional = call.Arguments.All(arg => arg.Item1 is null);

				if (named) return TypeCheckNamedParameters(call, funcDeclaration!);
				if (positional) return TypeCheckPositionalParameters(call, funcDeclaration!);
				throw new CannotMixNamedAndUnnamedArgumentsException(call.GetName(), call.Line);
			}

			var typedCallee = Expression((IUntypedAuraExpression)call.Callee) as ITypedAuraCallable;
			if (!funcDeclaration!.GetParams().Any())
			{
				return new TypedCall(typedCallee!, new List<ITypedAuraExpression>(), funcDeclaration!.GetReturnType(),
					call.Line);
			}
			else
			{
				// Add default values, if any
				var typedArgs = new List<ITypedAuraExpression>();
				foreach (var arg in funcDeclaration!.GetParams())
				{
					var index = funcDeclaration.GetParamIndex(arg.Name.Value);
					var defaultValue = arg.ParamType.DefaultValue ??
									   throw new MustSpecifyValueForArgumentWithoutDefaultValueException(call.GetName(), arg.Name.Value, call.Line);
					if (index >= typedArgs.Count) typedArgs.Add(defaultValue);
					else typedArgs.Insert(index, defaultValue);
				}

				return new TypedCall(typedCallee!, typedArgs, funcDeclaration!.GetReturnType(), call.Line);
			}
		}, call);
	}

	/// <summary>
	/// Type checks a get expression
	/// </summary>
	/// <param name="get">The get expression to type check</param>
	/// <returns>A valid, type checked get expression</returns>
	private TypedGet GetExpr(UntypedGet get)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			// Type check object, which must be gettable
			var objExpr = Expression(get.Obj);
			if (objExpr.Typ is not IGettable g) throw new CannotGetFromNonClassException(objExpr.ToString()!, objExpr.Typ, get.GetName(), get.Line);
			// Fetch the gettable's attribute
			var attrTyp = g.Get(get.Name.Value);
			if (attrTyp is null) throw new ClassAttributeDoesNotExistException(objExpr.ToString()!, get.GetName(), get.Line);

			return new TypedGet(objExpr, get.Name, attrTyp, get.Line);
		}, get);
	}

	private TypedSet SetExpr(UntypedSet set)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			var typedObj = Expression(set.Obj);
			if (typedObj.Typ is not IGettable g) throw new CannotSetOnNonClassException(set.Line);
			var typedValue = Expression(set.Value);
			return new TypedSet(typedObj, set.Name, typedValue, typedValue.Typ, set.Line);
		}, set);
	}

	/// <summary>
	/// Type checks a get index expression
	/// </summary>
	/// <param name="getIndex">The get index expression to type check</param>
	/// <returns>A valid, type checked get index expression</returns>
	/// <exception cref="ExpectIndexableException">Thrown if the object being indexed does
	/// not implement the IIndexable interface</exception>
	/// <exception cref="TypeMismatchException">Thrown if the value used as the index is not the
	/// correct type</exception>
	private TypedGetIndex GetIndexExpr(UntypedGetIndex getIndex)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			var expr = Expression(getIndex.Obj);
			var indexExpr = Expression(getIndex.Index);
			// Ensure that the object is indexable
			if (expr.Typ is not IIndexable indexableExpr) throw new ExpectIndexableException(getIndex.Line);
			if (!indexableExpr.IndexingType().IsSameType(indexExpr.Typ))
				throw new TypeMismatchException(getIndex.Line);

			return new TypedGetIndex(expr, indexExpr, indexableExpr.GetIndexedType(), getIndex.Line);
		}, getIndex);
	}

	/// <summary>
	/// Type checks a get index range expression
	/// </summary>
	/// <param name="getIndexRange">The get index range expression to type check</param>
	/// <returns>A valid, type checked get index range expression</returns>
	/// <exception cref="ExpectRangeIndexableException">Thrown if the object being indexed does
	/// not implement hte IRangeIndexable interface</exception>
	/// <exception cref="TypeMismatchException">Thrown if the values used as the indices are not the
	/// correct type</exception>
	private TypedGetIndexRange GetIndexRangeExpr(UntypedGetIndexRange getIndexRange)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			var expr = Expression(getIndexRange.Obj);
			var lower = Expression(getIndexRange.Lower);
			var upper = Expression(getIndexRange.Upper);
			// Ensure that the object is range indexable
			if (expr.Typ is not IRangeIndexable rangeIndexableExpr)
				throw new ExpectRangeIndexableException(getIndexRange.Line);
			if (!rangeIndexableExpr.IndexingType().IsSameType(lower.Typ))
				throw new TypeMismatchException(getIndexRange.Line);
			if (!rangeIndexableExpr.IndexingType().IsSameType(upper.Typ))
				throw new TypeMismatchException(getIndexRange.Line);

			return new TypedGetIndexRange(expr, lower, upper, expr.Typ, getIndexRange.Line);
		}, getIndexRange);
	}

	/// <summary>
	/// Type checks a grouping expression
	/// </summary>
	/// <param name="grouping">The grouping expression to type check</param>
	/// <returns>A valid, type checked grouping expression</returns>
	private TypedGrouping GroupingExpr(UntypedGrouping grouping)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			var typedExpr = Expression(grouping.Expr);
			return new TypedGrouping(typedExpr, typedExpr.Typ, grouping.Line);
		}, grouping);
	}

	/// <summary>
	/// Type check if expression
	/// </summary>
	/// <param name="if_">The if expression to type check</param>
	/// <returns>A valid, type checked if expression</returns>
	private TypedIf IfExpr(UntypedIf if_)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			var typedCond = ExpressionAndConfirm(if_.Condition, new Bool());
			var typedThen = BlockExpr(if_.Then);
			// Type check else branch
			ITypedAuraExpression? typedElse = null;
			if (if_.Else is not null)
			{
				typedElse = ExpressionAndConfirm(if_.Else, typedThen.Typ);
			}

			return new TypedIf(typedCond, typedThen, typedElse, typedThen.Typ, if_.Line);
		}, if_);
	}

	private IntLiteral IntLiteralExpr(IntLiteral literal) => literal;

	private FloatLiteral FloatLiteralExpr(FloatLiteral literal) => literal;

	private StringLiteral StringLiteralExpr(StringLiteral literal) => literal;

	private ListLiteral<ITypedAuraExpression> ListLiteralExpr(ListLiteral<IUntypedAuraExpression> literal)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			var items = literal.Value;
			var typedItem = Expression(items.First());
			var typedItems = items.Select(item => ExpressionAndConfirm(item, typedItem.Typ)).ToList();
			return new ListLiteral<ITypedAuraExpression>(typedItems, new List(typedItem.Typ), literal.Line);
		}, literal);
	}

	private MapLiteral<ITypedAuraExpression, ITypedAuraExpression> MapLiteralExpr(
		MapLiteral<IUntypedAuraExpression, IUntypedAuraExpression> literal)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			var m = literal.Value;
			var typedKey = Expression(m.Keys.First());
			var typedValue = Expression(m.Values.First());
			var typedM = m.Select(pair =>
			{
				var typedK = ExpressionAndConfirm(pair.Key, typedKey.Typ);
				var typedV = ExpressionAndConfirm(pair.Value, typedValue.Typ);
				return (typedK, typedV);
			}).ToDictionary(pair => pair.Item1, pair => pair.Item2);
			return new MapLiteral<ITypedAuraExpression, ITypedAuraExpression>(typedM, typedKey.Typ, typedValue.Typ,
				literal.Line);
		}, literal);
	}

	private BoolLiteral BoolLiteralExpr(BoolLiteral literal) => literal;

	private TypedNil NilExpr(UntypedNil literal) => new(literal.Line);

	private CharLiteral CharLiteralExpr(CharLiteral literal) => literal;

	/// <summary>
	/// Type checks a `this` expression
	/// </summary>
	/// <param name="this_">The `this` expression to type check</param>
	/// <returns>A valid, type checked `this` expression</returns>
	private TypedThis ThisExpr(UntypedThis this_) => new(this_.Keyword, _enclosingClassStore.Peek().Typ, this_.Line);

	/// <summary>
	/// Type checks a unary expression
	/// </summary>
	/// <param name="unary">The unary expression to type check</param>
	/// <returns>A valid, type checked unary expression</returns>
	/// <exception cref="MismatchedUnaryOperatorAndOperandException">Thrown if the unary expression's operator and
	/// operand are not compatible</exception>
	private TypedUnary UnaryExpr(UntypedUnary unary)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			var typedRight = Expression(unary.Right);
			// Ensure that operand is a valid type and the operand can be used with it
			if (unary.Operator.Typ is TokType.Minus)
			{
				if (typedRight.Typ is not Int && typedRight.Typ is not Float)
					throw new MismatchedUnaryOperatorAndOperandException(unary.Operator.Value, typedRight.Typ, unary.Line);
			}
			else if (unary.Operator.Typ is TokType.Minus)
			{
				if (typedRight.Typ is not Bool)
					throw new MismatchedUnaryOperatorAndOperandException(unary.Operator.Value, typedRight.Typ, unary.Line);
			}

			return new TypedUnary(unary.Operator, typedRight, typedRight.Typ, unary.Line);
		}, unary);
	}

	/// <summary>
	/// Type checks a variable expression
	/// </summary>
	/// <param name="v">The variable expression to type check</param>
	/// <returns>A valid, type checked variable expression</returns>
	private TypedVariable VariableExpr(UntypedVariable v)
	{
		var localVar = FindOrThrow(v.Name.Value, null, v.Line);
		var kind = !localVar.Kind.IsSameType(new Unknown(string.Empty))
			? localVar.Kind
			: FindOrThrow(((Unknown)localVar.Kind).Name, null, v.Line).Kind;
		return new TypedVariable(v.Name, kind, v.Line);
	}

	private TypedIs IsExpr(UntypedIs is_)
	{
		var typedExpr = Expression(is_.Expr);
		// Ensure the expected type is an interface
		var i = FindAndConfirm(is_.Expected.Value, null,
			new Interface(string.Empty, new List<NamedFunction>(), Visibility.Private), is_.Line);

		return new TypedIs(typedExpr, i, is_.Line);
	}

	/// <summary>
	/// Type checks a logical expression
	/// </summary>
	/// <param name="logical">The logical expression to type check</param>
	/// <returns>A valid, type checked logical expression</returns>
	private TypedLogical LogicalExpr(UntypedLogical logical)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			var typedLeft = Expression(logical.Left);
			var typedRight = ExpressionAndConfirm(logical.Right, typedLeft.Typ);
			return new TypedLogical(typedLeft, logical.Operator, typedRight, new Bool(), logical.Line);
		}, logical);
	}

	private void ExitScope()
	{
		// Before exiting block, remove any variables created in this scope
		_variableStore.ExitScope(_scope);
		_scope--;
	}

	private List<ITypedAuraStatement> NonReturnableBody(List<IUntypedAuraStatement> body)
	{
		var typedBody = new List<ITypedAuraStatement>();
		foreach (var stmt in body)
		{
			var typedStmt = Statement(stmt);
			typedBody.Add(typedStmt);
		}

		return typedBody;
	}

	private T InNewScope<T>(Func<T> f) where T : ITypedAuraAstNode
	{
		_scope++;
		var typedNode = f();
		ExitScope();
		return typedNode;
	}


	private AuraType TypeTokenToType(Tok tok)
	{
		return tok.Typ switch
		{
			TokType.Int => new Int(),
			TokType.Float => new Float(),
			TokType.String => new AuraString(),
			TokType.Bool => new Bool(),
			TokType.Any => new Any(),
			TokType.Char => new AuraChar(),
			_ => throw new UnexpectedTypeException(tok.Line)
		};
	}

	private List<Param> TypeCheckParams(List<Param> untypedParams)
	{
		return untypedParams.Select(p =>
		{
			var typedDefaultValue = p.ParamType.DefaultValue is not null
				? (ILiteral)Expression(p.ParamType.DefaultValue)
				: null;
			var paramTyp = p.ParamType.Typ is not Unknown u
				? p.ParamType.Typ
				: FindOrThrow(u.Name, null, p.Name.Line).Kind;
			return new Param(p.Name, new ParamType(paramTyp, p.ParamType.Variadic, typedDefaultValue));
		}).ToList();
	}

	private List<ParamType> TypeCheckParamTypes(List<Param> untypedParams)
	{
		return TypeCheckParams(untypedParams)
			.Select(p => p.ParamType)
			.ToList();
	}

	private TypedCall TypeCheckPositionalParameters(UntypedCall call, ICallable declaration)
	{
		var typedCallee = Expression((IUntypedAuraExpression)call.Callee) as ITypedAuraCallable;
		// Ensure the function call has the correct number of arguments
		if (declaration.HasVariadicParam() && call.Arguments.Count < declaration.GetParams().Count)
			throw new IncorrectNumberOfArgumentsException(call.Arguments.Count, declaration.GetParams().Count, call.Line);
		if (!declaration.HasVariadicParam() && declaration.GetParamTypes().Count != call.Arguments.Count)
			throw new IncorrectNumberOfArgumentsException(call.Arguments.Count, declaration.GetParams().Count, call.Line);
		// The arguments are already in order when using positional arguments, so just extract the arguments
		var orderedArgs = call.Arguments
			.Select(pair => pair.Item2)
			.ToList();

		var typedArgs = new List<ITypedAuraExpression>();
		var paramTypes = declaration.GetParamTypes();
		var i = 0;
		foreach (var arg in orderedArgs)
		{
			var typedArg = i >= paramTypes.Count
				? ExpressionAndConfirm(arg, paramTypes[^1].Typ)
				: ExpressionAndConfirm(arg, paramTypes[i].Typ);
			typedArgs.Add(typedArg);
			i++;
		}

		return new TypedCall(typedCallee!, typedArgs, declaration.GetReturnType(), call.Line);
	}

	private TypedCall TypeCheckNamedParameters(UntypedCall call, ICallable declaration)
	{
		var typedCallee = Expression((IUntypedAuraExpression)call.Callee) as ITypedAuraCallable;
		// Insert each named argument into its correct position
		var orderedArgs = new List<IUntypedAuraExpression>();
		foreach (var arg in call.Arguments)
		{
			var index = declaration.GetParamIndex(arg.Item1!.Value.Value);
			if (index >= orderedArgs.Count) orderedArgs.Add(arg.Item2);
			else orderedArgs.Insert(index, arg.Item2);
		}

		// Filter out the parameters that aren't included in the argument list. We will ensure that the omitted parameters have
		// a default value later. However, if they have a default value, they were already type checked when the function was
		// first declared, so they don't need to be type checked again here.
		var includedParams = declaration.GetParams()
			.Where(p => call.Arguments.Any(arg => arg.Item1!.Value.Value == p.Name.Value));
		// Type check the included named arguments
		var typedArgs = orderedArgs
			.Zip(includedParams)
			.Select(pair => ExpressionAndConfirm(pair.First, pair.Second.ParamType.Typ))
			.ToList();
		// With named arguments, you may omit arguments if they have been declared with a default value.
		// Check for any missing parameters and fill in their default value, if they have one
		var missingParams = declaration.GetParams()
			.Where(p => call.Arguments.All(arg => arg.Item1!.Value.Value != p.Name.Value));
		foreach (var missingParam in missingParams)
		{
			var index = declaration.GetParamIndex(missingParam.Name.Value);
			var defaultValue = missingParam.ParamType.DefaultValue ??
							   throw new MustSpecifyValueForArgumentWithoutDefaultValueException(call.GetName(), missingParam.Name.Value, call.Line);
			if (index >= orderedArgs.Count) typedArgs.Add((ITypedAuraExpression)defaultValue);
			else typedArgs.Insert(index, (ITypedAuraExpression)defaultValue);
		}

		return new TypedCall(typedCallee!, typedArgs, declaration.GetReturnType(), call.Line);
	}

	private AuraType TypeCheckReturnTypeTok(Tok? returnTok) =>
		returnTok is not null ? TypeTokenToType(returnTok.Value) : new Nil();
}
