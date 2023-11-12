using System.Text;
using AuraLang.AST;
using AuraLang.Exceptions.Compiler;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using Char = AuraLang.Types.Char;
using String = AuraLang.Types.String;
using Tuple = AuraLang.Types.Tuple;

namespace AuraLang.Compiler;

public class AuraCompiler
{
    /// <summary>
    /// The typed Aura AST that will be compiled to Go
    /// </summary>
    private List<TypedAuraStatement> _typedAst;
    /// <summary>
    /// Aura allows implicit returns in certain situations, and the behavior of the return statement differs depending on the situaiton and whether its implicit
    /// or explicit. Because of that, the compiler keeps track of any enclosing types, which it refers to when compiling a return statement. The enclosing types
    /// that the compiler is interested in are `if` expressions, blocks, functions, and classes.
    /// </summary>
    private Stack<TypedAuraAstNode> _enclosingType = new();
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
    private GoDocument _goDocument = new();
    /// <summary>
    /// Tracks the current line in the output file
    /// </summary>
    private int _line;

	public AuraCompiler(List<TypedAuraStatement> typedAst)
	{
        _typedAst = typedAst;
        _line = 1;
	}

    public string Compile()
    {
        foreach (var node in _typedAst)
        {
            var s = Statement(node);
            _goDocument.WriteStmt(s, node.Line, node);
        }

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
            TypedLiteral<object> l => LiteralExpr(l),
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
            return $"for {init}; {cond}; {{{body}\n}}";
        }, for_);
    }

    private string ForEachStmt(TypedForEach foreach_)
    {
        return InNewEnclosingType(() =>
        {
            var iter = Expression(foreach_.Iterable);
            var body = CompileLoopBody(foreach_.Body);
            return $"for _, {foreach_.EachName.Value} := range {iter} {{{body}\n}}";
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
                var decl_ = $"var {varName} {AuraTypeToGoType(let.Typ)}";
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
                if (let.TypeAnnotation && _enclosingType.Peek() is TypedFor)
                {
                    return $"var {let.Name.Value} {AuraTypeToGoType(let.Typ)} = {value}";
                }
                else
                {
                    return $"{let.Name.Value} := {value}";
                }
        }
    }

    private string ModStmt(TypedMod mod)
    {
        return $"package {mod.Value.Value}";
    }

    private string ReturnStmt(TypedReturn r)
    {
        var value = Expression(r.Value);
        value = value == "nil" ? string.Empty : $" {value}";
        if (value == "nil") value = string.Empty;
        return $"return{value}";
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

            return $"type {className} struct {{\n{compiledParams}\n}}\n\n{compiledMethods}";
        }, c);
    }

    private string WhileStmt(TypedWhile w)
    {
        var cond = Expression(w.Condition);
        var body = CompileLoopBody(w.Body);
        return $"for {cond} {{{body}\n}}";
    }

    private string ImportStmt(TypedImport i)
    {
        if (IsStdlibImportName(i.Package.Value))
        {
            //var name = ExtractStdlibPkgName(i.Package.Value);
            //return BuildStdlibPkgImportStmt(name);
            return "";
        }
        else
        {
            return "";
        }
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
        var compiledStmts = new AuraStringBuilder("\n");
        foreach (var stmt in b.Statements)
        {
            var s = Statement(stmt);
            compiledStmts.WriteString(s, stmt.Line, stmt);
        }
        return $"{{{compiledStmts.String()}\n}}";
    }

    private string CallExpr(TypedCall c)
    {
        var callee = Expression((TypedAuraExpression)c.Callee);
        var compiledParams = c.Arguments
            .Select(Expression)
            .Aggregate(string.Empty, (prev, curr) => $"{prev}, {curr}")
            .ToList();
        return $"{callee}({compiledParams})";
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
        var else_ = if_.Else is not null ? $"else {Expression(if_.Else)}" : string.Empty;
        return $"if {cond} {then}{else_}";
    }

    private string LiteralExpr(TypedLiteral<object> literal)
    {
        switch (literal.Typ)
        {
            case String:
                return $"\"{literal.Value}\"";
            case Char:
                return $"'{literal.Value}'";
            case Int:
                return $"{literal.Value}";
            case Float:
                return $"{literal.Value}";
            case Bool:
                return (bool)literal.Value ? "true" : "false";
            case List:
                var items = (literal.Value as List<TypedAuraExpression>)
                    .Select(Expression)
                    .Aggregate(string.Empty, (prev, curr) => $"{prev}, {curr}")
                    .ToList();
                return $"{AuraTypeToGoType(literal.Typ)}{{{items}}}";
            case Nil:
                return "nil";
            case Map:
                var items_ = (literal.Value as Dictionary<TypedAuraExpression, TypedAuraExpression>)
                    .Select(pair =>
                    {
                        var keyExpr = Expression(pair.Key);
                        var valueExpr = Expression(pair.Value);
                        return $"{keyExpr}: {valueExpr}";
                    })
                    .Aggregate(string.Empty, (prev, curr) => $"{prev},\n{curr}")
                    .ToList();
                return $"{AuraTypeToGoType(literal.Typ)}{{\n{items_}}}";
            case Tuple:
                var tupleItems = ((List<TypedAuraExpression>)literal.Value)
                    .Select(Expression)
                    .Aggregate(string.Empty, (prev, curr) => $"{prev},{curr}")
                    .ToList();
                return $"{AuraTypeToGoType(literal.Typ)}{{{tupleItems}}}";
            default:
                return $"{literal.Value}";
        }
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

    private string AuraTypeToGoType(AuraType typ) => typ.ToString();

    private string CompileLoopBody(List<TypedAuraStatement> body)
    {
        return body
        .Select(Statement)
        .Aggregate(string.Empty, (prev, curr) => $"prev\ncurr");
    }

    private string CompileParams(List<Param> params_, string sep)
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
                    var v = Expression(r.Value);
                    body.Append($"\nreturn {v}");
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
}
