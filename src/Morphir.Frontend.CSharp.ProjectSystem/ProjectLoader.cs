using System.Diagnostics;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;

namespace Morphir.Frontend.CSharp.ProjectSystem;

public sealed record ProjectSettings(string Framework, string Configuration = "Debug", IReadOnlyDictionary<string, string>? Properties = null);
public sealed record SourceInput(string Path, string TextSha256, bool Generated);
public sealed record ReferenceInput(string Path, string Sha256);
public sealed record ProjectInput(string Project, string Assembly, string Framework, string Configuration, string LanguageVersion,
    IReadOnlyList<string> Symbols, string Nullable, bool CheckOverflow, string Platform,
    IReadOnlyList<SourceInput> Sources, IReadOnlyList<SourceInput> AdditionalFiles, IReadOnlyList<ReferenceInput> References,
    IReadOnlyList<string> Generators, IReadOnlyList<SourceInput> AnalyzerConfigs, IReadOnlyDictionary<string, string> Properties, IReadOnlyList<string> ProjectReferences);
public sealed record ProjectLoadResult(CSharpCompilation? Compilation, IReadOnlyList<CSharpCompilation> SourceProjects,
    IReadOnlyList<FrontendDiagnostic> Diagnostics, IReadOnlyList<ProjectInput> Inputs)
{
    public bool Success => Compilation is not null && Diagnostics.Count == 0;
}

/// <summary>Build-verified MSBuild inputs, followed by an explicit equivalent generator pass for every source project.</summary>
public static class ProjectLoader
{
    private static readonly object Registration = new();
    public static async Task<ProjectLoadResult> LoadAsync(string projectPath, ProjectSettings settings, CancellationToken cancellationToken = default)
    {
        projectPath = Path.GetFullPath(projectPath);
        if (!File.Exists(projectPath) || Path.GetExtension(projectPath) != ".csproj") return Failure("MCS3001", "An existing .csproj input is required.", projectPath);
        if (settings.Framework != "net10.0") return Failure("MCS3002", "Explicit framework net10.0 is required by the fixed .NET 10 baseline.", projectPath);
        var properties = new Dictionary<string, string>(settings.Properties ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase)
        {
            ["TargetFramework"] = settings.Framework,
            ["Configuration"] = settings.Configuration
        };
        if (properties.Keys.Any(k => !System.Text.RegularExpressions.Regex.IsMatch(k, "^[A-Za-z_][A-Za-z0-9_]*$")))
            return Failure("MCS3001", "Invalid MSBuild property name.", projectPath);
        var versionStart = new ProcessStartInfo("dotnet", "--version") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        using (var versionProcess = Process.Start(versionStart)!)
        {
            var output = versionProcess.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = versionProcess.StandardError.ReadToEndAsync(cancellationToken);
            await versionProcess.WaitForExitAsync(cancellationToken);
            var version = (await output).Trim();
            if (versionProcess.ExitCode != 0 || version != "10.0.100")
                return Failure("MCS3002", "The build must resolve exact SDK 10.0.100; resolved " + version + ". " + await error, projectPath);
        }
        var build = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        build.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        foreach (var arg in new[] { "build", projectPath, "--framework", settings.Framework, "--configuration", settings.Configuration, "--no-restore", "--nologo", "-m:1", "-nr:false", "-v:minimal" }) build.ArgumentList.Add(arg);
        foreach (var property in properties.OrderBy(p => p.Key, StringComparer.Ordinal))
            build.ArgumentList.Add("-p:" + property.Key + "=" + Escape(property.Value));
        try
        {
            using var process = Process.Start(build)!;
            var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken); var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
            try { await process.WaitForExitAsync(cancellationToken); }
            finally { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            var output = await stdout + await stderr;
            if (process.ExitCode != 0) return Failure("MCS3003", "Project build failed; restore the selected framework and correct compiler/generator errors.\n" + output.Trim(), projectPath);
            lock (Registration)
            {
                if (!MSBuildLocator.IsRegistered)
                {
                    var runtime = new DirectoryInfo(RuntimeEnvironment.GetRuntimeDirectory());
                    var sdk = Path.Combine(runtime.Parent!.Parent!.Parent!.FullName, "sdk", "10.0.100");
                    MSBuildLocator.RegisterMSBuildPath(sdk);
                }
            }
            return await LoadWorkspaceAsync(projectPath, settings, properties, cancellationToken);
        }
        catch (Exception e) when (e is IOException or InvalidOperationException or System.ComponentModel.Win32Exception or ArgumentException)
        { return Failure("MCS3001", e.Message, projectPath); }
    }
    private static string Escape(string value) => value.Replace("%", "%25", StringComparison.Ordinal).Replace(";", "%3B", StringComparison.Ordinal).Replace(",", "%2C", StringComparison.Ordinal);
    private static ProjectLoadResult Failure(string code, string message, string path) => new(null, [], [new(code, message, path, 1, 1)], []);
    private static string TextHash(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<ProjectLoadResult> LoadWorkspaceAsync(string projectPath, ProjectSettings settings, Dictionary<string, string> properties, CancellationToken cancellationToken)
    {
        using var workspace = MSBuildWorkspace.Create(properties);
        workspace.LoadMetadataForReferencedProjects = false;
        var diagnostics = new System.Collections.Concurrent.ConcurrentQueue<FrontendDiagnostic>();
        using var handler = workspace.RegisterWorkspaceFailedHandler(args =>
        {
            if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                diagnostics.Enqueue(new("MCS3004", args.Diagnostic.Message, projectPath, 1, 1));
        });
        var root = await workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken);
        var built = new Dictionary<ProjectId, CSharpCompilation>();
        var inputs = new List<ProjectInput>();
        async Task<CSharpCompilation> Build(Project project)
        {
            if (built.TryGetValue(project.Id, out var existing)) return existing;
            var references = new List<MetadataReference>(project.MetadataReferences);
            foreach (var sourceReference in project.ProjectReferences)
            {
                var referenced = project.Solution.GetProject(sourceReference.ProjectId)!;
                var compiled = await Build(referenced);
                references.Add(compiled.ToMetadataReference(sourceReference.Aliases, sourceReference.EmbedInteropTypes));
            }
            var trees = new List<SyntaxTree>();
            foreach (var document in project.Documents.OrderBy(d => d.FilePath, StringComparer.Ordinal))
                if (await document.GetSyntaxTreeAsync(cancellationToken) is { } tree) trees.Add(tree);
            var compilation = CSharpCompilation.Create(project.AssemblyName, trees, references, (CSharpCompilationOptions)project.CompilationOptions!);
            var generators = new List<ISourceGenerator>();
            foreach (var reference in project.AnalyzerReferences)
            {
                if (reference is AnalyzerFileReference file) file.AnalyzerLoadFailed += (_, args) => diagnostics.Enqueue(new("MCS3005", args.Message, file.FullPath, 1, 1));
                generators.AddRange(reference.GetGenerators(LanguageNames.CSharp));
            }
            var parse = (CSharpParseOptions)project.ParseOptions!;
            var analyzerOptions = project.AnalyzerOptions;
            foreach (var input in analyzerOptions.AdditionalFiles)
                if (input.GetText(cancellationToken) is null)
                    diagnostics.Enqueue(new("MCS3005", "Configured generator AdditionalFile is unavailable.", input.Path, 1, 1));
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generators, analyzerOptions.AdditionalFiles, parse, analyzerOptions.AnalyzerConfigOptionsProvider);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var generated, out var generatorDiagnostics, cancellationToken);
            foreach (var result in driver.GetRunResult().Results)
                if (result.Exception is not null) diagnostics.Enqueue(new("MCS3006", result.Exception.Message, project.FilePath!, 1, 1));
            void Report(Diagnostic diagnostic)
            {
                if (diagnostic.Severity != DiagnosticSeverity.Error && diagnostic.Id is not ("CS8784" or "CS8785")) return;
                var location = diagnostic.Location.GetLineSpan();
                diagnostics.Enqueue(new("MCS3007", diagnostic.Id + ": " + diagnostic.GetMessage(), string.IsNullOrEmpty(location.Path) ? project.FilePath! : location.Path, location.StartLinePosition.Line + 1, location.StartLinePosition.Character + 1));
            }
            foreach (var diagnostic in generatorDiagnostics.Concat(generated.GetDiagnostics(cancellationToken))) Report(diagnostic);
            var analyzers = project.AnalyzerReferences.SelectMany(r => r.GetAnalyzers(LanguageNames.CSharp)).ToImmutableArray();
            if (analyzers.Length != 0)
                foreach (var diagnostic in await generated.WithAnalyzers(analyzers, analyzerOptions).GetAnalyzerDiagnosticsAsync(cancellationToken)) Report(diagnostic);
            var updated = (CSharpCompilation)generated;
            built.Add(project.Id, updated);
            var allSources = updated.SyntaxTrees.Select(t => new SourceInput(t.FilePath, TextHash(t.GetText(cancellationToken).ToString()), !trees.Contains(t))).OrderBy(t => t.Path, StringComparer.Ordinal).ToArray();
            var additional = analyzerOptions.AdditionalFiles.Select(f => new SourceInput(f.Path, TextHash(f.GetText(cancellationToken)?.ToString() ?? ""), false)).OrderBy(f => f.Path, StringComparer.Ordinal).ToArray();
            var metadata = project.MetadataReferences.OfType<PortableExecutableReference>().Where(r => r.FilePath is not null).OrderBy(r => r.FilePath, StringComparer.Ordinal)
                .Select(r => new ReferenceInput(r.FilePath!, Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(r.FilePath!))))).ToArray();
            var configs = new List<SourceInput>();
            foreach (var document in project.AnalyzerConfigDocuments.OrderBy(d => d.FilePath, StringComparer.Ordinal))
                configs.Add(new(document.FilePath!, TextHash((await document.GetTextAsync(cancellationToken)).ToString()), false));
            inputs.Add(new(project.FilePath!, project.AssemblyName!, settings.Framework, settings.Configuration, parse.LanguageVersion.ToString(),
                parse.PreprocessorSymbolNames.Order(StringComparer.Ordinal).ToArray(), updated.Options.NullableContextOptions.ToString(), updated.Options.CheckOverflow,
                updated.Options.Platform.ToString(), allSources, additional, metadata, generators.Select(g => g.GetGeneratorType().AssemblyQualifiedName!).Order(StringComparer.Ordinal).ToArray(), configs, new SortedDictionary<string, string>(properties, StringComparer.Ordinal),
                project.ProjectReferences.Select(r => project.Solution.GetProject(r.ProjectId)!.FilePath + "|" + string.Join(",", r.Aliases) + "|embed=" + r.EmbedInteropTypes).Order(StringComparer.Ordinal).ToArray()));
            return updated;
        }
        var rootCompilation = await Build(root);
        return diagnostics.Count == 0
            ? new(rootCompilation, built.Values.Where(c => !ReferenceEquals(c, rootCompilation)).ToArray(), [], inputs.OrderBy(i => i.Project, StringComparer.Ordinal).ToArray())
            : new(null, [], diagnostics.Distinct().OrderBy(d => d.File, StringComparer.Ordinal).ThenBy(d => d.Line).ThenBy(d => d.Column).ThenBy(d => d.Code, StringComparer.Ordinal).ThenBy(d => d.Message, StringComparer.Ordinal).ToArray(), inputs);
    }
}
