﻿using System.Globalization;
using System.Text;
using AuraLang.AST;
using AuraLang.Exceptions.Compiler;
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
    private readonly List<TypedAuraStatement> _typedAst;
    /// <summary>
    /// Aura allows implicit returns in certain situations, and the behavior of the return statement differs depending on the situaiton and whether its implicit
    /// or explicit. Because of that, the compiler keeps track of any enclosing types, which it refers to when compiling a return statement. The enclosing types
    /// that the compiler is interested in are `if` expressions, blocks, functions, and classes.
    /// </summary>
    private readonly Stack<TypedAuraAstNode> _enclosingType = new();
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
    /// Tracks the current line in the output file
    /// </summary>
    private int _line;
    /// <summary>
    /// Contains all exceptions thrown during the compilation process. 
    /// </summary>
    private readonly CompilerExceptionContainer _exContainer = new();
    private string ProjectName { get; }

	public AuraCompiler(List<TypedAuraStatement> typedAst, string projectName)
	{
        _typedAst = typedAst;
        _line = 1;
        ProjectName = projectName;
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

    private string Statement(TypedAuraStatement stmt)
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
            TypedComment c => CommentStmt(c),
            TypedContinue c => ContinueStmt(c),
            TypedBreak b => BreakStmt(b),
            _ => throw new UnknownStatementException(stmt.Line)
        };
    }

    private string Expression(TypedAuraExpression expr)
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
            TypedLiteral<string> l => StringLiteralExpr(l),
            TypedLiteral<char> c => CharLiteralExpr(c),
            TypedLiteral<long> i => IntLiteralExpr(i),
            TypedLiteral<double> f => FloatLiteralExpr(f),
            TypedLiteral<bool> b => BoolLiteralExpr(b),
            TypedLiteral<List<TypedAuraExpression>> l => ListLiteralExpr(l),
            TypedNil n => NilLiteralExpr(n),
            TypedLiteral<Dictionary<TypedAuraExpression, TypedAuraExpression>> d => MapLiteralExpr(d),
            TypedLogical l => LogicalExpr(l),
            TypedSet s => SetExpr(s),
            TypedThis t => ThisExpr(t),
            TypedUnary u => UnaryExpr(u),
            TypedVariable v => VariableExpr(v),
            TypedAnonymousFunction f => AnonymousFunctionExpr(f),
            _ => throw new UnknownExpressionException(expr.Line)
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
            var init = Statement(for_.Initializer);
            var cond = Expression(for_.Condition);
            var body = CompileLoopBody(for_.Body);
            // The compiler will always compile an Aura `for` loop to a Go `for` loop without the increment part of the `for` loop's signature. The increment is instead
            // added to the end of the loop's body. The loop's execution will remain the same, so it doesn't seem worth it to extract it from the body and put it back
            // into the loop's signature.
            return body != string.Empty ? $"for {init}; {cond}; {{{body}\n}}" : $"for {init}; {cond}; {{}}";
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
                var else_ = if_.Else is TypedIf ? IfExpr(if_.Else as TypedIf) : ParseReturnableBody((if_.Else as TypedBlock).Statements, varName);
                return $"{decl_}\nif {init} {{{then}\n}} else {{{else_}\n}}";
            default:
                var value = Expression(let.Initializer);
                // We check to see if we are inside a `for` loop because Aura and Go differ in whether a a long variable initialization is allowed
                // as the initializer of a `for` loop. Go only allows the short syntax (i.e. `x := 0`), whereas Aura allows both styles of variable
                // declaration. Therefore, if the user has entered the full `let`-style syntax (i.e. `let x: int = 0`) inside the signature of a `for`
                // loop, it must be compiled to the short syntax in the final Go file.
                if (let.TypeAnnotation)
                {
                    var b = _enclosingType.TryPeek(out var for_);
                    if (!b || for_ is not TypedFor)
                    {
                        return $"var {let.Name.Value} {AuraTypeToGoType(let.Initializer.Typ)} = {value}";
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

    private string ImportStmt(TypedImport i)
    {
        if (IsStdlibImportName(i.Package.Value))
        {
            var name = ExtractStdlibPkgName(i.Package.Value);
            return BuildStdlibPkgImportStmt(name);
        }

        return i.Alias is null
            ? $"import \"{i.Package.Value}\""
            : $"import {i.Alias.Value.Value} \"{i.Package.Value}\"";
    }

    private string CommentStmt(TypedComment com)
    {
        return com.Text.Value;
    }

    private string ContinueStmt(TypedContinue con)
    {
        return "continue";
    }

    private string BreakStmt(TypedBreak b)
    {
        return "break";
    }

    private string AssignmentExpr(TypedAssignment assign)
    {
        var value = Expression(assign.Value);
        return $"{assign.Name.Value} = {value}";
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
        var callee = Expression((TypedAuraExpression)c.Callee);
        var compiledParams = c.Arguments.Select(Expression);
        return $"{callee}({string.Join(", ", compiledParams)})";
    }

    private string CallExpr_GetCallee(TypedCall c)
    {
        var get = c.Callee as TypedGet;
        var obj = Expression(get!.Obj);

        var callee = IsStdlibPkg(obj)
            ? $"{obj}.{ConvertSnakeCaseToCamelCase(get.Name.Value)}"
            : $"{obj}.{get.Name.Value}";
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
        return $"if {cond} {then}{else_}";
    }

    private string StringLiteralExpr(TypedLiteral<string> literal) => $"\"{literal.Value}\"";

    private string CharLiteralExpr(TypedLiteral<char> literal) => $"'{literal.Value}'";

    private string IntLiteralExpr(TypedLiteral<long> literal) => $"{literal.Value}";

    private string FloatLiteralExpr(TypedLiteral<double> literal) => string.Format(CultureInfo.InvariantCulture, "{0:0.##}", literal.Value);
    
    private string BoolLiteralExpr(TypedLiteral<bool> literal) => literal.Value ? "true" : "false";

    private string ListLiteralExpr(TypedLiteral<List<TypedAuraExpression>> literal)
    {
        var items = literal.Value
            .Select(Expression);
        return $"{AuraTypeToGoType(literal.Typ)}{{{string.Join(", ", items)}}}";
    }

    private string NilLiteralExpr(TypedNil nil) => "nil";

    private string MapLiteralExpr(TypedLiteral<Dictionary<TypedAuraExpression, TypedAuraExpression>> literal)
    {
        var items = literal.Value.Select(pair =>
        {
            var keyExpr = Expression(pair.Key);
            var valueExpr = Expression(pair.Value);
            return $"{keyExpr}: {valueExpr}";
        });
        return items.Any() ? $"{AuraTypeToGoType(literal.Typ)}{{\n{string.Join(", ", items)}\n}}" : $"{AuraTypeToGoType(literal.Typ)}{{}}";
    }

    private string TupleLiteralExpr(TypedLiteral<List<TypedAuraExpression>> literal)
    {
        var tupleItems = ((List<TypedAuraExpression>)literal.Value)
            .Select(Expression)
            .Aggregate(string.Empty, (prev, curr) => $"{prev},{curr}")
            .ToList();
        return $"{AuraTypeToGoType(literal.Typ)}{{{tupleItems}}}";
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

    private string VariableExpr(TypedVariable v) => v.Name.Value;

    private string YieldStmt(TypedYield y, string decl) => $"{decl} = {y.Value}";

    private string AuraTypeToGoType(AuraType typ) => typ.ToString();

    private string CompileLoopBody(List<TypedAuraStatement> body)
    {
        return body.Any()
            ? body
               .Select(Statement)
               .Aggregate(string.Empty, (prev, curr) => $"{prev}\n{curr}")
            : string.Empty;
    }

    private string CompileParams(List<TypedParam> params_, string sep)
    {
        return params_
            .Select(p => $"{p.Name.Value} {AuraTypeToGoType(p.ParamType.Typ)}")
            .Aggregate(string.Empty, (prev, curr) => $"{prev}{sep}{curr}");
    }

    private string CompileArgs(List<TypedAuraExpression> args)
    {
        return args
            .Select(Expression)
            .Aggregate(string.Empty, (prev, curr) => $"{prev}, {curr}");
    }

    private string ParseReturnableBody(List<TypedAuraStatement> stmts, string decl)
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

    private bool IsStdlibPkg(string pkg)
    {
        return pkg switch
        {
            "io" or "strings" or "lists" => true,
            _ => false
        };
    }

    private bool IsStdlibImportName(string pkg)
    {
        return pkg switch
        {
            "aura/io" or "aura/strings" or "aura/lists" => true,
            _ => false
        };
    }

    private string ExtractStdlibPkgName(string pkg) => pkg.Split('/').Last();

    private string BuildStdlibPkgImportStmt(string pkg) => $"import {pkg} \"{ProjectName}/stdlib/{pkg}\"";

    private string LogicalOperatorToGoOperator(Tok op)
    {
        return op.Typ switch
        {
            TokType.And => "&&",
            TokType.Or => "||",
            _ => op.Value
        };
    }

    private string InNewEnclosingType(Func<string> f, TypedAuraAstNode node)
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
}
