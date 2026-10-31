using System.Text.Json;
using Morphir.Frontend.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Morphir.IR.Classic;

namespace Morphir.Tests;

internal sealed record ParityCase(string Id, string Reference, string Package, string[] Namespaces, string[] Sources, string[] Features, string? Dependency);
internal sealed record ElmReference(string Id, string Project, string Expected);

internal static class ElmParity
{
    private static JsonDocument Catalog() => JsonDocument.Parse(Support.Read("fixtures/elm-parity/cases.json"));
    public static IReadOnlyList<ParityCase> Cases
    {
        get
        {
            using var json = Catalog();
            return json.RootElement.GetProperty("cases").EnumerateArray().Select(c => new ParityCase(
                c.GetProperty("id").GetString()!, c.GetProperty("reference").GetString()!, c.GetProperty("package").GetString()!,
                Strings(c, "namespaces"), Strings(c, "sources"), Strings(c, "features"), c.TryGetProperty("dependency", out var dep) ? dep.GetString() : null)).ToArray();
        }
    }
    public static IReadOnlyList<ElmReference> References
    {
        get
        {
            using var json = Catalog();
            return json.RootElement.GetProperty("references").EnumerateObject().Select(r =>
                new ElmReference(r.Name, r.Value.GetProperty("project").GetString()!, r.Value.GetProperty("expected").GetString()!)).ToArray();
        }
    }
    public static string[] AuditIds
    {
        get { using var json = Catalog(); return Strings(json.RootElement, "auditIds"); }
    }
    public static string[] RejectedAuditIds
    {
        get { using var json = Catalog(); return Strings(json.RootElement,"rejectedAuditIds"); }
    }
    private static string[] Strings(JsonElement value, string key) => value.GetProperty(key).EnumerateArray().Select(x => x.GetString()!).ToArray();
    public static FrontendResult Convert(string id)
    {
        var item = Cases.Single(c => c.Id == id);
        using var catalog = Catalog();
        var settings = catalog.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("id").GetString() == id);
        var check = settings.TryGetProperty("checkOverflow", out var overflow) && overflow.GetBoolean();
        var compilation = SourceCompilation.Create(item.Sources.Select(p => (p, Support.Read(p))), check);
        if (settings.TryGetProperty("symbols", out var symbols))
        {
            var options = new CSharpParseOptions(LanguageVersion.CSharp14, DocumentationMode.Diagnose,
                preprocessorSymbols: symbols.EnumerateArray().Select(s => s.GetString()!));
            compilation = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(item.Sources.Select(p => CSharpSyntaxTree.ParseText(Support.Read(p), options, p)));
        }
        if (item.Dependency is null) return Morphir.Frontend.CSharp.Frontend.Convert(compilation, new(item.Package, item.Namespaces));
        var owner = settings.TryGetProperty("dependencyType",out var declaringType)?declaringType.GetString()!:"System.Math";
        var member = settings.TryGetProperty("dependencyMember",out var methodName)?methodName.GetString()!:"Abs";
        var method = compilation.GetTypeByMetadataName(owner)!.GetMembers(member).OfType<IMethodSymbol>()
            .Single(m => owner!="System.Math" || m.Parameters.Length == 1 && m.Parameters[0].Type.SpecialType == SpecialType.System_Int32);
        var package = Morphir.IR.Classic.Path.From(item.Dependency);
        var module = Morphir.IR.Classic.Path.From("Rules"); var name = Name.From(settings.TryGetProperty("dependencyValue",out var target)?target.GetString()!:"Abs");
        Entry<Name,IrType<Unit>>[] inputs=method.Parameters.Length==0?[]:[new(Name.From("x"),Types.Int)];
        var specification = new PackageSpecification<Unit>([new(module, new([], [new(name, new("", new(inputs, Types.Int)))]))]);
        var contract = new ExternalFunctionContract(method, new(package, module, name), false, false, "");
        return Morphir.Frontend.CSharp.Frontend.Convert(compilation, new(item.Package, item.Namespaces, [new(package, specification, [contract])]));
    }
    public static string Expected(string id) => Support.Read(References.Single(r => r.Id == Cases.Single(c => c.Id == id).Reference).Expected);
}
