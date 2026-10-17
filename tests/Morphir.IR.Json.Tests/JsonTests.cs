using System.Text.Json;
using Morphir.IR.Classic;
using Morphir.IR.Json;
using Morphir.Tests;

namespace Morphir.IR.Json.Tests;

[Trait("Delivery", "D1")]
public class JsonTests
{
    [Fact]
    public void ObjectPropertyOrderAndWhitespaceAreIrrelevant()
    {
        Assert.Null(StructuralComparison.Compare("{\"x\":[1,2],\"y\":{\"a\":true,\"b\":null}}", " {\"y\":{\"b\":null,\"a\":true},\"x\":[1,2]}"));
        Assert.Null(StructuralComparison.Compare("1", "1.0"));
    }
    [Theory]
    [InlineData("[1,2]", "[2,1]")]
    [InlineData("{}", "{\"x\":null}")]
    [InlineData("null", "{}")]
    [InlineData("{}", "[]")]
    [InlineData("1", "\"1\"")]
    [InlineData("true", "false")]
    [InlineData("\"Public\"", "\"public\"")]
    [InlineData("[[[\"a\"],1],[[\"b\"],2]]", "[[[\"b\"],2],[[\"a\"],1]]")]
    public void ExactStructuralDifferencesFail(string e, string a) => Assert.NotNull(StructuralComparison.Compare(e, a));
    [Theory]
    [InlineData("{\"x\":1,\"x\":1}", "{}")]
    [InlineData("{}", "{\"x\":1,\"x\":1}")]
    [InlineData("[]", "[{\"x\":1,\"x\":2}]")]
    public void DuplicateKeysFailEvenInOtherwiseUnequalSubtrees(string e, string a) => Assert.Throws<JsonException>(() => StructuralComparison.Compare(e, a));
    [Fact]
    public void MismatchReportsJsonPathAndValues()
    {
        var d = StructuralComparison.Compare("{\"m\":[1]}", "{\"m\":[2]}")!;
        Assert.Equal("$.m[0]", d.Path); Assert.Equal("1", d.Expected); Assert.Equal("2", d.Actual);
    }
    [Fact]
    public void EveryDistributionFieldIsCompared()
    {
        var expected = Support.Read("fixtures/D1/expected/eligibility.json");
        foreach (var text in new[] { expected.Replace("Public", "Private"), expected.Replace("\"doc\":null", "\"doc\":\"\""), expected.Replace("\"formatVersion\":3", "\"formatVersion\":2"), expected.Replace("\"Reference\",{}", "\"Reference\",null") })
            Assert.NotNull(StructuralComparison.Compare(expected, text));
    }
    [Fact]
    public void AtomicWriteIsDeterministicUtf8AndLeavesNoTemporaryFile()
    {
        var lib = Support.Success(Support.Convert(Support.Read("fixtures/D1/csharp/Eligibility.cs")));
        var dir = System.IO.Path.Combine(Support.Root, "artifacts", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
        var path = System.IO.Path.Combine(dir, "ir.json");
        Codec.WriteAtomic(path, lib); var first = File.ReadAllBytes(path); Codec.WriteAtomic(path, lib);
        Assert.Equal(first, File.ReadAllBytes(path)); Assert.Single(Directory.GetFiles(dir));
        Assert.NotEqual((byte)0xef, first[0]);
    }
}
