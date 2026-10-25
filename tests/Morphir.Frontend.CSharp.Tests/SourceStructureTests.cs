using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Morphir.IR.Classic;
using Morphir.IR.Json;
using Morphir.Tests;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp.Tests;

[Trait("Contract", "SourceStructure")]
[Trait("Delivery", "D9")]
public class SourceStructureTests
{
    private const string Header = "using System; using System.Numerics; namespace Example.Structure; ";
    private static Library<Unit, IrType<Unit>> Convert(string code, bool check = false) => Support.Success(Support.Convert(Header + code, selectors: ["Example.Structure"], check: check));
    private static IEnumerable<V> Walk(V value)
    {
        yield return value;
        IEnumerable<V> children = value switch
        {
            SourceValue<Unit, IrType<Unit>> source => source.Children,
            ApplyValue<Unit, IrType<Unit>> apply => [apply.Function, apply.Argument],
            FieldValue<Unit, IrType<Unit>> field => [field.Subject],
            IfValue<Unit, IrType<Unit>> conditional => [conditional.Condition, conditional.Then, conditional.Else],
            TupleValue<Unit, IrType<Unit>> tuple => tuple.Elements,
            _ => []
        };
        foreach (var child in children) foreach (var nested in Walk(child)) yield return nested;
    }
    private static IEnumerable<SourceSyntax> Syntax(SourceSyntax value) => value.Children.SelectMany(Syntax).Prepend(value);
    private static V Body(Library<Unit, IrType<Unit>> library, string name) => library.Definition.Modules.SelectMany(m => m.Value.Value.Values).Single(v => v.Key.Key == Name.From(name).Key).Value.Value.Value.Body;
    private static bool Maybe(IrType<Unit> type) => type is ReferenceType<Unit> r && r.Name.Module.Key == "maybe";
    [Fact]
    public void ReportedLoanAndIntegerProductsHaveOnlyTwoReadsAndOneMultiply()
    {
        var lib = Convert("public record Loan(string SecurityId, decimal Quantity, decimal Price); public sealed class SomeObj {public int IntOne; public int IntTwo;} public static class Rules {public static decimal MarketValue(Loan loan)=>loan.Quantity*loan.Price; public static int SomeMethod(SomeObj obj)=>obj.IntOne*obj.IntTwo;}");
        var loan = Assert.IsType<AliasDefinition<Unit>>(lib.Definition.Modules.SelectMany(m => m.Value.Value.Types).Single(t => t.Key.Key == "loan").Value.Value.Value);
        var fields = Assert.IsType<RecordType<Unit>>(loan.Body).Fields;
        Assert.Equal(new[] { "security\0id", "quantity", "price" }, fields.Select(f => f.Key.Key));
        Assert.False(Maybe(fields[0].Value));
        Assert.Equal("string", Assert.IsType<ReferenceType<Unit>>(fields[0].Value).Name.Local.Key);
        foreach (var (name, module, operation) in new[] { ("MarketValue", "Decimal", "mul"), ("SomeMethod", "Basics", "multiply") })
        {
            var values = Walk(Body(lib, name)).ToArray();
            Assert.Equal(2, values.OfType<FieldValue<Unit, IrType<Unit>>>().Count());
            Assert.Equal(2, values.OfType<ApplyValue<Unit, IrType<Unit>>>().Count());
            var target = Assert.Single(values.OfType<ReferenceValue<Unit, IrType<Unit>>>());
            Assert.Equal(Morphir.IR.Classic.Path.From(module).Key, target.Name.Module.Key);
            Assert.Equal(Name.From(operation).Key, target.Name.Local.Key);
            Assert.DoesNotContain(values, v => v is SourceValue<Unit, IrType<Unit>> or MatchValue<Unit, IrType<Unit>> or LetValue<Unit, IrType<Unit>>);
        }
        Assert.DoesNotContain(lib.Definition.Modules, m => m.Key.Key.Contains("compat", StringComparison.Ordinal));
    }
    [Theory]
    [InlineData("#nullable enable\n")]
    [InlineData("#nullable disable\n")]
    public void OnlyExplicitNullableTypesUseMaybeIncludingNestedSlots(string directive)
    {
        var lib = Support.Success(Support.Convert(directive + Header + "public record Data(string Plain,string? Optional,Tuple<string,string?> Pair,Func<string,string?> Callback); public static class Rules{public static string? F(string plain,string? optional)=>plain; public static string G()=>null!;}", selectors: ["Example.Structure"]));
        var data = Assert.IsType<AliasDefinition<Unit>>(lib.Definition.Modules.SelectMany(m => m.Value.Value.Types).Single(t => t.Key.Key == "data").Value.Value.Value);
        var fields = Assert.IsType<RecordType<Unit>>(data.Body).Fields;
        Assert.False(Maybe(fields[0].Value)); Assert.True(Maybe(fields[1].Value));
        var tuple = Assert.IsType<ReferenceType<Unit>>(fields[2].Value); Assert.False(Maybe(tuple));
        Assert.False(Maybe(tuple.Arguments[0])); Assert.True(Maybe(tuple.Arguments[1]));
        var function = lib.Definition.Modules.SelectMany(m => m.Value.Value.Values).Single(v => v.Key.Key == "f").Value.Value.Value;
        Assert.False(Maybe(function.Inputs[0].Type)); Assert.True(Maybe(function.Inputs[1].Type)); Assert.True(Maybe(function.Output));
        var nonnullable = lib.Definition.Modules.SelectMany(m => m.Value.Value.Values).Single(v => v.Key.Key == "g").Value.Value.Value;
        Assert.False(Maybe(nonnullable.Output)); Assert.Contains(Walk(nonnullable.Body), v => v is SourceValue<Unit, IrType<Unit>> { Kind: "Null" });
    }
    [Fact]
    public void StatementsBranchesNamesAndDefaultsAreWrittenStructure()
    {
        var lib = Convert("public static class Rules{public static int F(int a){int x; ; if(false){x=1;}else{x=2;} x=a; return x+0;} public const int Total=2*3; public static int G()=>sizeof(int);}");
        var source = Walk(Body(lib, "F")).OfType<SourceValue<Unit, IrType<Unit>>>().ToArray();
        Assert.Equal(3, source.Count(v => v.Kind == "Assignment"));
        Assert.Single(source, v => v.Kind == "If"); Assert.Single(source, v => v.Kind == "Return");
        Assert.Single(source, v => v.Kind == "EmptyStatement");
        Assert.Empty(source.Single(v => v.Kind == "VariableDeclaration").Children);
        Assert.IsType<ApplyValue<Unit, IrType<Unit>>>(Body(lib, "Total"));
        Assert.IsType<SourceValue<Unit, IrType<Unit>>>(Body(lib, "G"));
        Assert.DoesNotContain(Walk(Body(lib, "F")), v => v is LetValue<Unit, IrType<Unit>>);
    }
    [Fact]
    public void NullableFlowAndVarInferenceDoNotInventNullableDeclarations()
    {
        var lib = Convert("public static class Rules{public static string F(bool b){var s=b?\"text\":null;return s!;} public static string G(bool b)=>b?\"text\":null;}");
        foreach (var name in new[] { "F", "G" })
        {
            var definition = lib.Definition.Modules.SelectMany(m => m.Value.Value.Values).Single(v => v.Key.Key == Name.From(name).Key).Value.Value.Value;
            Assert.False(Maybe(definition.Output));
            Assert.DoesNotContain(Walk(definition.Body), v => Maybe(v.Attributes));
        }
    }
    [Fact]
    public void ZeroArgumentMethodGroupKeepsItsDeclaredFunctionTypeWithoutAUnitAdapter()
    {
        var lib = Convert("public static class Rules{public static int T()=>1;public static Func<int> F()=>T;}");
        var definition = lib.Definition.Modules.SelectMany(m => m.Value.Value.Values).Single(v => v.Key.Key == "f").Value.Value.Value;
        Assert.Equal(JsonSerializer.Serialize(Codec.Type(definition.Output,_=>new{})),JsonSerializer.Serialize(Codec.Type(definition.Body.Attributes,_=>new{})));
        Assert.DoesNotContain(Walk(definition.Body),v=>v is LambdaValue<Unit,IrType<Unit>> or UnitValue<Unit,IrType<Unit>>);
    }
    [Fact]
    public void ExplicitStaticInitializationIsRetainedWithoutInventingImplicitInitialization()
    {
        var lib = Convert("public static class Rules{public static int Storage=1;static Rules(){Storage=2;}public static int F()=>Storage;}");
        var values = lib.Definition.Modules.SelectMany(m=>m.Value.Value.Values).ToArray();
        Assert.Equal(3,values.Length);
        Assert.Contains(values,v=>Walk(v.Value.Value.Value.Body).OfType<SourceValue<Unit,IrType<Unit>>>().Any(s=>s.Kind=="Assignment"));
    }
    [Fact]
    public void ComputedPropertiesKeepTheirAccessWithoutExpandingOrPretendingToBeStorage()
    {
        var lib = Convert("public class Cell{public int Value;public int P=>Value+1;public int Read()=>P;public int Qualified()=>this.P;}");
        foreach (var name in new[]{"Read","Qualified"})
        {
            var body = Assert.IsType<SourceValue<Unit,IrType<Unit>>>(Body(lib,name));
            Assert.Equal("MemberAccess",body.Kind);
            Assert.DoesNotContain(Walk(body),v=>v is ApplyValue<Unit,IrType<Unit>>);
        }
    }
    [Theory]
    [InlineData("public class Cell(int x){public int Value=x;public int Change(){x=2;return x;}}", "ClassDeclaration")]
    [InlineData("public class Cell{public int P{get=>field;set=>field=value;}=3;public Cell():this(2){}public Cell(int x){P=x;}}", "ThisConstructorInitializer")]
    [InlineData("public record Cell{public int Value;public Cell(int value){Value=value;}protected Cell(Cell other){Value=other.Value;}}", "ConstructorDeclaration")]
    [InlineData("public struct Cell{public int X;public static bool operator true(Cell a)=>a.X>0;public static bool operator false(Cell a)=>a.X==0;public static Cell operator &(Cell a,Cell b)=>a;public static Cell F(Cell a,Cell b)=>a&&b;}", "LogicalAndExpression")]
    public void ExplicitDataBodiesRemainSourceWithoutHiddenStorageOrAlgorithms(string code, string kind)
    {
        var lib = Convert(code);
        Assert.Contains(lib.Source!.Declarations.SelectMany(Syntax), n => n.Kind == kind);
        var values = lib.Definition.Modules.SelectMany(m => m.Value.Value.Values).SelectMany(v => Walk(v.Value.Value.Value.Body));
        Assert.DoesNotContain(values, v => v is MatchValue<Unit, IrType<Unit>> or LetValue<Unit, IrType<Unit>> or UpdateRecordValue<Unit, IrType<Unit>>);
        Assert.DoesNotContain(lib.Definition.Modules.SelectMany(m => m.Value.Value.Types).Select(t => t.Value.Value.Value).OfType<AliasDefinition<Unit>>().SelectMany(t => t.Body is RecordType<Unit> record ? record.Fields : []), f => f.Key.Key.Contains("field", StringComparison.Ordinal));
    }
    [Fact]
    public void NamedOptionalAndParamsCallsHaveExactlyTheWrittenArguments()
    {
        var lib = Convert("public static class Rules {public static int Target(int a=3,int b=4)=>a; public static int Named()=>Target(b:2,a:1); public static int Optional()=>Target(); public static int Params(params int[] values)=>values[0]; public static int Packed()=>Params(1,2);}");
        var named = Assert.IsType<SourceValue<Unit, IrType<Unit>>>(Body(lib, "Named"));
        Assert.Equal("Invocation", named.Kind);
        Assert.Equal(new[] { "b", "a" }, named.Children.Skip(1).Cast<SourceValue<Unit, IrType<Unit>>>().Select(v => v.Properties.Single(p => p.Key == "name").Value));
        Assert.Single(Assert.IsType<SourceValue<Unit, IrType<Unit>>>(Body(lib, "Optional")).Children);
        var packed = Walk(Body(lib, "Packed")).OfType<SourceValue<Unit, IrType<Unit>>>().ToArray();
        Assert.Equal(2, packed.Count(v => v.Kind == "Argument")); Assert.DoesNotContain(packed, v => v.Kind == "ArrayCreation");
        Assert.Contains(Walk(Body(lib, "Params")), v => v is SourceValue<Unit, IrType<Unit>> { Kind: "Index" });
        var tokens = lib.Source!.Declarations.SelectMany(Syntax).Where(n => n.Token is not null).Select(n => n.Token).ToArray();
        Assert.Contains("params", tokens); Assert.Contains("3", tokens); Assert.Contains("4", tokens);
    }
    [Fact]
    public void ConstructorsAccessorsAliasingCyclesAndEqualityAreNotExecuted()
    {
        var lib = Convert("public record Cell {public int Value; public Cell? Next; public Cell(int x){Value=x;} public int P{get=>Value;set=>Value=value;} } public static class Rules{public static Cell F(Cell input){var a=input; a.Value=1; a.Next=a; a.P=3; return a with{Value=4};} public static bool Eq(Cell a,Cell b)=>a==b; public static Cell Make()=>new Cell(2);}");
        var nodes = Walk(Body(lib, "F")).OfType<SourceValue<Unit, IrType<Unit>>>().ToArray();
        Assert.Equal(4, nodes.Count(n => n.Kind == "Assignment")); // Three statements plus one explicit with initializer.
        Assert.Contains(nodes, n => n.Kind == "With");
        Assert.DoesNotContain(lib.Definition.Modules.SelectMany(m => m.Value.Value.Values), v => v.Key.Key.Contains("finite", StringComparison.Ordinal));
        Assert.Equal("Binary", Assert.IsType<SourceValue<Unit, IrType<Unit>>>(Body(lib, "Eq")).Kind);
        Assert.Equal("New", Assert.IsType<SourceValue<Unit, IrType<Unit>>>(Body(lib, "Make")).Kind);
    }
    [Fact]
    public void SourceEnvelopeIsVersionedAndKeepsExactSelectedSyntax()
    {
        const string code = "namespace Example.Structure; public readonly record struct Data(int Value); public static class Rules{public static int F(int @value)=>checked((int)(@value+1));}";
        var compilation = Support.Compile(code); var lib = Support.Success(Frontend.Convert(compilation, new("Example", ["Example.Structure"])));
        var expected = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.BaseTypeDeclarationSyntax>().Where(t => t.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.BaseNamespaceDeclarationSyntax).ToArray();
        Assert.Equal(expected.Length, lib.Source!.Declarations.Count);
        for (int i = 0; i < expected.Length; i++) Assert.Equal(expected[i].DescendantTokens().Select(t => (t.Kind().ToString(), t.Text)), Syntax(lib.Source.Declarations[i]).Where(n => n.Token is not null).Select(n => (n.Kind, n.Token!)));
        using var json = JsonDocument.Parse(Codec.Encode(lib));
        Assert.Equal("Morphir.CSharp.Source", json.RootElement.GetProperty("format").GetString()); Assert.Equal(1, json.RootElement.GetProperty("formatVersion").GetInt32());
        Assert.Equal(3, json.RootElement.GetProperty("classicModelVersion").GetInt32());
        Assert.DoesNotContain("fromSmall", Codec.Encode(lib), StringComparison.Ordinal);
    }
    [Theory]
    [InlineData("public static int F(int a)=>checked(a*2);", "checked")]
    [InlineData("public static int F(int a)=>unchecked(a*2);", "unchecked")]
    public void CheckedContextIsSourceAndCompilerOverflowOptionDoesNotAddOperations(string member, string kind)
    {
        var lib = Convert("public static class Rules{" + member + "}", true);
        Assert.Equal(kind, Assert.IsType<SourceValue<Unit, IrType<Unit>>>(Body(lib, "F")).Kind);
        var plain = Convert("public static class Rules{public static int F(int a)=>a*2;}", true);
        Assert.IsType<ApplyValue<Unit, IrType<Unit>>>(Body(plain, "F"));
    }
    [Theory]
    [InlineData("public class Box<T>{}", "MCS3100")]
    [InlineData("public static class Rules{public static int F(int[] a)=>a[0];}", "MCS3101")]
    [InlineData("public static class Rules{public static int F(){int[] a=null;return 1;}}", "MCS3101")]
    [InlineData("public class Cell:System.IDisposable{public void Dispose(){}}", "MCS3100")]
    [InlineData("public static class Rules{public static int F(){T L<T>(T value)=>value;return 1;}}", "MCS3100")]
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
