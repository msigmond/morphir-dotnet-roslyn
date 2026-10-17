using System.Text.Json;
using Microsoft.CodeAnalysis;
using Morphir.Frontend.CSharp;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D5")]
public class NumericCatalogTests
{
    [Fact]
    public void NumericPublicMemberInventoryMatchesThePinnedReferencePack()
    {
        var compilation = SourceCompilation.Create([]);
        var names = new[] { "System.Char", "System.Byte", "System.SByte", "System.Int16", "System.UInt16", "System.Int32", "System.UInt32", "System.Int64", "System.UInt64", "System.IntPtr", "System.UIntPtr", "System.Single", "System.Double", "System.Half", "System.Int128", "System.UInt128", "System.Numerics.BigInteger", "System.Numerics.Complex" };
        var actual = names.SelectMany(name => compilation.GetTypeByMetadataName(name)!.GetMembers()
            .Where(m => m.DeclaredAccessibility == Accessibility.Public && m is not IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet })
            .Select(m => m.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat))).Order(StringComparer.Ordinal).ToArray();
        using var inventory = JsonDocument.Parse(Support.Read("docs/catalogs/numeric-members.json"));
        var expected = inventory.RootElement.GetProperty("members").EnumerateArray().Select(m => m.GetProperty("member").GetString()!).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal("5.0.0", inventory.RootElement.GetProperty("roslyn").GetString());
        Assert.Equal("Microsoft.NETCore.App.Ref/10.0.0", inventory.RootElement.GetProperty("referencePack").GetString());
        Assert.Equal(actual, expected);
    }
    [Fact]
    public void NumericRecordSlotsPreserveTheirExactRepresentationsAndHelperOwnerCollisionsFail()
    {
        const string code = """
        using System.Numerics;
        namespace Example.D5;
        public readonly record struct Numbers(long Count,double Amount,char Symbol,BigInteger Units,Complex Pair);
        public static class Rules {
          public static Numbers Pack(long count,double amount,char symbol,BigInteger units,Complex pair)=>new(count,amount,symbol,units,pair);
          public static long Count(Numbers value)=>value.Count;
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D5"]));
        var number = Support.Integer(9007199254740993L); var big = Support.Integer(System.Numerics.BigInteger.One << 256);
        var complex = new Dictionary<string, object> { { "real", -0.0 }, { "imaginary", double.PositiveInfinity } };
        var packed = (Dictionary<string, object>)Support.Call(lib, "D5.Rules", "Pack", number, 3.5, 65535, big, complex);
        Assert.Equal(number, packed["count"]); Assert.Equal(big, packed["units"]); Assert.Equal(complex, packed["pair"]);
        Assert.Equal(65535, packed["symbol"]); Assert.Equal(3.5, packed["amount"]);
        Assert.Equal(number, Support.Call(lib, "D5.Rules", "Count", packed));
        const string collision = "namespace Example.Compat {public static class Integers {public static int F()=>0;}} namespace Example.D5 {public static class Rules {public static int F(int a)=>a+1;}}";
        var rejected = Support.Convert(collision, selectors: ["Example"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, d => d.Code == "MCS1004");
    }
}
