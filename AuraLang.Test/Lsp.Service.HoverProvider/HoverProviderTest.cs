using AuraLang.AST;
using AuraLang.Lsp.Service.HoverProvider;
using AuraLang.Parser;
using AuraLang.Scanner;
using AuraLang.Shared;
using AuraLang.Stdlib;
using AuraLang.Token;
using AuraLang.TypeChecker;
using AuraLang.Types;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Position = AuraLang.Location.Position;

namespace AuraLang.Test.Lsp.Service.HoverProvider;

public class HoverProviderTest
{
	[Test]
	public void TestHoverProvider_Yield()
	{
		const string source = """
                              mod main

                              import aura/io

                              fn main() {
                                  b := if true {
                                      yield true
                                  } else {
                                      yield false
                                  }
                                  io.printf("%v\n", b)
                              }

                              """;
		// Within range
		var hoverText = ProvideHoverText(source, new Position(9, 6));
		MakeAssertions(
			hoverText,
			new TypedYield(new Tok(TokType.Yield, "yield"), new BoolLiteral(new Tok(TokType.True, "true"))).HoverText
		);
		// Out of range
		hoverText = ProvideHoverText(source, new Position(4, 6));
		MakeAssertions_Invalid(hoverText);
	}

	[Test]
	public void TestHoverProvider_Break()
	{
		const string source = """
                              mod main

                              import aura/io

                              fn main() {
                                  while true {
                                      if true {
                                          break
                                      }
                                  }
                              }

                              """;
		// Within range
		var hoverText = ProvideHoverText(source, new Position(13, 7));
		MakeAssertions(hoverText, new TypedBreak(new Tok(TokType.Break, "break")).HoverText);
		// Out of range
		hoverText = ProvideHoverText(source, new Position(8, 7));
		MakeAssertions_Invalid(hoverText);
	}

	[Test]
	public void TestHoverProvider_Continue()
	{
		const string source = """
                              mod main

                              import aura/io

                              fn main() {
                                  while true {
                                      if true {
                                          continue
                                      }
                                  }
                              }

                              """;
		// Within range
		var hoverText = ProvideHoverText(source, new Position(13, 7));
		MakeAssertions(hoverText, new TypedContinue(new Tok(TokType.Continue, "continue")).HoverText);
		// Out of range
		hoverText = ProvideHoverText(source, new Position(8, 7));
		MakeAssertions_Invalid(hoverText);
	}

	[Test]
	public void TestHoverProvider_Import()
	{
		const string source = """
                              mod main

                              import aura/io

                              fn main() {
                                 io.println("Hello world")
                              }

                              """;
		// Within range
		var hoverText = ProvideHoverText(source, new Position(8, 2));
		MakeAssertions(
			hoverText,
			new TypedImport(
				new Tok(TokType.Import, "import"),
				new Tok(TokType.Identifier, "aura/io"),
				null
			).HoverText
		);
		// Out of range
		hoverText = ProvideHoverText(source, new Position(0, 2));
		MakeAssertions_Invalid(hoverText);
	}

	[Test]
	public void TestHoverProvider_Struct()
	{
		const string source = """
                              mod tmp

                              struct Greeter(name: string)

                              """;
		// Within range
		var hoverText = ProvideHoverText(source, new Position(8, 2));
		MakeAssertions(
			hoverText,
			new TypedStruct(
				new Tok(TokType.Struct, "struct"),
				new Tok(TokType.Identifier, "Greeter"),
				new List<Param>
				{
					new(
						new Tok(TokType.Identifier, "name"),
						new ParamType(
							new AuraString(),
							false,
							null
						)
					)
				},
				new Tok(TokType.RightParen, ")"),
				null
			).HoverText
		);
		// Out of range
		hoverText = ProvideHoverText(source, new Position(2, 2));
		MakeAssertions_Invalid(hoverText);
	}

	[Test]
	public void TestHoverProvider_FunctionSignature()
	{
		const string source = """
                              mod tmp

                              interface IGreeter {
                                  fn say_hi()
                              }

                              """;
		// Within range
		var hoverText = ProvideHoverText(source, new Position(8, 3));
		MakeAssertions(
			hoverText,
			new TypedFunctionSignature(
				null,
				new Tok(TokType.Fn, "fn"),
				new Tok(TokType.Identifier, "say_hi"),
				new List<Param>(),
				new Tok(TokType.RightParen, ")"),
				new AuraNil(),
				null
			).HoverText
		);
		// Out of range
		hoverText = ProvideHoverText(source, new Position(5, 3));
		MakeAssertions_Invalid(hoverText);
	}

	[Test]
	public void TestHoverProvider_Interface()
	{
		const string source = """
                              mod tmp

                              interface IGreeter {
                                  fn say_hi()
                              }

                              """;
		// Within range
		var hoverText = ProvideHoverText(source, new Position(12, 2));
		MakeAssertions(
			hoverText,
			new TypedInterface(
				new Tok(TokType.Interface, "interface"),
				new Tok(TokType.Identifier, "IGreeter"),
				new List<TypedFunctionSignature>
				{
					new(
						null,
						new Tok(TokType.Fn, "fn"),
						new Tok(TokType.Identifier, "say_hi"),
						new List<Param>(),
						new Tok(TokType.RightParen, ")"),
						new AuraNil(),
						null
					)
				},
				Visibility.Private,
				new Tok(TokType.RightBrace, "}"),
				null
			).HoverText
		);
		// Out of range
		hoverText = ProvideHoverText(source, new Position(5, 2));
		MakeAssertions_Invalid(hoverText);
	}

	[Test]
	public void TestHoverProvider_Mod()
	{
		const string source = """
                              mod main

                              """;
		// Within range
		var hoverText = ProvideHoverText(source, new Position(2, 0));
		MakeAssertions(
			hoverText,
			new TypedMod(new Tok(TokType.Mod, "mod"), new Tok(TokType.Identifier, "main")).HoverText
		);
		// Out of range
		hoverText = ProvideHoverText(source, new Position(0, 1));
		MakeAssertions_Invalid(hoverText);
	}

	[Test]
	public void TestHoverProvider_Let()
	{
		const string source = """
                              mod main

                              fn main() {
                                  name := "Bob"
                              }

                              """;
		// Within range
		var hoverText = ProvideHoverText(source, new Position(6, 3));
		MakeAssertions(
			hoverText,
			new TypedLet(
				null,
				new List<(bool, Tok)> { (false, new Tok(TokType.Identifier, "name")) },
				false,
				new StringLiteral(new Tok(TokType.StringLiteral, "Bob"))
			).HoverText
		);
		// Out of range
		hoverText = ProvideHoverText(source, new Position(2, 3));
		MakeAssertions_Invalid(hoverText);
	}

	[Test]
	public void TestHoverProvider_AnonymousFunction()
	{
		const string source = """
                              mod main

                              fn main() {
                                  f := fn() -> int {
                                      return 5
                                  }
                              }

                              """;
		// Within range
		var hoverText = ProvideHoverText(source, new Position(10, 3));
		MakeAssertions(
			hoverText,
			new TypedAnonymousFunction(
				new Tok(TokType.Fn, "fn"),
				new List<Param>(),
				new TypedBlock(
					new Tok(TokType.LeftBrace, "{"),
					new List<ITypedAuraStatement>
					{
						new TypedReturn(
							new Tok(TokType.Return, "return"),
							new IntLiteral(new Tok(TokType.IntLiteral, "5"))
						)
					},
					new Tok(TokType.RightBrace, "}"),
					new AuraFunction(new List<Param>(), new AuraInt())
				),
				new AuraInt()
			).HoverText
		);
		// Out of range
		hoverText = ProvideHoverText(source, new Position(7, 3));
		MakeAssertions_Invalid(hoverText);
	}

	[Test]
	public void TestHoverProvider_NamedFunction()
	{
		const string source = """
                              mod tmp

                              pub fn f(s: string) -> int {
                                  return 5
                              }

                              """;
		// Within range
		var hoverText = ProvideHoverText(source, new Position(7, 2));
		MakeAssertions(
			hoverText,
			new TypedNamedFunction(
				new Tok(TokType.Fn, "fn"),
				new Tok(TokType.Identifier, "f"),
				new List<Param>
				{
					new(
						new Tok(TokType.Identifier, "s"),
						new ParamType(
							new AuraString(),
							false,
							null
						)
					)
				},
				new TypedBlock(
					new Tok(TokType.LeftBrace, "{"),
					new List<ITypedAuraStatement>
					{
						new TypedReturn(
							new Tok(TokType.Return, "return"),
							new IntLiteral(new Tok(TokType.IntLiteral, "5"))
						)
					},
					new Tok(TokType.RightBrace, "}"),
					new AuraInt()
				),
				new AuraInt(),
				Visibility.Public,
				null
			).HoverText
		);
		// Out of range
		hoverText = ProvideHoverText(source, new Position(3, 2));
		MakeAssertions_Invalid(hoverText);
	}

	[Test]
	public void TestHoverProvider_Defer()
	{
		const string source = """
                              mod main

                              import aura/io

                              fn main() {
                                  defer io.println("Hello world")
                              }

                              """;
		// Within range
		var hoverText = ProvideHoverText(source, new Position(5, 5));
		var ioModule = AuraStdlib.GetAllModules()["aura/io"];
		MakeAssertions(
			hoverText,
			new TypedDefer(
				new Tok(TokType.Defer, "defer"),
				new TypedCall(
					new TypedGet(
						new TypedVariable(new Tok(TokType.Identifier, "io"), ioModule!),
						new Tok(TokType.Identifier, "println"),
						new AuraNamedFunction(
							"println",
							Visibility.Public,
							new AuraFunction(
								new List<Param>
								{
									new(
										new Tok(TokType.Identifier, "s"),
										new ParamType(
											new AuraString(),
											false,
											null
										)
									)
								},
								new AuraNil()
							)
						)
					),
					new List<ITypedAuraExpression> { new StringLiteral(new Tok(TokType.StringLiteral, "Hello world")) },
					new Tok(TokType.RightParen, ")"),
					new AuraNamedFunction(
						"println",
						Visibility.Public,
						new AuraFunction(
							new List<Param>
							{
								new(
									new Tok(TokType.Identifier, "s"),
									new ParamType(
										new AuraString(),
										false,
										null
									)
								)
							},
							new AuraNil()
						)
					)
				)
			).HoverText
		);
		// Out of range
		hoverText = ProvideHoverText(source, new Position(2, 5));
		MakeAssertions_Invalid(hoverText);
	}

	[Test]
	public void TestHoverProvider_Variable()
	{
		const string source = """
                              mod main

                              import aura/io

                              fn main() {
                                  s := "Hello world"
                                  s
                              }

                              """;
		// Within range
		var hoverText = ProvideHoverText(source, new Position(4, 6));
		MakeAssertions(hoverText, new TypedVariable(new Tok(TokType.Identifier, "s"), new AuraString()).HoverText);
		// Out of range
		hoverText = ProvideHoverText(source, new Position(2, 6));
		MakeAssertions_Invalid(hoverText);
	}

	[Test]
	public void TestHoverProvider_This()
	{
		const string source = """
                              mod main

                              import aura/io

                              fn main() {
                                  greeter := Greeter("Bob")
                                  greeter.say_hi()
                              }

                              class Greeter(name: string) {
                                 pub fn say_hi() {
                                     io.println("Hello " + this.name)
                                 }
                              }

                              """;
		// Within range
		var hoverText = ProvideHoverText(source, new Position(32, 11));
		MakeAssertions(
			hoverText,
			new TypedThis(
				new Tok(TokType.This, "this"),
				new AuraClass(
					"Greeter",
					new List<Param>
					{
						new(
							new Tok(TokType.Identifier, "name"),
							new ParamType(
								new AuraString(),
								false,
								null
							)
						)
					},
					new List<AuraNamedFunction>
					{
						new(
							"say_hi",
							Visibility.Public,
							new AuraFunction(new List<Param>(), new AuraNil())
						)
					},
					new List<AuraInterface>(),
					Visibility.Private
				)
			).HoverText
		);
		// Out of range
		hoverText = ProvideHoverText(source, new Position(3, 11));
		MakeAssertions_Invalid(hoverText);
	}

	[Test]
	public void TestHoverProvider_Call()
	{
		const string source = """
                              mod main

                              fn main() {
                                  f()
                              }

                              fn f() -> string {
                                  return "Hello world"
                              }

                              """;
		// Within range
		var hoverText = ProvideHoverText(source, new Position(4, 3));
		MakeAssertions(
			hoverText,
			new TypedCall(
				new TypedVariable(
					new Tok(TokType.Identifier, "f"),
					new AuraNamedFunction(
						"f",
						Visibility.Private,
						new AuraFunction(new List<Param>(), new AuraString())
					)
				),
				new List<ITypedAuraExpression>(),
				new Tok(TokType.RightParen, ")"),
				new AuraNamedFunction(
					"f",
					Visibility.Private,
					new AuraFunction(new List<Param>(), new AuraString())
				)
			).HoverText
		);
		// Out of range
		hoverText = ProvideHoverText(source, new Position(6, 3));
		MakeAssertions_Invalid(hoverText);
	}

	[Test]
	public void TestHoverProvider_Assignment()
	{
		const string source = """
                              mod main

                              fn main() {
                                  mut i := 5
                                  i = 6
                              }

                              """;
		// Within range
		var hoverText = ProvideHoverText(source, new Position(4, 4));
		MakeAssertions(
			hoverText,
			new TypedAssignment(
				new Tok(TokType.Identifier, "i"),
				new IntLiteral(new Tok(TokType.IntLiteral, "6")),
				new AuraInt()
			).HoverText
		);
		// Out of range
		hoverText = ProvideHoverText(source, new Position(6, 4));
		MakeAssertions_Invalid(hoverText);
	}

	private Hover ProvideHoverText(string source, Position position)
	{
		// Type check the source code
		var tokens = new AuraScanner(source, string.Empty).ScanTokens();
		var untypedAst = new AuraParser(tokens.Where(tok => tok.Typ is not TokType.Newline).ToList(), string.Empty)
			.Parse();
		var typeChecker = new AuraTypeChecker(string.Empty, string.Empty);
		typeChecker.BuildSymbolsTable(untypedAst);
		var typedAst = typeChecker.CheckTypes(untypedAst);

		// Provide hover text
		var hoverProvider = new AuraHoverProvider();
		return hoverProvider.GetHoverText(
			position,
			typedAst
		);
	}

	private void MakeAssertions(Hover actual, string expected)
	{
		Assert.Multiple(
			() =>
			{
				Assert.That(actual, Is.Not.Null);
				Assert.That(actual.Contents.Third.Value, Is.EqualTo($"```\n{expected}\n```"));
			}
		);
	}

	private void MakeAssertions_Invalid(Hover actual) { Assert.That(actual.Contents.Value, Is.Null); }
}
