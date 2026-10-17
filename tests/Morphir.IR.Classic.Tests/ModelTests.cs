using Morphir.IR.Classic;

namespace Morphir.IR.Classic.Tests;

[Trait("Delivery", "D1")]
public class ModelTests
{
    [Theory]
    [InlineData("SDK", "s,d,k")]
    [InlineData("valueInUSD", "value,in,u,s,d")]
    [InlineData("_fooBar 123", "foo,bar,123")]
    public void NamesFollowUpstreamTokenization(string input, string words) => Assert.Equal(words, string.Join(",", Name.From(input).Words));
    [Fact]
    public void TypeAndValueAttributeParametersAreIndependent()
    {
        IrType<string> t = new RecordType<string>("source", [new(Name.From("Age"), new ReferenceType<string>("field", new(Morphir.IR.Classic.Path.From("Morphir.SDK"), Morphir.IR.Classic.Path.From("Basics"), Name.From("Int")), []))]);
        Value<string, IrType<string>> v = new RecordValue<string, IrType<string>>(t, []);
        Assert.Same(t, v.Attributes);
    }
}
