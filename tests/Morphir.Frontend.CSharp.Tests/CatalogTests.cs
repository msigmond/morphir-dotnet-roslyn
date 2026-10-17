using System.Text.Json;
using Microsoft.CodeAnalysis;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.Tests;

[Trait("Delivery", "D2")]
public class CatalogTests
{
    [Fact]
    public void OperationInventoryExactlyCoversPinnedRoslynNames()
    {
        using var catalog = JsonDocument.Parse(Support.Read("docs/catalogs/operations.json"));
        Assert.Equal("5.0.0", catalog.RootElement.GetProperty("roslyn").GetString());
        var entries = catalog.RootElement.GetProperty("operations").EnumerateArray().ToArray();
        Assert.Equal(Enum.GetNames<OperationKind>().Order(StringComparer.Ordinal), entries.Select(e => e.GetProperty("operation").GetString()!).Order(StringComparer.Ordinal));
        Assert.All(entries, e => Validate(e));
    }
    [Fact]
    public void EveryRequiredFixedTypeAndLanguageFormHasAnOwnerAndState()
    {
        using var framework = JsonDocument.Parse(Support.Read("docs/catalogs/framework.json"));
        var entries = framework.RootElement.GetProperty("types").EnumerateArray().ToArray();
        Assert.Equal(entries.Length, entries.Select(e => e.GetProperty("type").GetString()).Distinct().Count());
        Assert.All(entries, e => Validate(e));
        foreach (var expected in new[] { "System.Half", "System.Int128", "System.UInt128", "System.Numerics.BigInteger", "System.Numerics.Complex", "System.DateOnly", "System.TimeOnly", "System.DateTime", "System.DateTimeOffset", "System.TimeSpan", "System.Guid" })
            Assert.Contains(entries, e => e.GetProperty("type").GetString() == expected);
        Assert.Equal(14, entries.Count(e => e.GetProperty("type").GetString()!.StartsWith("System.Collections.Generic.", StringComparison.Ordinal)));
        Assert.Equal(6, entries.Count(e => e.GetProperty("type").GetString()!.StartsWith("System.Collections.Immutable.", StringComparison.Ordinal)));
        Assert.Equal(2, entries.Count(e => e.GetProperty("type").GetString()!.StartsWith("System.Collections.ObjectModel.", StringComparison.Ordinal)));
        using var forms = JsonDocument.Parse(Support.Read("docs/catalogs/forms.json"));
        var allForms = forms.RootElement.GetProperty("forms").EnumerateArray().ToArray();
        Assert.All(allForms, e => Validate(e));
        foreach (var owner in new[] { "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "D10", "D11", "D12", "D13", "D14", "D15", "D16", "D17", "D18.1", "D18.2", "D18.3", "D18.4" })
            Assert.Contains(allForms, e => e.GetProperty("owner").GetString() == owner);
        Assert.Contains(allForms, e => e.GetProperty("state").GetString() == "pending");
    }
    private static void Validate(JsonElement entry)
    {
        var owner = entry.GetProperty("owner").GetString();
        Assert.Matches("^D(?:[1-9]|1[0-7]|18\\.[1-4])$", owner!);
        Assert.Contains(entry.GetProperty("state").GetString(), new[] { "supported", "pending", "required-rejection" });
    }
}
