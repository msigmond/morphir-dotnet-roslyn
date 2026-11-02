using System.Security.Cryptography;
using System.Text.Json;
using Morphir.IR.Classic;
using Morphir.IR.Json;
using Morphir.Tests;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit,Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp.Tests;

[Trait("Delivery","D9")]
[Trait("Contract","Classic")]
public class ClassicFrontendTests
{
    private static FrontendResult Convert(string code) => Frontend.Convert(Support.Compile("namespace Example.Business;"+code),new("Example",["Example.Business"]));
    private static V Body(Library<Unit,IrType<Unit>> lib,string name)=>lib.Definition.Modules.SelectMany(m=>m.Value.Value.Values).Single(v=>v.Key.Key==Name.From(name).Key).Value.Value.Value.Body;
    [Theory]
    [MemberData(nameof(ExactCases))]
    public void CompleteDistributionExactlyMatchesElmMake(string id)
    {
        var actual = ElmParity.Convert(id);
        var difference = StructuralComparison.Compare(ElmParity.Expected(id), Codec.Encode(Support.Success(actual)));
        Assert.True(difference is null, id + ": " + difference);
    }
    public static IEnumerable<object[]> ExactCases() => ElmParity.Cases.Select(c => new object[] { c.Id });
    [Fact]
    public void ProductsAndDivisionRemainSimpleWithoutRuntimeProofs()
    {
        var lib=Support.Success(Convert("public record Loan(string SecurityId,decimal Quantity,decimal Price);public static class Rules{public static decimal MarketValue(Loan loan){return loan.Quantity*loan.Price;}public static int Divide(int a,int b){return a/b;}}"));
        var product=Assert.IsType<ApplyValue<Unit,IrType<Unit>>>(Body(lib,"MarketValue"));
        var partial=Assert.IsType<ApplyValue<Unit,IrType<Unit>>>(product.Function);
        Assert.Equal("mul",Assert.IsType<ReferenceValue<Unit,IrType<Unit>>>(partial.Function).Name.Local.Key);
        Assert.IsType<FieldValue<Unit,IrType<Unit>>>(partial.Argument);Assert.IsType<FieldValue<Unit,IrType<Unit>>>(product.Argument);
        Assert.IsType<ApplyValue<Unit,IrType<Unit>>>(Body(lib,"Divide"));
        Assert.DoesNotContain(lib.Definition.Modules,m=>m.Key.Key.Contains("compat",StringComparison.Ordinal));
    }
    [Fact]
    public void LocalsKeepNamesOrderAndConcreteDeclaredTypes()
    {
        var lib=Support.Success(Convert("public static class Rules{public static int F(int a){int x=a+1;int y=x*2;return y;}}"));
        var first=Assert.IsType<LetValue<Unit,IrType<Unit>>>(Body(lib,"F"));Assert.Equal("x",first.Name.Key);
        var second=Assert.IsType<LetValue<Unit,IrType<Unit>>>(first.Body);Assert.Equal("y",second.Name.Key);
        Assert.IsType<ReferenceType<Unit>>(first.Definition.Output);Assert.Equal("y",Assert.IsType<VariableValue<Unit,IrType<Unit>>>(second.Body).Name.Key);
    }
    [Theory]
    [InlineData("public static int F(int a){a=2;return a;}")]
    [InlineData("public static int F(){int a;a=2;return a;}")]
    [InlineData("public static int F(int? a)=>a.Value;")]
    [InlineData("public static int F(int x){int Local(int x)=>x+1;return Local(x);}")]
    [InlineData("public static int F(){int foo_bar=1;int fooBar=2;return foo_bar;}")]
    [InlineData("public static decimal F(int a)=>a;")]
    [InlineData("public static decimal F(int a){decimal x=a;return x;}")]
    [InlineData("public static decimal F(bool a)=>a?1:2m;")]
    [InlineData("public static char F()=>\'\\uD800\';")]
    public void UnmappedClassicFormsFailAtSourceWithoutLeakingExtensions(string member)
    {
        var result=Convert("public static class Rules{"+member+"}");Assert.Null(result.Distribution);
        Assert.Contains(result.Diagnostics,d=>d.Code=="MCS4001"&&d.Line>0&&d.Column>0);
    }
    [Fact]
    public void CapturedPrimaryStateIsNotEmittedAsAnUnboundClassicVariable()
    {
        var result=Convert("public class Cell(int value){public int Read()=>value;}");
        Assert.IsType<FieldValue<Unit,IrType<Unit>>>(Body(Support.Success(result),"Read"));
    }
    [Theory]
    [InlineData("public class Cell{public int X=2;}")]
    [InlineData("public record Cell{public int X{get;init;}=2;}")]
    [InlineData("public class Node{public Node? Next;}")]
    [InlineData("public class Cell(int value){public int X;}")]
    [InlineData("public enum State:ulong{Huge=18446744073709551615UL}")]
    [InlineData("public class Cell{public int Value{get=>1;set{}}}")]
    [InlineData("public class Cell{public int Value;public Cell(int value){Value=value;System.Console.WriteLine(value);}}") ]
    public void UnmappedDataCannotBeSilentlyDroppedFromClassic(string code)
    {
        var result=Convert(code);Assert.Null(result.Distribution);Assert.Contains(result.Diagnostics,d=>d.Code=="MCS4001");
    }
    [Fact]
    public void ConstructionHasTheRecordResultTypeAndNoConstructorModule()
    {
        var lib=Support.Success(Convert("public readonly record struct Item(int Value);public static class Rules{public static Item F(int value)=>new(value);}"));
        var call=Assert.IsType<ApplyValue<Unit,IrType<Unit>>>(Body(lib,"F"));
        Assert.Equal("item",Assert.IsType<ReferenceType<Unit>>(call.Attributes).Name.Local.Key);
        Assert.IsType<ConstructorValue<Unit,IrType<Unit>>>(call.Function);
        Assert.DoesNotContain(lib.Definition.Modules,m=>m.Key.Key.EndsWith("item",StringComparison.Ordinal));
    }
    [Fact]
    public void D13SampleRegressionCompilesToStandardNodes()
    {
        var compilation=SourceCompilation.Create([("ClassicLoan.cs",Support.Read("fixtures/elm-parity/current-sample/csharp/Input.cs"))]);
        var lib=Support.Success(Frontend.Convert(compilation,new("Example",["Example.CurrentSupported"])));
        Assert.Equal(4,lib.Definition.Modules.Count);
        Assert.IsType<LetValue<Unit,IrType<Unit>>>(Body(lib,"Calculate"));
        Assert.IsType<ApplyValue<Unit,IrType<Unit>>>(Body(lib,"MarketValue"));
    }
    [Fact]
    public void SingleFileBusinessDemoBuildsTheExpectedRentalPlan()
    {
        var source = Support.Read("samples/FestivalRobotRental/BusinessLogic.cs");
        var assembly = Support.Emit(source);
        var business = assembly.GetType("Example.FestivalRobotRental.RentalBusiness")!;
        var plan = business.GetMethod("Demo")!.Invoke(null, null)!;
        object? Field(object value, string name) => value.GetType().GetProperty(name)!.GetValue(value);
        var quotes = (Array)Field(plan, "Quotes")!;
        var reviews = ((Array)Field(plan, "Reviews")!).Cast<object>().ToArray();
        Assert.Equal(new[] { true, true, false, false, false }, reviews.Select(review => (bool)Field(review, "Accepted")!));
        Assert.Equal(2, quotes.Length);
        Assert.Equal(3, Field(plan, "RobotCount"));
        Assert.Equal(2, Field(plan, "CartonCount"));
        Assert.Equal("ORD-001", Field(plan, "FirstOrder"));
        Assert.Equal(460m, Field(Field(plan, "Total")!, "Amount"));
        Assert.Equal(0.75, Field(plan, "CapacityUse"));
        Assert.Equal(6.05m, Field(plan, "RewardCredit"));
    }
    [Fact]
    public void ReferenceFixtureBytesRemainIndependentAndFrozen()
    {
        using var manifest=JsonDocument.Parse(Support.Read("fixtures/classic-business-logic/provenance.json"));
        foreach(var file in manifest.RootElement.GetProperty("files").EnumerateArray())
            Assert.Equal(file.GetProperty("sha256").GetString(),System.Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(System.IO.Path.Combine(Support.Root,file.GetProperty("path").GetString()!)))));
    }
    [Theory]
    [InlineData("multiply", "subtract")]
    [InlineData("Public", "Private")]
    [InlineData("binding", "otherBinding")]
    public void ExactComparisonDoesNotNormalizeDifferences(string before, string after)
    {
        var expected = "{\"name\":\"binding\",\"operator\":\"multiply\",\"access\":\"Public\"}";
        Assert.NotNull(StructuralComparison.Compare(expected, expected.Replace(before, after, StringComparison.Ordinal)));
    }
}
