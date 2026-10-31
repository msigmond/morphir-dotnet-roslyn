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
    [InlineData("Sigmond")]
    [InlineData("Sigmond.Buslog")]
    public void CompleteBusinessDistributionMatchesIndependentElm(string package)
    {
        var root=System.IO.Path.Combine(Support.Root,"fixtures/classic-business-logic/csharp");
        var compilation=SourceCompilation.Create(Directory.GetFiles(root,"*.cs").Order(StringComparer.Ordinal).Select(p=>(p,File.ReadAllText(p))));
        var lib=Support.Success(Frontend.Convert(compilation,new(package,["Sigmond.Buslog.Model","Sigmond.Buslog.Algo"])));
        Assert.Null(lib.Source);Assert.Equal(2,lib.Definition.Modules.Count);
        var json=Codec.Encode(lib);using var document=JsonDocument.Parse(json);
        Assert.Equal(new[]{"formatVersion","distribution"},document.RootElement.EnumerateObject().Select(p=>p.Name));
        Assert.Equal(3,document.RootElement.GetProperty("formatVersion").GetInt32());
        var difference=ClassicParity.Compare(Support.Read("fixtures/classic-business-logic/expected/elm.json"),json,alignPackage:package!="Sigmond");
        Assert.True(difference is null,difference?.ToString());
        Assert.IsType<LetValue<Unit,IrType<Unit>>>(Body(lib,"Calculate"));
        Assert.IsType<ApplyValue<Unit,IrType<Unit>>>(Body(lib,"MarketValue"));
        var function=lib.Definition.Modules.SelectMany(m=>m.Value.Value.Values).Single(v=>v.Key.Key==Name.From("Calculate").Key).Value.Value.Value;
        var first=Assert.IsType<LetValue<Unit,IrType<Unit>>>(function.Body);
        Assert.Equal(Name.From("marketValue").Key,first.Name.Key);
        Assert.IsType<ReferenceValue<Unit,IrType<Unit>>>(Assert.IsType<ApplyValue<Unit,IrType<Unit>>>(first.Definition.Body).Function);
        var second=Assert.IsType<LetValue<Unit,IrType<Unit>>>(first.Body);
        Assert.Equal(Name.From("dailyFee").Key,second.Name.Key);
        // The initializer must call the global definition, despite the local having the same name.
        var call=Assert.IsType<ApplyValue<Unit,IrType<Unit>>>(second.Definition.Body);
        Assert.Equal(Name.From("dailyFee").Key,Assert.IsType<ReferenceValue<Unit,IrType<Unit>>>(call.Function).Name.Local.Key);
    }
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
    [InlineData("public static int F(bool b){if(b)return 1;return 2;}")]
    [InlineData("public static int F(){int a;a=2;return a;}")]
    [InlineData("public static int F(int? a)=>a.Value;")]
    [InlineData("public static int F(){int foo_bar=1;int fooBar=2;return foo_bar;}")]
    [InlineData("public static decimal F(int a)=>a;")]
    [InlineData("public static decimal F(int a){decimal x=a;return x;}")]
    [InlineData("public static decimal F(decimal a)=>-a;")]
    [InlineData("public static decimal F(bool a)=>a?1:2m;")]
    public void UnmappedClassicFormsFailAtSourceWithoutLeakingExtensions(string member)
    {
        var result=Convert("public static class Rules{"+member+"}");Assert.Null(result.Distribution);
        Assert.Contains(result.Diagnostics,d=>d.Code=="MCS4001"&&d.Line>0&&d.Column>0);
    }
    [Fact]
    public void CapturedPrimaryStateIsNotEmittedAsAnUnboundClassicVariable()
    {
        var result=Convert("public class Cell(int value){public int Read()=>value;}");
        Assert.Null(result.Distribution);Assert.Contains(result.Diagnostics,d=>d.Code=="MCS4001");
    }
    [Theory]
    [InlineData("public class Cell{public int X=2;}")]
    [InlineData("public record Cell{public int X{get;init;}=2;}")]
    [InlineData("public class Node{public Node? Next;}")]
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
    public void CumulativeClassicSampleCompilesToStandardNodes()
    {
        var compilation=SourceCompilation.Create([("ClassicLoan.cs",Support.Read("samples/CurrentSupportedExample/ClassicLoan.cs"))]);
        var lib=Support.Success(Frontend.Convert(compilation,new("Example",["Example.CurrentSupported"])));
        Assert.Null(lib.Source);Assert.Equal(2,lib.Definition.Modules.Count);
        Assert.IsType<LetValue<Unit,IrType<Unit>>>(Body(lib,"Calculate"));
        Assert.IsType<ApplyValue<Unit,IrType<Unit>>>(Body(lib,"MarketValue"));
    }
    [Fact]
    public void ExplicitSourceOptionRetainsFullSyntaxContract()
    {
        var source=Frontend.Convert(Support.Compile("namespace Example.Business;public static class Rules{public static int F(int a){a=2;return a;}}"),new("Example",["Example.Business"],OutputFormat:FrontendOutputFormat.Source));
        var lib=Support.Success(source);Assert.NotNull(lib.Source);
        using var json=JsonDocument.Parse(Codec.Encode(lib));Assert.Equal("Morphir.CSharp.Source",json.RootElement.GetProperty("format").GetString());
    }
    [Fact]
    public void ReferenceFixtureBytesRemainIndependentAndFrozen()
    {
        using var manifest=JsonDocument.Parse(Support.Read("fixtures/classic-business-logic/provenance.json"));
        foreach(var file in manifest.RootElement.GetProperty("files").EnumerateArray())
            Assert.Equal(file.GetProperty("sha256").GetString(),System.Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(System.IO.Path.Combine(Support.Root,file.GetProperty("path").GetString()!)))));
    }
    [Fact]
    public void ParityComparisonDoesNotHideChangedOperatorsOrVisibility()
    {
        var expected=Support.Read("fixtures/classic-business-logic/expected/elm.json");
        Assert.NotNull(ClassicParity.Compare(expected,expected.Replace("multiply","subtract",StringComparison.Ordinal),false));
        Assert.NotNull(ClassicParity.Compare(expected,expected.Replace("Public","Private",StringComparison.Ordinal),false));
    }
}
