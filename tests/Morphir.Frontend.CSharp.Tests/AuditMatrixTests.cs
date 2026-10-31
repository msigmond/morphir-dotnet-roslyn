using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Morphir.IR.Classic;
using Morphir.IR.Json;
using Morphir.Tests;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp.Tests;

[Trait("Contract", "SourceStructure")]
[Trait("Delivery", "D9")]
public class AuditMatrixTests
{
    // Written independently from the emitter. Each ID is an approved audit obligation.
    public static IEnumerable<object[]> Cases()
    {
        object[] Case(string id, string members, string syntax, string? projection = null, string data = "") => [id, "using System;using System.Numerics;namespace Example.Audit;" + data + "public static class Rules{" + members + "}", syntax, projection ?? ""];
        yield return Case("N1", "public static string F(string s)=>s;", "PredefinedType");
        yield return Case("N2", "public static string? F()=>\"x\";", "NullableType");
        yield return Case("N3", "public static int F(int? x)=>x.Value;", "SimpleMemberAccessExpression", "MemberAccess");
        yield return Case("N4", "public static int F(int? x){if(x.HasValue)return x.Value;return 0;}", "IfStatement", "If");
        yield return Case("N5", "public static int? F(string? s)=>s?.Length??2;", "ConditionalAccessExpression", "ConditionalAccess");
        yield return Case("N6", "public static int? F(int? a,int? b)=>a*b;", "MultiplyExpression", "Binary");
        yield return Case("N7", "public static int? F(bool b)=>b?1:null;", "ConditionalExpression", "Null");
        yield return Case("A1", "public static int F(int a,int b)=>a*b+a;", "MultiplyExpression");
        yield return Case("A2", "public static ulong F()=>18446744073709551615UL;", "NumericLiteralExpression");
        yield return Case("A3", "public static int F(int a,int b)=>a>>>b;", "UnsignedRightShiftExpression", "Binary");
        yield return Case("A4", "public static int F(int a,int b)=>a/b;", "DivideExpression");
        yield return Case("A5", "public static BigInteger F(Complex a)=>(BigInteger)a;", "CastExpression", "Cast");
        yield return Case("A6", "public static long F(int a)=>a;", "IdentifierName");
        yield return Case("A7", "public static float F(float a,float b)=>a*b;", "MultiplyExpression");
        yield return Case("A8", "public static double F()=>double.NaN;", "SimpleMemberAccessExpression", "NamedReference");
        yield return Case("A9", "public static Complex F(Complex a,Complex b)=>a*b;", "MultiplyExpression", "Binary");
        yield return Case("A10", "public static Half F(Half a)=>a;", "IdentifierName");
        yield return Case("D1", "public static decimal F()=>0.10m;", "NumericLiteralExpression");
        yield return Case("D2", "public static decimal F(decimal a,decimal b)=>a*b;", "MultiplyExpression");
        yield return Case("D3", "public static decimal F(decimal a,decimal b)=>a/b;", "DivideExpression", "Binary");
        yield return Case("D4", "public static double F(decimal a)=>(double)a;", "CastExpression", "Cast");
        yield return Case("D5", "public static decimal F(double a)=>(decimal)a;", "CastExpression", "Cast");
        yield return Case("D6", "public static decimal F(decimal a,int digits)=>decimal.Round(a,digits);", "InvocationExpression", "Invocation");
        yield return Case("D7", "public static byte F(decimal a)=>a.Scale;", "SimpleMemberAccessExpression");
        yield return Case("C1", "public static int T(int a=2,int b=3)=>a;public static int F()=>T(b:4,a:5);", "NameColon", "Argument");
        yield return Case("C2", "public static int T(params int[] a)=>1;public static int F()=>T(1,2);", "ParamsKeyword", "Invocation");
        yield return Case("C3", "public static int F(params int[] a)=>a[5];", "ElementAccessExpression", "Index");
        yield return Case("C4", "public static int T()=>1;public static Func<int> F()=>T;", "GenericName", "MethodReference");
        yield return Case("C5", "public static int F(Func<int> callback)=>callback();", "InvocationExpression", "Invocation");
        yield return Case("C6", "public static int F(int a){int L()=>a;return L();}", "LocalFunctionStatement", "LocalFunction");
        yield return Case("C7", "public static int F()=>Environment.TickCount;", "SimpleMemberAccessExpression", "NamedReference");
        yield return Case("C8", "public static Cell T()=>new();public static Cell F()=>T();", "InvocationExpression", "Invocation", "public class Cell{}");
        yield return Case("C9", "public static int F(int x)=>F(x);", "InvocationExpression", "Invocation");
        yield return Case("C10", "public static int F(Cell c)=>c.Read();", "InvocationExpression", "MethodReference", "public class Cell{public int Read()=>1;}");
        yield return Case("O1", "public static int F()=>1;", "RecordStructDeclaration", null, "public readonly record struct Cell(int X);");
        yield return Case("O2", "public static int F()=>1;", "ParameterList", null, "public class Cell(int x){public int Read()=>x;}");
        yield return Case("O3", "public static Cell F()=>new Cell(2){X=3};", "ObjectCreationExpression", "New", "public class Cell{public int X;public Cell(int x){X=x;}}");
        yield return Case("O4", "public static int F(Cell c)=>c.P;", "GetAccessorDeclaration", "MemberAccess", "public class Cell{public int X;public int P{get=>X;set=>X=value;}}");
        yield return Case("O5", "public static int F(Cell c){var a=c;a.X=3;return c.X;}", "SimpleAssignmentExpression", "Assignment", "public class Cell{public int X;}");
        yield return Case("O6", "public static int F(Cell c){c.X=2;return c.X;}", "SimpleAssignmentExpression", "Assignment", "public class Cell{public int X;}");
        yield return Case("O7", "public static Cell F(Cell c){c.Next=c;return c;}", "SimpleAssignmentExpression", "Assignment", "public class Cell{public Cell? Next;}");
        yield return Case("O8", "public static Cell F(Cell c)=>c with{X=3};", "WithExpression", "With", "public record Cell(int X);");
        yield return Case("O9", "public static bool F(Cell a,Cell b)=>a==b;", "EqualsExpression", "Binary", "public record Cell(double X);");
        yield return Case("O10", "public static bool F(Cell a,Cell b)=>ReferenceEquals(a,b);", "InvocationExpression", "Invocation", "public class Cell{}");
        yield return Case("O11", "public static Cell F(Cell a,Cell b)=>a+b;", "OperatorDeclaration", "Binary", "public struct Cell{public static Cell operator +(Cell a,Cell b)=>a;}");
        yield return Case("T1", "public static Flags F(Flags a,Flags b)=>a|b;", "BitwiseOrExpression", "Binary", "[Flags]public enum Flags{A=1,B=2}");
        yield return Case("T2", "public static int F()=>1;", "BitwiseOrExpression", "Binary", "public enum Flags{A=1,B=2,All=A|B}");
        yield return Case("T3", "public static (int Code,bool Flag) F((int Code,bool Flag) t)=>t;", "TupleType");
        yield return Case("T4", "public static Tuple<int,int> F()=>Tuple.Create(1,2);", "InvocationExpression", "Invocation");
        yield return Case("T5", "public static bool F((int,int) a,(int,int) b)=>a==b;", "EqualsExpression", "Binary");
        yield return Case("T6", "public static int F(){var (a,_)= (1,2);return a;}", "DeclarationExpression", "Assignment");
        yield return Case("S1", "public static int F(int a){a=2;return a;}", "ReturnStatement", "Return");
        yield return Case("S2", "public static bool F(bool a,bool b)=>a&&b;", "LogicalAndExpression", "Binary");
        yield return Case("S3", "public static int F(){if(false)return 2;return 1+3;}", "IfStatement", "If");
        yield return Case("S4", "public static int F()=>default(int);", "DefaultExpression", "Default");
        yield return Case("S5", "public static int F(){int a;;a=2;return a;}", "EmptyStatement", "EmptyStatement");
        yield return Case("S6", "public static int F(int @checked)=>(int)(@checked);", "CastExpression", "Cast");
        yield return Case("G1", "public static int F(int a)=>checked(a+1);", "CheckedExpression", "checked");
        yield return Case("G2", "public static int Storage;public static int F(){Storage=1;return Environment.TickCount;}", "SimpleAssignmentExpression", "Assignment");
        yield return Case("G3", "public static int F()=>1;", "MethodDeclaration"); // Negative delivery gates have separate assertions.
        yield return Case("G4", "public static decimal F(decimal a,decimal b)=>a+b;", "AddExpression");
        yield return Case("G5", "public static int F(int? a){return (int)a;}", "CastExpression", "Cast");
    }
    private static IEnumerable<SourceSyntax> Syntax(SourceSyntax source) => source.Children.SelectMany(Syntax).Prepend(source);
    private static IEnumerable<V> Values(V value)
    {
        yield return value;
        var children = value switch
        {
            SourceValue<Unit, IrType<Unit>> s => s.Children,
            ApplyValue<Unit, IrType<Unit>> a => (IReadOnlyList<V>)[a.Function,a.Argument],
            FieldValue<Unit, IrType<Unit>> f => [f.Subject],
            IfValue<Unit, IrType<Unit>> i => [i.Condition,i.Then,i.Else],
            TupleValue<Unit, IrType<Unit>> t => t.Elements,
            _ => []
        };
        foreach (var child in children) foreach (var v in Values(child)) yield return v;
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void ApprovedCaseRetainsStructureWithoutRuntimeExpansion(string id, string code, string syntaxKind, string projection)
    {
        Assert.NotEmpty(id);
        var compilation = Support.Compile(code);
        var lib = Support.Success(Frontend.Convert(compilation, new("Example", ["Example.Audit"], OutputFormat: FrontendOutputFormat.Source)));
        var syntax = lib.Source!.Declarations.SelectMany(Syntax).ToArray();
        Assert.Contains(syntax, n => n.Kind == syntaxKind);
        var roots = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Where(n => n.Parent is BaseNamespaceDeclarationSyntax).ToArray();
        Assert.Equal(roots.Length, lib.Source.Declarations.Count);
        for (int i = 0; i < roots.Length; i++) Assert.Equal(roots[i].DescendantTokens().Select(t => t.Text), Syntax(lib.Source.Declarations[i]).Where(n => n.Token is not null).Select(n => n.Token!));
        var values = lib.Definition.Modules.SelectMany(m => m.Value.Value.Values).SelectMany(v => Values(v.Value.Value.Value.Body)).ToArray();
        if (projection.Length != 0) Assert.Contains(values, v => v is SourceValue<Unit, IrType<Unit>> s && s.Kind == projection);
        Assert.DoesNotContain(values, v => v is LetValue<Unit, IrType<Unit>> or MatchValue<Unit, IrType<Unit>> or UpdateRecordValue<Unit, IrType<Unit>> or LambdaValue<Unit, IrType<Unit>>);
        Assert.DoesNotContain(lib.Definition.Modules, m => m.Key.Key.Contains("compat", StringComparison.Ordinal));
        Assert.DoesNotContain("fromSmall", Codec.Encode(lib), StringComparison.Ordinal);
    }
    [Fact]
    public void MatrixCoversEveryApprovedCaseExactlyOnce()
    {
        var approved = Regex.Matches(Support.Read("docs/reviews/source-structure-audit.md"), @"(?m)^\| ([NA DCOTSG]+\d+) \|").Select(m => m.Groups[1].Value).Order(StringComparer.Ordinal);
        var cases = Cases().Select(c => (string)c[0]).ToArray();
        Assert.Equal(cases.Length, cases.Distinct().Count()); Assert.Equal(approved, cases.Order(StringComparer.Ordinal));
    }
}
