using AuraLang.AST;
using AuraLang.Cli.Options;
using AuraLang.Parser;
using AuraLang.Scanner;
using AuraLang.Shared;

namespace AuraLang.Cli.Commands;

public class AuraFmt : AuraCommand
{
	/// <summary>
	/// The number of tabs to precede the current line in the source file with. For top-level declarations, this will be 0.
	/// </summary>
	private int Tabs = 0;

	public AuraFmt(FmtOptions opts) : base(opts) { }

	/// <summary>
	/// Formats the entire Aura project
	/// </summary>
	/// <returns>An integer status indicating if the command succeeded</returns>
	public override int Execute()
	{
		TraverseProject(FormatFile);
		return 0;
	}

	/// <summary>
	/// Formats an individual Aura source file
	/// </summary>
	/// <param name="path">The path of the Aura source file</param>
	public void FormatFile(string path)
	{
		var contents = FormatAuraSourceCode(File.ReadAllText(path), path);
		File.WriteAllText(path, contents);
	}

	public string FormatAuraSourceCode(string source, string filePath)
	{
		// Scan
		var tokens = new AuraScanner(source, filePath).ScanTokens();
		// Parse
		var untypedAst = new AuraParser(tokens, filePath).Parse();
		// Format AST
		var formatted = Format(untypedAst);
		// Turn back into a string
		var s = string.Join(string.Empty, formatted);
		if (s[^1] is not '\n') s += '\n';
		return s;
	}

	private List<string> Format(List<IUntypedAuraStatement> nodes)
	{
        if (nodes.FindAll(n => n is UntypedImport).Count > 1)
        {
            var firstImportIndex = nodes.FindIndex(n => n is UntypedImport);
            var imports = nodes.FindAll(n => n is UntypedImport).Select(stmt => (UntypedImport)stmt).ToList();
            var formattedImports = MultipleImportStmts(imports);

            var formattedNodes = nodes.Where(n => n is not UntypedImport).Select(Statement).ToList();
            formattedNodes.Insert(firstImportIndex, formattedImports);
            return formattedNodes;
        }

		return nodes.Select(Statement).ToList();
	}

	private string Statement(IUntypedAuraStatement stmt)
	{
		return stmt switch
		{
			UntypedDefer defer => DeferStmt(defer),
			UntypedExpressionStmt expressionStmt => ExpressionStmt(expressionStmt),
			UntypedFor for_ => ForStmt(for_),
			UntypedForEach foreach_ => ForEachStmt(foreach_),
			UntypedNamedFunction f => NamedFunctionStmt(f),
			UntypedLet let => LetStmt(let),
			UntypedMod mod => ModStmt(mod),
			UntypedReturn r => ReturnStmt(r),
			UntypedClass c => ClassStmt(c),
			UntypedWhile w => WhileStmt(w),
			UntypedImport i => ImportStmt(i),
			UntypedComment c => CommentStmt(c),
			UntypedContinue cont => ContinueStmt(cont),
			UntypedBreak b => BreakStmt(b),
			UntypedYield y => YieldStmt(y),
			UntypedInterface inter => InterfaceStmt(inter),
			UntypedNewLine => "\n",
			_ => throw new Exception() // TODO Create exception
		};
	}

	private string Expression(IUntypedAuraExpression expr)
	{
		return expr switch
		{
			UntypedAssignment assign => AssignmentExpr(assign),
			UntypedBinary binary => BinaryExpr(binary),
			UntypedBlock block => BlockExpr(block),
			UntypedCall call => CallExpr(call),
			UntypedGet get => GetExpr(get),
			UntypedGetIndex getIndex => GetIndexExpr(getIndex),
			UntypedGetIndexRange getIndexRange => GetIndexRangeExpr(getIndexRange),
			UntypedGrouping grouping => GroupingExpr(grouping),
			UntypedIf iff => IfExpr(iff),
			IntLiteral i => IntLiteralExpr(i),
			FloatLiteral f => FloatLiteralExpr(f),
			StringLiteral s => StringLiteralExpr(s),
			ListLiteral<IUntypedAuraExpression> l => ListLiteralExpr(l),
			MapLiteral<IUntypedAuraExpression, IUntypedAuraExpression> m => MapLiteralExpr(m),
			BoolLiteral b => BoolLiteralExpr(b),
			UntypedNil n => NilExpr(n),
			CharLiteral c => CharLiteralExpr(c),
			UntypedLogical lo => LogicalExpr(lo),
			UntypedSet set => SetExpr(set),
			UntypedThis th => ThisExpr(th),
			UntypedUnary u => UnaryExpr(u),
			UntypedVariable v => VariableExpr(v),
			UntypedAnonymousFunction af => AnonymousFunctionExpr(af),
			UntypedIs iss => IsExpr(iss),
			UntypedPlusPlusIncrement ppi => IncrementExpr(ppi),
			UntypedMinusMinusDecrement ddm => DecrementExpr(ddm),
			_ => throw new Exception() // TODO Create exception
		};
	}

	private string DeferStmt(UntypedDefer defer) => $"defer {CallExpr((UntypedCall)defer.Call)}";

	private string ExpressionStmt(UntypedExpressionStmt expressionStmt) => Expression(expressionStmt.Expression);

	private string ForStmt(UntypedFor for_)
	{
		var init = for_.Initializer is not null
			? Statement(for_.Initializer)
			: string.Empty;
		var cond = for_.Condition is not null
			? Expression(for_.Condition)
			: string.Empty;
		var inc = for_.Increment is not null
			? Expression(for_.Increment)
			: string.Empty;
		var body = WithIndent(() =>
		{
			return string.Join($"\n{AddTabs(Tabs)}", for_.Body.Where(stmt => stmt is not UntypedNewLine).Select(Statement));
		});

		return $"for {init}; {cond}; {inc} {{\n{AddTabs(Tabs + 1)}{body}\n{AddTabs(Tabs)}}}";
	}

	private string ForEachStmt(UntypedForEach foreach_)
	{
		var body = string.Join('\n', foreach_.Body.Select(Statement));
		return $"foreach {foreach_.EachName.Value} in {Expression(foreach_.Iterable)} {{\n{body}\n}}";
	}

	private string NamedFunctionStmt(UntypedNamedFunction f)
	{
		var pub = f.Public == Visibility.Public ? "pub " : string.Empty;
		var paramz = string.Join(", ", f.Params.Select(p => p.Name.Value));

		var body = Expression(f.Body);

		return $"{AddTabs(Tabs)}{pub}fn {f.Name.Value}({paramz}) {body}";
	}

	private string LetStmt(UntypedLet let)
	{
		if (let.NameTyp is null) return ShortLetStmt(let);
		var mut = let.Mutable ? "mut " : string.Empty;
		return let.Initializer is not null
			? $"let {mut}{let.Name.Value}: {let.NameTyp!} = {Expression(let.Initializer)}"
			: $"let {mut}{let.Name.Value}: {let.NameTyp!}";
	}

	private string ShortLetStmt(UntypedLet let)
	{
		var mut = let.Mutable ? "mut " : string.Empty;
		var init = Expression(let.Initializer!);
		return $"{mut}{let.Name.Value} := {init}";
	}

	private string ModStmt(UntypedMod mod) => $"mod {mod.Value.Value}";

	private string ReturnStmt(UntypedReturn r)
	{
		var value = r.Value is not null
			? $" {Expression(r.Value)}"
			: string.Empty;
		return $"return{value}";
	}

	private string ClassStmt(UntypedClass c)
	{
		var pub = c.Public == Visibility.Public
			? "pub "
			: string.Empty;
		var paramz = string.Join(", ", c.Params.Select(p => $"{p.Name}: {p.ParamType.Typ}"));
		var methods = string.Join("\n\n", c.Body.Select(Statement));
		return $"{pub}class ({paramz}) {{\n{methods}\n}}";
	}

	private string WhileStmt(UntypedWhile w)
	{
		var cond = Expression(w.Condition);
		var body = string.Join("\n", w.Body.Select(Statement));
		return $"while {cond} {{\n{body}\n}}";
	}

	private string ImportStmt(UntypedImport i)
	{
		var alias = i.Alias is not null
			? $" as {i.Alias.Value.Value}"
			: string.Empty;
		return $"import {i.Package.Value}{alias}";
	}

    private string MultipleImportStmts(List<UntypedImport> imports)
    {
        var importNames = string.Join("\n\t", imports.Select(i => i.Package.Value));
        return $"import (\n\t{importNames}\n)";
    }

	private string CommentStmt(UntypedComment c) => c.Text.Value;

	private string ContinueStmt(UntypedContinue cont) => "continue";

	private string BreakStmt(UntypedBreak b) => "break";

	private string YieldStmt(UntypedYield y) => "yield";

	private string InterfaceStmt(UntypedInterface inter)
	{
		var pub = inter.Public == Visibility.Public
			? "pub "
			: string.Empty;
		var methods = string.Join("\n\n", inter.Methods.Select(m => m.ToString()));
		return $"{pub}interface {{\n{methods}\n}}";
	}

	private string AssignmentExpr(UntypedAssignment assign) => $"{assign.Name.Value} = {Expression(assign.Value)}";

	private string IncrementExpr(UntypedPlusPlusIncrement inc) => $"{Expression(inc.Name)}++";

	private string DecrementExpr(UntypedMinusMinusDecrement dec) => $"{Expression(dec.Name)}--";

	private string BinaryExpr(UntypedBinary binary) => $"{Expression(binary.Left)} {binary.Operator.Value} {Expression(binary.Right)}";

	private string BlockExpr(UntypedBlock block)
	{
		var s = $"{AddTabs(Tabs)}{{\n{AddTabs(Tabs)}";
		var body = WithIndent(() =>
		{
			return string.Join($"\n{AddTabs(Tabs)}", block.Statements.Where(stmt => stmt is not UntypedNewLine).Select(Statement));
		});

		return $"{{\n{AddTabs(Tabs + 1)}{body}\n{AddTabs(Tabs)}}}";
	}

	private string CallExpr(UntypedCall call)
	{
		var paramz = string.Join(", ", call.Arguments.Select(arg =>
		{
			var tag = arg.Item1 is not null
				? $"{arg.Item1}: "
				: string.Empty;
			return $"{tag}{Expression(arg.Item2)}";
		}));
		return $"{Expression((IUntypedAuraExpression)call.Callee)}({paramz})";
	}

	private string GetExpr(UntypedGet get) => $"{Expression(get.Obj)}.{get.Name.Value}";

	private string GetIndexExpr(UntypedGetIndex getIndex) => $"{Expression(getIndex.Obj)}[{Expression(getIndex.Index)}]";

	private string GetIndexRangeExpr(UntypedGetIndexRange getIndexRange)
		=> $"{Expression(getIndexRange.Obj)}[{Expression(getIndexRange.Lower)}:{Expression(getIndexRange.Upper)}]";

	private string GroupingExpr(UntypedGrouping grouping) => $"({Expression(grouping.Expr)})";

	private string IfExpr(UntypedIf iff)
	{
		var cond = Expression(iff.Condition);
		var then = Expression(iff.Then);
		var elsee = iff.Else is not null
			? $" {Expression(iff.Else)}"
			: string.Empty;
		return $"if {cond} {then}{elsee}";
	}

	private string IntLiteralExpr(IntLiteral i) => $"{i.I}";

	private string FloatLiteralExpr(FloatLiteral f) => $"{f.F}";

	private string StringLiteralExpr(StringLiteral s) => $"\"{s.S}\"";

	private string ListLiteralExpr(ListLiteral<IUntypedAuraExpression> l)
	{
		var values = string.Join(", ", l.L.Select(Expression));
		return $"[{l.Typ}]{{ {values} }}";
	}

	private string MapLiteralExpr(MapLiteral<IUntypedAuraExpression, IUntypedAuraExpression> m)
	{
		var values = m.M.Select(pair => $"{Expression(pair.Key)}: {Expression(pair.Value)}");
		return $"map[{m.KeyType} : {m.ValueType}]{{ {values} }}";
	}

	private string BoolLiteralExpr(BoolLiteral b) => $"{b.B}";

	private string NilExpr(UntypedNil n) => "nil";

	private string CharLiteralExpr(CharLiteral c) => $"'{c.C}'";

	private string LogicalExpr(UntypedLogical lo) => $"{Expression(lo.Left)} {lo.Operator.Value} {Expression(lo.Right)}";

	private string SetExpr(UntypedSet set) => $"{Expression(set.Obj)}.{set.Name.Value} = {Expression(set.Value)}";

	private string ThisExpr(UntypedThis th) => "this";

	private string UnaryExpr(UntypedUnary u) => $"{u.Operator.Value}{Expression(u.Right)}";

	private string VariableExpr(UntypedVariable v) => $"{v.Name.Value}";

	private string AnonymousFunctionExpr(UntypedAnonymousFunction af)
	{
		var paramz = af.Params.Select(p => $"{p.Name}: {p.ParamType.Typ}");
		var returnType = af.ReturnType is not null
			? $" -> {af.ReturnType.Value.Value}"
			: string.Empty;
		var body = Expression(af.Body);
		return $"fn({paramz}){returnType} {body}";
	}

	private string IsExpr(UntypedIs iss) => "is";

	private string WithIndent(Func<string> a)
	{
		Tabs++;
		var result = a();
		Tabs--;
		return result;
	}

	private string AddTabs(int n) => new('\t', n);
}
