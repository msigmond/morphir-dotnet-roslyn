using Microsoft.CodeAnalysis;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.Tests;

public class SourceStructureTests
{
    private const string Header = "using System;namespace Example.Structure;";
    [Theory]
    [InlineData("public class Box<T> where T:class{}", "MCS3100")]
    [InlineData("public static class Rules{public static int F(int[] a)=>a[0];}", "MCS3101")]
    [InlineData("public static class Rules{public static int F(){int[] a=null;return 1;}}", "MCS3101")]
    [InlineData("public class Cell:System.IDisposable{public void Dispose(){}}", "MCS3100")]
    [InlineData("public static class Rules{public static int F(){T L<T>(T value) where T:class=>value;return 1;}}", "MCS3100")]
    [InlineData("public static class Rules{public static int F(int a){while(a>0)a--;return a;}}", "MCS3105")]
    [InlineData("public static class Rules{public static Func<int,int> F()=>x=>x;}", "MCS3103")]
    [InlineData("public static class Rules{public static int F(){try{return 1;}catch{return 2;}}}", "MCS3181")]
    public void PendingSyntaxStillFailsWithNoPartialOutput(string code, string diagnostic)
    {
        var result = Support.Convert(Header + code, selectors: ["Example.Structure"]);
        Assert.False(result.Success); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, d => d.Code == diagnostic && d.Line > 0 && d.Column > 0);
    }
    [Fact]
    public void SelectionCompilerErrorsAndNormalizedCollisionsStillReject()
    {
        var outside = Support.Convert("namespace Other{public record Data(int Value);} namespace Example.Structure{public static class Rules{public static int F(Other.Data d)=>d.Value;}}", selectors: ["Example.Structure"]);
        Assert.Contains(outside.Diagnostics, d => d.Code == "MCS2201");
        Assert.Contains(Support.Convert(Header + "bad code", selectors: ["Example.Structure"]).Diagnostics, d => d.Code == "MCS0001");
        var collision = Support.Convert(Header + "public record Foo_Bar(int Value);public record FooBar(int Value);", selectors: ["Example.Structure"]);
        Assert.True(collision.Diagnostics.Count(d => d.Code == "MCS1004") >= 2);
        Assert.Contains(Support.Convert(Header + "public class Data{}", selectors: ["Example.Structure"], platform: Platform.X86).Diagnostics, d => d.Code == "MCS1003");
    }
}
