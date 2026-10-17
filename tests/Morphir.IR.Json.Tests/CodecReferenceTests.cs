using Morphir.IR.Classic;
using Morphir.IR.Json;
using Morphir.Tests;
using IrPath = Morphir.IR.Classic.Path;

namespace Morphir.IR.Json.Tests;

[Trait("Delivery", "D1")]
public class CodecReferenceTests
{
    [Fact]
    public void CompleteIndependentDependencySpecificationAndDocumentation()
    {
        var name = Name.From("Answer"); var module = IrPath.From("Rules");
        var dependency = new PackageSpecification<Unit>([new(module,new(
            [new(Name.From("Age"),new("dependency alias",new([],Types.Int)))],
            [new(name,new("external constant",new([],Types.Int)))],"external module"))]);
        var lib = new Library<Unit, IrType<Unit>>(IrPath.From("Model"), [new(IrPath.From("External"), dependency)], new([
            new(module,new(Access.Public,new([], [new(name,new(Access.Public,new("qualified reference",new([],Types.Int,
                new ReferenceValue<Unit,IrType<Unit>>(Types.Int,new(IrPath.From("External"),module,name))))))],"model codec coverage")))]));
        var difference = StructuralComparison.Compare(Support.Read("fixtures/D1/expected/model.json"), Codec.Encode(lib));
        Assert.True(difference is null, difference?.ToString());
    }
    [Fact]
    public void PublicSpecificationsFilterPrivateDefinitions()
    {
        var lib = Support.Success(Support.Convert(Support.Read("fixtures/D1/csharp/Private.cs"), "Sample", ["Sample.Business"]));
        var spec = Specifications.Public(lib.Definition);
        Assert.Equal(2, spec.Modules.Count);
        Assert.All(spec.Modules, m => { Assert.Empty(m.Value.Types); Assert.Empty(m.Value.Values); });
        var publicLib = Support.Success(Support.Convert(Support.Read("fixtures/D1/csharp/Eligibility.cs")));
        var publicSpec = Specifications.Public(publicLib.Definition);
        Assert.Single(publicSpec.Modules.SelectMany(m => m.Value.Types));
        Assert.Equal(5, publicSpec.Modules.SelectMany(m => m.Value.Values).Count());
    }
}
