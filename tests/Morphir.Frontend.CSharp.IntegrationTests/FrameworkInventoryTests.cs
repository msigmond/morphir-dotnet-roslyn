using System.Text.Json;
using Microsoft.CodeAnalysis;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery","D5")]
public class FrameworkInventoryTests
{
    [Theory]
    [InlineData("numeric-members.json",false)]
    [InlineData("decimal-members.json",true)]
    public void PinnedPublicDeclarationInventoriesRemainExact(string file,bool decimalOnly)
    {
        var names=decimalOnly?new[]{"System.Decimal"}:new[]{"System.Char","System.Byte","System.SByte","System.Int16","System.UInt16","System.Int32","System.UInt32","System.Int64","System.UInt64","System.IntPtr","System.UIntPtr","System.Single","System.Double","System.Half","System.Int128","System.UInt128","System.Numerics.BigInteger","System.Numerics.Complex"};
        var compilation=SourceCompilation.Create([]);
        var actual=names.SelectMany(n=>compilation.GetTypeByMetadataName(n)!.GetMembers().Where(m=>m.DeclaredAccessibility==Accessibility.Public&&m is not IMethodSymbol{MethodKind:MethodKind.PropertyGet or MethodKind.PropertySet}).Select(m=>m.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat))).Order(StringComparer.Ordinal).ToArray();
        using var inventory=JsonDocument.Parse(Support.Read("docs/catalogs/"+file));
        var expected=inventory.RootElement.GetProperty("members").EnumerateArray().Select(m=>m.GetProperty("member").GetString()!).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(actual,expected);
        Assert.Equal("5.0.0",inventory.RootElement.GetProperty("roslyn").GetString());
        Assert.Equal("Microsoft.NETCore.App.Ref/10.0.0",inventory.RootElement.GetProperty("referencePack").GetString());
    }
}
