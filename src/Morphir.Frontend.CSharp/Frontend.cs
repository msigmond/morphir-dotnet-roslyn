using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Morphir.IR.Classic;
using IrPath = Morphir.IR.Classic.Path;

namespace Morphir.Frontend.CSharp;

public sealed record FrontendSettings(string Package, IReadOnlyList<string> Namespaces, IReadOnlyList<ExternalDependencyContract>? Dependencies = null);
public sealed record FrontendDiagnostic(string Code, string Message, string File, int Line, int Column)
{
    public override string ToString() => $"{File}({Line},{Column}): {Code}: {Message}";
}
public sealed record FrontendResult(Library<Unit, IrType<Unit>>? Distribution, IReadOnlyList<FrontendDiagnostic> Diagnostics)
{
    public bool Success => Distribution is not null && Diagnostics.Count == 0;
}
/// <summary>Optional exact target mapping. Pure/total flags remain for API compatibility; structure needs neither assertion.</summary>
public sealed record ExternalFunctionContract(IMethodSymbol Method, FQName Target, bool Pure, bool TotalOnFullInputDomain, string Semantics);
public sealed record ExternalDependencyContract(IrPath Package, PackageSpecification<Unit> Specification, IReadOnlyList<ExternalFunctionContract> Functions);
public static class Frontend
{
    /// <summary>Translate source structure without executing or proving the code.</summary>
    public static FrontendResult Convert(CSharpCompilation compilation, FrontendSettings settings, IReadOnlyList<CSharpCompilation>? sourceProjects = null) =>
        new SourceTranslation(compilation, settings, sourceProjects ?? []).Run();
}
