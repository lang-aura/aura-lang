using AuraLang.AST;
using AuraLang.Exceptions.TypeChecker;
using AuraLang.ModuleCompiler;
using AuraLang.Prelude;
using AuraLang.Shared;
using AuraLang.Stdlib;
using AuraLang.Symbol;
using AuraLang.Token;
using AuraLang.Types;
using AuraLang.Visitor;

namespace AuraLang.TypeChecker;

public class AuraTypeChecker : IUntypedAuraStmtVisitor<ITypedAuraStatement>, IUntypedAuraExprVisitor<ITypedAuraExpression>
{
	private readonly IGlobalSymbolsTable _symbolsTable;
	private readonly IEnclosingClassStore _enclosingClassStore;
	private readonly IEnclosingFunctionDeclarationStore _enclosingFunctionDeclarationStore;
	private readonly AuraStdlib _stdlib = new();
	private readonly TypeCheckerExceptionContainer _exContainer;
	private readonly EnclosingNodeStore<IUntypedAuraExpression> _enclosingExpressionStore;
	private readonly EnclosingNodeStore<IUntypedAuraStatement> _enclosingStatementStore;
	private string ProjectName { get; }
	private string? ModuleName { get; set; }
	private readonly AuraPrelude _prelude = new();

	public AuraTypeChecker(IGlobalSymbolsTable symbolsTable, IEnclosingClassStore enclosingClassStore,
		IEnclosingFunctionDeclarationStore enclosingFunctionDeclarationStore,
		EnclosingNodeStore<IUntypedAuraExpression> enclosingExpressionStore,
		EnclosingNodeStore<IUntypedAuraStatement> enclosingStatementStore,
		string filePath, string projectName)
	{
		_symbolsTable = symbolsTable;
		_enclosingClassStore = enclosingClassStore;
		_enclosingFunctionDeclarationStore = enclosingFunctionDeclarationStore;
		_enclosingExpressionStore = enclosingExpressionStore;
		_enclosingStatementStore = enclosingStatementStore;
		_exContainer = new TypeCheckerExceptionContainer(filePath);
		ProjectName = projectName;
	}

	public void BuildSymbolsTable(List<IUntypedAuraStatement> stmts)
	{
		ModuleName = ((UntypedMod)stmts.First(stmt => stmt is UntypedMod)).Value.Value;

		foreach (var stmt in stmts)
		{
			try
			{
				switch (stmt)
				{
					case UntypedImport i:
						var im = Visit(i);
						break;
					case UntypedLet l:
						AddLetStmtToSymbolsTable(l);
						break;
					case UntypedNamedFunction nf:
						var f = ParseFunctionSignature(nf);
						_symbolsTable.TryAddSymbol(
							symbol: new AuraSymbol(
								Name: nf.Name.Value,
								Kind: f
							),
							symbolsNamespace: ModuleName
						);
						break;
					case UntypedClass c:
						var cl = ParseClassSignature(c);
						_symbolsTable.TryAddSymbol(
							symbol: new AuraSymbol(
								Name: c.Name.Value,
								Kind: cl
							),
							symbolsNamespace: ModuleName
						);
						break;
					case UntypedStruct @struct:
						var s = (TypedStruct)Visit(@struct);
						_symbolsTable.TryAddSymbol(
							symbol: new AuraSymbol(
								Name: s.Name.Value,
								Kind: new AuraStruct(
									name: s.Name.Value,
									parameters: s.Params,
									pub: Visibility.Private
								)
							),
							symbolsNamespace: ModuleName
						);
						break;
					case UntypedInterface interface_:
						_symbolsTable.TryAddSymbol(
							symbol: new AuraSymbol(
								Name: interface_.Name.Value,
								Kind: new AuraInterface(interface_.Name.Value, interface_.Methods, interface_.Public)
							),
							symbolsNamespace: ModuleName
						);

						foreach (var m in interface_.Methods)
						{
							_symbolsTable.TryAddSymbol(
								symbol: new AuraSymbol(
									Name: m.Name,
									Kind: m
								),
								symbolsNamespace: ModuleName
							);
						}
						break;
				}
			}
			catch (TypeCheckerException ex)
			{
				_exContainer.Add(ex);
			}
		}

		if (!_exContainer.IsEmpty()) throw _exContainer;
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

		if (!_exContainer.IsEmpty()) throw _exContainer;
		return typedAst;
	}

	private ITypedAuraStatement Statement(IUntypedAuraStatement stmt) => stmt.Accept(this);

	private ITypedAuraExpression Expression(IUntypedAuraExpression expr) => expr.Accept(this);

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
	/// <param name="symbolName">The variable's name</param>
	/// <param name="symbolNamespace">The namespace where the variable was defined</param>
	/// <param name="expected">The variable's expected type</param>
	/// <param name="line">The line in the Aura source file where the variable usage appears</param>
	private T FindAndConfirm<T>(string symbolName, string symbolNamespace, T expected, int line) where T : AuraType
	{
		var local = _symbolsTable.GetSymbol(symbolName, symbolNamespace) ?? throw new UnknownVariableException(symbolName, line);
		if (!expected.IsSameOrInheritingType(local.Kind)) throw new UnexpectedTypeException(line);
		return (T)local.Kind;
	}

	private AuraSymbol FindOrThrow(string varName, string symbolNamespace, int line)
	{
		var local = _symbolsTable.GetSymbol(varName, symbolNamespace) ?? throw new UnknownVariableException(varName, line);
		return local!;
	}

	/// <summary>
	/// Type checks a defer statement
	/// </summary>
	/// <param name="defer">The defer statement to type check</param>
	/// <returns>A valid, type checked defer statement</returns>
	public ITypedAuraStatement Visit(UntypedDefer defer)
	{
		return WithEnclosingStmt(
			f: () =>
			{
				var typedCall = Visit((UntypedCall)defer.Call);
				return new TypedDefer((TypedCall)typedCall, defer.Line);
			},
			node: defer,
			symbolNamespace: ModuleName!
		);
	}

	/// <summary>
	/// Type checks an expression statement
	/// </summary>
	/// <param name="exprStmt">The expression statement to type check</param>
	/// <returns>A valid, type checked expression statement</returns>
	public ITypedAuraStatement Visit(UntypedExpressionStmt exprStmt) =>
		new TypedExpressionStmt(Expression(exprStmt.Expression), exprStmt.Line);

	/// <summary>
	/// Type checks a for loop
	/// </summary>
	/// <param name="forStmt">The for loop to be type checked</param>
	/// <returns>A valid, type checked for loop</returns>
	public ITypedAuraStatement Visit(UntypedFor forStmt)
	{
		return WithEnclosingStmt(
			f: () =>
			{
				return InNewScope(() =>
				{
					var typedInit = forStmt.Initializer is not null ? Statement(forStmt.Initializer) : null;
					var typedCond = forStmt.Condition is not null
						? ExpressionAndConfirm(forStmt.Condition, new AuraBool())
						: null;
					var typedInc = forStmt.Increment is not null
						? Expression(forStmt.Increment)
						: null;
					var typedBody = NonReturnableBody(forStmt.Body);
					return new TypedFor(typedInit, typedCond, typedInc, typedBody, forStmt.Line);
				});
			},
			node: forStmt,
			symbolNamespace: ModuleName!
		);
	}

	/// <summary>
	/// Type checks a for each loop
	/// </summary>
	/// <param name="forEachStmt">The for each loop to be type checked</param>
	/// <returns>A valid, type checked for each loop</returns>
	/// <exception cref="ExpectIterableException">Thrown if the value being iterated over does not implement
	/// the IIterable interface</exception>
	public ITypedAuraStatement Visit(UntypedForEach forEachStmt)
	{
		return WithEnclosingStmt(
			f: () =>
			{
				return InNewScope(() =>
				{
					// Type check iterable
					var iter = Expression(forEachStmt.Iterable);
					if (iter.Typ is not IIterable typedIter) throw new ExpectIterableException(forEachStmt.Line);
					// Add current element variable to list of local variables
					_symbolsTable.TryAddSymbol(
						symbol: new AuraSymbol(
							Name: forEachStmt.EachName.Value,
							Kind: typedIter.GetIterType()),
						symbolsNamespace: ModuleName!);
					// Type check body
					var typedBody = NonReturnableBody(forEachStmt.Body);
					return new TypedForEach(forEachStmt.EachName, iter, typedBody, forEachStmt.Line);
				});
			},
			node: forEachStmt,
			symbolNamespace: ModuleName!
		);
	}

	/// <summary>
	/// Type checks a named function declaration
	/// </summary>
	/// <param name="f">The named function to type check</param>
	/// <param name="modName">The module name where the function is declared</param>
	/// <returns>A valid, type checked named function declaration</returns>
	/// <exception cref="TypeMismatchException">Thrown if the function's body doesn't return
	/// the same type as specified in the function's signature</exception>
	public ITypedAuraStatement Visit(UntypedNamedFunction f, string modName)
	{
		_enclosingFunctionDeclarationStore.Push(f);
		return WithEnclosingStmt(
			f: () =>
			{
				return InNewScope(() =>
				{
					var typedParams = TypeCheckParams(f.Params);
					// Add parameters as local variables
					foreach (var param in typedParams)
					{
						_symbolsTable.TryAddSymbol(
							symbol: new AuraSymbol(
								Name: param.Name.Value,
								Kind: param.ParamType.Typ
							),
							symbolsNamespace: ModuleName!
						);
					}

					var typedBody = (TypedBlock)Visit(f.Body);
					// Type check function's return type
					AuraType returnType = new AuraNil();
					if (f.ReturnType?.Count == 1) returnType = TypeCheckReturnTypeTok(f.ReturnType[0]);
					else if (f.ReturnType?.Count > 1)
					{
						returnType = new AuraAnonymousStruct(
							parameters: f.ReturnType.Select((tok, i) =>
							{
								return new Param(
									Name: new Tok(
										Typ: TokType.Identifier,
										Value: i.ToString(),
										Line: f.Line
									),
									ParamType: new(
										Typ: TypeTokenToType(tok),
										Variadic: false,
										DefaultValue: null
									)
								);
							}).ToList(),
							pub: Visibility.Private
						);
					}
					// Ensure the function's body returns the type specified in its signature
					if (!returnType.IsSameOrInheritingType(typedBody.Typ))
						throw new TypeMismatchException(f.Line);
					// Add function as local variable
					_symbolsTable.TryAddSymbol(
						symbol: new AuraSymbol(
							Name: f.Name.Value,
							Kind: new AuraNamedFunction(
								f.Name.Value,
								f.Public,
								new AuraFunction(
									TypeCheckParams(f.Params),
									returnType
								)
							)
						),
						symbolsNamespace: ModuleName!
					);
					return new TypedNamedFunction(f.Name, typedParams.ToList(), typedBody, returnType, f.Public, f.Line);
				});
			},
			node: f,
			symbolNamespace: ModuleName!
		);
	}

	public ITypedAuraStatement Visit(UntypedNewLine nl) => throw new NotImplementedException();

	/// <summary>
	/// Type checks an anonymous function declaration
	/// </summary>
	/// <param name="f">The anonymous function to type check</param>
	/// <returns>A valid, type checked anonymous function declaration</returns>
	/// <exception cref="TypeMismatchException">Thrown if the anonymous function's body returns a type different
	/// than the one specified in the function's signature</exception>
	public ITypedAuraExpression Visit(UntypedAnonymousFunction f)
	{
		return _enclosingExpressionStore.WithEnclosing(
			f: () =>
			{
				return InNewScope(() =>
				{
					var typedParams = TypeCheckParams(f.Params);
					// Add the function's parameters as local variables
					foreach (var param in typedParams)
					{
						_symbolsTable.TryAddSymbol(
							symbol: new AuraSymbol(
								Name: param.Name.Value,
								Kind: param.ParamType.Typ
							),
							symbolsNamespace: ModuleName!
						);
					}

					var typedBody = (TypedBlock)Visit(f.Body);
					// Parse the function's return type
					AuraType returnType = new AuraNil();
					if (f.ReturnType?.Count == 1) returnType = TypeTokenToType(f.ReturnType![0]);
					else if (f.ReturnType?.Count > 1)
					{
						returnType = new AuraAnonymousStruct(
							parameters: f.ReturnType!.Select((tok, i) =>
							{
								return new Param(
									Name: new Tok(
										Typ: TokType.Identifier,
										Value: i.ToString(),
										Line: f.Line
									),
									ParamType: new(
										Typ: TypeTokenToType(tok),
										Variadic: false,
										DefaultValue: null
									)
								);
							}).ToList(),
							pub: Visibility.Private
						);
					}
					// Ensure the function's body returns the type specified in its signature
					if (!returnType.IsSameOrInheritingType(typedBody.Typ))
						throw new TypeMismatchException(f.Line);

					return new TypedAnonymousFunction(typedParams, typedBody, returnType, f.Line);
				});
			},
			node: f
		);
	}

	private AuraNamedFunction ParseFunctionSignature(UntypedNamedFunction f)
	{
		var typedParams = TypeCheckParams(f.Params);
		AuraType returnType = new AuraNil();
		if (f.ReturnType?.Count == 1) returnType = TypeTokenToType(f.ReturnType![0]);
		if (f.ReturnType?.Count > 1)
		{
			returnType = new AuraAnonymousStruct(
				parameters: f.ReturnType!.Select((tok, i) =>
				{
					return new Param(
						Name: new Tok(
							Typ: TokType.Identifier,
							Value: i.ToString(),
							Line: f.Line
						),
						ParamType: new(
							Typ: TypeTokenToType(tok),
							Variadic: false,
							DefaultValue: null
						)
					);
				}).ToList(),
				pub: Visibility.Private
			);
		}
		//var returnType = TypeCheckReturnTypeTok(f.ReturnType);
		return new AuraNamedFunction(f.Name.Value, f.Public, new AuraFunction(typedParams, returnType));
	}

	public ITypedAuraStatement Visit(UntypedNamedFunction f)
	{
		_enclosingFunctionDeclarationStore.Push(f);
		var typedParams = TypeCheckParams(f.Params);
		AuraType returnType = new AuraNil();
		if (f.ReturnType?.Count == 1) returnType = TypeTokenToType(f.ReturnType![0]);
		if (f.ReturnType?.Count > 1)
		{
			returnType = new AuraAnonymousStruct(
				parameters: f.ReturnType!.Select((tok, i) =>
				{
					return new Param(
						Name: new Tok(
							Typ: TokType.Identifier,
							Value: i.ToString(),
							Line: f.Line
						),
						ParamType: new(
							Typ: TypeTokenToType(tok),
							Variadic: false,
							DefaultValue: null
						)
					);
				}).ToList(),
				pub: Visibility.Private
			);
		}
		// Add parameters as local variables
		foreach (var param in f.Params)
		{
			var paramTyp = param.ParamType.Typ;
			if (param.ParamType.Variadic) paramTyp = new AuraList(paramTyp);
			_symbolsTable.TryAddSymbol(
				symbol: new AuraSymbol(
					Name: param.Name.Value,
					Kind: paramTyp
				),
				symbolsNamespace: ModuleName!
			);
		}

		var typedBody = (TypedBlock)Visit(f.Body);
		// Ensure the function's body returns the same type specified in its signature
		if (!returnType.IsSameOrInheritingType(typedBody.Typ)) throw new TypeMismatchException(f.Line);

		return new TypedNamedFunction(f.Name, typedParams, typedBody, returnType, f.Public, f.Line);
	}

	private void AddLetStmtToSymbolsTable(UntypedLet let)
	{
		// Ensure that all variables being created either have a type annotation or a missing a type annotation (they cannot mix)
		var annotations = let.NameTyps.All(nt => nt is not null);
		var missingAnnotations = let.NameTyps.All(nt => nt is null);
		if (!annotations && !missingAnnotations) throw new CannotMixTypeAnnotationsException(let.Line);

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

		var nameTyp = let.NameTyps[0]!;
		// Type check initializer
		var defaultable = nameTyp as IDefaultable;
		if (let.Initializer is null && defaultable is null)
			throw new MustSpecifyInitialValueForNonDefaultableTypeException(nameTyp, let.Line);
		var typedInit = let.Initializer is not null
			? ExpressionAndConfirm(let.Initializer, nameTyp)
			: defaultable!.Default(let.Line);
		// Add new variable to list of locals
		_symbolsTable.TryAddSymbol(
			symbol: new AuraSymbol(
				Name: let.Names[0].Value,
				Kind: typedInit?.Typ ?? new AuraNil()
			),
			symbolsNamespace: ModuleName!
		);
	}

	private void TypeCheckMultipleVariablesInLetStmt(UntypedLet let)
	{
		// Package the let statement's variable names into an anonymous struct
		var names = new AuraAnonymousStruct(
			parameters: let.Names!.Select((name, i) =>
			{
				return new Param(
					Name: new Tok(
						Typ: TokType.Identifier,
						Value: i.ToString(),
						Line: let.Line
					),
					ParamType: new(
						Typ: let.NameTyps[i]!,
						Variadic: false,
						DefaultValue: null
					)
				);
			}).ToList(),
			pub: Visibility.Private
		);
		// Type check initializer
		var typedInit = ExpressionAndConfirm(let.Initializer!, names);
		// Add new variables to list of locals
		foreach (var (name, typ) in let.Names.Zip(let.NameTyps))
		{
			_symbolsTable.TryAddSymbol(
				symbol: new AuraSymbol(
					Name: name.Value,
					Kind: typ!
				),
				symbolsNamespace: ModuleName!
			);
		}
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
		_symbolsTable.TryAddSymbol(
			symbol: new AuraSymbol(
				Name: let.Names[0].Value,
				Kind: typedInit?.Typ ?? new AuraNil()
			),
			symbolsNamespace: ModuleName!
		);
	}

	private void TypeCheckMultipleVariablesInShortLetStmt(UntypedLet let)
	{
		// Type check initializer
		var typedInit = Expression(let.Initializer!);
		// Add new variables to list of locals
		foreach (var (name, typ) in let.Names.Zip(((AuraAnonymousStruct)typedInit.Typ).Parameters.Select(p => p.ParamType.Typ)))
		{
			_symbolsTable.TryAddSymbol(
				symbol: new AuraSymbol(
					Name: name.Value,
					Kind: typ!
				),
				symbolsNamespace: ModuleName!
			);
		}
	}

	/// <summary>
	/// Type checks a let statement
	/// </summary>
	/// <param name="let">The let statement to type check</param>
	/// <returns>A valid, type checked let statement</returns>
	public ITypedAuraStatement Visit(UntypedLet let)
	{
		// Ensure that all variables being created either have a type annotation or a missing a type annotation (they cannot mix)
		var annotations = let.NameTyps.All(nt => nt is not null);
		var missingAnnotations = let.NameTyps.All(nt => nt is null);
		if (!annotations && !missingAnnotations) throw new CannotMixTypeAnnotationsException(let.Line);

		if (missingAnnotations) return ShortLetStmt(let);

		if (let.Names.Count > 1) return LetStmtMultipleNames(let);

		var typedLet = WithEnclosingStmt(
			f: () =>
			{
				var nameTyp = let.NameTyps[0]!;
				// Type check initializer
				var defaultable = nameTyp as IDefaultable;
				if (let.Initializer is null && defaultable is null)
					throw new MustSpecifyInitialValueForNonDefaultableTypeException(nameTyp, let.Line);
				var typedInit = let.Initializer is not null
					? ExpressionAndConfirm(let.Initializer, nameTyp)
					: defaultable!.Default(let.Line);

				return new TypedLet(let.Names, true, let.Mutable, typedInit, let.Line);
			},
			node: let,
			symbolNamespace: ModuleName!
		);

		// Add new variable to list of locals
		_symbolsTable.TryAddSymbol(
			symbol: new AuraSymbol(
				Name: typedLet.Names[0].Value,
				Kind: typedLet.Initializer?.Typ ?? new AuraNone()
			),
			symbolsNamespace: ModuleName!
		);

		return typedLet;
	}

	private TypedLet LetStmtMultipleNames(UntypedLet let)
	{
		var typedLet = WithEnclosingStmt(
			f: () =>
			{
				// Package the let statement's variable names into an anonymous struct
				var names = new AuraAnonymousStruct(
					parameters: let.Names!.Select((name, i) =>
					{
						return new Param(
							Name: new Tok(
								Typ: TokType.Identifier,
								Value: i.ToString(),
								Line: let.Line
							),
							ParamType: new(
								Typ: let.NameTyps[i]!,
								Variadic: false,
								DefaultValue: null
							)
						);
					}).ToList(),
					pub: Visibility.Private
				);
				// Type check initializer
				var typedInit = ExpressionAndConfirm(let.Initializer!, names);

				return new TypedLet(
					Names: let.Names,
					TypeAnnotation: true,
					Mutable: false,
					Initializer: typedInit,
					Line: let.Line
				);
			},
			node: let,
			symbolNamespace: ModuleName!
		);

		// Add new variables to list of locals
		foreach (var (name, typ) in let.Names.Zip(let.NameTyps))
		{
			_symbolsTable.TryAddSymbol(
				symbol: new AuraSymbol(
					Name: name.Value,
					Kind: typ!
				),
				symbolsNamespace: ModuleName!
			);
		}

		return typedLet;
	}

	/// <summary>
	/// Type checks a short let statement
	/// </summary>
	/// <param name="let">The short let statement to type check</param>
	/// <returns>A valid, type checked short let statement</returns>
	private TypedLet ShortLetStmt(UntypedLet let)
	{
		if (let.Names.Count > 1) return ShortLetStmtMultipleNames(let);

		var typedShortLet = WithEnclosingStmt(
			f: () =>
			{
				// Type check initializer
				var typedInit = let.Initializer is not null ? Expression(let.Initializer) : null;
				return new TypedLet(let.Names, false, let.Mutable, typedInit, let.Line);
			},
			node: let,
			symbolNamespace: ModuleName!
		);

		// Add new variable to list of locals
		_symbolsTable.TryAddSymbol(
			symbol: new AuraSymbol(
				Name: typedShortLet.Names[0].Value,
				Kind: typedShortLet.Initializer!.Typ
			),
			symbolsNamespace: ModuleName!
		);

		return typedShortLet;
	}

	private TypedLet ShortLetStmtMultipleNames(UntypedLet let)
	{
		var typedLet = WithEnclosingStmt(
			f: () =>
			{
				// Type check initializer
				var typedInit = Expression(let.Initializer!);

				return new TypedLet(
					Names: let.Names,
					TypeAnnotation: false,
					Mutable: false,
					Initializer: typedInit,
					Line: let.Line
				);
			},
			node: let,
			symbolNamespace: ModuleName!
		);

		// Add new variables to list of locals
		foreach (var (name, typ) in let.Names.Zip(((AuraAnonymousStruct)typedLet.Initializer!.Typ).Parameters.Select(p => p.ParamType.Typ)))
		{
			_symbolsTable.TryAddSymbol(
				symbol: new AuraSymbol(
					Name: name.Value,
					Kind: typ!
				),
				symbolsNamespace: ModuleName!
			);
		}

		return typedLet;
	}

	/// <summary>
	/// Type checks a mod statement, and saves the typed mod as the current mod
	/// </summary>
	/// <param name="mod">The mod statement to be type checked</param>
	/// <returns>A valid, type checked mod statement</returns>
	public ITypedAuraStatement Visit(UntypedMod mod)
	{
		var m = new TypedMod(mod.Value, mod.Line);
		return m;
	}

	/// <summary>
	/// Type checks a return statement
	/// </summary>
	/// <param name="r">The return statement to type check</param>
	/// <returns>A valid, type checked return statement</returns>
	public ITypedAuraStatement Visit(UntypedReturn r)
	{
		return WithEnclosingStmt(
			f: () =>
			{
				if (r.Value is null) return new TypedReturn(null, r.Line);
				if (r.Value!.Count == 1) return new TypedReturn(Expression(r.Value[0]), r.Line);
				// If the return statement contains more than one expression, we package the expressions up as a struct
				var typedReturnValues = r.Value.Select(Expression);
				return new TypedReturn(
					Value: new TypedAnonymousStruct(
						Params: typedReturnValues.Select((v, i) =>
						{
							return new Param(
								Name: new Tok(
									Typ: TokType.Identifier,
									Value: i.ToString(),
									Line: r.Line
								),
								ParamType: new(
									Typ: v.Typ,
									Variadic: false,
									DefaultValue: null
								)
							);
						}).ToList(),
						Values: typedReturnValues.ToList(),
						Line: r.Line
					),
					Line: r.Line);
			},
			node: r,
			symbolNamespace: ModuleName!
		);
	}

	private AuraClass ParseClassSignature(UntypedClass class_)
	{
		var typedParams = class_.Params.Select(p =>
		{
			var typedDefaultValue = p.ParamType.DefaultValue is not null
				? (ILiteral)Expression(p.ParamType.DefaultValue)
				: null;
			var paramTyp = p.ParamType.Typ;
			return new Param(p.Name, new ParamType(paramTyp, p.ParamType.Variadic, typedDefaultValue));
		});
		var methodSignatures = class_.Methods.Select(ParseFunctionSignature);
		var implements = class_.Implementing.Any()
			? class_.Implementing.Select(impl =>
			{
				var local = FindOrThrow(impl.Value, ModuleName!, class_.Line);
				var i = local.Kind as AuraInterface ??
						throw new CannotImplementNonInterfaceException(impl.Value, class_.Line);
				return i;
			})
			: new List<AuraInterface>();

		return new AuraClass(class_.Name.Value, typedParams.ToList(), methodSignatures.ToList(), implements.ToList(), class_.Public);
	}

	/// <summary>
	/// Type checks a class declaration
	/// </summary>
	/// <param name="class_">The class declaration to type check</param>
	/// <returns>A valid, type checked class declaration</returns>
	public ITypedAuraStatement Visit(UntypedClass class_)
	{
		return WithEnclosingStmt(
			f: () =>
			{
				var typedParams = class_.Params.Select(p =>
				{
					var typedDefaultValue = p.ParamType.DefaultValue is not null
						? (ILiteral)Expression(p.ParamType.DefaultValue)
						: null;
					var paramTyp = p.ParamType.Typ;
					return new Param(p.Name, new ParamType(paramTyp, p.ParamType.Variadic, typedDefaultValue));
				});

				var methodSignatures = class_.Methods.Select(m => ParseFunctionSignature(m)).ToList();
				var methodTypes = methodSignatures
					.Select(method =>
					{
						var typedMethodParams = method.GetParams().Select(p =>
						{
							var typedMethodDefaultValue = p.ParamType.DefaultValue is not null
								? (ILiteral)Expression(p.ParamType.DefaultValue)
								: null;
							var methodParamType = p.ParamType.Typ;
							return new Param(p.Name,
								new ParamType(methodParamType, p.ParamType.Variadic, typedMethodDefaultValue));
						});
						return new AuraNamedFunction(
							method.Name,
							method.Public,
							new AuraFunction(typedMethodParams.ToList(), method.GetReturnType()));
					})
					.ToList();

				// Get type of implementing interface
				var implements = class_.Implementing.Any()
					? class_.Implementing.Select(impl =>
					{
						var local = FindOrThrow(impl.Value, ModuleName!, class_.Line);
						var i = local.Kind as AuraInterface ??
								throw new CannotImplementNonInterfaceException(impl.Value, class_.Line);
						return i;
					})
					: new List<AuraInterface>();

				// Store the partially typed class as the current enclosing class
				var partiallyTypedClass = new PartiallyTypedClass(
					class_.Name,
					class_.Params,
					methodSignatures,
					class_.Public,
					new AuraClass(class_.Name.Value, typedParams.ToList(), methodTypes, implements.ToList(), class_.Public),
					class_.Line);
				_enclosingClassStore.Push(partiallyTypedClass);
				// Finish type checking the class's methods
				var typedMethods = class_.Methods
					.Select(m => (TypedNamedFunction)Visit(m))
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
			},
			node: class_,
			symbolNamespace: ModuleName!
		);
	}

	/// <summary>
	/// Type checks a while loop
	/// </summary>
	/// <param name="while_">The while loop to be type checked</param>
	/// <returns>A valid, type checked while loop</returns>
	public ITypedAuraStatement Visit(UntypedWhile while_)
	{
		return WithEnclosingStmt(
			f: () =>
			{
				return InNewScope(() =>
				{
					var typedCond = ExpressionAndConfirm(while_.Condition, new AuraBool());
					var typedBody = NonReturnableBody(while_.Body);
					return new TypedWhile(typedCond, typedBody, while_.Line);
				});
			},
			node: while_,
			symbolNamespace: ModuleName!
		);
	}

	public ITypedAuraStatement Visit(UntypedMultipleImport import_)
	{
		var typedImports = import_.Packages.Select(pkg => (TypedImport)Visit(pkg)).ToList();
		return new TypedMultipleImport(typedImports, import_.Line);
	}

	public ITypedAuraStatement Visit(UntypedImport import_)
	{
		// First, check if the module being imported is built-in
		if (!_stdlib.TryGetModule(import_.Package.Value, out var module))
		{
			var typedAsts = new AuraModuleCompiler($"src/{import_.Package.Value}", ProjectName, this).TypeCheckModule();

			var exportedTypes = typedAsts
				.Select(typedAst =>
				{
					// Extract public methods and classes
					var methods = typedAst.Item2
						.Where(node => node.Typ is AuraNamedFunction)
						.Select(node => (node.Typ as AuraNamedFunction)!);
					var interfaces = typedAst.Item2
						.Where(node => node.Typ is AuraInterface)
						.Select(node => (node.Typ as AuraInterface)!);
					var classes = typedAst.Item2
						.Where(node => node.Typ is AuraClass)
						.Select(node => (node.Typ as AuraClass)!);
					var variables = typedAst.Item2
						.Where(node => node is TypedLet)
						.Select(node => (node as TypedLet)!);
					return (methods, interfaces, classes, variables);
				})
				.Aggregate((a, b) => (a.methods.Concat(b.methods), a.interfaces.Concat(b.interfaces), a.classes.Concat(b.classes),
					a.variables.Concat(b.variables)));
			var importedModule = new AuraModule(
				name: import_.Alias?.Value ?? import_.Package.Value,
				publicFunctions: exportedTypes.methods.ToList(),
				publicInterfaces: exportedTypes.interfaces.ToList(),
				publicClasses: exportedTypes.classes.ToList(),
				publicVariables: exportedTypes.variables.ToDictionary(v => v.Names[0].Value, v => v.Initializer!)
			);
			// Add module to list of local variables
			_symbolsTable.AddModule(importedModule);
		}
		else
		{
			_symbolsTable.AddModule(module!);
		}
		return new TypedImport(import_.Package, import_.Alias, import_.Line);
	}

	/// <summary>
	/// Type checks a comment. This method is basically a no-op, since comments don't have a type, nor do they
	/// contain any typed information.
	/// </summary>
	/// <param name="comment">The comment to type check</param>
	/// <returns>A valid, type checked comment</returns>
	public ITypedAuraStatement Visit(UntypedComment comment) => new TypedComment(comment.Text, comment.Line);

	/// <summary>
	/// Type checks a continue statement. This method is basically a no-op, since continue statements don't
	/// have a type.
	/// </summary>
	/// <param name="continue_">The continue statement to type check</param>
	/// <returns>A valid, type checked continue statement</returns>
	public ITypedAuraStatement Visit(UntypedContinue continue_)
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
	public ITypedAuraStatement Visit(UntypedBreak b)
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
	public ITypedAuraStatement Visit(UntypedYield y)
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
	public ITypedAuraStatement Visit(UntypedInterface i) => new TypedInterface(i.Name, i.Methods, i.Public, i.Line);

	/// <summary>
	/// Type checks an assignment expression
	/// </summary>
	/// <param name="assignment">The assignment expression to type check</param>
	/// <returns>A valid, type checked assignment expression</returns>
	public ITypedAuraExpression Visit(UntypedAssignment assignment)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			// Fetch the variable being assigned to
			var v = FindOrThrow(assignment.Name.Value, ModuleName!, assignment.Line);
			// Ensure that the new value and the variable have the same type
			var typedExpr = ExpressionAndConfirm(assignment.Value, v.Kind);
			return new TypedAssignment(assignment.Name, typedExpr, typedExpr.Typ, assignment.Line);
		}, assignment);
	}

	public ITypedAuraExpression Visit(UntypedPlusPlusIncrement inc)
	{
		var name = Expression(inc.Name);
		// Ensure that expression has type of either int or float
		if (name.Typ is not AuraInt && name.Typ is not AuraFloat) throw new CannotIncrementNonNumberException(inc.Line);

		return new TypedPlusPlusIncrement(name, name.Typ, inc.Line);
	}

	public ITypedAuraExpression Visit(UntypedMinusMinusDecrement dec)
	{
		var name = Expression(dec.Name);
		// Ensure that expression has type of either int or float
		if (name.Typ is not AuraInt && name.Typ is not AuraFloat) throw new CannotDecrementNonNumberException(dec.Line);

		return new TypedMinusMinusDecrement(name, name.Typ, dec.Line);
	}

	/// <summary>
	/// Type checks a binary expression
	/// </summary>
	/// <param name="binary">The binary expression to type check</param>
	/// <returns>A valid, type checked binary expression</returns>
	public ITypedAuraExpression Visit(UntypedBinary binary)
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
	public ITypedAuraExpression Visit(UntypedBlock block)
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
	public ITypedAuraExpression Visit(UntypedCall call)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			string? namespace_ = null;
			if (call.Callee is UntypedGet ug)
			{
				var typedGet = (TypedGet)Visit(ug);
				if (typedGet.Obj.Typ is IImportableModule im)
				{
					var f_ = FindOrThrow(ug.GetName(), im.GetModuleName(), call.Line);
					var funcDeclaration_ = f_.Kind as ICallable;
					// Ensure the function call has the correct number of arguments
					if (funcDeclaration_!.GetParams().Count != call.Arguments.Count + 1) throw new IncorrectNumberOfArgumentsException(call.Arguments.Count, funcDeclaration_.GetParams().Count, call.Line);
					// Type check arguments
					var typedArgs = new List<ITypedAuraExpression>();
					if (funcDeclaration_.GetParams().Count > 0)
					{
						var endIndex = funcDeclaration_.GetParams().Count - 1;
						typedArgs = TypeCheckArguments(call.Arguments.Select(pair => pair.Item2).ToList(), funcDeclaration_.GetParams().GetRange(1, endIndex), call.Line);
					}

					return new TypedCall(typedGet, typedArgs, funcDeclaration_.GetReturnType(), call.Line);
				}
				if (typedGet.Obj.Typ is AuraModule m) namespace_ = m.Name;
			}
			if (IsSymbolInPreludeNamespace(call.GetName())) namespace_ = "prelude";

			var f = FindOrThrow(call.Callee.GetName(), namespace_ ?? ModuleName!, call.Line);
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
	public ITypedAuraExpression Visit(UntypedGet get)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			// TODO check if object is a module (maybe check if a namespace exists matching the object's value?)
			if (get.Obj is UntypedVariable v && _symbolsTable.GetNamespace(v.Name.Value) is not null)
			{
				var n = _symbolsTable.GetNamespace(v.Name.Value);
				if (n is not null)
				{
					var symbolNamespace = v.Name.Value;
					var attribute = FindOrThrow(get.Name.Value, symbolNamespace, get.Line);
					return new TypedGet(
						Obj: new TypedVariable(
							Name: new Tok(
								Typ: TokType.Identifier,
								Value: v.Name.Value,
								Line: get.Line
							),
							Typ: n.ParseAsModule(),
							Line: get.Line
						),
						Name: get.Name,
						Typ: attribute.Kind,
						Line: get.Line
					);
				}
			}

			// Type check object, which must be gettable
			var objExpr = Expression(get.Obj);
			if (objExpr.Typ is not IGettable g) throw new CannotGetFromNonClassException(objExpr.ToString()!, objExpr.Typ, get.GetName(), get.Line);
			// Check if a stdlib package needs to be imported
			if (g is AuraString)
			{
				Visit(new UntypedImport(
					Package: new Tok(
						Typ: TokType.Identifier,
						Value: "aura/strings",
						Line: 1
					),
					Alias: new Tok(
						Typ: TokType.Identifier,
						Value: "strings",
						Line: 1
					),
					Line: 1
				));
			}
			if (g is AuraList)
			{
				Visit(new UntypedImport(
					Package: new Tok(
						Typ: TokType.Identifier,
						Value: "aura/lists",
						Line: 1
					),
					Alias: new Tok(
						Typ: TokType.Identifier,
						Value: "lists",
						Line: 1
					),
					Line: 1
				));
			}
			if (g is AuraError)
			{
				Visit(new UntypedImport(
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
			}
			// Fetch the gettable's attribute
			var attrTyp = g.Get(get.Name.Value) ?? throw new ClassAttributeDoesNotExistException(objExpr.ToString()!, get.GetName(), get.Line);
			return new TypedGet(objExpr, get.Name, attrTyp, get.Line);
		}, get);
	}

	public ITypedAuraExpression Visit(UntypedSet set)
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
	public ITypedAuraExpression Visit(UntypedGetIndex getIndex)
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
	public ITypedAuraExpression Visit(UntypedGetIndexRange getIndexRange)
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
	public ITypedAuraExpression Visit(UntypedGrouping grouping)
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
	public ITypedAuraExpression Visit(UntypedIf if_)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			var typedCond = ExpressionAndConfirm(if_.Condition, new AuraBool());
			var typedThen = (TypedBlock)Visit(if_.Then);
			// Type check else branch
			ITypedAuraExpression? typedElse = null;
			if (if_.Else is not null)
			{
				typedElse = ExpressionAndConfirm(if_.Else, typedThen.Typ);
			}

			return new TypedIf(typedCond, typedThen, typedElse, typedThen.Typ, if_.Line);
		}, if_);
	}

	public ITypedAuraExpression Visit(IntLiteral literal) => literal;

	public ITypedAuraExpression Visit(FloatLiteral literal) => literal;

	public ITypedAuraExpression Visit(StringLiteral literal) => literal;

	public ITypedAuraExpression Visit<U>(ListLiteral<U> literal) where U : IAuraAstNode
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			var items = literal.Value.Select(item => (IUntypedAuraExpression)item);
			var typedItem = Expression(items.First());
			var typedItems = items.Select(item => ExpressionAndConfirm(item, typedItem.Typ)).ToList();
			return new ListLiteral<ITypedAuraExpression>(typedItems, typedItem.Typ, literal.Line);
		}, literal);
	}

	public ITypedAuraExpression Visit<TK, TV>(MapLiteral<TK, TV> literal)
		where TK : IAuraAstNode
		where TV : IAuraAstNode
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			var m = literal.Value.Select(pair => ((IUntypedAuraExpression)pair.Key, (IUntypedAuraExpression)pair.Value)).ToDictionary(pair => pair.Item1, pair => pair.Item2);
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

	public ITypedAuraExpression Visit(BoolLiteral literal) => literal;

	public ITypedAuraExpression Visit(UntypedNil literal) => new TypedNil(literal.Line);

	public ITypedAuraExpression Visit(CharLiteral literal) => literal;

	/// <summary>
	/// Type checks a `this` expression
	/// </summary>
	/// <param name="this_">The `this` expression to type check</param>
	/// <returns>A valid, type checked `this` expression</returns>
	public ITypedAuraExpression Visit(UntypedThis this_) => new TypedThis(this_.Keyword, _enclosingClassStore.Peek()!.Typ, this_.Line);

	/// <summary>
	/// Type checks a unary expression
	/// </summary>
	/// <param name="unary">The unary expression to type check</param>
	/// <returns>A valid, type checked unary expression</returns>
	/// <exception cref="MismatchedUnaryOperatorAndOperandException">Thrown if the unary expression's operator and
	/// operand are not compatible</exception>
	public ITypedAuraExpression Visit(UntypedUnary unary)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			var typedRight = Expression(unary.Right);
			// Ensure that operand is a valid type and the operand can be used with it
			if (unary.Operator.Typ is TokType.Minus)
			{
				if (typedRight.Typ is not AuraInt && typedRight.Typ is not AuraFloat)
					throw new MismatchedUnaryOperatorAndOperandException(unary.Operator.Value, typedRight.Typ, unary.Line);
			}
			else if (unary.Operator.Typ is TokType.Minus)
			{
				if (typedRight.Typ is not AuraBool)
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
	public ITypedAuraExpression Visit(UntypedVariable v)
	{
		var namespace_ = IsSymbolInPreludeNamespace(v.Name.Value)
			? "prelude"
			: ModuleName!;
		var localVar = FindOrThrow(v.Name.Value, namespace_, v.Line);
		var kind = !localVar.Kind.IsSameType(new AuraUnknown(string.Empty))
			? localVar.Kind
			: FindOrThrow(((AuraUnknown)localVar.Kind).Name, ModuleName!, v.Line).Kind;
		return new TypedVariable(v.Name, kind, v.Line);
	}

	public ITypedAuraExpression Visit(UntypedIs is_)
	{
		var typedExpr = Expression(is_.Expr);
		// Ensure the expected type is an interface
		var i = FindAndConfirm(is_.Expected.Value, ModuleName!,
			new AuraInterface(string.Empty, new List<AuraNamedFunction>(), Visibility.Private), is_.Line);

		return new TypedIs(typedExpr, i, is_.Line);
	}

	/// <summary>
	/// Type checks a logical expression
	/// </summary>
	/// <param name="logical">The logical expression to type check</param>
	/// <returns>A valid, type checked logical expression</returns>
	public ITypedAuraExpression Visit(UntypedLogical logical)
	{
		return _enclosingExpressionStore.WithEnclosing(() =>
		{
			var typedLeft = Expression(logical.Left);
			var typedRight = ExpressionAndConfirm(logical.Right, typedLeft.Typ);
			return new TypedLogical(typedLeft, logical.Operator, typedRight, new AuraBool(), logical.Line);
		}, logical);
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


	private AuraType TypeTokenToType(Tok tok)
	{
		return tok.Typ switch
		{
			TokType.Int => new AuraInt(),
			TokType.Float => new AuraFloat(),
			TokType.String => new AuraString(),
			TokType.Bool => new AuraBool(),
			TokType.Any => new AuraAny(),
			TokType.Char => new AuraChar(),
			TokType.Error => new AuraError(),
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
			var paramTyp = p.ParamType.Typ is not AuraUnknown u
				? p.ParamType.Typ
				: FindOrThrow(u.Name, ModuleName!, p.Name.Line).Kind;
			return new Param(p.Name, new ParamType(paramTyp, p.ParamType.Variadic, typedDefaultValue));
		}).ToList();
	}

	private List<ParamType> TypeCheckParamTypes(List<Param> untypedParams)
	{
		return TypeCheckParams(untypedParams)
			.Select(p => p.ParamType)
			.ToList();
	}

	private List<ITypedAuraExpression> TypeCheckArguments(List<IUntypedAuraExpression> arguments, List<Param> parameters, int line)
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
		if (declaration.HasVariadicParam() && call.Arguments.Count < declaration.GetParams().Count)
			throw new IncorrectNumberOfArgumentsException(call.Arguments.Count, declaration.GetParams().Count, call.Line);
		if (!declaration.HasVariadicParam() && declaration.GetParamTypes().Count != call.Arguments.Count)
			throw new IncorrectNumberOfArgumentsException(call.Arguments.Count, declaration.GetParams().Count, call.Line);
		// The arguments are already in order when using positional arguments, so just extract the arguments
		var orderedArgs = call.Arguments
			.Select(pair => pair.Item2)
			.ToList();

		var typedArgs = TypeCheckArguments(orderedArgs, declaration.GetParams(), call.Line);

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
			if (index >= orderedArgs.Count) typedArgs.Add(defaultValue);
			else typedArgs.Insert(index, defaultValue);
		}

		return new TypedCall(typedCallee!, typedArgs, declaration.GetReturnType(), call.Line);
	}

	private AuraType TypeCheckReturnTypeTok(Tok? returnTok) =>
		returnTok is not null ? TypeTokenToType(returnTok.Value) : new AuraNil();

	private TU WithEnclosingStmt<TU, T>(Func<TU> f, T node, string symbolNamespace)
		where TU : ITypedAuraStatement
		where T : IUntypedAuraStatement
	{
		_symbolsTable.AddScope(symbolNamespace);
		var typedNode = _enclosingStatementStore.WithEnclosing(f, node);
		_symbolsTable.ExitScope(symbolNamespace);
		return typedNode;
	}

	private bool IsSymbolInPreludeNamespace(string symbolName)
	{
		var typ = _prelude.GetPrelude().Get(symbolName);
		if (typ is null) return false;
		else return true;
	}

	public ITypedAuraStatement Visit(UntypedCheck check)
	{
		var typedCall = Visit(check.Call);
		// The `check` keyword is only valid when the enclosing function and the checked function call both have a return
		// type of `error`
		var enclosingFuncDeclaration = _enclosingFunctionDeclarationStore.Peek() ?? throw new InvalidUseOfCheckKeywordException(check.Line);
		if (!enclosingFuncDeclaration.ReturnType?.Select(rt => rt.Value).Contains("error") ?? false) throw new InvalidUseOfCheckKeywordException(check.Line);
		if (typedCall.Typ is not AuraError) throw new InvalidUseOfCheckKeywordException(check.Line);

		return new TypedCheck(
			Call: (TypedCall)typedCall,
			Line: check.Line
		);
	}

	public ITypedAuraStatement Visit(UntypedStruct @struct)
	{
		return WithEnclosingStmt(
			f: () =>
			{
				var typedParams = @struct.Params.Select(p =>
				{
					var typedDefaultValue = p.ParamType.DefaultValue is not null
						? (ILiteral)Expression(p.ParamType.DefaultValue)
						: null;
					var paramTyp = p.ParamType.Typ;
					return new Param(p.Name, new ParamType(paramTyp, p.ParamType.Variadic, typedDefaultValue));
				});

				return new TypedStruct(@struct.Name, typedParams.ToList(), @struct.Line);
			},
			node: @struct,
			symbolNamespace: ModuleName!
		);
	}

	public ITypedAuraExpression Visit(UntypedAnonymousStruct anonymousStruct)
	{
		throw new NotImplementedException();
	}
}
