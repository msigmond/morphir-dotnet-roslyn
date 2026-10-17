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
            [new(Name.From("Age"),new("dependency alias",new AliasSpecification<Unit>([],Types.Int)))],
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

[Trait("Delivery", "D6")]
public class DecimalCodecReferenceTests
{
    [Fact]
    public void ExactDecimalLiteralMatchesTheIndependentCompleteElmCodecDistribution()
    {
        var definition = new ValueDefinition<Unit, IrType<Unit>>([], Types.Decimal, new LiteralValue<Unit, IrType<Unit>>(Types.Decimal, new DecimalLiteral("0.1")));
        var model = new Library<Unit, IrType<Unit>>(IrPath.From("Example"), [], new([
            new(IrPath.From("D6.Model"), new(Access.Public, new([], [new(Name.From("exactTenth"), new(Access.Public, new("Exact decimal literal payload", definition)))])))]));
        Assert.Null(StructuralComparison.Compare(Support.Read("fixtures/D6/expected/model-literal.json"), Codec.Encode(model)));
    }
}

[Trait("Delivery", "D8")]
public class CustomCodecReferenceTests
{
    [Fact]
    public void PublicAndOpaqueCustomSpecificationsMatchCompleteOriginalCodecReference()
    {
        IReadOnlyList<Entry<Name, IReadOnlyList<Entry<Name, IrType<Unit>>>>> constructors = [new(Name.From("Token"), [new(Name.From("arg1"), Types.Int)])];
        var definitions = new PackageDefinition<Unit, IrType<Unit>>([new(IrPath.From("Types"),new(Access.Public,new(
            [new(Name.From("Hidden"),new(Access.Public,new("private constructors",new CustomDefinition<Unit>([],new(Access.Private,constructors))))),
             new(Name.From("Token"),new(Access.Public,new("public constructors",new CustomDefinition<Unit>([],new(Access.Public,constructors)))))],[])))]);
        var spec = Specifications.Public(definitions); Assert.IsType<OpaqueSpecification<Unit>>(spec.Modules.Single().Value.Types.First().Value.Value);
        Assert.IsType<CustomSpecification<Unit>>(spec.Modules.Single().Value.Types.Last().Value.Value);
        var lib = new Library<Unit, IrType<Unit>>(IrPath.From("Model"), [new(IrPath.From("External"), spec)], definitions);
        var difference = StructuralComparison.Compare(Support.Read("fixtures/D8/expected/model.json"), Codec.Encode(lib)); Assert.True(difference is null, difference?.ToString());
    }
}
