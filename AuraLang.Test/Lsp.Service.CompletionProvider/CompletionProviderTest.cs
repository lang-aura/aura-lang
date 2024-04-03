using AuraLang.Lsp.Service.CompletionProvider;
using AuraLang.Parser;
using AuraLang.Scanner;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.TypeChecker;
using AuraLang.Types;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using Position = AuraLang.Location.Position;

namespace AuraLang.Test.Lsp.Service.CompletionProvider;

public class CompletionProviderTest
{
	[Test]
	public void TestCompletionProvider_ClassVariable()
	{
		const string triggerCharacter = ".";
		const string source = """
                              mod main

                              import aura/io

                              class Greeter(name: string) {
                                  pub fn say_hi() {
                                      io.println("Hello " + this.name)
                                  }
                              }

                              fn main() {
                                  greeter := Greeter("Bob")
                                  greeter
                              }

                              """;
		var completionList = ProvideCompletionList(
			source,
			new Position(12, 12),
			triggerCharacter
		);
		var expected = new AuraClass(
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
		).ProvideCompletableOptions(triggerCharacter);
		expected.Items = expected.Items.OrderBy(item => item.Label).ToArray();
		MakeAssertions(
			completionList,
			expected
		);
	}

	[Test]
	public void TestCompletionProvider_ListVariable()
	{
		const string triggerCharacter = ".";
		const string source = """
                              mod main

                              fn main() {
                                  l := [int]{ 1, 2, 3 }
                                  l
                              }

                              """;
		var completionList = ProvideCompletionList(
			source,
			new Position(6, 4),
			triggerCharacter
		);
		var expected = new AuraList(new AuraInt()).ProvideCompletableOptions(triggerCharacter);
		expected.Items = expected.Items.OrderBy(item => item.Label).ToArray();
		MakeAssertions(
			completionList,
			expected
		);
	}

	[Test]
	public void TestCompletionProvider_ListLiteral()
	{
		const string triggerCharacter = ".";
		const string source = """
                              mod main

                              fn main() {
                                  [int]{ 1, 2, 3 }
                              }

                              """;
		var completionList = ProvideCompletionList(
			source,
			new Position(21, 3),
			triggerCharacter
		);
		var expected = new AuraList(new AuraInt()).ProvideCompletableOptions(triggerCharacter);
		expected.Items = expected.Items.OrderBy(item => item.Label).ToArray();
		MakeAssertions(
			completionList,
			expected
		);
	}

	[Test]
	public void TestCompletionProvider_StructVariable()
	{
		const string triggerCharacter = ".";
		const string source = """
                              mod main

                              struct S(name: string)

                              fn main() {
                                  s := S("Bob")
                                  s
                              }

                              """;
		var completionList = ProvideCompletionList(
			source,
			new Position(6, 6),
			triggerCharacter
		);
		var expected = new AuraStruct(
			"S",
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
			Visibility.Private
		).ProvideCompletableOptions(triggerCharacter);
		expected.Items = expected.Items.OrderBy(item => item.Label).ToArray();
		MakeAssertions(
			completionList,
			expected
		);
	}

	[Test]
	public void TestCompletionProvider_MapVariable()
	{
		const string triggerCharacter = ".";
		const string source = """
                              mod main

                              fn main() {
                                  m := map[string : int]{
                                      "Hello": 1,
                                      "World": 2,
                                  }
                                  m
                              }

                              """;
		var completionList = ProvideCompletionList(
			source,
			new Position(6, 7),
			triggerCharacter
		);
		var expected = new AuraMap(new AuraString(), new AuraInt()).ProvideCompletableOptions(triggerCharacter);
		expected.Items = expected.Items.OrderBy(item => item.Label).ToArray();
		MakeAssertions(
			completionList,
			expected
		);
	}

	[Test]
	public void TestCompletionProvider_MapLiteral()
	{
		const string triggerCharacter = ".";
		const string source = """
                              mod main

                              fn main() {
                                  map[string : int]{
                                      "Hello": 1,
                                      "World": 2,
                                  }
                              }

                              """;
		var completionList = ProvideCompletionList(
			source,
			new Position(6, 6),
			triggerCharacter
		);
		var expected = new AuraMap(new AuraString(), new AuraInt()).ProvideCompletableOptions(triggerCharacter);
		expected.Items = expected.Items.OrderBy(item => item.Label).ToArray();
		MakeAssertions(
			completionList,
			expected
		);
	}

	[Test]
	public void TestCompletionProvider_StringLiteral()
	{
		const string triggerCharacter = ".";
		const string source = """
                              mod main

                              fn main() {
                                  "Hello world"
                              }

                              """;
		var completionList = ProvideCompletionList(
			source,
			new Position(18, 3),
			triggerCharacter
		);
		var expected = new AuraString().ProvideCompletableOptions(triggerCharacter);
		expected.Items = expected.Items.OrderBy(item => item.Label).ToArray();
		MakeAssertions(
			completionList,
			expected
		);
	}

	[Test]
	public void TestCompletionProvider_StringVariable()
	{
		const string triggerCharacter = ".";
		const string source = """
                              mod main

                              fn main() {
                                 s := "Hello world"
                                 s
                              }

                              """;
		var completionList = ProvideCompletionList(
			source,
			new Position(5, 4),
			triggerCharacter
		);
		var expected = new AuraString().ProvideCompletableOptions(triggerCharacter);
		expected.Items = expected.Items.OrderBy(item => item.Label).ToArray();
		MakeAssertions(
			completionList,
			expected
		);
	}

	private CompletionList? ProvideCompletionList(string source, Position position, string triggerCharacter)
	{
		// Type check the source code
		var tokens = new AuraScanner(source, string.Empty).ScanTokens();
		var untypedAst = new AuraParser(tokens.Where(tok => tok.Typ is not TokType.Newline).ToList(), string.Empty)
			.Parse();
		var typeChecker = new AuraTypeChecker(string.Empty, string.Empty);
		typeChecker.BuildSymbolsTable(untypedAst);
		var typedAst = typeChecker.CheckTypes(untypedAst);

		// Provide completion options
		var completionProvider = new AuraCompletionProvider();
		return completionProvider.ComputeCompletionOptions(
			position,
			triggerCharacter,
			typedAst
		);
	}

	private void MakeAssertions(CompletionList? actual, CompletionList? expected)
	{
		if (expected is null) Assert.That(actual, Is.Null);

		Assert.Multiple(
			() =>
			{
				Assert.That(actual, Is.Not.Null);
				Assert.That(actual!.Items, Has.Length.EqualTo(expected!.Items.Length));
				foreach (var item in actual.Items.Zip(expected.Items))
					Assert.That(
						JsonConvert.SerializeObject(item.First),
						Is.EqualTo(JsonConvert.SerializeObject(item.Second))
					);
			}
		);
	}
}
