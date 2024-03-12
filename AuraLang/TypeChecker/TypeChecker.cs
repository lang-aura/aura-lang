using AuraLang.AST;
using AuraLang.Exceptions.TypeChecker;
using AuraLang.ImportedModuleProvider;
using AuraLang.LocalFileSystemModuleProvider;
using AuraLang.ModuleCompiler;
using AuraLang.Prelude;
using AuraLang.Shared;
using AuraLang.Stdlib;
using AuraLang.Stores;
using AuraLang.Symbol;
using AuraLang.Token;
using AuraLang.Types;
using AuraLang.Visitor;
using Range = AuraLang.Location.Range;

namespace AuraLang.TypeChecker;

public class AuraTypeChecker : IUntypedAuraStmtVisitor<ITypedAuraStatement>,
	IUntypedAuraExprVisitor<ITypedAuraExpression>
{
	private readonly IGlobalSymbolsTable _symbolsTable;
	private readonly IEnclosingClassStore _enclosingClassStore;
	private readonly IEnclosingFunctionDeclarationStore _enclosingFunctionDeclarationStore;
	private readonly TypeCheckerExceptionContainer _exContainer;
	private readonly EnclosingNodeStore<IUntypedAuraExpression> _enclosingExpressionStore;
	private readonly EnclosingNodeStore<IUntypedAuraStatement> _enclosingStatementStore;
	private string ProjectName { get; }
	private string? ModuleName { get; set; }
	private readonly AuraPrelude _prelude = new();
	public IImportedModuleProvider ImportedModuleProvider { get; }
	private string _filePath;

	public AuraTypeChecker(
		IGlobalSymbolsTable symbolsTable,
		IEnclosingClassStore enclosingClassStore,
		IEnclosingFunctionDeclarationStore enclosingFunctionDeclarationStore,
		EnclosingNodeStore<IUntypedAuraExpression> enclosingExpressionStore,
		EnclosingNodeStore<IUntypedAuraStatement> enclosingStatementStore,
		IImportedModuleProvider importedFileProvider,
		string filePath,
		string projectName
	)
	{
		_symbolsTable = symbolsTable;
		_enclosingClassStore = enclosingClassStore;
		_enclosingFunctionDeclarationStore = enclosingFunctionDeclarationStore;
		_enclosingExpressionStore = enclosingExpressionStore;
		_enclosingStatementStore = enclosingStatementStore;
		_exContainer = new TypeCheckerExceptionContainer(filePath);
		ImportedModuleProvider = importedFileProvider;
		ProjectName = projectName;
		_filePath = filePath;
	}

	public AuraTypeChecker(
		IGlobalSymbolsTable symbolsTable,
		IImportedModuleProvider importedModuleProvider,
		string filePath,
		string projectName
	)
	{
		_symbolsTable = symbolsTable;
		_enclosingClassStore = new EnclosingClassStore();
		_enclosingFunctionDeclarationStore = new EnclosingFunctionDeclarationStore();
		_enclosingExpressionStore = new EnclosingNodeStore<IUntypedAuraExpression>();
		_enclosingStatementStore = new EnclosingNodeStore<IUntypedAuraStatement>();
		_exContainer = new TypeCheckerExceptionContainer(filePath);
		ImportedModuleProvider = importedModuleProvider;
		ProjectName = projectName;
		_filePath = filePath;
	}

	public AuraTypeChecker(
		IImportedModuleProvider importedModuleProvider,
		string filePath,
		string projectName
	)
	{
		_symbolsTable = new GlobalSymbolsTable();
		_enclosingClassStore = new EnclosingClassStore();
		_enclosingFunctionDeclarationStore = new EnclosingFunctionDeclarationStore();
		_enclosingExpressionStore = new EnclosingNodeStore<IUntypedAuraExpression>();
		_enclosingStatementStore = new EnclosingNodeStore<IUntypedAuraStatement>();
		_exContainer = new TypeCheckerExceptionContainer(filePath);
		ImportedModuleProvider = importedModuleProvider;
		ProjectName = projectName;
		_filePath = filePath;
	}

	public AuraTypeChecker(string filePath, string projectName)
	{
		_symbolsTable = new GlobalSymbolsTable();
		_enclosingClassStore = new EnclosingClassStore();
		_enclosingFunctionDeclarationStore = new EnclosingFunctionDeclarationStore();
		_enclosingExpressionStore = new EnclosingNodeStore<IUntypedAuraExpression>();
		_enclosingStatementStore = new EnclosingNodeStore<IUntypedAuraStatement>();
		_exContainer = new TypeCheckerExceptionContainer(filePath);
		ImportedModuleProvider = new AuraLocalFileSystemImportedModuleProvider();
		ProjectName = projectName;
		_filePath = filePath;
	}

	public void BuildSymbolsTable(List<IUntypedAuraStatement> stmts)
	{
		ModuleName = ((UntypedMod)stmts.First(stmt => stmt is UntypedMod)).Value.Value;

		foreach (var stmt in stmts)
			try
			{
				switch (stmt)
				{
					case UntypedImport i:
						Visit(i);
						break;
					case UntypedLet l:
						AddLetStmtToSymbolsTable(l);
						break;
					case UntypedNamedFunction nf:
						var f = ParseFunctionSignature(nf);
						_symbolsTable.TryAddSymbol(new AuraSymbol(nf.Name.Value, f), ModuleName);
						break;
					case UntypedClass c:
						var cl = ParseClassSignature(c);
						_symbolsTable.TryAddSymbol(new AuraSymbol(c.Name.Value, cl), ModuleName);
						break;
					case UntypedStruct @struct:
						var s = (TypedStruct)Visit(@struct);
						_symbolsTable.TryAddSymbol(
							new AuraSymbol(
								s.Name.Value,
								new AuraStruct(
									s.Name.Value,
									s.Params,
									Visibility.Private
								)
							),
							ModuleName
						);
						break;
					case UntypedInterface @interface:
						_symbolsTable.TryAddSymbol(
							new AuraSymbol(
								@interface.Name.Value,
								new AuraInterface(
									@interface.Name.Value,
									@interface
										.Methods.Select(
											m => (AuraNamedFunction)((TypedFunctionSignature)Visit(m)).Typ
										)
										.ToList(),
									@interface.Public
								)
							),
							ModuleName
						);

						foreach (var m in @interface.Methods)
							_symbolsTable.TryAddSymbol(
								new AuraSymbol(m.Name.Value, ((TypedFunctionSignature)Visit(m)).Typ),
								ModuleName
							);

						break;
				}
			}
			catch (TypeCheckerException ex)
			{
				_exContainer.Add(ex);
			}

		if (!_exContainer.IsEmpty()) throw _exContainer;
	}

	public List<ITypedAuraStatement> CheckTypes(List<IUntypedAuraStatement> untypedAst)
	{
		var typedAst = new List<ITypedAuraStatement>();

		foreach (var stmt in untypedAst)
			try
			{
				var typedStmt = Statement(stmt);
				typedAst.Add(typedStmt);
			}
			catch (TypeCheckerException ex)
			{
				_exContainer.Add(ex);
			}
			catch (TypeCheckerExceptionContainer exC)
			{
				typedAst = exC.Valid;
				_exContainer.Add(exC);
			}

		if (!_exContainer.IsEmpty())
		{
			_exContainer.Valid = typedAst;
			throw _exContainer;
		}

		return typedAst;
	}

	private ITypedAuraStatement Statement(IUntypedAuraStatement stmt)
	{
		return stmt.Accept(this);
	}

	private ITypedAuraExpression Expression(IUntypedAuraExpression expr)
	{
		return expr.Accept(this);
	}

	/// <summary>
	///     Type checks an expression and ensures that it matches an expected type
	/// </summary>
	/// <param name="expr">The expression to type check</param>
	/// <param name="expected">The expected type</param>
	/// <returns>The typed expression, as long as it matches the expected type</returns>
	/// <exception cref="UnexpectedTypeException">
	///     Thrown if the typed expression doesn't match
	///     the expected type
	/// </exception>
	private ITypedAuraExpression ExpressionAndConfirm(IUntypedAuraExpression expr, AuraType expected)
	{
		var typedExpr = Expression(expr);
		if (!expected.IsSameOrInheritingType(typedExpr.Typ))
			throw new UnexpectedTypeException(
				expected,
				typedExpr.Typ,
				expr.Range
			);

		return typedExpr;
	}

	/// <summary>
	///     Finds a variable in the symbols table, and confirms that it matches an expected type.
	/// </summary>
	/// <param name="symbolName">The variable's name</param>
	/// <param name="symbolNamespace">The namespace where the variable was defined</param>
	/// <param name="expected">The variable's expected type</param>
	/// <param name="range">The range in the Aura source file where the variable usage appears</param>
	private T FindAndConfirm<T>(
		string symbolName,
		string symbolNamespace,
		T expected,
		Range range
	) where T : AuraType
	{
		var local = _symbolsTable.GetSymbol(symbolName, symbolNamespace) ??
					throw new UnknownVariableException(symbolName, range);
		if (!expected.IsSameOrInheritingType(local.Kind))
			throw new UnexpectedTypeException(
				expected,
				local.Kind,
				range
			);

		return (T)local.Kind;
	}

	private AuraSymbol FindOrThrow(
		string varName,
		string symbolNamespace,
		Range range
	)
	{
		var local = _symbolsTable.GetSymbol(varName, symbolNamespace) ??
					throw new UnknownVariableException(varName, range);
		return local;
	}

	/// <summary>
	///     Type checks a defer statement
	/// </summary>
	/// <param name="defer">The defer statement to type check</param>
	/// <returns>A valid, type checked defer statement</returns>
	public ITypedAuraStatement Visit(UntypedDefer defer)
	{
		return WithEnclosingStmt(
			() =>
			{
				var typedCall = Visit((UntypedCall)defer.Call);
				return new TypedDefer(defer.Defer, (TypedCall)typedCall);
			},
			defer,
			ModuleName!
		);
	}

	/// <summary>
	///     Type checks an expression statement
	/// </summary>
	/// <param name="exprStmt">The expression statement to type check</param>
	/// <returns>A valid, type checked expression statement</returns>
	public ITypedAuraStatement Visit(UntypedExpressionStmt exprStmt)
	{
		return new TypedExpressionStmt(Expression(exprStmt.Expression));
	}

	/// <summary>
	///     Type checks a for loop
	/// </summary>
	/// <param name="forStmt">The for loop to be type checked</param>
	/// <returns>A valid, type checked for loop</returns>
	public ITypedAuraStatement Visit(UntypedFor forStmt)
	{
		return WithEnclosingStmt(
			() =>
			{
				return InNewScope(
					() =>
					{
						var typedInit = forStmt.Initializer is not null ? Statement(forStmt.Initializer) : null;
						var typedCond = forStmt.Condition is not null
							? ExpressionAndConfirm(forStmt.Condition, new AuraBool())
							: null;
						var typedInc = forStmt.Increment is not null ? Expression(forStmt.Increment) : null;
						var typedBody = NonReturnableBody(forStmt.Body);
						return new TypedFor(
							forStmt.For,
							typedInit,
							typedCond,
							typedInc,
							typedBody,
							forStmt.ClosingBrace
						);
					}
				);
			},
			forStmt,
			ModuleName!
		);
	}

	/// <summary>
	///     Type checks a for each loop
	/// </summary>
	/// <param name="forEachStmt">The for each loop to be type checked</param>
	/// <returns>A valid, type checked for each loop</returns>
	/// <exception cref="ExpectIterableException">
	///     Thrown if the value being iterated over does not implement
	///     the IIterable interface
	/// </exception>
	public ITypedAuraStatement Visit(UntypedForEach forEachStmt)
	{
		return WithEnclosingStmt(
			() =>
			{
				return InNewScope(
					() =>
					{
						// Type check iterable
						var iter = Expression(forEachStmt.Iterable);
						if (iter.Typ is not IIterable typedIter)
							throw new ExpectIterableException(iter.Typ, forEachStmt.Range);

						// Add current element variable to list of local variables
						_symbolsTable.TryAddSymbol(
							new AuraSymbol(forEachStmt.EachName.Value, typedIter.GetIterType()),
							ModuleName!
						);
						// Type check body
						var typedBody = NonReturnableBody(forEachStmt.Body);
						return new TypedForEach(
							forEachStmt.ForEach,
							forEachStmt.EachName,
							iter,
							typedBody,
							forEachStmt.ClosingBrace
						);
					}
				);
			},
			forEachStmt,
			ModuleName!
		);
	}

	public ITypedAuraStatement Visit(UntypedNewLine nl)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	///     Type checks an anonymous function declaration
	/// </summary>
	/// <param name="f">The anonymous function to type check</param>
	/// <returns>A valid, type checked anonymous function declaration</returns>
	/// <exception cref="TypeMismatchException">
	///     Thrown if the anonymous function's body returns a type different
	///     than the one specified in the function's signature
	/// </exception>
	public ITypedAuraExpression Visit(UntypedAnonymousFunction f)
	{
		return _enclosingExpressionStore.WithEnclosing(
			() =>
			{
				return InNewScope(
					() =>
					{
						var typedParams = TypeCheckParams(f.Params);
						// Add the function's parameters as local variables
						foreach (var param in typedParams)
							_symbolsTable.TryAddSymbol(
								new AuraSymbol(param.Name.Value, param.ParamType.Typ),
								ModuleName!
							);

						var typedBody = (TypedBlock)Visit(f.Body);
						// Parse the function's return type
						AuraType returnType = new AuraNil();
						if (f.ReturnType?.Count == 1)
							returnType = f.ReturnType![0];
						else if (f.ReturnType?.Count > 1)
							returnType = new AuraAnonymousStruct(
								f.ReturnType!
									.Select(
										(typ, i) => new Param(
											new Tok(TokType.Identifier, i.ToString()),
											new ParamType(
												typ,
												false,
												null
											)
										)
									)
									.ToList(),
								Visibility.Private
							);

						// Ensure the function's body returns the type specified in its signature
						if (!returnType.IsSameOrInheritingType(typedBody.Typ))
							throw new TypeMismatchException(
								returnType,
								typedBody.Typ,
								f.Range
							);

						return new TypedAnonymousFunction(
							f.Fn,
							typedParams,
							typedBody,
							returnType
						);
					}
				);
			},
			f
		);
	}

	private AuraNamedFunction ParseFunctionSignature(UntypedNamedFunction f)
	{
		var typedParams = TypeCheckParams(f.Params);
		AuraType returnType = new AuraNil();
		if (f.ReturnType?.Count == 1) returnType = f.ReturnType![0];

		if (f.ReturnType?.Count > 1)
			returnType = new AuraAnonymousStruct(
				f.ReturnType!
					.Select(
						(typ, i) => new Param(
							new Tok(TokType.Identifier, i.ToString()),
							new ParamType(
								typ,
								false,
								null
							)
						)
					)
					.ToList(),
				Visibility.Private
			);

		if (returnType is AuraUnknown u)
		{
			var symbol = _symbolsTable.GetSymbol(u.Name, ModuleName!);
			returnType = symbol?.Kind ?? u;
		}

		return new AuraNamedFunction(
			f.Name.Value,
			f.Public,
			new AuraFunction(typedParams, returnType),
			f.Documentation
		);
	}

	public ITypedAuraStatement Visit(UntypedNamedFunction f)
	{
		_enclosingFunctionDeclarationStore.Push(f);
		var typedParams = TypeCheckParams(f.Params);
		AuraType returnType = new AuraNil();
		if (f.ReturnType?.Count == 1) returnType = f.ReturnType![0];

		if (f.ReturnType?.Count > 1)
			returnType = new AuraAnonymousStruct(
				f.ReturnType!
					.Select(
						(typ, i) => new Param(
							new Tok(TokType.Identifier, i.ToString()),
							new ParamType(
								typ,
								false,
								null
							)
						)
					)
					.ToList(),
				Visibility.Private
			);

		if (returnType is AuraUnknown u)
		{
			var symbol = _symbolsTable.GetSymbol(u.Name, ModuleName!);
			returnType = symbol?.Kind ?? u;
		}

		// Add parameters as local variables
		foreach (var param in f.Params)
		{
			var paramTyp = param.ParamType.Typ;
			if (param.ParamType.Variadic) paramTyp = new AuraList(paramTyp);

			_symbolsTable.TryAddSymbol(new AuraSymbol(param.Name.Value, paramTyp), ModuleName!);
		}

		var typedBody = (TypedBlock)Visit(f.Body);
		// Ensure the function's body returns the same type specified in its signature
		if (returnType is AuraResult r)
		{
			if (!r.Success.IsSameOrInheritingType(typedBody.Typ) &&
				!r.Failure.IsSameOrInheritingType(typedBody.Typ))
				throw new TypeMismatchException(
					r.Failure,
					typedBody.Typ,
					f.Body.ClosingBrace.Range
				);
		}
		else
		{
			if (!returnType.IsSameOrInheritingType(typedBody.Typ))
				throw new TypeMismatchException(
					returnType,
					typedBody.Typ,
					f.Body.ClosingBrace.Range
				);
		}

		return new TypedNamedFunction(
			f.Fn,
			f.Name,
			typedParams,
			typedBody,
			returnType,
			f.Public,
			f.Documentation
		);
	}

	private void AddLetStmtToSymbolsTable(UntypedLet let)
	{
		// Ensure that all variables being created either have a type annotation or a missing a type annotation (they cannot mix)
		var annotations = let.NameTyps.All(nt => nt is not AuraUnknown);
		var missingAnnotations = let.NameTyps.All(nt => nt is AuraUnknown);
		if (!annotations &&
			!missingAnnotations)
			throw new CannotMixTypeAnnotationsException(let.Range);

		if (missingAnnotations)
		{
			AddShortLetStmtToSymbolsTable(let);
			return;
		}

		if (let.Names.Count > 1)
		{
			TypeCheckMultipleVariablesInLetStmt(let);
			return;
		}

		var nameTyp = let.NameTyps[0];
		// Type check initializer
		var defaultable = nameTyp as IDefaultable;
		if (let.Initializer is null &&
			defaultable is null)
			throw new MustSpecifyInitialValueForNonDefaultableTypeException(nameTyp, let.Range);

		var typedInit = let.Initializer is not null
			? ExpressionAndConfirm(let.Initializer, nameTyp)
			: defaultable!.Default(let.Range);
		// Add new variable to list of locals
		_symbolsTable.TryAddSymbol(new AuraSymbol(let.Names[0].Value, typedInit.Typ), ModuleName!);
	}

	private void TypeCheckMultipleVariablesInLetStmt(UntypedLet let)
	{
		// Package the let statement's variable names into an anonymous struct
		var names = new AuraAnonymousStruct(
			let
				.Names.Select(
					(_, i) => new Param(
						new Tok(
							TokType.Identifier,
							i.ToString(),
							let.Range
						),
						new ParamType(
							let.NameTyps[i],
							false,
							null
						)
					)
				)
				.ToList(),
			Visibility.Private
		);
		// Type check initializer
		var typedInit = ExpressionAndConfirm(let.Initializer!, names);
		// Add new variables to list of locals
		foreach (var (name, typ) in let.Names.Zip(let.NameTyps))
			_symbolsTable.TryAddSymbol(new AuraSymbol(name.Value, typ), ModuleName!);
	}

	private void AddShortLetStmtToSymbolsTable(UntypedLet let)
	{
		if (let.Names.Count > 1)
		{
			TypeCheckMultipleVariablesInShortLetStmt(let);
			return;
		}

		// Type check initializer
		var typedInit = let.Initializer is not null ? Expression(let.Initializer) : null;
		// Add new variable to list of locals
		_symbolsTable.TryAddSymbol(new AuraSymbol(let.Names[0].Value, typedInit?.Typ ?? new AuraNil()), ModuleName!);
	}

	private void TypeCheckMultipleVariablesInShortLetStmt(UntypedLet let)
	{
		// Type check initializer
		var typedInit = Expression(let.Initializer!);
		// Add new variables to list of locals
		foreach (var (name, typ) in let.Names.Zip(
					 ((AuraAnonymousStruct)typedInit.Typ).Parameters.Select(p => p.ParamType.Typ)
				 ))
			_symbolsTable.TryAddSymbol(new AuraSymbol(name.Value, typ), ModuleName!);
	}

	/// <summary>
	///     Type checks a let statement
	/// </summary>
	/// <param name="let">The let statement to type check</param>
	/// <returns>A valid, type checked let statement</returns>
	public ITypedAuraStatement Visit(UntypedLet let)
	{
		// Ensure that all variables being created either have a type annotation or a missing a type annotation (they cannot mix)
		var annotations = let.NameTyps.All(nt => nt is not AuraUnknown);
		var missingAnnotations = let.NameTyps.All(nt => nt is AuraUnknown);
		if (!annotations &&
			!missingAnnotations)
			throw new CannotMixTypeAnnotationsException(let.Range);

		if (missingAnnotations) return ShortLetStmt(let);

		if (let.Names.Count > 1) return LetStmtMultipleNames(let);

		var typedLet = WithEnclosingStmt(
			() =>
			{
				var nameTyp = let.NameTyps[0];
				// Type check initializer
				var defaultable = nameTyp as IDefaultable;
				if (let.Initializer is null &&
					defaultable is null)
					throw new MustSpecifyInitialValueForNonDefaultableTypeException(nameTyp, let.Range);

				var typedInit = let.Initializer is not null
					? ExpressionAndConfirm(let.Initializer, nameTyp)
					: defaultable!.Default(let.Range);

				return new TypedLet(
					let.Let,
					let.Names,
					true,
					let.Mutable,
					typedInit
				);
			},
			let,
			ModuleName!
		);

		// Add new variable to list of locals
		_symbolsTable.TryAddSymbol(
			new AuraSymbol(typedLet.Names[0].Value, typedLet.Initializer?.Typ ?? new AuraNone()),
			ModuleName!
		);

		return typedLet;
	}

	private TypedLet LetStmtMultipleNames(UntypedLet let)
	{
		var typedLet = WithEnclosingStmt(
			() =>
			{
				// Package the let statement's variable names into an anonymous struct
				var names = new AuraAnonymousStruct(
					let
						.Names.Select(
							(_, i) => new Param(
								new Tok(
									TokType.Identifier,
									i.ToString(),
									let.Range
								),
								new ParamType(
									let.NameTyps[i],
									false,
									null
								)
							)
						)
						.ToList(),
					Visibility.Private
				);
				// Type check initializer
				var typedInit = ExpressionAndConfirm(let.Initializer!, names);

				return new TypedLet(
					let.Let,
					let.Names,
					true,
					false,
					typedInit
				);
			},
			let,
			ModuleName!
		);

		// Add new variables to list of locals
		foreach (var (name, typ) in let.Names.Zip(let.NameTyps))
			_symbolsTable.TryAddSymbol(new AuraSymbol(name.Value, typ), ModuleName!);

		return typedLet;
	}

	/// <summary>
	///     Type checks a short let statement
	/// </summary>
	/// <param name="let">The short let statement to type check</param>
	/// <returns>A valid, type checked short let statement</returns>
	private TypedLet ShortLetStmt(UntypedLet let)
	{
		if (let.Names.Count > 1) return ShortLetStmtMultipleNames(let);

		var typedShortLet = WithEnclosingStmt(
			() =>
			{
				// Type check initializer
				var typedInit = let.Initializer is not null ? Expression(let.Initializer) : null;
				return new TypedLet(
					null,
					let.Names,
					false,
					let.Mutable,
					typedInit
				);
			},
			let,
			ModuleName!
		);

		// Add new variable to list of locals
		_symbolsTable.TryAddSymbol(
			new AuraSymbol(typedShortLet.Names[0].Value, typedShortLet.Initializer!.Typ),
			ModuleName!
		);

		return typedShortLet;
	}

	private TypedLet ShortLetStmtMultipleNames(UntypedLet let)
	{
		var typedLet = WithEnclosingStmt(
			() =>
			{
				// Type check initializer
				var typedInit = Expression(let.Initializer!);

				return new TypedLet(
					null,
					let.Names,
					false,
					false,
					typedInit
				);
			},
			let,
			ModuleName!
		);

		// Add new variables to list of locals
		foreach (var (name, typ) in let.Names.Zip(
					 ((AuraAnonymousStruct)typedLet.Initializer!.Typ).Parameters.Select(p => p.ParamType.Typ)
				 ))
			_symbolsTable.TryAddSymbol(new AuraSymbol(name.Value, typ), ModuleName!);

		return typedLet;
	}

	/// <summary>
	///     Type checks a mod statement, and saves the typed mod as the current mod
	/// </summary>
	/// <param name="mod">The mod statement to be type checked</param>
	/// <returns>A valid, type checked mod statement</returns>
	public ITypedAuraStatement Visit(UntypedMod mod)
	{
		var m = new TypedMod(mod.Mod, mod.Value);
		return m;
	}

	/// <summary>
	///     Type checks a return statement
	/// </summary>
	/// <param name="r">The return statement to type check</param>
	/// <returns>A valid, type checked return statement</returns>
	public ITypedAuraStatement Visit(UntypedReturn r)
	{
		return WithEnclosingStmt(
			() =>
			{
				if (r.Value is null) return new TypedReturn(r.Return, null);

				if (r.Value!.Count == 1) return new TypedReturn(r.Return, Expression(r.Value[0]));

				// If the return statement contains more than one expression, we package the expressions up as a struct
				var typedReturnValues = r.Value.Select(Expression).ToList();
				return new TypedReturn(
					r.Return,
					new TypedAnonymousStruct(
						new Tok(
							TokType.Struct,
							"struct",
							r.Range
						),
						typedReturnValues
							.Select(
								(v, i) => new Param(
									new Tok(
										TokType.Identifier,
										i.ToString(),
										r.Range
									),
									new ParamType(
										v.Typ,
										false,
										null
									)
								)
							)
							.ToList(),
						typedReturnValues.ToList(),
						new Tok(
							TokType.RightParen,
							")",
							r.Range
						)
					)
				);
			},
			r,
			ModuleName!
		);
	}

	private AuraClass ParseClassSignature(UntypedClass @class)
	{
		var typedParams = @class.Params.Select(
			p =>
			{
				var typedDefaultValue = p.ParamType.DefaultValue is not null
					? (ILiteral)Expression(p.ParamType.DefaultValue)
					: null;
				var paramTyp = p.ParamType.Typ;
				return new Param(
					p.Name,
					new ParamType(
						paramTyp,
						p.ParamType.Variadic,
						typedDefaultValue
					)
				);
			}
		);
		var methodSignatures = @class.Methods.Select(ParseFunctionSignature);
		var implements = @class.Implementing.Any()
			? @class.Implementing.Select(
				impl =>
				{
					var local = FindOrThrow(
						impl.Value,
						ModuleName!,
						impl.Range
					);
					var i = local.Kind as AuraInterface ??
							throw new CannotImplementNonInterfaceException(impl.Value, @class.Range);
					return i;
				}
			)
			: new List<AuraInterface>();

		return new AuraClass(
			@class.Name.Value,
			typedParams.ToList(),
			methodSignatures.ToList(),
			implements.ToList(),
			@class.Public
		);
	}

	/// <summary>
	///     Type checks a class declaration
	/// </summary>
	/// <param name="class">The class declaration to type check</param>
	/// <returns>A valid, type checked class declaration</returns>
	public ITypedAuraStatement Visit(UntypedClass @class)
	{
		return WithEnclosingStmt(
			() =>
			{
				var typedParams = @class.Params.Select(
						p =>
						{
							var typedDefaultValue = p.ParamType.DefaultValue is not null
								? (ILiteral)Expression(p.ParamType.DefaultValue)
								: null;
							var paramTyp = p.ParamType.Typ;
							return new Param(
								p.Name,
								new ParamType(
									paramTyp,
									p.ParamType.Variadic,
									typedDefaultValue
								)
							);
						}
					)
					.ToList();

				var methodSignatures = @class.Methods.Select(ParseFunctionSignature).ToList();
				var methodTypes = methodSignatures
					.Select(
						method =>
						{
							var typedMethodParams = method
								.GetParams()
								.Select(
									p =>
									{
										var typedMethodDefaultValue = p.ParamType.DefaultValue is not null
											? (ILiteral)Expression(p.ParamType.DefaultValue)
											: null;
										var methodParamType = p.ParamType.Typ;
										return new Param(
											p.Name,
											new ParamType(
												methodParamType,
												p.ParamType.Variadic,
												typedMethodDefaultValue
											)
										);
									}
								);
							return new AuraNamedFunction(
								method.Name,
								method.Public,
								new AuraFunction(typedMethodParams.ToList(), method.GetReturnType())
							);
						}
					)
					.ToList();

				// Get type of implementing interface
				var implements = @class.Implementing.Any()
					? @class.Implementing.Select(
							impl =>
							{
								var local = FindOrThrow(
									impl.Value,
									ModuleName!,
									impl.Range
								);
								var i = local.Kind as AuraInterface ??
										throw new CannotImplementNonInterfaceException(impl.Value, @class.Range);
								return i;
							}
						)
						.ToList()
					: new List<AuraInterface>();

				// Store the partially typed class as the current enclosing class
				var partiallyTypedClass = new PartiallyTypedClass(
					@class.Class,
					@class.Name,
					@class.Params,
					methodSignatures,
					@class.Public,
					@class.ClosingBrace,
					new AuraClass(
						@class.Name.Value,
						typedParams,
						methodTypes,
						implements,
						@class.Public
					)
				);
				_enclosingClassStore.Push(partiallyTypedClass);
				// Finish type checking the class's methods
				var typedMethods = @class.Methods.Select(m => (TypedNamedFunction)Visit(m)).ToList();

				// If the class implements any interfaces, ensure that it contains all required methods
				if (implements.Any())
				{
					var valid = implements
						.Select(
							impl =>
							{
								return impl
									.Functions.Select(
										f =>
										{
											return typedMethods
												.Where(m => m.Public == Visibility.Public)
												.Select(tm => tm.GetFunctionType())
												.Contains(f);
										}
									)
									.All(b => b);
							}
						)
						.All(b => b);
					if (!valid)
						throw new MissingInterfaceMethodException(
							string.Empty,
							string.Empty,
							@class.Range
						); // TODO
				}

				_enclosingClassStore.Pop();
				return new FullyTypedClass(
					@class.Class,
					@class.Name,
					typedParams.ToList(),
					typedMethods,
					@class.Public,
					implements.ToList(),
					@class.ClosingBrace,
					@class.Documentation
				);
			},
			@class,
			ModuleName!
		);
	}

	/// <summary>
	///     Type checks a while loop
	/// </summary>
	/// <param name="while">The while loop to be type checked</param>
	/// <returns>A valid, type checked while loop</returns>
	public ITypedAuraStatement Visit(UntypedWhile @while)
	{
		return WithEnclosingStmt(
			() =>
			{
				return InNewScope(
					() =>
					{
						var typedCond = ExpressionAndConfirm(@while.Condition, new AuraBool());
						var typedBody = NonReturnableBody(@while.Body);
						return new TypedWhile(
							@while.While,
							typedCond,
							typedBody,
							@while.ClosingBrace
						);
					}
				);
			},
			@while,
			ModuleName!
		);
	}

	public ITypedAuraStatement Visit(UntypedMultipleImport import)
	{
		var typedImports = import.Packages.Select(pkg => (TypedImport)Visit(pkg)).ToList();
		return new TypedMultipleImport(
			import.Import,
			typedImports,
			import.ClosingBrace
		);
	}

	public ITypedAuraStatement Visit(UntypedImport import)
	{
		// First, check if the module being imported is built-in
		if (!AuraStdlib.TryGetModule(import.Package.Value, out var module))
		{
			var typedAsts = new AuraModuleCompiler(
				$"src/{import.Package.Value}",
				ProjectName,
				this
			).TypeCheckModule();

			var exportedTypes = typedAsts
				.Select(
					typedAst =>
					{
						// Extract public methods and classes
						var methods = typedAst
							.Item2.Where(node => node.Typ is AuraNamedFunction)
							.Select(node => (node.Typ as AuraNamedFunction)!);
						var interfaces = typedAst
							.Item2.Where(node => node.Typ is AuraInterface)
							.Select(node => (node.Typ as AuraInterface)!);
						var classes = typedAst
							.Item2.Where(node => node.Typ is AuraClass)
							.Select(node => (node.Typ as AuraClass)!);
						var variables =
							typedAst.Item2.Where(node => node is TypedLet).Select(node => (node as TypedLet)!);
						return (methods, interfaces, classes, variables);
					}
				)
				.Aggregate(
					(a, b) => (a.methods.Concat(b.methods), a.interfaces.Concat(b.interfaces),
						a.classes.Concat(b.classes),
						a.variables.Concat(b.variables))
				);
			var importedModule = new AuraModule(
				import.Alias?.Value ?? import.Package.Value,
				exportedTypes.methods.ToList(),
				exportedTypes.interfaces.ToList(),
				exportedTypes.classes.ToList(),
				exportedTypes.variables.ToDictionary(v => v.Names[0].Value, v => v.Initializer!)
			);
			// Add module to list of local variables
			_symbolsTable.AddModule(importedModule);
		}
		else
		{
			_symbolsTable.AddModule(module!);
		}

		return new TypedImport(
			import.Import,
			import.Package,
			import.Alias
		);
	}

	/// <summary>
	///     Type checks a comment. This method is basically a no-op, since comments don't have a type, nor do they
	///     contain any typed information.
	/// </summary>
	/// <param name="comment">The comment to type check</param>
	/// <returns>A valid, type checked comment</returns>
	public ITypedAuraStatement Visit(UntypedComment comment)
	{
		return new TypedComment(comment.Text);
	}

	/// <summary>
	///     Type checks a continue statement. This method is basically a no-op, since continue statements don't
	///     have a type.
	/// </summary>
	/// <param name="continue">The continue statement to type check</param>
	/// <returns>A valid, type checked continue statement</returns>
	public ITypedAuraStatement Visit(UntypedContinue @continue)
	{
		var enclosingStmt = _enclosingStatementStore.Peek();
		if (enclosingStmt is not UntypedWhile &&
			enclosingStmt is not UntypedFor &&
			enclosingStmt is not UntypedForEach)
			throw new InvalidUseOfContinueKeywordException(@continue.Range);

		return new TypedContinue(@continue.Continue);
	}

	/// <summary>
	///     Type checks a break statement. This method is basically a no-op, since break statements don't
	///     have a type.
	/// </summary>
	/// <param name="b">The break statement to type check</param>
	/// <returns>A valid, type checked break statement</returns>
	public ITypedAuraStatement Visit(UntypedBreak b)
	{
		var enclosingStmt = _enclosingStatementStore.Peek();
		if (enclosingStmt is not UntypedWhile &&
			enclosingStmt is not UntypedFor &&
			enclosingStmt is not UntypedForEach)
			throw new InvalidUseOfBreakKeywordException(b.Range);

		return new TypedBreak(b.Break);
	}

	/// <summary>
	///     Type checks a yield statement. The <c>yield</c> keyword can only be used inside of an <c>if</c> expression or
	///     a block. All other uses of the <c>yield</c> keyword will result in an exception.
	/// </summary>
	/// <param name="y">The yield statement to type check</param>
	/// <returns>A valid, type checked yield statement</returns>
	/// <exception cref="InvalidUseOfYieldKeywordException">
	///     Thrown if the yield statement is not used inside of an <c>if</c>
	///     expression or a block
	/// </exception>
	public ITypedAuraStatement Visit(UntypedYield y)
	{
		var enclosingExpr = _enclosingExpressionStore.Peek();
		if (enclosingExpr is not UntypedIf &&
			enclosingExpr is not UntypedBlock)
			throw new InvalidUseOfYieldKeywordException(y.Range);

		var value = Expression(y.Value);
		return new TypedYield(y.Yield, value);
	}

	/// <summary>
	///     Type checks an interface declaration
	/// </summary>
	/// <param name="i">The interface declaration to type check</param>
	/// <returns>A valid, type checked interface</returns>
	public ITypedAuraStatement Visit(UntypedInterface i)
	{
		return new TypedInterface(
			i.Interface,
			i.Name,
			i.Methods.Select(m => (TypedFunctionSignature)Visit(m)).ToList(),
			i.Public,
			i.ClosingBrace,
			i.Documentation
		);
	}

	public ITypedAuraExpression Visit(UntypedInterfacePlaceholder ip)
	{
		// Ensure the expected type is an interface
		var i = FindAndConfirm(
			ip.InterfaceValue.Value,
			ModuleName!,
			new AuraInterface(
				ip.InterfaceValue.Value,
				new List<AuraNamedFunction>(),
				Visibility.Private
			),
			ip.Range
		);
		return new TypedInterfacePlaceholder(ip.InterfaceValue, i);
	}

	public ITypedAuraStatement Visit(UntypedFunctionSignature fnSignature)
	{
		return new TypedFunctionSignature(
			fnSignature.Visibility,
			fnSignature.Fn,
			fnSignature.Name,
			fnSignature.Params,
			fnSignature.ClosingParen,
			fnSignature.ReturnType,
			fnSignature.Documentation
		);
	}

	/// <summary>
	///     Type checks an assignment expression
	/// </summary>
	/// <param name="assignment">The assignment expression to type check</param>
	/// <returns>A valid, type checked assignment expression</returns>
	public ITypedAuraExpression Visit(UntypedAssignment assignment)
	{
		return _enclosingExpressionStore.WithEnclosing(
			() =>
			{
				// Fetch the variable being assigned to
				var v = FindOrThrow(
					assignment.Name.Value,
					ModuleName!,
					assignment.Name.Range
				);
				// Ensure that the new value and the variable have the same type
				var typedExpr = ExpressionAndConfirm(assignment.Value, v.Kind);
				return new TypedAssignment(
					assignment.Name,
					typedExpr,
					typedExpr.Typ
				);
			},
			assignment
		);
	}

	public ITypedAuraExpression Visit(UntypedPlusPlusIncrement inc)
	{
		var name = Expression(inc.Name);
		// Ensure that expression has type of either int or float
		if (name.Typ is not AuraInt &&
			name.Typ is not AuraFloat)
			throw new CannotIncrementNonNumberException(name.Typ, inc.Range);

		return new TypedPlusPlusIncrement(
			name,
			inc.PlusPlus,
			name.Typ
		);
	}

	public ITypedAuraExpression Visit(UntypedMinusMinusDecrement dec)
	{
		var name = Expression(dec.Name);
		// Ensure that expression has type of either int or float
		if (name.Typ is not AuraInt &&
			name.Typ is not AuraFloat)
			throw new CannotDecrementNonNumberException(name.Typ, dec.Range);

		return new TypedMinusMinusDecrement(
			name,
			dec.MinusMinus,
			name.Typ
		);
	}

	/// <summary>
	///     Type checks a binary expression
	/// </summary>
	/// <param name="binary">The binary expression to type check</param>
	/// <returns>A valid, type checked binary expression</returns>
	public ITypedAuraExpression Visit(UntypedBinary binary)
	{
		return _enclosingExpressionStore.WithEnclosing(
			() =>
			{
				var typedLeft = Expression(binary.Left);
				// The right-hand expression must have the same type as the left-hand expression
				var typedRight = ExpressionAndConfirm(binary.Right, typedLeft.Typ);
				return new TypedBinary(
					typedLeft,
					binary.Operator,
					typedRight,
					typedLeft.Typ
				);
			},
			binary
		);
	}

	/// <summary>
	///     Type checks a block expression
	/// </summary>
	/// <param name="block">The block expression to type check</param>
	/// <returns>A valid, type checked block expression</returns>
	public ITypedAuraExpression Visit(UntypedBlock block)
	{
		return _enclosingExpressionStore.WithEnclosing(
			() =>
			{
				var blockExContainer = new TypeCheckerExceptionContainer(_filePath);
				return InNewScope(
					() =>
					{
						var typedStmts = new List<ITypedAuraStatement>();
						foreach (var stmt in block.Statements)
							try
							{
								typedStmts.Add(Statement(stmt));
							}
							catch (TypeCheckerException e)
							{
								blockExContainer.Add(e);
							}

						// The block's type is the type of its last statement
						AuraType blockTyp = new AuraNil();
						if (typedStmts.Any())
						{
							var lastStmt = typedStmts.Last();
							blockTyp = lastStmt switch
							{
								// TODO When a block returns multiple types, it should be packaged up in a tuple so that it still returns one type
								TypedReturn r => r.Value is not null ? r.Value.Typ : new AuraNil(),
								TypedYield y => y.Value.Typ,
								_ => lastStmt.Typ is not AuraNone ? lastStmt.Typ : new AuraNil()
							};
						}

						if (!blockExContainer.IsEmpty())
						{
							blockExContainer.Valid = typedStmts;
							throw blockExContainer;
						}

						return new TypedBlock(
							block.OpeningBrace,
							typedStmts.ToList(),
							block.ClosingBrace,
							blockTyp
						);
					}
				);
			},
			block
		);
	}

	/// <summary>
	///     Type checks a call expression
	/// </summary>
	/// <param name="call">The call expression to type check</param>
	/// <returns>A valid, type checked call expression</returns>
	/// <exception cref="TooFewArgumentsException">
	///     Thrown if the number of arguments provided does
	///     not match the expected number of parameters
	/// </exception>
	public ITypedAuraExpression Visit(UntypedCall call)
	{
		return _enclosingExpressionStore.WithEnclosing(
			() =>
			{
				ICallable? funcDeclaration = null;
				string? @namespace = null;
				if (call.Callee is UntypedGet ug)
				{
					var typedGet = (TypedGet)Visit(ug);
					if (typedGet.Obj.Typ is IImportableModule im)
					{
						var f_ = FindOrThrow(
							ug.GetName(),
							im.GetModuleName(),
							ug.Range
						);
						var funcDeclaration_ = f_.Kind as ICallable;
						// Ensure the function call has the correct number of arguments
						if (funcDeclaration_!.GetParams().Count != call.Arguments.Count + 1)
						{
							if (call.Arguments.Count + 1 > funcDeclaration_.GetParams().Count)
								throw new TooManyArgumentsException(
									call.Arguments.Select(arg => Expression(arg.Item2)),
									funcDeclaration_.GetParams(),
									call
										.Arguments.Skip(funcDeclaration_.GetParams().Count)
										.Select(arg => arg.Item2.Range)
										.ToArray()
								);
							throw new TooFewArgumentsException(
								call.Arguments.Select(arg => Expression(arg.Item2)),
								funcDeclaration_.GetParams(),
								call.ClosingParen.Range
							);
						}

						// Type check arguments
						var typedArgs = new List<ITypedAuraExpression>();
						if (funcDeclaration_.GetParams().Count > 0)
						{
							var endIndex = funcDeclaration_.GetParams().Count - 1;
							typedArgs = TypeCheckArguments(
								call.Arguments.Select(pair => pair.Item2).ToList(),
								funcDeclaration_.GetParams().GetRange(1, endIndex)
							);
						}

						return new TypedCall(
							typedGet,
							typedArgs,
							call.ClosingParen,
							funcDeclaration_
						);
					}

					if (typedGet.Obj.Typ is AuraModule m) @namespace = m.Name;
					// A class's methods aren't added to the symbols table on their own, so if the object of the get expression is a class,
					// we need to get the method from the class type instead of the symbols table
					if (typedGet.Obj.Typ is AuraClass c)
					{
						AuraType fn = new AuraNone();
						if (typedGet.Obj is TypedThis)
							fn = c.Get(typedGet.Name.Value) ?? throw new UnknownVariableException(
								typedGet.Name.Value,
								typedGet.Name.Range
							);
						else
						{
							var typeWithVis = c.GetWithVisibility(typedGet.Name.Value);
							if (typeWithVis is null)
								throw new UnknownVariableException(typedGet.Name.Value, typedGet.Name.Range);
							if (typeWithVis.Value.Item1 is Visibility.Private)
								throw new CannotInvokePrivateMethodOutsideClass(
									typedGet.Name.Value,
									typedGet.Name.Range
								);
							fn = typeWithVis!.Value.Item2;
						}

						funcDeclaration = fn as ICallable;
					}
				}

				if (funcDeclaration is null)
				{
					if (IsSymbolInPreludeNamespace(call.GetName())) @namespace = "prelude";

					var f = FindOrThrow(
						call.Callee.GetName(),
						@namespace ?? ModuleName!,
						call.Callee.Range
					);
					funcDeclaration = f.Kind as ICallable;
				}


				// Type check arguments
				if (call.Arguments.Any())
				{
					var named = call.Arguments.All(arg => arg.Item1 is not null);
					var positional = call.Arguments.All(arg => arg.Item1 is null);

					if (named) return TypeCheckNamedParameters(call, funcDeclaration!);

					if (positional) return TypeCheckPositionalParameters(call, funcDeclaration!);

					throw new CannotMixNamedAndUnnamedArgumentsException(call.GetName(), call.Range);
				}

				var typedCallee = Expression((IUntypedAuraExpression)call.Callee) as ITypedAuraCallable;
				if (!funcDeclaration!.GetParams().Any())
					return new TypedCall(
						typedCallee!,
						new List<ITypedAuraExpression>(),
						call.ClosingParen,
						funcDeclaration
					);

				{
					// Add default values, if any
					var typedArgs = new List<ITypedAuraExpression>();
					foreach (var arg in funcDeclaration.GetParams())
					{
						var index = funcDeclaration.GetParamIndex(arg.Name.Value);
						var defaultValue = arg.ParamType.DefaultValue ??
										   throw new MustSpecifyValueForArgumentWithoutDefaultValueException(
											   call.GetName(),
											   arg.Name.Value,
											   call.ClosingParen.Range
										   );
						if (index >= typedArgs.Count)
							typedArgs.Add(defaultValue);
						else
							typedArgs.Insert(index, defaultValue);
					}

					return new TypedCall(
						typedCallee!,
						typedArgs,
						call.ClosingParen,
						funcDeclaration
					);
				}
			},
			call
		);
	}

	/// <summary>
	///     Type checks a get expression
	/// </summary>
	/// <param name="get">The get expression to type check</param>
	/// <returns>A valid, type checked get expression</returns>
	public ITypedAuraExpression Visit(UntypedGet get)
	{
		return _enclosingExpressionStore.WithEnclosing(
			() =>
			{
				// TODO check if object is a module (maybe check if a namespace exists matching the object's value?)
				if (get.Obj is UntypedVariable v &&
					_symbolsTable.GetNamespace(v.Name.Value) is not null)
				{
					var n = _symbolsTable.GetNamespace(v.Name.Value);
					if (n is not null)
					{
						var symbolNamespace = v.Name.Value;
						var attribute = FindOrThrow(
							get.Name.Value,
							symbolNamespace,
							get.Name.Range
						);
						return new TypedGet(
							new TypedVariable(
								new Tok(
									TokType.Identifier,
									v.Name.Value,
									get.Range
								),
								n.ParseAsModule()
							),
							get.Name,
							attribute.Kind
						);
					}
				}

				// Type check object, which must be gettable
				var objExpr = Expression(get.Obj);
				if (objExpr.Typ is not IGettable g)
					throw new CannotGetFromNonClassException(
						objExpr.ToString()!,
						objExpr.Typ,
						get.GetName(),
						get.Range
					);

				// Check if a stdlib package needs to be imported
				if (g is AuraString)
					Visit(
						new UntypedImport(
							new Tok(TokType.Import, "import"),
							new Tok(TokType.Identifier, "aura/strings"),
							new Tok(TokType.Identifier, "strings")
						)
					);

				if (g is AuraList)
					Visit(
						new UntypedImport(
							new Tok(TokType.Import, "import"),
							new Tok(TokType.Identifier, "aura/lists"),
							new Tok(TokType.Identifier, "lists")
						)
					);

				if (g is AuraError)
					Visit(
						new UntypedImport(
							new Tok(TokType.Import, "import"),
							new Tok(TokType.Identifier, "aura/errors"),
							new Tok(TokType.Identifier, "errors")
						)
					);

				if (g is AuraResult)
					Visit(
						new UntypedImport(
							new Tok(TokType.Import, "import"),
							new Tok(TokType.Identifier, "aura/results"),
							new Tok(TokType.Identifier, "results")
						)
					);

				if (g is AuraMap)
					Visit(
						new UntypedImport(
							new Tok(TokType.Import, "import"),
							new Tok(TokType.Identifier, "aura/maps"),
							new Tok(TokType.Identifier, "maps")
						)
					);

				// Fetch the gettable's attribute
				var attrTyp = g.Get(get.Name.Value) ??
							  throw new ClassAttributeDoesNotExistException(
								  objExpr.ToString()!,
								  get.GetName(),
								  get.Range
							  );
				return new TypedGet(
					objExpr,
					get.Name,
					attrTyp
				);
			},
			get
		);
	}

	public ITypedAuraExpression Visit(UntypedSet set)
	{
		return _enclosingExpressionStore.WithEnclosing(
			() =>
			{
				var typedObj = Expression(set.Obj);
				if (typedObj.Typ is not IGettable) throw new CannotSetOnNonClassException(typedObj.Typ, set.Range);

				var typedValue = Expression(set.Value);
				return new TypedSet(
					typedObj,
					set.Name,
					typedValue,
					typedValue.Typ
				);
			},
			set
		);
	}

	/// <summary>
	///     Type checks a get index expression
	/// </summary>
	/// <param name="getIndex">The get index expression to type check</param>
	/// <returns>A valid, type checked get index expression</returns>
	/// <exception cref="ExpectIndexableException">
	///     Thrown if the object being indexed does
	///     not implement the IIndexable interface
	/// </exception>
	/// <exception cref="TypeMismatchException">
	///     Thrown if the value used as the index is not the
	///     correct type
	/// </exception>
	public ITypedAuraExpression Visit(UntypedGetIndex getIndex)
	{
		return _enclosingExpressionStore.WithEnclosing(
			() =>
			{
				var expr = Expression(getIndex.Obj);
				var indexExpr = Expression(getIndex.Index);
				// Ensure that the object is indexable
				if (expr.Typ is not IIndexable indexableExpr)
					throw new ExpectIndexableException(expr.Typ, getIndex.Range);

				if (!indexableExpr.IndexingType().IsSameType(indexExpr.Typ))
					throw new TypeMismatchException(
						indexableExpr.IndexingType(),
						indexExpr.Typ,
						getIndex.Range
					);

				return new TypedGetIndex(
					expr,
					indexExpr,
					getIndex.ClosingBracket,
					indexableExpr.GetIndexedType()
				);
			},
			getIndex
		);
	}

	/// <summary>
	///     Type checks a get index range expression
	/// </summary>
	/// <param name="getIndexRange">The get index range expression to type check</param>
	/// <returns>A valid, type checked get index range expression</returns>
	/// <exception cref="ExpectRangeIndexableException">
	///     Thrown if the object being indexed does
	///     not implement hte IRangeIndexable interface
	/// </exception>
	/// <exception cref="TypeMismatchException">
	///     Thrown if the values used as the indices are not the
	///     correct type
	/// </exception>
	public ITypedAuraExpression Visit(UntypedGetIndexRange getIndexRange)
	{
		return _enclosingExpressionStore.WithEnclosing(
			() =>
			{
				var expr = Expression(getIndexRange.Obj);
				var lower = Expression(getIndexRange.Lower);
				var upper = Expression(getIndexRange.Upper);
				// Ensure that the object is range indexable
				if (expr.Typ is not IRangeIndexable rangeIndexableExpr)
					throw new ExpectRangeIndexableException(expr.Typ, getIndexRange.Range);

				if (!rangeIndexableExpr.IndexingType().IsSameType(lower.Typ))
					throw new TypeMismatchException(
						rangeIndexableExpr.IndexingType(),
						lower.Typ,
						getIndexRange.Range
					);

				if (!rangeIndexableExpr.IndexingType().IsSameType(upper.Typ))
					throw new TypeMismatchException(
						rangeIndexableExpr.IndexingType(),
						upper.Typ,
						getIndexRange.Range
					);

				return new TypedGetIndexRange(
					expr,
					lower,
					upper,
					getIndexRange.ClosingBracket,
					expr.Typ
				);
			},
			getIndexRange
		);
	}

	/// <summary>
	///     Type checks a grouping expression
	/// </summary>
	/// <param name="grouping">The grouping expression to type check</param>
	/// <returns>A valid, type checked grouping expression</returns>
	public ITypedAuraExpression Visit(UntypedGrouping grouping)
	{
		return _enclosingExpressionStore.WithEnclosing(
			() =>
			{
				var typedExpr = Expression(grouping.Expr);
				return new TypedGrouping(
					grouping.OpeningParen,
					typedExpr,
					grouping.ClosingParen,
					typedExpr.Typ
				);
			},
			grouping
		);
	}

	/// <summary>
	///     Type check if expression
	/// </summary>
	/// <param name="if">The if expression to type check</param>
	/// <returns>A valid, type checked if expression</returns>
	public ITypedAuraExpression Visit(UntypedIf @if)
	{
		return _enclosingExpressionStore.WithEnclosing(
			() =>
			{
				var typedCond = ExpressionAndConfirm(@if.Condition, new AuraBool());
				var typedThen = (TypedBlock)Visit(@if.Then);
				// Type check else branch
				ITypedAuraExpression? typedElse = null;
				if (@if.Else is not null) typedElse = ExpressionAndConfirm(@if.Else, typedThen.Typ);

				return new TypedIf(
					@if.If,
					typedCond,
					typedThen,
					typedElse,
					typedThen.Typ
				);
			},
			@if
		);
	}

	public ITypedAuraExpression Visit(IntLiteral literal)
	{
		return literal;
	}

	public ITypedAuraExpression Visit(FloatLiteral literal)
	{
		return literal;
	}

	public ITypedAuraExpression Visit(StringLiteral literal)
	{
		return literal;
	}

	public ITypedAuraExpression Visit<TU>(ListLiteral<TU> literal) where TU : IAuraAstNode
	{
		return _enclosingExpressionStore.WithEnclosing(
			() =>
			{
				var items = literal.Value.Select(item => (IUntypedAuraExpression)item).ToList();
				var typedItem = Expression(items.First());
				var typedItems = items.Select(item => ExpressionAndConfirm(item, typedItem.Typ)).ToList();
				return new ListLiteral<ITypedAuraExpression>(
					literal.OpeningBracket,
					typedItems,
					typedItem.Typ,
					literal.ClosingBrace
				);
			},
			literal
		);
	}

	public ITypedAuraExpression Visit<TK, TV>(MapLiteral<TK, TV> literal)
		where TK : IAuraAstNode where TV : IAuraAstNode
	{
		return _enclosingExpressionStore.WithEnclosing(
			() =>
			{
				var m = literal
					.Value.Select(pair => ((IUntypedAuraExpression)pair.Key, (IUntypedAuraExpression)pair.Value))
					.ToDictionary(pair => pair.Item1, pair => pair.Item2);
				var typedKey = Expression(m.Keys.First());
				var typedValue = Expression(m.Values.First());
				var typedM = m
					.Select(
						pair =>
						{
							var typedK = ExpressionAndConfirm(pair.Key, typedKey.Typ);
							var typedV = ExpressionAndConfirm(pair.Value, typedValue.Typ);
							return (typedK, typedV);
						}
					)
					.ToDictionary(pair => pair.Item1, pair => pair.Item2);
				return new MapLiteral<ITypedAuraExpression, ITypedAuraExpression>(
					literal.Map,
					typedM,
					typedKey.Typ,
					typedValue.Typ,
					literal.ClosingBrace
				);
			},
			literal
		);
	}

	public ITypedAuraExpression Visit(BoolLiteral literal)
	{
		return literal;
	}

	public ITypedAuraExpression Visit(UntypedNil literal)
	{
		return new TypedNil(literal.Nil);
	}

	public ITypedAuraExpression Visit(CharLiteral literal)
	{
		return literal;
	}

	/// <summary>
	///     Type checks a `this` expression
	/// </summary>
	/// <param name="this">The `this` expression to type check</param>
	/// <returns>A valid, type checked `this` expression</returns>
	public ITypedAuraExpression Visit(UntypedThis @this)
	{
		return new TypedThis(@this.This, _enclosingClassStore.Peek()!.Typ);
	}

	/// <summary>
	///     Type checks a unary expression
	/// </summary>
	/// <param name="unary">The unary expression to type check</param>
	/// <returns>A valid, type checked unary expression</returns>
	/// <exception cref="MismatchedUnaryOperatorAndOperandException">
	///     Thrown if the unary expression's operator and
	///     operand are not compatible
	/// </exception>
	public ITypedAuraExpression Visit(UntypedUnary unary)
	{
		return _enclosingExpressionStore.WithEnclosing(
			() =>
			{
				var typedRight = Expression(unary.Right);
				// Ensure that operand is a valid type and the operand can be used with it
				if (unary.Operator.Typ is TokType.Minus)
				{
					if (typedRight.Typ is not AuraInt &&
						typedRight.Typ is not AuraFloat)
						throw new MismatchedUnaryOperatorAndOperandException(
							unary.Operator.Value,
							typedRight.Typ,
							unary.Range
						);
				}
				else if (unary.Operator.Typ is TokType.Minus)
				{
					if (typedRight.Typ is not AuraBool)
						throw new MismatchedUnaryOperatorAndOperandException(
							unary.Operator.Value,
							typedRight.Typ,
							unary.Range
						);
				}

				return new TypedUnary(
					unary.Operator,
					typedRight,
					typedRight.Typ
				);
			},
			unary
		);
	}

	/// <summary>
	///     Type checks a variable expression
	/// </summary>
	/// <param name="v">The variable expression to type check</param>
	/// <returns>A valid, type checked variable expression</returns>
	public ITypedAuraExpression Visit(UntypedVariable v)
	{
		var @namespace = IsSymbolInPreludeNamespace(v.Name.Value) ? "prelude" : ModuleName!;
		var localVar = FindOrThrow(
			v.Name.Value,
			@namespace,
			v.Name.Range
		);
		var kind = !localVar.Kind.IsSameType(new AuraUnknown(string.Empty))
			? localVar.Kind
			: FindOrThrow(
					((AuraUnknown)localVar.Kind).Name,
					ModuleName!,
					v.Range
				)
				.Kind;
		return new TypedVariable(v.Name, kind);
	}

	public ITypedAuraExpression Visit(UntypedIs @is)
	{
		var typedExpr = Expression(@is.Expr);
		var typedInterfacePlaceholder = Visit(@is.Expected);
		return new TypedIs(typedExpr, (TypedInterfacePlaceholder)typedInterfacePlaceholder);
	}

	/// <summary>
	///     Type checks a logical expression
	/// </summary>
	/// <param name="logical">The logical expression to type check</param>
	/// <returns>A valid, type checked logical expression</returns>
	public ITypedAuraExpression Visit(UntypedLogical logical)
	{
		return _enclosingExpressionStore.WithEnclosing(
			() =>
			{
				var typedLeft = Expression(logical.Left);
				var typedRight = ExpressionAndConfirm(logical.Right, typedLeft.Typ);
				return new TypedLogical(
					typedLeft,
					logical.Operator,
					typedRight,
					new AuraBool()
				);
			},
			logical
		);
	}

	private void ExitScope()
	{
		// Before exiting block, remove any variables created in this scope
		_symbolsTable.ExitScope(ModuleName!);
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
		_symbolsTable.AddScope(ModuleName!);
		var typedNode = f();
		ExitScope();
		return typedNode;
	}

	private List<Param> TypeCheckParams(List<Param> untypedParams)
	{
		return untypedParams
			.Select(
				p =>
				{
					var typedDefaultValue = p.ParamType.DefaultValue is not null
						? (ILiteral)Expression(p.ParamType.DefaultValue)
						: null;
					var paramTyp = p.ParamType.Typ is not AuraUnknown u
						? p.ParamType.Typ
						: FindOrThrow(
								u.Name,
								ModuleName!,
								p.Name.Range
							)
							.Kind;
					return new Param(
						p.Name,
						new ParamType(
							paramTyp,
							p.ParamType.Variadic,
							typedDefaultValue
						)
					);
				}
			)
			.ToList();
	}

	private List<ITypedAuraExpression> TypeCheckArguments(
		List<IUntypedAuraExpression> arguments,
		List<Param> parameters
	)
	{
		var typedArgs = new List<ITypedAuraExpression>();
		var paramTypes = parameters.Select(p => p.ParamType).ToList();
		var i = 0;

		foreach (var arg in arguments)
		{
			var typedArg = i >= paramTypes.Count
				? ExpressionAndConfirm(arg, paramTypes[^1].Typ)
				: ExpressionAndConfirm(arg, paramTypes[i].Typ);
			typedArgs.Add(typedArg);
			i++;
		}

		return typedArgs;
	}

	private TypedCall TypeCheckPositionalParameters(UntypedCall call, ICallable declaration)
	{
		var typedCallee = Expression((IUntypedAuraExpression)call.Callee) as ITypedAuraCallable;
		// Ensure the function call has the correct number of arguments
		if (declaration.HasVariadicParam() &&
			call.Arguments.Count < declaration.GetParams().Count)
		{
			if (call.Arguments.Count + 1 > declaration.GetParams().Count)
				throw new TooManyArgumentsException(
					call.Arguments.Select(arg => Expression(arg.Item2)),
					declaration.GetParams(),
					call.Arguments.Skip(declaration.GetParams().Count).Select(arg => arg.Item2.Range).ToArray()
				);
			throw new TooFewArgumentsException(
				call.Arguments.Select(arg => Expression(arg.Item2)),
				declaration.GetParams(),
				call.ClosingParen.Range
			);
		}

		if (!declaration.HasVariadicParam() &&
			declaration.GetParamTypes().Count != call.Arguments.Count)
		{
			if (call.Arguments.Count + 1 > declaration.GetParams().Count)
				throw new TooManyArgumentsException(
					call.Arguments.Select(arg => Expression(arg.Item2)),
					declaration.GetParams(),
					call.Arguments.Skip(declaration.GetParams().Count).Select(arg => arg.Item2.Range).ToArray()
				);
			throw new TooFewArgumentsException(
				call.Arguments.Select(arg => Expression(arg.Item2)),
				declaration.GetParams(),
				call.ClosingParen.Range
			);
		}

		// The arguments are already in order when using positional arguments, so just extract the arguments
		var orderedArgs = call.Arguments.Select(pair => pair.Item2).ToList();

		var typedArgs = TypeCheckArguments(orderedArgs, declaration.GetParams());

		return new TypedCall(
			typedCallee!,
			typedArgs,
			call.ClosingParen,
			declaration
		);
	}

	private TypedCall TypeCheckNamedParameters(UntypedCall call, ICallable declaration)
	{
		var typedCallee = Expression((IUntypedAuraExpression)call.Callee) as ITypedAuraCallable;
		// Insert each named argument into its correct position
		var orderedArgs = new List<IUntypedAuraExpression>();
		foreach (var arg in call.Arguments)
		{
			var index = declaration.GetParamIndex(arg.Item1!.Value.Value);
			if (index >= orderedArgs.Count)
				orderedArgs.Add(arg.Item2);
			else
				orderedArgs.Insert(index, arg.Item2);
		}

		// Filter out the parameters that aren't included in the argument list. We will ensure that the omitted parameters have
		// a default value later. However, if they have a default value, they were already type checked when the function was
		// first declared, so they don't need to be type checked again here.
		var includedParams = declaration
			.GetParams()
			.Where(p => call.Arguments.Any(arg => arg.Item1!.Value.Value == p.Name.Value));
		// Type check the included named arguments
		var typedArgs = orderedArgs
			.Zip(includedParams)
			.Select(pair => ExpressionAndConfirm(pair.First, pair.Second.ParamType.Typ))
			.ToList();
		// With named arguments, you may omit arguments if they have been declared with a default value.
		// Check for any missing parameters and fill in their default value, if they have one
		var missingParams = declaration
			.GetParams()
			.Where(p => call.Arguments.All(arg => arg.Item1!.Value.Value != p.Name.Value));
		foreach (var missingParam in missingParams)
		{
			var index = declaration.GetParamIndex(missingParam.Name.Value);
			var defaultValue = missingParam.ParamType.DefaultValue ??
							   throw new MustSpecifyValueForArgumentWithoutDefaultValueException(
								   call.GetName(),
								   missingParam.Name.Value,
								   call.Range
							   );
			if (index >= orderedArgs.Count)
				typedArgs.Add(defaultValue);
			else
				typedArgs.Insert(index, defaultValue);
		}

		return new TypedCall(
			typedCallee!,
			typedArgs,
			call.ClosingParen,
			declaration
		);
	}

	private TU WithEnclosingStmt<TU, T>(
		Func<TU> f,
		T node,
		string symbolNamespace
	)
		where TU : ITypedAuraStatement where T : IUntypedAuraStatement
	{
		_symbolsTable.AddScope(symbolNamespace);
		var typedNode = _enclosingStatementStore.WithEnclosing(f, node);
		_symbolsTable.ExitScope(symbolNamespace);
		return typedNode;
	}

	private bool IsSymbolInPreludeNamespace(string symbolName)
	{
		var typ = _prelude.GetPrelude().Get(symbolName);
		return typ is not null;
	}

	public ITypedAuraStatement Visit(UntypedCheck check)
	{
		var typedCall = Visit(check.Call);
		// The `check` keyword is only valid when the enclosing function and the checked function call both have a return
		// type of `result`
		var enclosingFuncDeclaration = _enclosingFunctionDeclarationStore.Peek() ??
									   throw new InvalidUseOfCheckKeywordException(check.Range);
		// When using the `check` keyword, the enclosing function must return a `result` type as its only return value
		if (enclosingFuncDeclaration.ReturnType?.Count != 1) throw new InvalidUseOfCheckKeywordException(check.Range);

		if (enclosingFuncDeclaration.ReturnType?.First() is not AuraResult)
			throw new InvalidUseOfCheckKeywordException(check.Range);

		// The function call must also return a `result` as its only return value
		if (typedCall.Typ is not AuraResult) throw new InvalidUseOfCheckKeywordException(check.Range);

		return new TypedCheck(check.Check, (TypedCall)typedCall);
	}

	public ITypedAuraStatement Visit(UntypedStruct @struct)
	{
		return WithEnclosingStmt(
			() =>
			{
				var typedParams = @struct.Params.Select(
					p =>
					{
						var typedDefaultValue = p.ParamType.DefaultValue is not null
							? (ILiteral)Expression(p.ParamType.DefaultValue)
							: null;
						var paramTyp = p.ParamType.Typ;
						return new Param(
							p.Name,
							new ParamType(
								paramTyp,
								p.ParamType.Variadic,
								typedDefaultValue
							)
						);
					}
				);

				return new TypedStruct(
					@struct.Struct,
					@struct.Name,
					typedParams.ToList(),
					@struct.ClosingParen,
					@struct.Documentation
				);
			},
			@struct,
			ModuleName!
		);
	}

	public ITypedAuraExpression Visit(UntypedAnonymousStruct anonymousStruct)
	{
		throw new NotImplementedException();
	}
}
