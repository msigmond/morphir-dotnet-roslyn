using System.Diagnostics;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Morphir.Frontend.CSharp.ProjectSystem;
using Morphir.IR.Classic;
using Morphir.IR.Json;
using Morphir.Tests;
using IrPath = Morphir.IR.Classic.Path;
using V = Morphir.IR.Classic.Value<Morphir.IR.Classic.Unit, Morphir.IR.Classic.IrType<Morphir.IR.Classic.Unit>>;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Contract", "SourceStructure")]
[Trait("Delivery", "D2")]
public class StructureIntegrationTests
{
    public static IEnumerable<object[]> Fixtures() => Directory.GetFiles(System.IO.Path.Combine(Support.Root, "fixtures"), "*.cs", SearchOption.AllDirectories)
        .Where(p => p.Contains(System.IO.Path.DirectorySeparatorChar + "csharp" + System.IO.Path.DirectorySeparatorChar, StringComparison.Ordinal) && !p.EndsWith("D7/csharp/External.cs", StringComparison.Ordinal))
        .Select(p => new object[] { System.IO.Path.GetRelativePath(Support.Root, p) });
    [Theory]
    [MemberData(nameof(Fixtures))]
    public void DeliveredSourceFixturesKeepAllDeclaredSyntaxWithoutHelpers(string file)
    {
        var compilation = SourceCompilation.Create([(file, Support.Read(file))]);
        var namespaces = file.EndsWith("D1/csharp/Selection.cs", StringComparison.Ordinal) ? ["Sample.Business"]
            : compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().Select(n => n.Name.ToString()).Distinct().ToArray();
        var lib = Support.Success(Frontend.Convert(compilation, new("Example", namespaces)));
        var expected = compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Where(t => !t.Ancestors().OfType<BaseTypeDeclarationSyntax>().Any()
            && compilation.GetSemanticModel(t.SyntaxTree).GetDeclaredSymbol(t) is INamedTypeSymbol type && namespaces.Any(ns => type.ContainingNamespace.ToDisplayString() == ns || type.ContainingNamespace.ToDisplayString().StartsWith(ns + ".", StringComparison.Ordinal))).ToArray();
        Assert.Equal(expected.Length, lib.Source!.Declarations.Count);
        for (int i = 0; i < expected.Length; i++) Assert.Equal(expected[i].DescendantTokens().Select(t => t.Text), Syntax(lib.Source.Declarations[i]).Where(n => n.Token is not null).Select(n => n.Token!));
        Assert.DoesNotContain(lib.Definition.Modules, m => m.Key.Key.Contains("compat", StringComparison.Ordinal));
        Assert.Equal(Codec.Encode(lib), Codec.Encode(Support.Success(Frontend.Convert(compilation, new("Example", namespaces)))));
    }
    [Fact]
    public void MetadataCallsNeedNoPurityContractButConfiguredMappingsNeedExactAbi()
    {
        var compilation = Support.Compile("namespace Example.Structure;public static class Rules{public static int F(int x)=>System.Math.Abs(x);}");
        var method = compilation.GetTypeByMetadataName("System.Math")!.GetMembers("Abs").OfType<IMethodSymbol>().Single(m => m.Parameters.Length == 1 && m.Parameters[0].Type.SpecialType == SpecialType.System_Int32);
        var package = IrPath.From("MathSpec"); var module = IrPath.From("Rules"); var name = Name.From("Abs");
        var spec = new PackageSpecification<Unit>([new(module,new([], [new(name,new("",new([new(Name.From("x"),Types.Int)],Types.Int)))]))]);
        var contract = new ExternalFunctionContract(method,new(package,module,name),false,false,"");
        var lib = Support.Success(Frontend.Convert(compilation,new("Example",["Example.Structure"],[new(package,spec,[contract])])));
        Assert.Single(lib.Dependencies);
        var nodes = lib.Definition.Modules.SelectMany(m => m.Value.Value.Values).SelectMany(v => Values(v.Value.Value.Value.Body));
        Assert.Contains(nodes, v => v is ReferenceValue<Unit,IrType<Unit>> r && r.Name.Package.Key == package.Key && r.Name.Local.Key == name.Key);
        var bad = new PackageSpecification<Unit>([new(module,new([], [new(name,new("",new([new(Name.From("x"),Types.Bool)],Types.Int)))]))]);
        var failure = Frontend.Convert(compilation,new("Example",["Example.Structure"],[new(package,bad,[contract])]));
        Assert.Null(failure.Distribution); Assert.Contains(failure.Diagnostics,d=>d.Code=="MCS2809");
    }
    [Fact]
    public void HistoricalExternalCallFixtureBindsMetadataWithoutRuntimeAssertions()
    {
        var trusted=SourceCompilation.Create([("Trusted.cs",Support.Read("fixtures/D7/external/Trusted.cs"))]).WithAssemblyName("Trusted");
        using var bytes=new MemoryStream();var emitted=trusted.Emit(bytes);Assert.True(emitted.Success,string.Join("\n",emitted.Diagnostics));
        var compilation=SourceCompilation.Create([("External.cs",Support.Read("fixtures/D7/csharp/External.cs"))]).AddReferences(MetadataReference.CreateFromImage(bytes.ToArray()));
        var lib=Support.Success(Frontend.Convert(compilation,new("Example",["Example.D7"])));
        var method=lib.Definition.Modules.SelectMany(m=>m.Value.Value.Values).Single(v=>v.Key.Key==Name.From("DefaultEligible").Key).Value.Value.Value;
        var call=Assert.IsType<SourceValue<Unit,IrType<Unit>>>(method.Body);
        Assert.Equal("Invocation",call.Kind);Assert.Equal(2,call.Children.Count); // Target and the one written argument; no omitted default.
        Assert.Empty(lib.Dependencies);
    }
    [Fact]
    public async Task CumulativeProjectRetainsGeneratorInputsAndIndependentSampleStructures()
    {
        var project = System.IO.Path.Combine(Support.Root,"samples/CurrentSupportedExample/CurrentSupportedExample.csproj");
        var loaded = await ProjectLoader.LoadAsync(project,new("net10.0"));
        Assert.True(loaded.Success,string.Join("\n",loaded.Diagnostics));
        Assert.Contains(loaded.Inputs.SelectMany(i=>i.Sources),s=>s.Generated&&s.Path.EndsWith("Rules.Generated.cs",StringComparison.Ordinal));
        Assert.Contains(loaded.Inputs.SelectMany(i=>i.AdditionalFiles),s=>s.Path.EndsWith("threshold.txt",StringComparison.Ordinal));
        Assert.NotEmpty(loaded.Inputs.SelectMany(i=>i.AnalyzerConfigs));
        var lib=Support.Success(Frontend.Convert(loaded.Compilation!,new("Example",["Example.CurrentSupported"]),loaded.SourceProjects));
        using var contract=JsonDocument.Parse(Support.Read("fixtures/source-structure/contract.json"));
        var values=lib.Definition.Modules.SelectMany(m=>m.Value.Value.Values).ToArray();
        foreach(var name in contract.RootElement.GetProperty("requiredValues").EnumerateArray()) Assert.Contains(values,v=>v.Key.Key==Name.From(name.GetString()!).Key);
        foreach(var product in contract.RootElement.GetProperty("products").EnumerateArray())
        {
            var value=values.Single(v=>v.Key.Key==Name.From(product.GetProperty("name").GetString()!).Key).Value.Value.Value.Body;
            var nodes=Values(value).ToArray();
            Assert.Equal(product.GetProperty("fields").EnumerateArray().Select(n=>Name.From(n.GetString()!).Key),nodes.OfType<FieldValue<Unit,IrType<Unit>>>().Select(f=>f.Field.Key));
            var target=Assert.Single(nodes.OfType<ReferenceValue<Unit,IrType<Unit>>>());
            Assert.Equal(IrPath.From(product.GetProperty("module").GetString()!).Key,target.Name.Module.Key);
            Assert.Equal(Name.From(product.GetProperty("operation").GetString()!).Key,target.Name.Local.Key);
            Assert.Equal(2,nodes.OfType<ApplyValue<Unit,IrType<Unit>>>().Count());
        }
        Assert.DoesNotContain(lib.Definition.Modules,m=>m.Key.Key.Contains("compat",StringComparison.Ordinal));
        var expected=loaded.SourceProjects.Prepend(loaded.Compilation!).SelectMany(c=>c.SyntaxTrees).SelectMany(t=>t.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>()).Where(n=>!n.Ancestors().OfType<BaseTypeDeclarationSyntax>().Any()&&n.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Any(ns=>ns.Name.ToString().StartsWith("Example.CurrentSupported",StringComparison.Ordinal))).ToArray();
        Assert.Equal(expected.Length,lib.Source!.Declarations.Count);
        for(int i=0;i<expected.Length;i++) Assert.Equal(expected[i].DescendantTokens().Select(t=>t.Text),Syntax(lib.Source.Declarations[i]).Where(n=>n.Token is not null).Select(n=>n.Token!));
        var folder=System.IO.Path.Combine(Support.Root,"artifacts","source-cli-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var output=System.IO.Path.Combine(folder,"ir.json");
        var args=new[]{"--project",project,"--framework","net10.0","--package","Example","--namespace","Example.CurrentSupported","--output",output};
        Assert.Equal(0,(await Cli(args)).Exit);
        Assert.Null(StructuralComparison.Compare(Codec.Encode(lib),File.ReadAllText(output)));
    }
    [Fact]
    public async Task SourceCliPreservesOldOutputOnCompilerOrRepresentationFailure()
    {
        var folder=System.IO.Path.Combine(Support.Root,"artifacts","source-cli-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var input=System.IO.Path.Combine(folder,"Input.cs");var output=System.IO.Path.Combine(folder,"ir.json");
        var args=new[]{"--source",input,"--package","Example","--namespace","Example.Structure","--output",output};
        File.WriteAllText(input,"namespace Example.Structure;public static class Rules{public static int F(int a,int b)=>a/b;}");
        Assert.Equal(0,(await Cli(args)).Exit);var bytes=File.ReadAllBytes(output);
        foreach(var source in new[]{"invalid C#","namespace Example.Structure;public class Data<T>{}"})
        {
            File.WriteAllText(input,source);Assert.Equal(1,(await Cli(args)).Exit);Assert.Equal(bytes,File.ReadAllBytes(output));
        }
        var fresh=System.IO.Path.Combine(folder,"fresh.json");args[^1]=fresh;Assert.Equal(1,(await Cli(args)).Exit);Assert.False(File.Exists(fresh));
        Assert.Equal(2,(await Cli(["--bad","x"])).Exit);
    }
    private static IEnumerable<SourceSyntax> Syntax(SourceSyntax source)=>source.Children.SelectMany(Syntax).Prepend(source);
    private static IEnumerable<V> Values(V value)
    {
        yield return value;
        var children=value switch{ApplyValue<Unit,IrType<Unit>> a=>(IReadOnlyList<V>)[a.Function,a.Argument],FieldValue<Unit,IrType<Unit>> f=>[f.Subject],SourceValue<Unit,IrType<Unit>> s=>s.Children,_=>[]};
        foreach(var child in children)foreach(var v in Values(child))yield return v;
    }
    private static async Task<(int Exit,string Text)> Cli(string[] arguments)
    {
        var config=new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var cli=System.IO.Path.Combine(Support.Root,"src/Morphir.Frontend.CSharp.Cli/bin",config,"net10.0/Morphir.Frontend.CSharp.Cli.dll");
        var start=new ProcessStartInfo("dotnet"){RedirectStandardOutput=true,RedirectStandardError=true};start.ArgumentList.Add(cli);
        foreach(var arg in arguments)start.ArgumentList.Add(arg);
        using var process=Process.Start(start)!;var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();return(process.ExitCode,await stdout+await stderr);
    }
}
