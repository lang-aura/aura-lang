using AuraLang.AST;
using AuraLang.Exceptions.TypeChecker;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.TypeChecker;
using AuraLang.Types;
using Moq;
using Newtonsoft.Json;
using AuraChar = AuraLang.Types.Char;
using AuraList = AuraLang.Types.List;
using AuraString = AuraLang.Types.String;

namespace AuraLang.Test.TypeChecker;

public class TypeCheckerTest
{
    private readonly Mock<IVariableStore> _variableStore = new();
    private readonly Mock<IEnclosingClassStore> _enclosingClassStore = new();
    private readonly Mock<ICurrentModuleStore> _currentModuleStore = new();
    private readonly Mock<EnclosingExpressionStore> _enclosingExprStore = new();

    [SetUp]
    public void Setup()
    {
        _enclosingExprStore.CallBase = true;
    }
    
    [Test]
    public void TestTypeCheck_Assignment()
    {
        _variableStore.Setup(v => v.Find("i", It.IsAny<string>())).Returns(new Local("i", new Int(), 1, "main"));
        
        var typedAst = ArrangeAndAct(
            new List<UntypedAuraStatement>
            {
                new UntypedExpressionStmt(
                    new UntypedAssignment(
                        new Tok(TokType.Identifier, "i", 1),
                        new UntypedIntLiteral(6, 1),
                        1),
                    1)
            });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedAssignment(
                new Tok(TokType.Identifier, "i", 1),
                new TypedLiteral<long>(6, new Int(), 1),
                new Int(),
                1),
            1));
    }

    [Test]
    public void TestTypeCheck_Binary()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedBinary(
                    new UntypedBoolLiteral(true, 1),
                    new Tok(TokType.And, "and", 1),
                    new UntypedBoolLiteral(false, 1),
                    1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedBinary(
                new TypedLiteral<bool>(true, new Bool(), 1),
                new Tok(TokType.And, "and", 1),
                new TypedLiteral<bool>(false, new Bool(), 1),
                new Int(),
                1),
            1));
    }

    [Test]
    public void TestTypeCheck_Block_EmptyBody()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedBlock(
                    new List<UntypedAuraStatement>(),
                    1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedBlock(
                new List<TypedAuraStatement>(),
                new Nil(),
                1),
            1));
    }

    [Test]
    public void TestTypeCheck_Block()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedBlock(
                    new List<UntypedAuraStatement>
                    {
                        new UntypedLet(
                            new Tok(TokType.Identifier, "i", 2),
                            new Int(),
                            false,
                            new UntypedIntLiteral(5, 2),
                            2)
                    },
                    1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedBlock(
                new List<TypedAuraStatement>
                {
                    new TypedLet(
                        new Tok(TokType.Identifier, "i", 2),
                        true,
                        false,
                        new TypedLiteral<long>(5, new Int(), 2),
                        2)
                },
                new Nil(),
                1),
            1));
    }

    [Test]
    public void TestTypeCheck_Call_NoArgs()
    {
        _currentModuleStore.Setup(cms => cms.GetName())
            .Returns("main");
        _variableStore.Setup(v => v.Find("f", "main")).Returns(new Local(
            "f",
            new Function(
                "f",
                new AnonymousFunction(
                    new List<ParamType>(),
                    new Nil())),
            1,
            "main"));
        
        var typedAst = ArrangeAndAct(
            new List<UntypedAuraStatement>
            {
                new UntypedExpressionStmt(
                    new UntypedCall(
                        new UntypedVariable(new Tok(TokType.Identifier, "f", 1), 1),
                        new List<UntypedAuraExpression>(),
                        1),
                    1)
            });
        MakeAssertions(typedAst,  new TypedExpressionStmt(
            new TypedCall(
                new TypedVariable(
                    new Tok(TokType.Identifier, "f", 1),
                    new Function("f", new AnonymousFunction(new List<ParamType>(), new Nil())),
                    1),
                new List<TypedAuraExpression>(),
                new Nil(),
                1),
            1)) ;
    }

    [Test]
    public void TestTypeCheck_Get()
    {
        _variableStore.Setup(v => v.Find("greeter", "main")).Returns(
            new Local(
                "greeter",
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
                1,
                "main"));
            
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedGet(
                    new UntypedVariable(
                        new Tok(TokType.Identifier, "greeter", 1),
                        1),
                    new Tok(TokType.Identifier, "name", 1),
                    1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
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
            1));
    }

    [Test]
    public void TestTypeCheck_GetIndex()
    {
        _variableStore.Setup(v => v.Find("names", "main"))
            .Returns(new Local(
                "names",
                new AuraList(new AuraString()),
                1,
                "main"));
        
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedGetIndex(
                    new UntypedVariable(
                        new Tok(TokType.Identifier, "names", 1),
                        1),
                    new UntypedIntLiteral(0, 1),
                    1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedGetIndex(
                new TypedVariable(
                    new Tok(TokType.Identifier, "names", 1),
                    new AuraList(new AuraString()),
                    1),
                new TypedLiteral<long>(0, new Int(), 1),
                new AuraString(),
                1),
            1));
    }

    [Test]
    public void TestTypeCheck_GetIndexRange()
    {
        _variableStore.Setup(v => v.Find("names", "main"))
            .Returns(new Local(
                "names",
                new AuraList(new AuraString()),
                1,
                "main"));
        
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedGetIndexRange(
                    new UntypedVariable(
                        new Tok(TokType.Identifier, "names", 1),
                        1),
                    new UntypedIntLiteral(0, 1),
                    new UntypedIntLiteral(2, 1),
                    1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedGetIndexRange(
                new TypedVariable(
                    new Tok(TokType.Identifier, "names", 1),
                    new AuraList(new AuraString()),
                    1),
                new TypedLiteral<long>(0, new Int(), 1),
                new TypedLiteral<long>(2, new Int(), 1),
                new AuraList(new AuraString()),
                1),
            1));
    }

    [Test]
    public void TestTypeCheck_Grouping()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedGrouping(
                    new UntypedStringLiteral("Hello world", 1),
                    1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedGrouping(
                new TypedLiteral<string>("Hello world",  new AuraString(), 1),
                new AuraString(),
                1),
            1));
    }

    [Test]
    public void TestTypeCheck_If_EmptyThenBranch()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedIf(
                    new UntypedBoolLiteral(true, 1),
                    new UntypedBlock(
                        new List<UntypedAuraStatement>(),
                        1),
                    null,
                    1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedIf(
                new TypedLiteral<bool>(true, new Bool(), 1),
                new TypedBlock(
                    new List<TypedAuraStatement>(),
                    new Nil(),
                    1),
                null,
                new Nil(),
                1),
            1));
    }

    [Test]
    public void TestTypeCheck_IntLiteral()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedIntLiteral(5, 1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedLiteral<long>(5, new Int(), 1),
            1));
    }

    [Test]
    public void TestTypeCheck_FloatLiteral()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedFloatLiteral(5.1, 1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedLiteral<double>(5.1, new Float(), 1),
            1));
    }

    [Test]
    public void TestTypeCheck_StringLiteral()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedStringLiteral("Hello world", 1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedLiteral<string>("Hello world", new AuraString(), 1),
            1));
    }

    [Test]
    public void TestTypeCheck_ListLiteral()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedListLiteral<UntypedAuraExpression>(
                    new List<UntypedAuraExpression>
                    {
                        new UntypedIntLiteral(1, 1)
                    },
                    1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedLiteral<List<TypedAuraExpression>>(
                new List<TypedAuraExpression>
                {
                    new TypedLiteral<long>(1, new Int(), 1)
                },
                new AuraList(new Int()),
                1),
            1));
    }

    [Test]
    public void TestTypeCheck_MapLiteral()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedMapLiteral(
                    new Dictionary<UntypedAuraExpression, UntypedAuraExpression>
                    {
                        { new UntypedStringLiteral("Hello", 1), new UntypedIntLiteral(1, 1) }
                    },
                    new AuraString(),
                    new Int(),
                    1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedLiteral<Dictionary<TypedAuraExpression, TypedAuraExpression>>(
                new Dictionary<TypedAuraExpression, TypedAuraExpression>
                {
                    {new TypedLiteral<string>("Hello", new AuraString(), 1), new TypedLiteral<long>(1, new Int(), 1)}
                },
                new Map(new AuraString(), new Int()),
                1),
            1));
    }

    [Test]
    public void TestTypeCheck_BoolLiteral()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedBoolLiteral(true, 1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedLiteral<bool>(true, new Bool(), 1),
            1));
    }

    [Test]
    public void TestTypeCheck_NilLiteral()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedNil(1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedNil(1),
            1));
    }

    [Test]
    public void TestTypeCheck_CharLiteral()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedCharLiteral('a', 1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedLiteral<char>('a', new AuraChar(), 1),
            1));
    }

    [Test]
    public void TestTypeCheck_Logical()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedLogical(
                    new UntypedIntLiteral(5, 1),
                    new Tok(TokType.Less, "<", 1),
                    new UntypedIntLiteral(10, 1),
                    1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedLogical(
                new TypedLiteral<long>(5, new Int(), 1),
                new Tok(TokType.Less, "<", 1),
                new TypedLiteral<long>(10, new Int(), 1),
                new Bool(),
                1),
            1));
    }

    [Test]
    public void TestTypeCheck_Set()
    {
        _variableStore.Setup(v => v.Find("greeter", "main"))
            .Returns(new Local(
                "greeter",
                new Class(
                    "Greeter",
                    new List<string>
                    {
                        "name"
                    },
                    new List<ParamType>
                    {
                        new(new AuraString(), false)
                    },
                    new List<Function>()),
                1,
                "main"));
        
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedSet(
                    new UntypedVariable(
                        new Tok(TokType.Identifier, "greeter", 1),
                        1),
                    new Tok(TokType.Identifier, "name", 1),
                    new UntypedStringLiteral("Bob", 1),
                    1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
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
            1));
    }

    [Test]
    public void TestTypeCheck_This()
    {
        _enclosingClassStore.Setup(ecs => ecs.Peek())
            .Returns(new PartiallyTypedClass(
                new Tok(TokType.Identifier, "Greeter", 1),
                new List<Param>(),
                new List<PartiallyTypedFunction>(),
                Visibility.Public,
                new Class(
                    "Greeter",
                    new List<string>(),
                    new List<ParamType>(),
                    new List<Function>()),
                1));
        
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedThis(
                    new Tok(TokType.This, "this", 1),
                    1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedThis(
                new Tok(TokType.This, "this", 1),
                new Class(
                    "Greeter",
                    new List<string>(),
                    new List<ParamType>(),
                    new List<Function>()),
                1),
            1));
    }

    [Test]
    public void TestTypeCheck_Unary_Bang()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedUnary(
                    new Tok(TokType.Bang, "!", 1),
                    new UntypedBoolLiteral(true, 1),
                    1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedUnary(
                new Tok(TokType.Bang, "!", 1),
                new TypedLiteral<bool>(true, new Bool(), 1),
                new Bool(),
                1),
            1));
    }

    [Test]
    public void TestTypeCheck_Unary_Minus()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedUnary(
                    new Tok(TokType.Minus, "-", 1),
                    new UntypedIntLiteral(5, 1),
                    1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedUnary(
                new Tok(TokType.Minus, "-", 1),
                new TypedLiteral<long>(5, new Int(), 1),
                new Int(),
                1),
            1));
    }

    [Test]
    public void TestTypeCheck_Variable()
    {
        _variableStore.Setup(v => v.Find("name", "main"))
            .Returns(new Local(
                "name",
                new AuraString(),
                1,
                "main"));
        
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedVariable(
                    new Tok(TokType.Identifier, "name", 1),
                    1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedVariable(
                new Tok(TokType.Identifier, "name", 1),
                new AuraString(),
                1),
            1));
    }

    [Test]
    public void TestTypeCheck_Defer()
    {
        _currentModuleStore.Setup(cms => cms.GetName())
            .Returns("main");
        _variableStore.Setup(v => v.Find("f", "main"))
            .Returns(new Local(
                "f",
                new Function(
                    "f",
                    new AnonymousFunction(
                        new List<ParamType>(),
                        new Nil())),
                1,
                "main"));
        
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedDefer(
                new UntypedCall(
                    new UntypedVariable(
                        new Tok(TokType.Identifier, "f", 1),
                        1),
                    new List<UntypedAuraExpression>(),
                    1),
                1)
        });
        MakeAssertions(typedAst, new TypedDefer(
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
            1));
    }

    [Test]
    public void TestTypeCheck_For_EmptyBody()
    {
        _currentModuleStore.Setup(cms => cms.GetName())
            .Returns("main");
        _variableStore.Setup(v => v.Find("i", "main"))
            .Returns(new Local(
                "i",
                new Int(),
                1,
                "main"));
        
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedFor(
                new UntypedLet(
                    new Tok(TokType.Identifier, "i", 1),
                    new None(),
                    false,
                    new UntypedIntLiteral(0, 1),
                    1),
                new UntypedLogical(
                    new UntypedVariable(
                        new Tok(TokType.Identifier, "i", 1),
                        1),
                    new Tok(TokType.Less, "<", 1),
                    new UntypedIntLiteral(10, 1),
                    1),
                new List<UntypedAuraStatement>(),
                1)
        });
        MakeAssertions(typedAst, new TypedFor(
            new TypedLet(
                new Tok(TokType.Identifier, "i", 1),
                false,
                false,
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
            1));
    }

    [Test]
    public void TestTypeCheck_ForEach_EmptyBody()
    {
        _currentModuleStore.Setup(cms => cms.GetName())
            .Returns("main");
        _variableStore.Setup(v => v.Find("names", "main"))
            .Returns(new Local(
                "names",
                new AuraList(new AuraString()),
                1,
                "main"));
        
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedForEach(
                new Tok(TokType.Identifier, "name", 1),
                new UntypedVariable(
                    new Tok(TokType.Identifier, "names", 1),
                    1),
                new List<UntypedAuraStatement>(),
                1)
        });
        MakeAssertions(typedAst, new TypedForEach(
            new Tok(TokType.Identifier, "name", 1),
            new TypedVariable(
                new Tok(TokType.Identifier, "names", 1),
                new AuraList(new AuraString()),
                1),
            new List<TypedAuraStatement>(),
            1));
    }

    [Test]
    public void TestTypeCheck_NamedFunction_NoParams_NoReturnType_NoBody()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedNamedFunction(
                new Tok(TokType.Identifier, "f", 1),
                new List<Param>(),
                new UntypedBlock(new List<UntypedAuraStatement>(), 1),
                new Nil(),
                Visibility.Public,
                1)
        });
        MakeAssertions(typedAst, new TypedNamedFunction(
            new Tok(TokType.Identifier, "f", 1),
            new List<Param>(),
            new TypedBlock(new List<TypedAuraStatement>(), new Nil(), 1),
            new Nil(),
            Visibility.Public,
            1));
    }

    [Test]
    public void TestTypeCheck_AnonymousFunction_NoParams_NoReturnType_NoBody()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedExpressionStmt(
                new UntypedAnonymousFunction(
                    new List<Param>(),
                    new UntypedBlock(new List<UntypedAuraStatement>(), 1),
                    new Nil(),
                    1),
                1)
        });
        MakeAssertions(typedAst, new TypedExpressionStmt(
            new TypedAnonymousFunction(
                new List<Param>(),
                new TypedBlock(
                    new List<TypedAuraStatement>(),
                    new Nil(),
                    1),
                new Nil(),
                1),
            1));
    }

    [Test]
    public void TestTypeCheck_Let_Long()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedLet(
                new Tok(TokType.Identifier, "i", 1),
                new Int(),
                false,
                new UntypedIntLiteral(1, 1),
                1)
        });
        MakeAssertions(typedAst, new TypedLet(
            new Tok(TokType.Identifier, "i", 1),
            true,
            false,
            new TypedLiteral<long>(1, new Int(), 1),
            1));
    }

    [Test]
    public void TestTypeCheck_Long_Short()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedLet(
                new Tok(TokType.Identifier, "i", 1),
                new None(),
                false,
                new UntypedIntLiteral(1, 1),
                1)
        });
        MakeAssertions(typedAst, new TypedLet(
            new Tok(TokType.Identifier, "i", 1),
            false,
            false,
            new TypedLiteral<long>(1, new Int(), 1),
            1));
    }

    [Test]
    public void TestTypeCheck_Return_NoValue()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedReturn(
                null,
                1)
        });
        MakeAssertions(typedAst, new TypedReturn(
            null,
            1));
    }

    [Test]
    public void TestTypeCheck_Return()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedReturn(
                new UntypedIntLiteral(5, 1),
                1)
        });
        MakeAssertions(typedAst, new TypedReturn(
            new TypedLiteral<long>(5, new Int(), 1),
            1));
    }

    [Test]
    public void TestTypeCheck_Class_NoParams_NoMethods()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedClass(
                new Tok(TokType.Identifier, "Greeter", 1),
                new List<Param>(),
                new List<UntypedNamedFunction>(),
                Visibility.Private,
                1)
        });
        MakeAssertions(typedAst, new FullyTypedClass(
            new Tok(TokType.Identifier, "Greeter", 1),
            new List<Param>(),
            new List<TypedNamedFunction>(),
            Visibility.Private,
            1));
    }

    [Test]
    public void TestTypeCheck_While_EmptyBody()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedWhile(
                new UntypedBoolLiteral(true, 1),
                new List<UntypedAuraStatement>(),
                1)
        });
        MakeAssertions(typedAst, new TypedWhile(
            new TypedLiteral<bool>(true, new Bool(), 1),
            new List<TypedAuraStatement>(),
            1));
    }

    [Test]
    public void TestTypeCheck_Import_NoAlias()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedImport(
                new Tok(TokType.Identifier, "test_pkg", 1),
                null,
                1)
        });
        MakeAssertions(typedAst, new TypedImport(
            new Tok(TokType.Identifier, "test_pkg", 1),
            null,
            1));
    }

    [Test]
    public void TestTypeCheck_Comment()
    {
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedComment(
                new Tok(TokType.Comment, "// this is a comment", 1),
                1)
        });
        MakeAssertions(typedAst, new TypedComment(
            new Tok(TokType.Comment, "// this is a comment", 1),
            1));
    }

    [Test]
    public void TestTypeCheck_Yield()
    {
        _enclosingExprStore.Setup(expr => expr.Peek()).Returns(new UntypedBlock(new List<UntypedAuraStatement>(), 1));
                     
        var typedAst = ArrangeAndAct(new List<UntypedAuraStatement>
        {
            new UntypedYield(
                new UntypedIntLiteral(5, 1),
                1)
        });
        MakeAssertions(typedAst, new TypedYield(
            new TypedLiteral<long>(5, new Int(), 1),
            1));
    }

    [Test]
    public void TestTypeCheck_Yield_Invalid()
    {
        _enclosingExprStore.Setup(expr => expr.Peek()).Returns(new UntypedNil(1));
        
        var typeChecker = new AuraTypeChecker(_variableStore.Object, _enclosingClassStore.Object,
            _currentModuleStore.Object, _enclosingExprStore.Object);
        Assert.Throws<TypeCheckerExceptionContainer>(() => typeChecker.CheckTypes(AddModStmtIfNecessary(new List<UntypedAuraStatement>
            {
                new UntypedYield(new UntypedIntLiteral(5, 1), 1)
            })));
    }

    private List<TypedAuraStatement> ArrangeAndAct(List<UntypedAuraStatement> untypedAst)
        => new AuraTypeChecker(_variableStore.Object, _enclosingClassStore.Object, _currentModuleStore.Object, _enclosingExprStore.Object)
            .CheckTypes(AddModStmtIfNecessary(untypedAst));

    private List<UntypedAuraStatement> AddModStmtIfNecessary(List<UntypedAuraStatement> untypedAst)
    {
        if (untypedAst.Count > 0 && untypedAst[0] is not UntypedMod)
        {
            var untypedAstWithMod = new List<UntypedAuraStatement>
            {
                new UntypedMod(
                    new Tok(TokType.Identifier, "main", 1),
                    1)
            };
            untypedAstWithMod.AddRange(untypedAst);
            return untypedAstWithMod;
        }

        return untypedAst;
    }
    
    private void MakeAssertions(List<TypedAuraStatement> typedAst, TypedAuraStatement expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(typedAst, Is.Not.Null);
            Assert.That(typedAst, Has.Count.EqualTo(2));

            var expectedJson = JsonConvert.SerializeObject(expected);
            var actualJson = JsonConvert.SerializeObject(typedAst[1]);
            Assert.That(actualJson, Is.EqualTo(expectedJson));
        });
    }
}