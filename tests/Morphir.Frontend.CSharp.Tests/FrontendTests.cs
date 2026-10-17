using Microsoft.CodeAnalysis;
using Morphir.Frontend.CSharp;
using Morphir.IR.Classic;
using Morphir.IR.Json;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.Tests;

[Trait("Delivery", "D1")]
public class FrontendTests
{
    [Fact] public void IndependentOrdinaryElmDistribution() => Support.Parity("fixtures/D1/expected/eligibility.json", Support.Read("fixtures/D1/csharp/Eligibility.cs"));
    [Fact] public void IndependentPrivateCodecDistribution() => Support.Parity("fixtures/D1/expected/private.json", Support.Read("fixtures/D1/csharp/Private.cs"), "Sample", "Sample.Business");
    [Fact]
    public void PrefixAndSourceDependence()
    {
        var code = Support.Read("fixtures/D1/csharp/Eligibility.cs");
        Support.Parity("fixtures/D1/expected/prefix.json", code.Replace("Example.D1", "Sample.Business"), "Sample", "Sample.Business");
        var original = Support.Json(code);
        foreach (var changed in new[] { code.Replace("EligibleLabel", "Label"), code.Replace("20 + 22", "21 + 22"), code.Replace("Age", "Years"), code.Replace(">= 18", ">= 19"), code.Replace("&&", "||") })
            Assert.NotNull(StructuralComparison.Compare(original, Support.Json(changed)));
    }
    [Fact]
    public void CompleteNamespaceSegmentsAndOverlaps()
    {
        var code = Support.Read("fixtures/D1/csharp/Selection.cs");
        var one = Support.Json(code, "Sample", "Sample.Business");
        Assert.Null(StructuralComparison.Compare(one, Support.Json(code, "Sample", "Sample.Business", "Sample.Business.Tax")));
        Support.Parity("fixtures/D1/expected/selection.json", code, "Sample", "Sample.Business");
    }
    [Fact]
    public void OutsideSourceRequiresExplicitSelector()
    {
        var code = Support.Read("fixtures/D1/csharp/Outside.cs");
        var bad = Support.Convert(code, "Sample", ["Sample.Business"]);
        Assert.Null(bad.Distribution); Assert.Contains(bad.Diagnostics, d => d.Code == "MCS2201" && d.Message.Contains("Shared.Customer") && d.Line > 0);
        Support.Parity("fixtures/D1/expected/outside.json", code, "Sample", "Sample.Business", "Shared");
    }
    [Theory]
    [InlineData("public static int F() => checked(1 + 2);", "MCS2001")]
    [InlineData("public static int F() { checked { return 1; } }", "MCS2001")]
    [InlineData("public static int F() { if (false) { checked { } } return 1; }", "MCS2001")]
    [InlineData("public static async System.Threading.Tasks.Task<int> F() => await System.Threading.Tasks.Task.FromResult(1);", "MCS2002")]
    [InlineData("public static unsafe int* F(int* p) => p;", "MCS2003")]
    [InlineData("public static int F(System.Span<int> p) => 1;", "MCS2003")]
    [InlineData("public static int F(int a,int b) => a + b;", "MCS2302")]
    [InlineData("public static int F(int a) => a / 2;", "MCS2106")]
    [InlineData("public static int F() => System.Environment.TickCount;", "MCS2107")]
    [InlineData("public static bool F(object a,object b) => object.ReferenceEquals(a,b);", "MCS2104")]
    [InlineData("public static int F(int[] a) => a[0];", "MCS2104")]
    [InlineData("public static int Field = 1;", "MCS2101")]
    [InlineData("public static int F(int a) => F(a);", "MCS2203")]
    [InlineData("public static int F(Customer c) => c.Age++;", "MCS0001")]
    public void UnsupportedSelectedFormsFailWholeConversion(string member, string code)
    {
        var r = Support.Convert("namespace Example.D1; public static class Rules { " + member + " }");
        Assert.False(r.Success); Assert.Null(r.Distribution); Assert.Contains(r.Diagnostics, d => d.Code == code && d.Line >= 1 && d.Column >= 1);
    }
    [Theory]
    [InlineData("UnsafeAddition", "MCS2302")]
    [InlineData("Checked", "MCS2001")]
    [InlineData("InputUpdate", "MCS2107")]
    [InlineData("Decimal", "MCS2104")]
    [InlineData("Effects", "MCS2107")]
    public void IsolatedRejectionFixtures(string file, string code)
    {
        var result = Support.Convert(Support.Read($"fixtures/D1/rejected/{file}.cs"));
        Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, d => d.Code == code);
    }
    [Fact]
    public void CheckedOperatorAndRefStructAreSyntaxRejections()
    {
        var r = Support.Convert("namespace Example.D1; public readonly struct N { public static N operator +(N a,N b)=>a; public static N operator checked +(N a,N b)=>a; }");
        Assert.Contains(r.Diagnostics, d => d.Code == "MCS2001");
        r = Support.Convert("namespace Example.D1; public ref struct R { }");
        Assert.Contains(r.Diagnostics, d => d.Code == "MCS2003");
    }
    [Fact]
    public void CheckedCompilationIsPreservedAndOnlyArithmeticRejected()
    {
        var source = "namespace Example.D1; public static class Rules { public static int F(int a)=>a+0; }";
        Assert.Contains(Support.Convert(source, check: true).Diagnostics, d => d.Code == "MCS2004");
        Support.Success(Support.Convert(source.Replace("a+0", "a"), check: true));
    }
    [Fact]
    public void CommentsStringsAndEscapedIdentifierAreNotCheckedSyntax()
    {
        Support.Success(Support.Convert("namespace Example.D1; // checked(1+2)\n public static class Rules { public static string F()=>\"checked\"; public static int G(int @checked)=>@checked+0; }"));
    }
    [Fact]
    public void ConfigurationAndCompilerErrorsReject()
    {
        var s = Support.Read("fixtures/D1/csharp/Eligibility.cs");
        Assert.Contains(Support.Convert(s, "").Diagnostics, d => d.Code == "MCS1001");
        Assert.Contains(Support.Convert(s, platform: Platform.X86).Diagnostics, d => d.Code == "MCS1003");
        Assert.Contains(Support.Convert(s, selectors: ["Example.D"]).Diagnostics, d => d.Code == "MCS1006");
        Assert.Contains(Support.Convert(s + " bad code").Diagnostics, d => d.Code == "MCS0001");
    }
    [Fact]
    public void CollisionsIncludeBothLocations()
    {
        var r = Support.Convert("namespace Example.D1; public readonly record struct Foo_Bar(int A); public readonly record struct FooBar(int B);");
        Assert.Equal(2, r.Diagnostics.Count(d => d.Code == "MCS1004"));
    }
    [Fact]
    public void PartialAndNestedOwnersStayDistinct()
    {
        const string s = "namespace Example.D1; public static partial class Outer { public readonly record struct Data(int Age); public static int F()=>1; } public static partial class Outer { private static int G()=>2; public static class Inner { public static int F()=>3; } }";
        var lib = Support.Success(Support.Convert(s));
        var outer = lib.Definition.Modules.Single(e => e.Key.Key == Morphir.IR.Classic.Path.From("D1.Outer").Key).Value.Value;
        Assert.Single(outer.Types); Assert.Equal(2, outer.Values.Count);
        Assert.Contains(lib.Definition.Modules, e => e.Key.Key == Morphir.IR.Classic.Path.From("D1.Outer.Inner").Key);
        Assert.DoesNotContain(lib.Definition.Modules, e => e.Key.Key.Contains("data", StringComparison.Ordinal));
    }
}
