using AuraLang.AST;
using AuraLang.Cli.Options;
using AuraLang.Parser;
using AuraLang.Scanner;
using AuraLang.Shared;
using AuraLang.Types;
using AuraLang.Visitor;

namespace AuraLang.Cli.Commands;

public class AuraFmt : AuraCommand, IUntypedAuraStmtVisitor<string>, IUntypedAuraExprVisitor<string>
{
	/// <summary>
	///     The number of tabs to precede the current line in the source file with. For top-level declarations, this will be 0.
	/// </summary>
	private int Tabs;

	public AuraFmt(FmtOptions opts) : base(opts) { }

	/// <summary>
	///     Formats the entire Aura project
	/// </summary>
	/// <returns>An integer status indicating if the command succeeded</returns>
	protected override async Task<int> ExecuteCommandAsync()
	{
		TraverseProject(FormatFile);
		return await Task.FromResult(0);
	}

	/// <summary>
	///     Formats an individual Aura source file
	/// </summary>
	/// <param name="path">The path of the Aura source file</param>
	public void FormatFile(string path, string contents)
	{
		var formatted = FormatAuraSourceCode(contents, path);
		File.WriteAllText(path, formatted);
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
		if (s[^1] is not '\n')
		{
			s += '\n';
		}

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
		return stmt.Accept(this);
	}

	private string Expression(IUntypedAuraExpression expr)
	{
		return expr.Accept(this);
	}

	public string Visit(UntypedDefer defer)
	{
		return $"defer {Visit((UntypedCall)defer.Call)}";
	}

	public string Visit(UntypedExpressionStmt expressionStmt)
	{
		return Expression(expressionStmt.Expression);
	}

	public string Visit(UntypedFor for_)
	{
		var init = for_.Initializer is not null ? Statement(for_.Initializer) : string.Empty;
		var cond = for_.Condition is not null ? Expression(for_.Condition) : string.Empty;
		var inc = for_.Increment is not null ? Expression(for_.Increment) : string.Empty;
		var body = WithIndent(
			() =>
			{
				return string.Join(
					$"\n{AddTabs(Tabs)}",
					for_.Body.Where(stmt => stmt is not UntypedNewLine).Select(Statement)
				);
			}
		);

		return $"for {init}; {cond}; {inc} {{\n{AddTabs(Tabs + 1)}{body}\n{AddTabs(Tabs)}}}";
	}

	public string Visit(UntypedForEach foreach_)
	{
		var body = string.Join('\n', foreach_.Body.Select(Statement));
		return $"foreach {foreach_.EachName.Value} in {Expression(foreach_.Iterable)} {{\n{body}\n}}";
	}

	public string Visit(UntypedNamedFunction f)
	{
		var pub = f.Public == Visibility.Public ? "pub " : string.Empty;
		var @params = string.Join(", ", f.Params.Select(p => p.Name.Value));

		var body = Expression(f.Body);

		return $"{AddTabs(Tabs)}{pub}fn {f.Name.Value}({@params}) {body}";
	}

	public string Visit(UntypedLet let)
	{
		if (let.NameTyps.Count == 0)
		{
			return ShortLetStmt(let);
		}

		var mut = let.Mutable ? "mut " : string.Empty;
		return let.Initializer is not null
			? $"let {mut}{let.Names[0].Value}: {let.NameTyps[0]!} = {Expression(let.Initializer)}"
			: $"let {mut}{let.Names[0].Value}: {let.NameTyps[0]!}";
	}

	private string ShortLetStmt(UntypedLet let)
	{
		var mut = let.Mutable ? "mut " : string.Empty;
		var init = Expression(let.Initializer!);
		return $"{mut}{let.Names[0].Value} := {init}";
	}

	public string Visit(UntypedMod mod)
	{
		return $"mod {mod.Value.Value}";
	}

	public string Visit(UntypedReturn r)
	{
		var value = r.Value is not null ? $" {Expression(r.Value[0])}" : string.Empty;
		return $"return{value}";
	}

	public string Visit(UntypedClass c)
	{
		var pub = c.Public == Visibility.Public ? "pub " : string.Empty;
		var paramz = string.Join(", ", c.Params.Select(p => $"{p.Name}: {p.ParamType.Typ}"));
		var methods = string.Join("\n\n", c.Body.Select(Statement));
		return $"{pub}class ({paramz}) {{\n{methods}\n}}";
	}

	public string Visit(UntypedWhile w)
	{
		var cond = Expression(w.Condition);
		var body = string.Join("\n", w.Body.Select(Statement));
		return $"while {cond} {{\n{body}\n}}";
	}

	public string Visit(UntypedImport i)
	{
		var alias = i.Alias is not null ? $" as {i.Alias.Value.Value}" : string.Empty;
		return $"import {i.Package.Value}{alias}";
	}

	public string Visit(UntypedMultipleImport imports)
	{
		if (imports.Packages.Count == 1)
		{
			return Statement(imports.Packages.First());
		}

		var importNames = string.Join("\n    ", imports.Packages.Select(i => i.Package.Value));
		return $"import (\n    {importNames}\n)";
	}

	private string MultipleImportStmts(List<UntypedImport> imports)
	{
		var importNames = string.Join("\n    ", imports.Select(i => i.Package.Value));
		return $"import (\n    {importNames}\n)";
	}

	public string Visit(UntypedComment c)
	{
		return c.Text.Value;
	}

	public string Visit(UntypedContinue cont)
	{
		return "continue";
	}

	public string Visit(UntypedBreak b)
	{
		return "break";
	}

	public string Visit(UntypedYield y)
	{
		return "yield";
	}

	public string Visit(UntypedInterface inter)
	{
		var pub = inter.Public == Visibility.Public ? "pub " : string.Empty;
		var methods = string.Join("\n\n", inter.Methods.Select(Visit));
		return $"{pub}interface {{\n{methods}\n}}";
	}

	public string Visit(UntypedFunctionSignature fnSignature)
	{
		var name = fnSignature.Visibility is not null
			? fnSignature.Name.Value.ToUpper()
			: fnSignature.Name.Value.ToLower();
		var @params = string.Join(", ", fnSignature.Params.Select(p => $"{p.Name.Value} {p.ParamType.Typ}"));
		return
			$"{name}({@params}){(fnSignature.ReturnType.IsSameType(new AuraNil()) ? string.Empty : $" -> {fnSignature.ReturnType}")}";
	}

	public string Visit(UntypedAssignment assign)
	{
		return $"{assign.Name.Value} = {Expression(assign.Value)}";
	}

	public string Visit(UntypedPlusPlusIncrement inc)
	{
		return $"{Expression(inc.Name)}++";
	}

	public string Visit(UntypedMinusMinusDecrement dec)
	{
		return $"{Expression(dec.Name)}--";
	}

	public string Visit(UntypedBinary binary)
	{
		return $"{Expression(binary.Left)} {binary.Operator.Value} {Expression(binary.Right)}";
	}

	public string Visit(UntypedBlock block)
	{
		var s = $"{AddTabs(Tabs)}{{\n{AddTabs(Tabs)}";
		var body = WithIndent(
			() =>
			{
				return string.Join(
					$"\n{AddTabs(Tabs)}",
					block.Statements.Where(stmt => stmt is not UntypedNewLine).Select(Statement)
				);
			}
		);

		return $"{{\n{AddTabs(Tabs + 1)}{body}\n{AddTabs(Tabs)}}}";
	}

	public string Visit(UntypedCall call)
	{
		var paramz = string.Join(
			", ",
			call.Arguments.Select(
				arg =>
				{
					var tag = arg.Item1 is not null ? $"{arg.Item1}: " : string.Empty;
					return $"{tag}{Expression(arg.Item2)}";
				}
			)
		);
		return $"{Expression((IUntypedAuraExpression)call.Callee)}({paramz})";
	}

	public string Visit(UntypedGet get)
	{
		return $"{Expression(get.Obj)}.{get.Name.Value}";
	}

	public string Visit(UntypedGetIndex getIndex)
	{
		return $"{Expression(getIndex.Obj)}[{Expression(getIndex.Index)}]";
	}

	public string Visit(UntypedGetIndexRange getIndexRange)
	{
		return $"{Expression(getIndexRange.Obj)}[{Expression(getIndexRange.Lower)}:{Expression(getIndexRange.Upper)}]";
	}

	public string Visit(UntypedGrouping grouping)
	{
		return $"({Expression(grouping.Expr)})";
	}

	public string Visit(UntypedIf iff)
	{
		var cond = Expression(iff.Condition);
		var then = Expression(iff.Then);
		var @else = iff.Else is not null ? $" {Expression(iff.Else)}" : string.Empty;
		return $"if {cond} {then}{@else}";
	}

	public string Visit(IntLiteral i)
	{
		return $"{i.Value}";
	}

	public string Visit(FloatLiteral f)
	{
		return $"{f.Value}";
	}

	public string Visit(StringLiteral s)
	{
		return $"\"{s.Value}\"";
	}

	public string Visit<U>(ListLiteral<U> l) where U : IAuraAstNode
	{
		var values = string.Join(", ", l.L.Select(item => Expression((IUntypedAuraExpression)item)));
		return $"[{l.Typ}]{{ {values} }}";
	}

	public string Visit<TK, TV>(MapLiteral<TK, TV> m) where TK : IAuraAstNode where TV : IAuraAstNode
	{
		var values = m.M.Select(
			pair => $"{Expression((IUntypedAuraExpression)pair.Key)}: {Expression((IUntypedAuraExpression)pair.Value)}"
		);
		return $"map[{m.KeyType} : {m.ValueType}]{{ {values} }}";
	}

	public string Visit(BoolLiteral b)
	{
		return $"{b.Value}";
	}

	public string Visit(UntypedNil n)
	{
		return "nil";
	}

	public string Visit(CharLiteral c)
	{
		return $"'{c.Value}'";
	}

	public string Visit(UntypedLogical lo)
	{
		return $"{Expression(lo.Left)} {lo.Operator.Value} {Expression(lo.Right)}";
	}

	public string Visit(UntypedSet set)
	{
		return $"{Expression(set.Obj)}.{set.Name.Value} = {Expression(set.Value)}";
	}

	public string Visit(UntypedThis th)
	{
		return "this";
	}

	public string Visit(UntypedUnary u)
	{
		return $"{u.Operator.Value}{Expression(u.Right)}";
	}

	public string Visit(UntypedVariable v)
	{
		return $"{v.Name.Value}";
	}

	public string Visit(UntypedAnonymousFunction af)
	{
		var paramz = af.Params.Select(p => $"{p.Name}: {p.ParamType.Typ}");
		var returnType = af.ReturnType is not null ? $" -> {af.ReturnType[0]}" : string.Empty;
		var body = Expression(af.Body);
		return $"fn({paramz}){returnType} {body}";
	}

	public string Visit(UntypedIs @is)
	{
		return "is";
	}

	private string WithIndent(Func<string> a)
	{
		Tabs++;
		var result = a();
		Tabs--;
		return result;
	}

	private string AddTabs(int n)
	{
		return new string(' ', n * 4);
	}

	public string Visit(UntypedNewLine newline)
	{
		return "\n";
	}

	public string Visit(UntypedCheck check)
	{
		throw new NotImplementedException();
	}

	public string Visit(UntypedStruct @struct)
	{
		throw new NotImplementedException();
	}

	public string Visit(UntypedAnonymousStruct anonymousStruct)
	{
		throw new NotImplementedException();
	}
}
