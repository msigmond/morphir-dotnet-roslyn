using Morphir.IR.Classic;
using Morphir.IR.Json;
using Morphir.Tests;

namespace Morphir.IR.Json.Tests;

public class SourceCodecTests
{
    private sealed record UnmappedValue : Value<Unit,IrType<Unit>>
    { public UnmappedValue() : base(Types.Int) {} }
    [Fact]
    public void UnknownNodesCannotBeEncodedAsStandardMorphir()
    {
        Assert.Throws<NotSupportedException>(() => Codec.Value(new UnmappedValue(), _ => new {}, t => Codec.Type(t, _ => new {})));
        Assert.Null(typeof(Library<Unit,IrType<Unit>>).GetProperty("Source"));
        Assert.DoesNotContain(typeof(Library<Unit,IrType<Unit>>).Assembly.GetExportedTypes(), t => t.Name.StartsWith("Source", StringComparison.Ordinal));
    }
    [Fact]
    public void WrittenGroupingDoesNotChangeStandardOutput()
    {
        var expression = new string('(',80)+"1"+new string(')',80);
        var grouped=Support.Json("namespace Example.D1;public static class Rules{public static int F()=>"+expression+";}");
        var plain=Support.Json("namespace Example.D1;public static class Rules{public static int F()=>1;}");
        Assert.Null(StructuralComparison.Compare(plain,grouped));
    }
}
