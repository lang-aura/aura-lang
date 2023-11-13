using AuraLang.AST;
using AuraLang.Shared;
using AuraLang.Token;
using AuraLang.TypeChecker;
using AuraLang.Types;
using Newtonsoft.Json;

namespace AuraLang.Test.TypeChecker;

public class TypeCheckerTest
{
    [Test]
    public void TestTypeCheck_Assignment()
    {
        var typedAst = ArrangeAndActWithGlobal(
            new List<UntypedAuraStatement>
            {
                new UntypedLet(
                    new Tok(TokType.Identifier, "i", 1),
                    new Int(),
                    true,
                    new UntypedIntLiteral(5, 1),
                    1)
            },
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
    public void TestTypeCheck_Call_NoArgs()
    {
        var typedAst = ArrangeAndActWithGlobal(
            new List<UntypedAuraStatement>
            {
                new UntypedNamedFunction(
                    new Tok(TokType.Identifier, "f", 1),
                    new List<Param>(),
                    new UntypedBlock(new List<UntypedAuraStatement>(), 1),
                    new Nil(),
                    Visibility.Private,
                    1)
            },
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

    private List<TypedAuraStatement> ArrangeAndAct(List<UntypedAuraStatement> untypedAst) => new AuraTypeChecker().CheckTypes(AddModStmtIfNecessary(untypedAst));

    private List<TypedAuraStatement> ArrangeAndActWithGlobal(List<UntypedAuraStatement> global,
        List<UntypedAuraStatement> untypedAst)
    {
        var typeChecker = new AuraTypeChecker();
        typeChecker.CheckTypes(AddModStmtIfNecessary(global));
        return typeChecker.CheckTypes(AddModStmtIfNecessary(untypedAst));
    }

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