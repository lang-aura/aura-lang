using AuraLang.AST;
using AuraLang.Exceptions.TypeChecker;
using AuraLang.Shared;
using AuraLang.Stdlib;
using AuraLang.Token;
using AuraLang.Types;
using AuraChar = AuraLang.Types.Char;
using AuraFunction = AuraLang.Types.Function;
using AuraString = AuraLang.Types.String;

namespace AuraLang.TypeChecker;

public class AuraTypeChecker
{
    private readonly IVariableStore _variableStore;
    private int _scope = 1;
    private readonly IEnclosingClassStore _enclosingClassStore;
    private readonly AuraStdlib _stdlib = new();
    private readonly ICurrentModuleStore _currentModule;
    private readonly TypeCheckerExceptionContainer _exContainer = new();
    private readonly EnclosingNodeStore<UntypedAuraExpression> _enclosingExpressionStore;
    private readonly EnclosingNodeStore<UntypedAuraStatement> _enclosingStatementStore;

    public AuraTypeChecker(IVariableStore variableStore, IEnclosingClassStore enclosingClassStore, ICurrentModuleStore currentModuleStore, EnclosingNodeStore<UntypedAuraExpression> enclosingExpressionStore, EnclosingNodeStore<UntypedAuraStatement> enclosingStatementStore)
    {
        _variableStore = variableStore;
        _enclosingClassStore = enclosingClassStore;
        _currentModule = currentModuleStore;
        _enclosingExpressionStore = enclosingExpressionStore;
        _enclosingStatementStore = enclosingStatementStore;
    }

    public List<TypedAuraStatement> CheckTypes(List<UntypedAuraStatement> untypedAst)
    {
        var typedAst = new List<TypedAuraStatement>();

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

    private TypedAuraStatement Statement(UntypedAuraStatement stmt)
    {
        return stmt switch
        {
            UntypedDefer defer => DeferStmt(defer),
            UntypedExpressionStmt expressionStmt => ExpressionStmt(expressionStmt),
            UntypedFor for_ => ForStmt(for_),
            UntypedForEach foreach_ => ForEachStmt(foreach_),
            UntypedNamedFunction f => PartialFunctionStmt(f),
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
            _ => throw new UnknownStatementTypeException(stmt.Line)
        };
    }

    private TypedAuraExpression Expression(UntypedAuraExpression expr)
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
            UntypedIntLiteral i => IntLiteralExpr(i),
            UntypedFloatLiteral f => FloatLiteralExpr(f),
            UntypedStringLiteral s => StringLiteralExpr(s),
            UntypedListLiteral<UntypedAuraExpression> l => ListLiteralExpr(l),
            UntypedMapLiteral m => MapLiteralExpr(m),
            UntypedBoolLiteral b => BoolLiteralExpr(b),
            UntypedNil n => NilExpr(n),
            UntypedCharLiteral c => CharLiteralExpr(c),
            UntypedLogical logical => LogicalExpr(logical),
            UntypedSet set => SetExpr(set),
            UntypedThis this_ => ThisExpr(this_),
            UntypedUnary unary => UnaryExpr(unary),
            UntypedVariable variable => VariableExpr(variable),
            UntypedAnonymousFunction f => AnonymousFunctionExpr(f),
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
    private TypedAuraExpression ExpressionAndConfirm(UntypedAuraExpression expr, AuraType expected)
    {
        var typedExpr = Expression(expr);
        if (!expected.IsSameOrInheritingType(typedExpr.Typ)) throw new UnexpectedTypeException(expr.Line);
        return typedExpr;
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
                var typedBody = NonReturnableBody(forStmt.Body);
                return new TypedFor(typedInit, typedCond, typedBody, forStmt.Line);
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
                _variableStore.Add(new Local(forEachStmt.EachName.Value, typedIter.GetIterType(), _scope, _currentModule.GetName()));
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
                        modName));
                }

                var typedBody = BlockExpr(f.Body);
                // Ensure the function's body returns the type specified in its signature
                var returnType = TypeCheckReturnTypeTok(f.ReturnType);
                if (!returnType.IsSameOrInheritingType(typedBody.Typ)) throw new TypeMismatchException(f.Line);
                // Add function as local variable
                _variableStore.Add(new Local(
                    f.Name.Value,
                    new AuraFunction(
                        f.Name.Value,
                        new AnonymousFunction(
                            TypeCheckParams(f.Params),
                            returnType)
                        ),
                    _scope,
                    modName));
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
                foreach(var param in typedParams)
                {
                    _variableStore.Add(new Local(
                        param.Name.Value,
                        param.ParamType.Typ,
                        _scope,
                        _currentModule.GetName()!));
                }

                var typedBody = BlockExpr(f.Body);
                // Ensure the function's body returns the type specified in its signature
                var returnType = TypeCheckReturnTypeTok(f.ReturnType);
                if (!returnType.IsSameOrInheritingType(typedBody.Typ)) throw new TypeMismatchException(f.Line);

                return new TypedAnonymousFunction(typedParams, typedBody, returnType, f.Line);
            });
        }, f);
    }

    private PartiallyTypedFunction PartialFunctionStmt(UntypedNamedFunction f)
    {
        var typedParams = TypeCheckParams(f.Params);
        var returnType = TypeCheckReturnTypeTok(f.ReturnType);
        // Add function as local
        _variableStore.Add(new Local(
            f.Name.Value,
            new AuraFunction(f.Name.Value, new AnonymousFunction(typedParams, returnType)),
            _scope,
            _currentModule.GetName()!));

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
        foreach(var param in f.Params)
        {
            var paramTyp = TypeTokenToType(param.ParamType.Typ);
            if (param.ParamType.Variadic) paramTyp = new List(paramTyp);
            _variableStore.Add(new Local(
                param.Name.Value,
                paramTyp,
                _scope + 1,
                _currentModule.GetName()!));
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
            var nameTyp = TypeTokenToType(let.NameTyp.Value);
            // Type check initializer
            var typedInit = let.Initializer is not null ? ExpressionAndConfirm(let.Initializer, nameTyp) : null;
            // Add new variable to list of locals
            _variableStore.Add(new Local(
                let.Name.Value,
                typedInit?.Typ ?? new Nil(),
                _scope,
                _currentModule.GetName()!));

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
                _currentModule.GetName()!));

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
        _currentModule.Set(m);
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
                    ? Expression(p.ParamType.DefaultValue)
                    : null;
                var paramTyp = TypeTokenToType(p.ParamType.Typ);
                return new TypedParam(p.Name, new TypedParamType(paramTyp, p.ParamType.Variadic, typedDefaultValue));
            });
            
            var partiallyTypedMethods = class_.Methods.Select(PartialFunctionStmt).ToList();
            var methodTypes = partiallyTypedMethods
                .Select(method =>
                {
                    var typedMethodParams = method.Params.Select(p =>
                    {
                        var typedMethodDefaultValue = p.ParamType.DefaultValue is not null
                            ? Expression(p.ParamType.DefaultValue)
                            : null;
                        var methodParamType = TypeTokenToType(p.ParamType.Typ);
                        return new TypedParam(p.Name, new TypedParamType(methodParamType, p.ParamType.Variadic, typedMethodDefaultValue));
                    });
                    return new AuraFunction(method.Name.Value,
                        new AnonymousFunction(typedMethodParams.ToList(), method.ReturnType));
                })
                .ToList();
            var paramNames = class_.Params.Select(p => p.Name.Value).ToList();

            // Add typed class to list of locals
            _variableStore.Add(new Local(
                class_.Name.Value,
                new Class(class_.Name.Value, paramNames, typedParams.Select(p => p.ParamType).ToList(), methodTypes),
                _scope,
                _currentModule.GetName()!));

            // Store the partially typed class as the current enclosing class
            var partiallyTypedClass = new PartiallyTypedClass(
                class_.Name,
                class_.Params,
                partiallyTypedMethods,
                class_.Public,
                new Class(class_.Name.Value, new List<string>(), typedParams.Select(p => p.ParamType).ToList(), methodTypes),
                class_.Line);
            _enclosingClassStore.Push(partiallyTypedClass);
            // Finish type checking the class's methods
            var typedMethods = partiallyTypedClass.Methods
                .Select(FinishFunctionStmt)
                .ToList();
            _enclosingClassStore.Pop();
            return new FullyTypedClass(class_.Name, typedParams.ToList(), typedMethods, class_.Public, class_.Line);
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
            // TODO Read file at import path and type check it
            // TODO Add module to list of local variables
            // TODO Add local module's public functions to current scope
            return new TypedImport(import_.Package, import_.Alias, import_.Line);
        }
        else
        {
            // Add module to list of local variables
            _variableStore.Add(new Local(
                "io",
                module,
                _scope,
                _currentModule.GetName()!));
            // Add local module's public functions to current scope
            foreach (var f in module.PublicFunctions)
            {
                _variableStore.Add(new Local(
                    f.Name,
                    f,
                    _scope,
                    module.Name));
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
            throw new InvalidUseOfBreakKeywordException(continue_.Line);
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
    /// Type checks an assignment expression
    /// </summary>
    /// <param name="assignment">The assignment expression to type check</param>
    /// <returns>A valid, type checked assignment expression</returns>
    private TypedAssignment AssignmentExpr(UntypedAssignment assignment)
    {
        return _enclosingExpressionStore.WithEnclosing(() =>
        {
            // Fetch the variable being assigned to
            var v = _variableStore.Find(assignment.Name.Value, _currentModule.GetName()!);
            // Ensure that the new value and the variable have the same type
            var typedExpr = ExpressionAndConfirm(assignment.Value, v!.Value.Kind);
            return new TypedAssignment(assignment.Name, typedExpr, typedExpr.Typ, assignment.Line);
        }, assignment);
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
                var typedStmts = block.Statements.Select(Statement);
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
            var typedCallee = Expression((UntypedAuraExpression)call.Callee) as ITypedAuraCallable;
            var funcDeclaration = _variableStore.Find(call.Callee.GetName(), call.Callee.GetModuleName() ?? _currentModule.GetName()!)!.Value.Kind as ICallable;
            // Ensure the function call has the correct number of arguments
            if (funcDeclaration!.GetParamTypes().Count != call.Arguments.Count) throw new IncorrectNumberOfArgumentsException(call.Line);
            // Type check arguments
            var named = call.Arguments.All(arg => arg.Item1 is not null);
            var unnamed = call.Arguments.All(arg => arg.Item1 is null);
            if (!named && !unnamed) throw new CannotMixNamedAndUnnamedArgumentsException(call.Line);

            var orderedArgs = call.Arguments
                .Select(pair => pair.Item2)
                .ToList();
            if (named)
            {
                orderedArgs = call.Arguments
                    .Where(pair => pair.Item1 is null)
                    .Select(pair => pair.Item2)
                    .ToList();
                foreach (var arg in call.Arguments)
                {
                    if (arg.Item1 is not null)
                    {
                        var index = funcDeclaration.GetParamIndex(arg.Item1!.Value.Value);
                        if (index >= orderedArgs.Count) orderedArgs.Add(arg.Item2);
                        else orderedArgs.Insert(index, arg.Item2);
                    }
                }
            }
            
            var typedArgs = orderedArgs
                .Zip(funcDeclaration.GetParamTypes())
                .Select(pair => ExpressionAndConfirm(pair.First, pair.Second.Typ))
                .ToList();
            
            return new TypedCall(typedCallee!, typedArgs, funcDeclaration.GetReturnType(), call.Line);
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
            if (objExpr.Typ is not IGettable g) throw new CannotGetFromNonClassException(get.Line);
            // Fetch the gettable's attribute
            var attrTyp = g.Get(get.Name.Value);
            if (attrTyp is null) throw new ClassAttributeDoesNotExistException(get.Line);
            
            return new TypedGet(objExpr, get.Name, attrTyp, get.Line);
        }, get);
    }

    private TypedSet SetExpr(UntypedSet set)
    {
        return _enclosingExpressionStore.WithEnclosing(() =>
        {
            var typedObj = Expression(set.Obj);
            // TODO Make sure the typed object is a class
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
            if (!indexableExpr.IndexingType().IsSameType(indexExpr.Typ)) throw new TypeMismatchException(getIndex.Line); 

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
            if (expr.Typ is not IRangeIndexable rangeIndexableExpr) throw new ExpectRangeIndexableException(getIndexRange.Line);
            if (!rangeIndexableExpr.IndexingType().IsSameType(lower.Typ)) throw new TypeMismatchException(getIndexRange.Line);
            if (!rangeIndexableExpr.IndexingType().IsSameType(upper.Typ)) throw new TypeMismatchException(getIndexRange.Line);

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
            TypedAuraExpression? typedElse = null;
            if (if_.Else is not null)
            {
                typedElse = ExpressionAndConfirm(if_.Else, typedThen.Typ);
            }
            return new TypedIf(typedCond, typedThen, typedElse, typedThen.Typ, if_.Line);
        }, if_);
    }

    private TypedLiteral<long> IntLiteralExpr(UntypedIntLiteral literal) => new(literal.GetValue(), new Int(), literal.Line);

    private TypedLiteral<double> FloatLiteralExpr(UntypedFloatLiteral literal) => new(literal.GetValue(), new Float(), literal.Line);

    private TypedLiteral<string> StringLiteralExpr(UntypedStringLiteral literal) => new(literal.GetValue(), new AuraString(), literal.Line);

    private TypedLiteral<List<TypedAuraExpression>> ListLiteralExpr(UntypedListLiteral<UntypedAuraExpression> literal)
    {
        return _enclosingExpressionStore.WithEnclosing(() =>
        {
            var items = literal.GetValue();
            var typedItem = Expression(items.First());
            var typedItems = items.Select(item => ExpressionAndConfirm(item, typedItem.Typ)).ToList();
            return new TypedLiteral<List<TypedAuraExpression>>(typedItems, new List(typedItem.Typ), literal.Line);
        }, literal);
    }

    private TypedLiteral<Dictionary<TypedAuraExpression, TypedAuraExpression>> MapLiteralExpr(UntypedMapLiteral literal)
    {
        return _enclosingExpressionStore.WithEnclosing(() =>
        {
            var m = literal.GetValue();
            var typedKey = Expression(m.Keys.First());
            var typedValue = Expression(m.Values.First());
            var typedM = m.Select(pair =>
            {
                var typedK = ExpressionAndConfirm(pair.Key, typedKey.Typ);
                var typedV = ExpressionAndConfirm(pair.Value, typedValue.Typ);
                return (typedK, typedV);
            }).ToDictionary(pair => pair.typedK, pair => pair.typedV);
            return new TypedLiteral<Dictionary<TypedAuraExpression, TypedAuraExpression>>(typedM, new Map(typedKey.Typ, typedValue.Typ), literal.Line);
        }, literal);
    }

    private TypedLiteral<bool> BoolLiteralExpr(UntypedBoolLiteral literal) => new(literal.GetValue(), new Bool(), literal.Line);

    private TypedNil NilExpr(UntypedNil literal) => new(literal.Line);

    private TypedLiteral<char> CharLiteralExpr(UntypedCharLiteral literal) => new(literal.GetValue(), new AuraChar(), literal.Line);

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
                if (typedRight.Typ is not Int && typedRight.Typ is not Float) throw new MismatchedUnaryOperatorAndOperandException(unary.Line);
            }
            else if (unary.Operator.Typ is TokType.Minus)
            {
                if (typedRight.Typ is not Bool) throw new MismatchedUnaryOperatorAndOperandException(unary.Line);
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
        var localVar = _variableStore.Find(v.Name.Value, _currentModule.GetName()!);
        return new TypedVariable(v.Name, localVar!.Value.Kind, v.Line);
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

    private List<TypedAuraStatement> NonReturnableBody(List<UntypedAuraStatement> body)
    {
        var typedBody = new List<TypedAuraStatement>();
        foreach (var stmt in body)
        {
            var typedStmt = Statement(stmt);
            typedBody.Add(typedStmt);
        }
        return typedBody;
    }

    private T InNewScope<T>(Func<T> f) where T : TypedAuraAstNode
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

    private List<TypedParam> TypeCheckParams(List<UntypedParam> untypedParams)
    {
        return untypedParams.Select(p =>
        {
            var typedDefaultValue = p.ParamType.DefaultValue is not null
                ? Expression(p.ParamType.DefaultValue)
                : null;
            var paramTyp = TypeTokenToType(p.ParamType.Typ);
            return new TypedParam(p.Name, new TypedParamType(paramTyp, p.ParamType.Variadic, typedDefaultValue));
        }).ToList();
    }

    private List<TypedParamType> TypeCheckParamTypes(List<UntypedParam> untypedParams)
    {
        return TypeCheckParams(untypedParams)
            .Select(p => p.ParamType)
            .ToList();
    }

    private AuraType TypeCheckReturnTypeTok(Tok? returnTok) => returnTok is not null ? TypeTokenToType(returnTok.Value) : new Nil();
}
