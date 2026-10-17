using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Morphir.Frontend.CSharp;
using Morphir.IR.Classic;
using Morphir.Tests;
using IrPath = Morphir.IR.Classic.Path;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D7")]
public class ExternalCallTests
{
    [Fact]
    public void ExactReviewedPureTotalExternalContractRetainsSpecificationDefaultsAndOrder()
    {
        var trusted = Support.Compile(Support.Read("fixtures/D7/external/Trusted.cs")).WithAssemblyName("TrustedContract");
        using var trustedImage = new MemoryStream(); var emission = trusted.Emit(trustedImage); Assert.True(emission.Success, string.Join("\n", emission.Diagnostics));
        var code = Support.Read("fixtures/D7/csharp/External.cs");
        var compilation = Support.Compile(code).AddReferences(MetadataReference.CreateFromImage(trustedImage.ToArray()));
        var method = (IMethodSymbol)compilation.GetTypeByMetadataName("Trusted.Rules")!.GetMembers("Meets").Single();
        var target = new FQName(IrPath.From("Reviewed"), IrPath.From("Rules"), Name.From("meets"));
        var specification = new PackageSpecification<Unit>([new(IrPath.From("Rules"), new([], [new(Name.From("meets"), new("Pure total threshold comparison", new([new(Name.From("age"), Types.Int), new(Name.From("minimum"), Types.Int)], Types.Bool)))], "Reviewed external values"))]);
        var contract = new ExternalFunctionContract(method, target, true, true, "All CLR Int32 age/minimum pairs; exactly age >= minimum; no effects or failures.");
        ExternalDependencyContract Dependency(ExternalFunctionContract function) => new(target.Package, specification, [function]);
        var lib = Support.Success(Morphir.Frontend.CSharp.Frontend.Convert(compilation, new("Example", ["Example.D7"], [Dependency(contract)])));
        Assert.Null(Morphir.IR.Json.StructuralComparison.Compare(Support.Read("fixtures/D7/expected/external.json"), Morphir.IR.Json.Codec.Encode(lib)));
        Assert.Single(lib.Dependencies); Assert.DoesNotContain(lib.Definition.Modules, module => module.Key.Key == IrPath.From("Reviewed.Rules").Key);
        var context = new AssemblyLoadContext(Guid.NewGuid().ToString(), true); trustedImage.Position = 0; var trustedAssembly = context.LoadFromStream(trustedImage);
        using var image = new MemoryStream(); var rootEmission = compilation.Emit(image); Assert.True(rootEmission.Success, string.Join("\n", rootEmission.Diagnostics)); image.Position = 0; var rules = context.LoadFromStream(image).GetType("Example.D7.Rules")!;
        var external = trustedAssembly.GetType("Trusted.Rules")!.GetMethod("Meets")!;
        object? Resolve(FQName name) => name.Package.Key == target.Package.Key && name.Module.Key == target.Module.Key && name.Local.Key == target.Local.Key ? (Func<object, object>)(age => (Func<object, object>)(minimum => external.Invoke(null, [age, minimum])!)) : null;
        foreach (var value in new[] { int.MinValue, 0, 17, 18, int.MaxValue }) foreach (var name in new[] { "Eligible", "DefaultEligible", "Order", "Reference" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.WithExternalValues(Resolve, () => Support.Call(lib, "D7.Rules", name, value)));
        foreach (var invalid in new[] { contract with { Pure = false }, contract with { TotalOnFullInputDomain = false }, contract with { Semantics = "" } })
        { var rejected = Morphir.Frontend.CSharp.Frontend.Convert(compilation, new("Example", ["Example.D7"], [Dependency(invalid)])); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, d => d.Code == "MCS2809"); }
        var unknown = Morphir.Frontend.CSharp.Frontend.Convert(compilation, new("Example", ["Example.D7"])); Assert.Null(unknown.Distribution); Assert.Contains(unknown.Diagnostics, d => d.Code == "MCS2202");
        context.Unload();
    }
}
