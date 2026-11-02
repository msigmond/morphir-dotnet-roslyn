using System.Diagnostics;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Morphir.Frontend.CSharp.ProjectSystem;
using Morphir.IR.Json;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Contract", "ExactElmParity")]
public class StructureIntegrationTests
{
    [Fact]
    public async Task PinnedElmMakeAndCSharpProduceIdenticalCompleteJsonForEveryCase()
    {
        using var version = JsonDocument.Parse(Support.Read("eng/node_modules/morphir-elm/package.json"));
        using var pinned = JsonDocument.Parse(Support.Read("eng/reference-toolchain.json"));
        Assert.Equal(pinned.RootElement.GetProperty("morphirElmVersion").GetString(), version.RootElement.GetProperty("version").GetString());
        var folder = System.IO.Path.Combine(Support.Root,"artifacts","live-elm-parity-"+Guid.NewGuid().ToString("N"));
        foreach (var reference in ElmParity.References)
        {
            var project=System.IO.Path.Combine(folder,reference.Id);CopyTree(System.IO.Path.Combine(Support.Root,reference.Project),project);
            var output=System.IO.Path.Combine(project,"ir.json");
            var run=await Run("node",[System.IO.Path.Combine(Support.Root,"eng/node_modules/morphir-elm/cli/morphir-elm.js"),"make","-p",project,"-o",output],project);
            Assert.True(run.Exit==0,reference.Id+": "+run.Text);
            var fresh=File.ReadAllText(output);
            Assert.Null(StructuralComparison.Compare(Support.Read(reference.Expected),fresh));
            foreach(var item in ElmParity.Cases.Where(c=>c.Reference==reference.Id))
            {
                var difference=StructuralComparison.Compare(fresh,Codec.Encode(Support.Success(ElmParity.Convert(item.Id))));
                Assert.True(difference is null,item.Id+": "+difference);
            }
        }
    }
    [Fact]
    public async Task PinnedElmRejectsConflictingLocalGenericBodyTypes()
    {
        var folder = System.IO.Path.Combine(Support.Root, "artifacts", "d10-local-conflict-" + Guid.NewGuid().ToString("N"));
        CopyTree(System.IO.Path.Combine(Support.Root, "fixtures/elm-parity/d10-local-conflict/elm"), folder);
        var run = await Run("node", [System.IO.Path.Combine(Support.Root, "eng/node_modules/morphir-elm/cli/morphir-elm.js"), "make", "-p", folder, "-o", System.IO.Path.Combine(folder, "ir.json")], folder);
        Assert.NotEqual(0, run.Exit);
        Assert.Contains("TypeCheckError", run.Text);
    }
    [Fact]
    public async Task PinnedElmRejectsTheNativeArrayModule()
    {
        var folder = System.IO.Path.Combine(Support.Root, "artifacts", "d11-native-array-" + Guid.NewGuid().ToString("N"));
        CopyTree(System.IO.Path.Combine(Support.Root, "fixtures/elm-parity/d11-native-array/elm"), folder);
        var run = await Run("node", [System.IO.Path.Combine(Support.Root, "eng/node_modules/morphir-elm/cli/morphir-elm.js"), "make", "-p", folder, "-o", System.IO.Path.Combine(folder, "ir.json")], folder);
        Assert.NotEqual(0, run.Exit);
        Assert.Contains("NoMorphirPackageFoundForElmModule", run.Text);
    }
    [Fact]
    public async Task PinnedElmRejectsNegativeLiteralPatterns()
    {
        var folder = System.IO.Path.Combine(Support.Root, "artifacts", "d12-negative-pattern-" + Guid.NewGuid().ToString("N"));
        CopyTree(System.IO.Path.Combine(Support.Root, "fixtures/elm-parity/d12-negative-pattern/elm"), folder);
        var run = await Run("node", [System.IO.Path.Combine(Support.Root, "eng/node_modules/morphir-elm/cli/morphir-elm.js"), "make", "-p", folder, "-o", System.IO.Path.Combine(folder, "ir.json")], folder);
        Assert.NotEqual(0, run.Exit);
        Assert.Contains("ParserError", run.Text);
    }
    [Fact]
    public async Task CliEmitsExactBusinessJsonAndPreservesOutputOnEveryFailure()
    {
        var folder=System.IO.Path.Combine(Support.Root,"artifacts","exact-cli-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var output=System.IO.Path.Combine(folder,"morphir-ir.json");
        var args=new List<string>{"--package","Sigmond.Buslog","--namespace","Sigmond.Buslog.Model","--namespace","Sigmond.Buslog.Algo","--output",output};
        foreach(var file in ElmParity.Cases.Single(c=>c.Id=="business").Sources){args.Add("--source");args.Add(System.IO.Path.Combine(Support.Root,file));}
        var result=await Cli(args.ToArray());Assert.True(result.Exit==0,result.Text);
        Assert.Null(StructuralComparison.Compare(ElmParity.Expected("business"),File.ReadAllText(output)));
        var previous=File.ReadAllBytes(output);var bad=System.IO.Path.Combine(folder,"Input.cs");
        foreach(var code in new[]{"invalid C#","namespace Sigmond.Buslog.Algo;public class Generic<T> where T:class{}","namespace Sigmond.Buslog.Algo;public static class Rules{public static int F(int a){a=2;return a;}}"})
        {
            File.WriteAllText(bad,code);
            var failure=await Cli(["--package","Sigmond.Buslog","--namespace","Sigmond.Buslog.Algo","--source",bad,"--output",output]);
            Assert.Equal(1,failure.Exit);Assert.Equal(previous,File.ReadAllBytes(output));
        }
        Assert.Equal(2,(await Cli(["--format","source"])).Exit);
        Assert.Equal(2,(await Cli(["--format","classic"])).Exit);
        var help=await Cli(["--help"]);Assert.Equal(0,help.Exit);Assert.DoesNotContain("--format",help.Text);Assert.Contains("Classic v3",help.Text);
    }
    [Fact]
    public async Task ProjectAndCliMatchElmForReferencedAndGeneratedSource()
    {
        var project=System.IO.Path.Combine(Support.Root,"fixtures/elm-parity/project-generated/Project.csproj");
        var loaded=await ProjectLoader.LoadAsync(project,new("net10.0"));
        Assert.True(loaded.Success,string.Join("\n",loaded.Diagnostics));
        Assert.Contains(loaded.Inputs.SelectMany(i=>i.Sources),s=>s.Generated&&s.Path.EndsWith("Rules.Generated.cs",StringComparison.Ordinal));
        var result=Frontend.Convert(loaded.Compilation!,new("Example",["Example.CurrentSupported","Example.Parity"]),loaded.SourceProjects);
        Assert.Null(StructuralComparison.Compare(ElmParity.Expected("project-generated"),Codec.Encode(Support.Success(result))));
        var output=System.IO.Path.Combine(Support.Root,"artifacts","exact-project-"+Guid.NewGuid().ToString("N")+".json");
        var run=await Cli(["--project",project,"--framework","net10.0","--configuration","Debug","--package","Example","--namespace","Example.CurrentSupported","--namespace","Example.Parity","--output",output]);
        Assert.True(run.Exit==0,run.Text);
        Assert.Null(StructuralComparison.Compare(ElmParity.Expected("project-generated"),File.ReadAllText(output)));
    }
    [Fact]
    public async Task ProjectConfigurationsPreserveBindingOptionsAndExactElmOutput()
    {
        var project=System.IO.Path.Combine(Support.Root,"fixtures/elm-parity/d2-project-options/D2Options.csproj");
        foreach(var configuration in new[]{"Debug","Release"})
        {
            var loaded=await ProjectLoader.LoadAsync(project,new("net10.0",configuration));
            Assert.True(loaded.Success,string.Join("\n",loaded.Diagnostics));
            Assert.Equal(configuration=="Debug",loaded.Compilation!.Options.CheckOverflow);
            Assert.Equal(Platform.X64,loaded.Compilation.Options.Platform);
            Assert.All(loaded.Compilation.SyntaxTrees,t=>Assert.Equal(LanguageVersion.CSharp14,((CSharpParseOptions)t.Options).LanguageVersion));
            var result=Frontend.Convert(loaded.Compilation,new("Example",["Example.CurrentSupported","Example.Parity"]),loaded.SourceProjects);
            Assert.Null(StructuralComparison.Compare(ElmParity.Expected("d2-options-"+configuration.ToLowerInvariant()),Codec.Encode(Support.Success(result))));
        }
    }
    [Fact]
    public async Task CompilerAndGeneratorFailuresAreAtomicInProjectMode()
    {
        var source=System.IO.Path.Combine(Support.Root,"fixtures/elm-parity/d2-project-options");
        var folder=System.IO.Path.Combine(Support.Root,"artifacts","d2-failures-"+Guid.NewGuid().ToString("N"));
        CopyTree(source,folder);
        var project=System.IO.Path.Combine(folder,"D2Options.csproj");
        var xml=File.ReadAllText(project).Replace("../../project-system/ThresholdGenerator/ThresholdGenerator.csproj",System.IO.Path.Combine(Support.Root,"fixtures/project-system/ThresholdGenerator/ThresholdGenerator.csproj"),StringComparison.Ordinal);
        File.WriteAllText(project,xml);
        var restore=await Run("dotnet",["restore",project,"--locked-mode","--packages",System.IO.Path.Combine(Support.Root,".packages"),"-m:1","-nr:false"]);
        Assert.True(restore.Exit==0,restore.Text);
        var output=System.IO.Path.Combine(folder,"existing.json");var previous=System.Text.Encoding.UTF8.GetBytes("preserve this output");
        foreach(var scenario in new[]{"diagnostic","exception","compiler"})
        {
            File.WriteAllBytes(output,previous);
            File.WriteAllText(System.IO.Path.Combine(folder,"threshold.txt"),scenario=="exception"?"-1":"invalid");
            File.WriteAllText(project,scenario=="exception"?xml.Replace("<Threshold>18</Threshold>","<Threshold>-1</Threshold>",StringComparison.Ordinal):xml);
            if(scenario=="compiler")File.WriteAllText(System.IO.Path.Combine(folder,"csharp/Input.cs"),"invalid C#");
            var failed=await Cli(["--project",project,"--framework","net10.0","--package","Example","--namespace","Example.CurrentSupported","--namespace","Example.Parity","--output",output]);
            Assert.Equal(1,failed.Exit);Assert.Equal(previous,File.ReadAllBytes(output));
            Assert.Contains(scenario=="exception"?"Generator failure fixture":scenario=="compiler"?"error":"D2GEN001",failed.Text);
        }
        Assert.False((await ProjectLoader.LoadAsync(project,new("net9.0"))).Success);
    }
    [Fact]
    public async Task SingleFileRobotRentalDemoHasExactSourceAndProjectParity()
    {
        var project = System.IO.Path.Combine(Support.Root, "samples/FestivalRobotRental/FestivalRobotRental.csproj");
        var source = System.IO.Path.Combine(Support.Root, "samples/FestivalRobotRental/BusinessLogic.cs");
        var loaded = await ProjectLoader.LoadAsync(project, new("net10.0"));
        Assert.True(loaded.Success, string.Join("\n", loaded.Diagnostics));
        var result = Frontend.Convert(loaded.Compilation!, new("Example", ["Example.FestivalRobotRental"]), loaded.SourceProjects);
        Assert.Null(StructuralComparison.Compare(Support.Read(Support.CurrentSampleReference), Codec.Encode(Support.Success(result))));
        foreach (var input in new[] { new[] { "--source", source }, new[] { "--project", project, "--framework", "net10.0" } })
        {
            var output = System.IO.Path.Combine(Support.Root, "artifacts", Guid.NewGuid() + ".json");
            var run = await Cli(input.Concat(["--package", "Example", "--namespace", "Example.FestivalRobotRental", "--output", output]).ToArray());
            Assert.True(run.Exit == 0, run.Text);
            Assert.Null(StructuralComparison.Compare(Support.Read(Support.CurrentSampleReference), File.ReadAllText(output)));
        }
    }
    [Fact]
    public async Task ProjectLoaderStillAcquiresGeneratorsAndCompilerInputs()
    {
        var project=System.IO.Path.Combine(Support.Root,"fixtures/elm-parity/project-generated/Project.csproj");
        var loaded=await ProjectLoader.LoadAsync(project,new("net10.0"));
        Assert.True(loaded.Success,string.Join("\n",loaded.Diagnostics));
        Assert.Contains(loaded.Inputs.SelectMany(i=>i.Sources),s=>s.Generated&&s.Path.EndsWith("Rules.Generated.cs",StringComparison.Ordinal));
        Assert.Contains(loaded.Inputs.SelectMany(i=>i.AdditionalFiles),s=>s.Path.EndsWith("threshold.txt",StringComparison.Ordinal));
        Assert.NotEmpty(loaded.Inputs.SelectMany(i=>i.AnalyzerConfigs));
        var result=Frontend.Convert(loaded.Compilation!,new("Example",["Example.CurrentSupported","Example.Parity"]),loaded.SourceProjects);
        Assert.Null(StructuralComparison.Compare(ElmParity.Expected("project-generated"),Codec.Encode(Support.Success(result))));
    }
    private static void CopyTree(string source,string target)
    {
        Directory.CreateDirectory(target);
        foreach(var file in Directory.GetFiles(source))File.Copy(file,System.IO.Path.Combine(target,System.IO.Path.GetFileName(file)),true);
        foreach(var directory in Directory.GetDirectories(source).Where(d=>System.IO.Path.GetFileName(d) is not ("bin" or "obj")))CopyTree(directory,System.IO.Path.Combine(target,System.IO.Path.GetFileName(directory)));
    }
    private static Task<(int Exit,string Text)> Cli(string[] arguments)
    {
        var config=new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        return Run("dotnet",[System.IO.Path.Combine(Support.Root,"src/Morphir.Frontend.CSharp.Cli/bin",config,"net10.0/Morphir.Frontend.CSharp.Cli.dll"),..arguments]);
    }
    private static async Task<(int Exit,string Text)> Run(string executable,string[] arguments,string? workingDirectory=null)
    {
        var start=new ProcessStartInfo(executable){RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=workingDirectory??Support.Root};
        start.Environment["MSBUILDDISABLENODEREUSE"]="1";
        foreach(var arg in arguments)start.ArgumentList.Add(arg);
        using var process=Process.Start(start)!;
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var stdout=process.StandardOutput.ReadToEndAsync(timeout.Token);var stderr=process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            return(process.ExitCode,await stdout+await stderr);
        }
        catch
        {
            if(!process.HasExited)process.Kill(true);
            throw;
        }
    }
}
