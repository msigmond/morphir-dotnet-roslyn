using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Morphir.Frontend.CSharp;

/// <summary>D1 explicit-file acquisition against the pinned .NET reference pack. Project loading belongs to D2.</summary>
public static class SourceCompilation
{
    public const string ReferencePackVersion = "10.0.0";
    private static readonly Lazy<MetadataReference[]> References = new(() =>
    {
        var runtime = new DirectoryInfo(RuntimeEnvironment.GetRuntimeDirectory());
        var dotnetRoot = runtime.Parent!.Parent!.Parent!.FullName;
        var referenceDirectory = System.IO.Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref", ReferencePackVersion, "ref", "net10.0");
        if (!Directory.Exists(referenceDirectory)) throw new IOException($"Required .NET reference pack {ReferencePackVersion} was not found at {referenceDirectory}; install SDK 10.0.100.");
        return Directory.GetFiles(referenceDirectory, "*.dll").Order(StringComparer.Ordinal).Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToArray();
    });
    public static CSharpCompilation Create(IEnumerable<(string Path, string Text)> sources, bool checkOverflow = false, Platform platform = Platform.X64) =>
        CSharpCompilation.Create("MorphirSource", sources.Select(s => CSharpSyntaxTree.ParseText(s.Text,
            new CSharpParseOptions(LanguageVersion.CSharp14, DocumentationMode.Diagnose), s.Path)), References.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, platform: platform, checkOverflow: checkOverflow, nullableContextOptions: NullableContextOptions.Enable, allowUnsafe: true));
}
