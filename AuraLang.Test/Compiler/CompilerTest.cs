using AuraLang.AST;
using AuraLang.Compiler;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.Types;
using AuraChar = AuraLang.Types.Char;
using AuraString = AuraLang.Types.String;
using AuraList = AuraLang.Types.List;

namespace AuraLang.Test.Compiler;

public class CompilerTest
{
    [Test]
    public void TestCompile_Assignment()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedAssignment(
                    new Tok(TokType.Identifier, "i", 1),
                    new TypedLiteral<long>(5, new Int(), 1),
                    new Int(),
                    1),
                1)
        });
        MakeAssertions(output, "i = 5");
    }

    [Test]
    public void TestCompile_Binary()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedBinary(
                    new TypedLiteral<long>(5, new Int(), 1),
                    new Tok(TokType.Plus, "+", 1),
                    new TypedLiteral<long>(5, new Int(), 1),
                    new Int(),
                    1),
                1)
        });
        MakeAssertions(output, "5 + 5");
    }

    [Test]
    public void TestCompile_Block_EmptyBody()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedBlock(
                    new List<TypedAuraStatement>(),
                    new Nil(),
                    1),
                1)
        });
        MakeAssertions(output, "{}");
    }

    [Test]
    public void TestCompile_Block()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedBlock(
                    new List<TypedAuraStatement>
                    {
                        new TypedLet(
                            new Tok(TokType.Identifier, "i", 2),
                            true,
                            false,
                            new TypedLiteral<long>(5, new Int(), 2),
                            2),
                    },
                    new Nil(),
                    1),
                1)
        });
        MakeAssertions(output, "{\nvar i int = 5\n}");
    }

    [Test]
    public void TestCompile_Call_NoArgs()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedCall(
                    new TypedVariable(
                        new Tok(TokType.Identifier, "f", 1),
                        new Function(
                            "f",
                            new AnonymousFunction(
                                new List<ParamType>(),
                                new Nil())),
                        1),
                    new List<TypedAuraExpression>(),
                    new Nil(),
                    1),
                1)
        });
        MakeAssertions(output, "f()");
    }

    [Test]
    public void TestCompile_OneParam()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedCall(
                    new TypedVariable(
                        new Tok(TokType.Identifier, "f", 1),
                        new Function(
                            "f",
                            new AnonymousFunction(
                                new List<ParamType>
                                {
                                    new ParamType(new Int(), false)
                                },
                                new Nil())),
                        1),
                    new List<TypedAuraExpression>
                    {
                        new TypedLiteral<long>(5, new Int(), 1)
                    },
                    new Nil(),
                    1),
                1)
        });
        MakeAssertions(output, "f(5)");
    }
    
    [Test]
    public void TestCompile_TwoParams()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedCall(
                    new TypedVariable(
                        new Tok(TokType.Identifier, "f", 1),
                        new Function(
                            "f",
                            new AnonymousFunction(
                                new List<ParamType>
                                {
                                    new(new Int(), false),
                                    new(new AuraString(), false)
                                },
                                new Nil())),
                        1),
                    new List<TypedAuraExpression>
                    {
                        new TypedLiteral<long>(5, new Int(), 1),
                        new TypedLiteral<string>("Hello world", new AuraString(), 1)
                    },
                    new Nil(),
                    1),
                1)
        });
        MakeAssertions(output, "f(5, \"Hello world\")");
    }

    [Test]
    public void TestCompile_Get()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedGet(
                    new TypedVariable(
                        new Tok(TokType.Identifier, "greeter", 1),
                        new Class(
                            "Greeter",
                            new List<string>
                            {
                                "name"
                            },
                            new List<ParamType>
                            {
                                new ParamType(new AuraString(), false)
                            },
                            new List<Function>()),
                        1),
                    new Tok(TokType.Identifier, "name", 1),
                    new AuraString(),
                    1),
                1)
        });
        MakeAssertions(output, "greeter.name");
    }

    [Test]
    public void TestCompile_GetIndex()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedGetIndex(
                    new TypedVariable(
                        new Tok(TokType.Identifier, "names", 1),
                        new Types.List(new AuraString()),
                        1),
                    new TypedLiteral<long>(0, new Int(), 1),
                    new AuraString(),
                    1),
                1)
        });
        MakeAssertions(output, "names[0]");
    }

    [Test]
    public void TestCompile_GetIndexRange()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedGetIndexRange(
                    new TypedVariable(
                        new Tok(TokType.Identifier, "names", 1),
                        new Types.List(new AuraString()),
                        1),
                    new TypedLiteral<long>(0, new Int(), 1),
                    new TypedLiteral<long>(2, new Int(), 1),
                    new Types.List(new AuraString()),
                    1),
                1)
        });
        MakeAssertions(output, "names[0:2]");
    }

    [Test]
    public void TestCompile_Grouping()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedGrouping(
                    new TypedLiteral<string>("Hello world", new AuraString(), 1),
                    new AuraString(),
                    1),
                1)
        });
        MakeAssertions(output, "(\"Hello world\")");
    }

    [Test]
    public void TestCompile_If()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedIf(
                    new TypedLiteral<bool>(true, new Bool(), 1),
                    new TypedBlock(
                        new List<TypedAuraStatement>
                        {
                            new TypedReturn(
                                new TypedLiteral<long>(1, new Int(), 2),
                                2)
                        },
                        new Int(),
                        1),
                    null,
                    new Int(),
                    1),
                1)
        });
        MakeAssertions(output, "if true {\nreturn 1\n}");
    }
    
    [Test]
    public void TestCompile_If_Else()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedIf(
                    new TypedLiteral<bool>(true, new Bool(), 1),
                    new TypedBlock(
                        new List<TypedAuraStatement>
                        {
                            new TypedReturn(
                                new TypedLiteral<long>(1, new Int(), 2),
                                2)
                        },
                        new Int(),
                        1),
                    new TypedBlock(
                        new List<TypedAuraStatement>
                        {
                            new TypedReturn(
                                new TypedLiteral<long>(2, new Int(), 4),
                                2)
                        },
                        new Int(),
                        3),
                    new Int(),
                    1),
                1)
        });
        MakeAssertions(output, "if true {\nreturn 1\n} else {\nreturn 2\n}");
    }

    [Test]
    public void TestCompile_IntLiteral()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedLiteral<long>(5, new Int(), 1),
                1)
        });
        MakeAssertions(output, "5");
    }

    [Test]
    public void TestCompile_FloatLiteral()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedLiteral<double>(5.1, new Float(), 1),
                1)
        });
        MakeAssertions(output, "5.1");
    }
    
    [Test]
    public void TestCompile_FloatLiteral_WithZeroDecimal()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedLiteral<double>(5.0, new Float(), 1),
                1)
        });
        MakeAssertions(output, "5");
    }

    [Test]
    public void TestCompile_StringLiteral()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedLiteral<string>("Hello world", new AuraString(), 1),
                1)
        });
        MakeAssertions(output, "\"Hello world\"");
    }

    [Test]
    public void TestCompile_ListLiteral()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedLiteral<List<TypedAuraExpression>>(new List<TypedAuraExpression>
                    {
                        new TypedLiteral<long>(1, new Int(), 1),
                        new TypedLiteral<long>(2, new Int(), 1),
                        new TypedLiteral<long>(3, new Int(), 1)
                    },
                    new AuraList(new Int()),
                    1),
                1)
        });
        MakeAssertions(output, "[]int{1, 2, 3}");
    }

    [Test]
    public void TestCompile_MapLiteral()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedLiteral<Dictionary<TypedAuraExpression, TypedAuraExpression>>(
                    new Dictionary<TypedAuraExpression, TypedAuraExpression>
                    {
                        { new TypedLiteral<string>("Hello", new AuraString(), 1), new TypedLiteral<long>(1, new Int(), 1) }
                    },
                    new Map(new AuraString(), new Int()),
                    1),
                1)
        });
        MakeAssertions(output, "map[string]int{\n\"Hello\": 1\n}");
    }

    [Test]
    public void TestCompile_MapLiteral_Empty()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedLiteral<Dictionary<TypedAuraExpression, TypedAuraExpression>>(
                    new Dictionary<TypedAuraExpression, TypedAuraExpression>(),
                    new Map(new AuraString(), new Int()),
                    1),
                1)
        });
        MakeAssertions(output, "map[string]int{}");
    }

    [Test]
    public void TestCompile_BoolLiteral()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedLiteral<bool>(true, new Bool(), 1),
                1)
        });
        MakeAssertions(output, "true");
    }

    [Test]
    public void TestCompile_NilLiteral()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedNil(1),
                1)
        });
        MakeAssertions(output, "nil");
    }

    [Test]
    public void TestCompile_CharLiteral()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedLiteral<char>('a', new AuraChar(), 1),
                1)
        });
        MakeAssertions(output, "'a'");
    }

    [Test]
    public void TestCompile_Logical()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedLogical(
                    new TypedLiteral<bool>(true, new Bool(), 1),
                    new Tok(TokType.And, "and", 1),
                    new TypedLiteral<bool>(false, new Bool(), 1),
                    new Bool(),
                    1),
                1)
        });
        MakeAssertions(output, "true && false");
    }

    [Test]
    public void TestCompile_Set()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedSet(
                    new TypedVariable(
                        new Tok(TokType.Identifier, "greeter", 1),
                        new Class(
                            "Greeter",
                            new List<string>
                            {
                                "name"
                            },
                            new List<ParamType>
                            {
                                new ParamType(new AuraString(), false)
                            },
                            new List<Function>()),
                        1),
                    new Tok(TokType.Identifier, "name", 1),
                    new TypedLiteral<string>("Bob", new AuraString(), 1),
                    new AuraString(),
                    1),
                1)
        });
        MakeAssertions(output, "greeter.name = \"Bob\"");
    }

    [Test]
    public void TestCompile_This()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedThis(
                    new Tok(TokType.This, "this", 1),
                    new Class(
                        "Greeter",
                        new List<string>(),
                        new List<ParamType>(),
                        new List<Function>()),
                    1),
                1)
        });
        MakeAssertions(output, "this");
    }

    [Test]
    public void TestCompile_Unary_Bang()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedUnary(
                    new Tok(TokType.Bang, "!", 1),
                    new TypedLiteral<bool>(true, new Bool(), 1),
                    new Bool(),
                    1),
                1)
        });
        MakeAssertions(output, "!true");
    }

    [Test]
    public void TestCompile_Unary_Minus()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedUnary(
                    new Tok(TokType.Minus, "-", 1),
                    new TypedLiteral<long>(5, new Int(), 1),
                    new Int(),
                    1),
                1)
        });
        MakeAssertions(output, "-5");
    }

    [Test]
    public void TestCompile_Variable()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedVariable(
                    new Tok(TokType.Identifier, "name", 1),
                    new AuraString(),
                    1),
                1)
        });
        MakeAssertions(output, "name");
    }

    [Test]
    public void TestCompile_Defer()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedDefer(
                new TypedCall(
                    new TypedVariable(
                        new Tok(TokType.Identifier, "f", 1),
                        new Function(
                            "f",
                            new AnonymousFunction(
                                new List<ParamType>(),
                                new Nil())),
                        1),
                    new List<TypedAuraExpression>(),
                    new Nil(),
                    1),
                1)
        });
        MakeAssertions(output, "defer f()");
    }

    [Test]
    public void TestCompile_For_EmptyBody()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedFor(
                new TypedLet(
                    new Tok(TokType.Identifier, "i", 1),
                    false,
                    true,
                    new TypedLiteral<long>(0, new Int(), 1),
                    1),
                new TypedLogical(
                    new TypedVariable(
                        new Tok(TokType.Identifier, "i", 1),
                        new Int(),
                        1),
                    new Tok(TokType.Less, "<", 1),
                    new TypedLiteral<long>(10, new Int(), 1),
                    new Bool(),
                    1),
                new List<TypedAuraStatement>(),
                1)
        });
        MakeAssertions(output, "for i := 0; i < 10; {}");
    }

    [Test]
    public void TestCompile_For()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedFor(
                new TypedLet(
                    new Tok(TokType.Identifier, "i", 1),
                    false,
                    true,
                    new TypedLiteral<long>(0, new Int(), 1),
                    1),
                new TypedLogical(
                    new TypedVariable(
                        new Tok(TokType.Identifier, "i", 1),
                        new Int(),
                        1),
                    new Tok(TokType.Less, "<", 1),
                    new TypedLiteral<long>(10, new Int(), 1),
                    new Bool(),
                    1),
                new List<TypedAuraStatement>
                {
                    new TypedLet(
                        new Tok(TokType.Identifier, "name", 2),
                        false,
                        false,
                        new TypedLiteral<string>("Bob", new AuraString(), 2),
                        2)
                },
                1)
        });
        MakeAssertions(output, "for i := 0; i < 10; {\nname := \"Bob\"\n}");
    }

    [Test]
    public void TestCompile_ForEach_EmptyBody()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedForEach(
                new Tok(TokType.Identifier, "name", 1),
                new TypedVariable(
                    new Tok(TokType.Identifier, "names", 1),
                    new AuraList(new AuraString()),
                    1),
                new List<TypedAuraStatement>(),
                1)
        });
        MakeAssertions(output, "for _, name := range names {}");
    }

    [Test]
    public void TestCompile_ForEach()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedForEach(
                new Tok(TokType.Identifier, "name", 1),
                new TypedVariable(
                    new Tok(TokType.Identifier, "names", 1),
                    new AuraList(new AuraString()),
                    1),
                new List<TypedAuraStatement>
                {
                    new TypedLet(
                        new Tok(TokType.Identifier, "i", 1),
                        true,
                        false,
                        new TypedLiteral<long>(5, new Int(), 1),
                        2)
                },
                1)
        });
        MakeAssertions(output, "for _, name := range names {\nvar i int = 5\n}");
    }

    [Test]
    public void TestCompile_NamedFunction_NoParams_NoReturnType_NoBody()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedNamedFunction(
                new Tok(TokType.Identifier, "f", 1),
                new List<Param>(),
                new TypedBlock(new List<TypedAuraStatement>(), new Nil(), 1),
                new Nil(),
                Visibility.Private,
                1)
        });
        MakeAssertions(output, "func f() {}");
    }

    [Test]
    public void TestCompile_AnonymousFunction_NoParams_NoReturnType_NoBody()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedExpressionStmt(
                new TypedAnonymousFunction(
                    new List<Param>(),
                    new TypedBlock(new List<TypedAuraStatement>(), new Nil(), 1),
                    new Nil(),
                    1),
                1)
        });
        MakeAssertions(output, "func() {}");
    }

    [Test]
    public void TestCompile_Let_Long()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedLet(
                new Tok(TokType.Identifier, "i", 1),
                true,
                false,
                new TypedLiteral<long>(5, new Int(), 1),
                1)
        });
        MakeAssertions(output, "var i int = 5");
    }

    [Test]
    public void TestCompile_Let_Short()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedLet(
                new Tok(TokType.Identifier, "i", 1),
                false,
                false,
                new TypedLiteral<long>(5, new Int(), 1),
                1)
        });
        MakeAssertions(output, "i := 5");
    }

    [Test]
    public void TestCompile_Mod()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedMod(
                new Tok(TokType.Identifier, "main", 1),
                1)
        });
        MakeAssertions(output, "package main");
    }

    [Test]
    public void TestCompile_Return_NoValue()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedReturn(null, 1)
        });
        MakeAssertions(output, "return");
    }

    [Test]
    public void TestCompile_Return()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedReturn(new TypedLiteral<long>(5, new Int(), 1), 1)
        });
        MakeAssertions(output, "return 5");
    }

    [Test]
    public void TestCompile_Class_NoParams_NoMethods()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new FullyTypedClass(
                new Tok(TokType.Identifier, "Greeter", 1),
                new List<Param>(),
                new List<TypedNamedFunction>(),
                Visibility.Public,
                1)
        });
        MakeAssertions(output, "type GREETER struct {}");
    }

    [Test]
    public void TestCompile_While_EmptyBody()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedWhile(
                new TypedLiteral<bool>(true, new Int(), 1),
                new List<TypedAuraStatement>(),
                1)
        });
        MakeAssertions(output, "for true {}");
    }

    [Test]
    public void TestCompile_Import_NoAlias()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedImport(
                new Tok(TokType.Identifier, "test_pkg", 1),
                null,
                1)
        });
        MakeAssertions(output, "import \"test_pkg\"");
    }

    [Test]
    public void TestCompile_Import_Alias()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedImport(
                new Tok(TokType.Identifier, "test_pkg", 1),
                new Tok(TokType.Identifier, "tp", 1),
                1)
        });
        MakeAssertions(output, "import tp \"test_pkg\"");
    }

    [Test]
    public void TestCompile_Import_StdlibPkg()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedImport(
                new Tok(TokType.Identifier, "aura/io", 1),
                null,
                1)
        });
        MakeAssertions(output, "import io \"test/stdlib/io\"");
    }

    [Test]
    public void TestCompile_Comment()
    {
        var output = ArrangeAndAct(new List<TypedAuraStatement>
        {
            new TypedComment(
                new Tok(TokType.Comment, "// this is a comment", 1),
                1)
        });
        MakeAssertions(output, "// this is a comment");
    }

    private string ArrangeAndAct(List<TypedAuraStatement> typedAst)
    {
        // Arrange
        var compiler = new AuraCompiler(typedAst, "test");
        // Act
        return compiler.Compile();
    }

    private void MakeAssertions(string output, string expected)
    {
        output = output.Trim();
        Assert.Multiple(() =>
        {
            Assert.That(output.Length, Is.EqualTo(expected.Length));
            Assert.That(output, Is.EqualTo(expected));
        });
    }
}