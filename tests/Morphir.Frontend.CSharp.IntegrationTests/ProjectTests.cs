using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Morphir.Frontend.CSharp.ProjectSystem;
using Morphir.IR.Classic;
using Morphir.IR.Json;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D2")]
public class ProjectTests
{
    [Fact]
    public void OrdinaryQualifiedCallsHaveCompleteIndependentParity()
    {
        Support.Parity("fixtures/D2/expected/calls.json", Support.Read("fixtures/D2/csharp/Calls.cs"), "Example", "Example.D2.Business", "Example.D2.Shared");
    }
    [Fact]
    public async Task ProjectInputsGeneratorsReferencesSelectionAndBehavior()
    {
        var directory = await Fixture();
        var loaded = await Load(directory);
        var lib = Convert(loaded);
        Assert.Null(StructuralComparison.Compare(Support.Read("fixtures/D2/expected/project.json"), Codec.Encode(lib)));
        Assert.Equal(2, loaded.Inputs.Count);
        File.WriteAllText(System.IO.Path.Combine(directory, "loaded-inputs.json"), System.Text.Json.JsonSerializer.Serialize(loaded.Inputs));
        var business = loaded.Inputs.Single(i => i.Assembly == "Business");
        Assert.Contains("D2_FEATURE", business.Symbols);
        Assert.Equal("Enable", business.Nullable);
        Assert.False(business.CheckOverflow);
        Assert.Contains(business.Sources, s => s.Generated && s.Path.EndsWith("Rules.Generated.cs", StringComparison.Ordinal));
        Assert.Contains(business.AdditionalFiles, f => f.Path.EndsWith("threshold.txt", StringComparison.Ordinal));
        Assert.NotEmpty(business.AnalyzerConfigs);
        Assert.NotEmpty(business.ProjectReferences);
        var failedSelection = Morphir.Frontend.CSharp.Frontend.Convert(loaded.Compilation!, new("Example", ["Example.D2.Business"]), loaded.SourceProjects);
        Assert.Null(failedSelection.Distribution); Assert.Contains(failedSelection.Diagnostics, d => d.Code == "MCS2201" && d.Message.Contains("Example.D2.Shared", StringComparison.Ordinal));
        // Emit/run the actual separate C# assemblies, not a joined or rewritten source compilation.
        var context = new AssemblyLoadContext(Guid.NewGuid().ToString("N"), isCollectible: true);
        var shared = Emit(context, loaded.SourceProjects.Single());
        context.Resolving += (_, name) => name.Name == shared.GetName().Name ? shared : null;
        var businessAssembly = Emit(context, loaded.Compilation!);
        foreach (var age in new[] { int.MinValue, -1, 0, 17, 18, 19, int.MaxValue })
            Assert.Equal(businessAssembly.GetType("Example.D2.Business.Rules")!.GetMethod("Eligible")!.Invoke(null, [age]), Support.Call(lib, "D2.Business.Rules", "Eligible", age));
        Assert.Equal(18, Support.Call(lib, "D2.Business.Rules", "GeneratedThreshold"));
        Assert.Equal(18, Support.Call(lib, "D2.Business.Rules", "Conditional"));
        var privateAdult = Lookup.Value(lib, new(lib.Package, Morphir.IR.Classic.Path.From("D2.Business.Rules"), Name.From("Adult")));
        Assert.NotNull(privateAdult);
        Assert.DoesNotContain(Specifications.Public(lib.Definition).Modules.Single(m => m.Key.Key == Morphir.IR.Classic.Path.From("D2.Business.Rules").Key).Value.Values, e => e.Key.Key == "adult");
        context.Unload();
        // Changing only the referenced source changes both IR and behavior.
        var sharedFile = System.IO.Path.Combine(directory, "Shared", "AgeRule.cs");
        File.WriteAllText(sharedFile, File.ReadAllText(sharedFile).Replace(">= 18", ">= 21", StringComparison.Ordinal));
        var changedReference = Convert(await Load(directory));
        Assert.False((bool)Support.Call(changedReference, "D2.Business.Rules", "Eligible", 18));
        Assert.NotNull(StructuralComparison.Compare(Codec.Encode(lib), Codec.Encode(changedReference)));
        // Conditional symbols and generator additional-file/property inputs are captured independently.
        var project = System.IO.Path.Combine(directory, "Business", "Business.csproj");
        File.WriteAllText(project, File.ReadAllText(project).Replace(";D2_FEATURE", "", StringComparison.Ordinal).Replace("<Threshold>18", "<Threshold>21", StringComparison.Ordinal));
        File.WriteAllText(System.IO.Path.Combine(directory, "Business", "threshold.txt"), "21\n");
        var changed = await Load(directory); var changedLib = Convert(changed);
        Assert.DoesNotContain("D2_FEATURE", changed.Inputs.Single(i => i.Assembly == "Business").Symbols);
        Assert.Equal(21, Support.Call(changedLib, "D2.Business.Rules", "GeneratedThreshold"));
        Assert.Equal(21, Support.Call(changedLib, "D2.Business.Rules", "Conditional"));
    }
    [Fact]
    public async Task LegalFriendAssemblyInternalCallsRemainPrivate()
    {
        var directory = await Fixture();
        var sharedProject = System.IO.Path.Combine(directory, "Shared", "Shared.csproj");
        File.WriteAllText(sharedProject, File.ReadAllText(sharedProject).Replace(";D2_FEATURE", ";D2_FEATURE;INTERNAL_REFERENCE", StringComparison.Ordinal));
        var loaded = await Load(directory); var lib = Convert(loaded);
        Assert.Null(StructuralComparison.Compare(Support.Read("fixtures/D2/expected/internal.json"), Codec.Encode(lib)));
        var sharedModule = lib.Definition.Modules.Single(m => m.Key.Key == Morphir.IR.Classic.Path.From("D2.Shared.AgeRule").Key);
        Assert.Equal(Access.Private, Assert.Single(sharedModule.Value.Value.Values).Value.Access);
        Assert.NotNull(Lookup.Value(lib, new(lib.Package, sharedModule.Key, Name.From("Adult"))));
        Assert.Equal(Access.Private, sharedModule.Value.Access);
        Assert.DoesNotContain(Specifications.Public(lib.Definition).Modules, m => m.Key.Key == sharedModule.Key.Key);
        Assert.True((bool)Support.Call(lib, "D2.Business.Rules", "Eligible", 18));
    }
    [Fact]
    public async Task MissingGeneratorInputGeneratorExceptionCompilerErrorAndExternalCallsFail()
    {
        var directory = await Fixture();
        var input = System.IO.Path.Combine(directory, "Business", "threshold.txt");
        var project = System.IO.Path.Combine(directory, "Business", "Business.csproj");
        File.Move(input, input + ".missing");
        var missing = await Load(directory); Assert.False(missing.Success);
        File.Move(input + ".missing", input);
        File.WriteAllText(input, "invalid\n");
        var failed = await Load(directory); Assert.False(failed.Success); Assert.Contains(failed.Diagnostics, d => d.Message.Contains("D2GEN001", StringComparison.Ordinal));
        File.WriteAllText(input, "-1\n"); File.WriteAllText(project, File.ReadAllText(project).Replace("<Threshold>18", "<Threshold>-1", StringComparison.Ordinal));
        failed = await Load(directory); Assert.False(failed.Success); Assert.Contains(failed.Diagnostics, d => d.Message.Contains("Generator failure fixture", StringComparison.Ordinal));
        File.WriteAllText(input, "18\n"); File.WriteAllText(project, File.ReadAllText(project).Replace("<Threshold>-1", "<Threshold>18", StringComparison.Ordinal));
        var source = System.IO.Path.Combine(directory, "Business", "Rules.cs"); var original = File.ReadAllText(source);
        File.WriteAllText(source, original + "\ninvalid code");
        failed = await Load(directory); Assert.False(failed.Success); Assert.Contains(failed.Diagnostics, d => d.Code == "MCS3003");
        File.WriteAllText(source, original.Replace("Adult(age);", "System.Math.Abs(age);", StringComparison.Ordinal).Replace("public static bool Eligible", "public static int Eligible", StringComparison.Ordinal));
        // Use an actual well-typed external call rather than a compiler error as the rejection evidence.
        File.WriteAllText(source, "namespace Example.D2.Business; public static partial class Rules { public static int External(int value) => System.Math.Abs(value); }");
        var external = await Load(directory); Assert.True(external.Success, string.Join("\n", external.Diagnostics));
        var result = Morphir.Frontend.CSharp.Frontend.Convert(external.Compilation!, new("Example", ["Example.D2.Business", "Example.D2.Shared"]), external.SourceProjects);
        Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, d => d.Code == "MCS2202");
    }
    [Fact]
    public async Task ActualOverflowPlatformAndFrameworkSettingsAreHonored()
    {
        var directory = await Fixture(); var project = System.IO.Path.Combine(directory, "Business", "Business.csproj");
        var original = File.ReadAllText(project);
        File.WriteAllText(project, original.Replace("<Threshold>", "<CheckForOverflowUnderflow>true</CheckForOverflowUnderflow><Threshold>", StringComparison.Ordinal));
        var source = System.IO.Path.Combine(directory, "Business", "More.cs");
        File.WriteAllText(source, "namespace Example.D2.Business; public static class Arithmetic { public static int Add(int value)=>value+0; }");
        var loaded = await Load(directory); Assert.True(loaded.Success, string.Join("\n", loaded.Diagnostics));
        Assert.True(loaded.Inputs.Single(i => i.Assembly == "Business").CheckOverflow);
        var rejected = Morphir.Frontend.CSharp.Frontend.Convert(loaded.Compilation!, new("Example", ["Example.D2.Business", "Example.D2.Shared"]), loaded.SourceProjects);
        Assert.Contains(rejected.Diagnostics, d => d.Code == "MCS2004");
        File.WriteAllText(project, original.Replace("<Threshold>", "<PlatformTarget>x86</PlatformTarget><Threshold>", StringComparison.Ordinal));
        loaded = await Load(directory); Assert.True(loaded.Success, string.Join("\n", loaded.Diagnostics));
        rejected = Morphir.Frontend.CSharp.Frontend.Convert(loaded.Compilation!, new("Example", ["Example.D2.Business", "Example.D2.Shared"]), loaded.SourceProjects);
        Assert.Contains(rejected.Diagnostics, d => d.Code == "MCS1003");
        Assert.Contains((await ProjectLoader.LoadAsync(project, new("net9.0"))).Diagnostics, d => d.Code == "MCS3002");
    }
    [Fact]
    public async Task CumulativeProjectHasCompleteParityAndCliProjectMode()
    {
        var project = System.IO.Path.Combine(Support.Root, "samples", "CurrentSupportedExample", "CurrentSupportedExample.csproj");
        var loaded = await ProjectLoader.LoadAsync(project, new("net10.0"));
        Assert.True(loaded.Success, string.Join("\n", loaded.Diagnostics));
        var lib = Support.Success(Morphir.Frontend.CSharp.Frontend.Convert(loaded.Compilation!, new("Example", ["Example.CurrentSupported"]), loaded.SourceProjects));
        var difference = StructuralComparison.Compare(Support.Read(Support.CurrentSampleReference), Codec.Encode(lib)); Assert.True(difference is null, difference?.ToString());
        Assert.Equal(18, Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "GeneratedThreshold"));
        Assert.True((bool)Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "ProjectEligible", 18));
        Assert.False((bool)Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "ProjectEligible", 17));
        var output = System.IO.Path.Combine(Support.Root, "artifacts", Guid.NewGuid().ToString("N") + ".json");
        var config = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var cli = System.IO.Path.Combine(Support.Root, "src", "Morphir.Frontend.CSharp.Cli", "bin", config, "net10.0", "Morphir.Frontend.CSharp.Cli.dll");
        var result = await Run("dotnet", [cli, "--project", project, "--framework", "net10.0", "--package", "Example", "--namespace", "Example.CurrentSupported", "--output", output]);
        Assert.Equal(0, result.Exit); Assert.Null(StructuralComparison.Compare(Codec.Encode(lib), File.ReadAllText(output)));
        var previous = File.ReadAllBytes(output);
        var invalid = await Fixture(); File.WriteAllText(System.IO.Path.Combine(invalid, "Business", "threshold.txt"), "missing input value");
        var failure = await Run("dotnet", [cli, "--project", System.IO.Path.Combine(invalid, "Business", "Business.csproj"), "--framework", "net10.0", "--package", "Example", "--namespace", "Example.D2.Business", "--output", output]);
        Assert.Equal(1, failure.Exit); Assert.Equal(previous, File.ReadAllBytes(output));
    }
    private static Library<Unit, IrType<Unit>> Convert(ProjectLoadResult loaded)
    {
        Assert.True(loaded.Success, string.Join("\n", loaded.Diagnostics));
        return Support.Success(Morphir.Frontend.CSharp.Frontend.Convert(loaded.Compilation!, new("Example", ["Example.D2.Business", "Example.D2.Shared"]), loaded.SourceProjects));
    }
    private static Task<ProjectLoadResult> Load(string directory) => ProjectLoader.LoadAsync(System.IO.Path.Combine(directory, "Business", "Business.csproj"), new("net10.0"));
    private static Assembly Emit(AssemblyLoadContext context, Microsoft.CodeAnalysis.CSharp.CSharpCompilation compilation)
    {
        using var bytes = new MemoryStream(); var result = compilation.Emit(bytes); Assert.True(result.Success, string.Join("\n", result.Diagnostics)); bytes.Position = 0; return context.LoadFromStream(bytes);
    }
    private static async Task<string> Fixture()
    {
        var source = System.IO.Path.Combine(Support.Root, "fixtures", "D2", "projects");
        var target = System.IO.Path.Combine(Support.Root, "artifacts", "d2-" + Guid.NewGuid().ToString("N"));
        foreach (var path in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = System.IO.Path.GetRelativePath(source, path);
            if (relative.Split(System.IO.Path.DirectorySeparatorChar).Any(p => p is "bin" or "obj")) continue;
            var output = System.IO.Path.Combine(target, relative); Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output)!); File.Copy(path, output);
        }
        var result = await Run("dotnet", ["restore", System.IO.Path.Combine(target, "Business", "Business.csproj"), "--packages", System.IO.Path.Combine(Support.Root, ".packages"), "--locked-mode", "-m:1"]);
        Assert.True(result.Exit == 0, result.Text); return target;
    }
    private static async Task<(int Exit, string Text)> Run(string command, string[] arguments)
    {
        var start = new ProcessStartInfo(command) { RedirectStandardOutput = true, RedirectStandardError = true };
        start.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        foreach (var arg in arguments) start.ArgumentList.Add(arg);
        using var process = Process.Start(start)!; var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(); return (process.ExitCode, await stdout + await stderr);
    }
}
