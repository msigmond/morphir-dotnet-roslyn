using Microsoft.CodeAnalysis;
using Morphir.Frontend.CSharp;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D5")]
public class NumericSafetyTests
{
    [Theory]
    [InlineData("public static int F(int a,int b)=>a/b;", "MCS2601")]
    [InlineData("public static long F(long a,long b)=>a%b;", "MCS2601")]
    [InlineData("public static int F(int a)=>a/-1;", "MCS2602")]
    [InlineData("public static long F(long a)=>a%-1;", "MCS2602")]
    [InlineData("public static int F(System.Numerics.BigInteger a)=>(int)a;", "MCS2603")]
    [InlineData("public static System.Numerics.BigInteger F(double a)=>new System.Numerics.BigInteger(a);", "MCS2604")]
    [InlineData("public static double F(System.Numerics.Complex a){if(false)return a.Magnitude;return 0;}", "MCS2107")]
    [InlineData("public static int F()=>checked(1+2);", "MCS2001")]
    [InlineData("public static int F(int a)=>checked((byte)a);", "MCS2001")]
    [InlineData("public static int F(int a){if(false){checked {a=a+1;}}return a;}", "MCS2001")]
    public void FailurePathsRejectTheWholeDistributionAtSource(string member, string diagnostic)
    {
        var result = Support.Convert("namespace Example.D5;public static class Rules{" + member + "}", selectors: ["Example.D5"]);
        Assert.Null(result.Distribution);
        Assert.Contains(result.Diagnostics, d => d.Code == diagnostic && d.Line > 0 && d.Column > 0 && d.File.Length > 0);
    }
    [Theory]
    [InlineData("public static long F(long a)=>a+1;")]
    [InlineData("public static int F(long a)=>(int)a;")]
    [InlineData("public static int F(int a){if(false)return a+1;return 0;}")]
    [InlineData("public static int F(long a){if(false)return (int)a;return 0;}")]
    public void CompilerOverflowSettingsRejectSemanticCheckedOperationsEvenWhenUnreachable(string member)
    {
        var result = Support.Convert("namespace Example.D5;public static class Rules{" + member + "}", selectors: ["Example.D5"], check: true);
        Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, d => d.Code == "MCS2004");
    }
    [Fact]
    public void SourceDomainsSurvivePromotionsAndNative32BitIsRejected()
    {
        const string code = "namespace Example.D5;public static class Rules{public static int F(byte a)=>a/-1;}";
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D5"]));
        Assert.Equal(-255, Support.Call(library, "D5.Rules", "F", 255));
        const string sizes = "namespace Example.D5;public static class Rules{public static unsafe int Size()=>sizeof(nint)+sizeof(System.Int128)+sizeof(System.Numerics.Complex)+sizeof(bool)+sizeof(char);}";
        Assert.Equal(43, Support.Call(Support.Success(Support.Convert(sizes, selectors: ["Example.D5"])), "D5.Rules", "Size"));
        var source = SourceCompilation.Create([("Input.cs", code)], platform: Platform.X86);
        var result = Frontend.Convert(source, new("Example", ["Example.D5"]));
        Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, d => d.Code == "MCS1003");
    }
}
