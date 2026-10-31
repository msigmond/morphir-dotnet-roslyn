using System.Diagnostics;
using System.Text.Json;
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
    public async Task CliEmitsExactBusinessJsonAndPreservesOutputOnEveryFailure()
    {
        var folder=System.IO.Path.Combine(Support.Root,"artifacts","exact-cli-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var output=System.IO.Path.Combine(folder,"morphir-ir.json");
        var args=new List<string>{"--package","Sigmond.Buslog","--namespace","Sigmond.Buslog.Model","--namespace","Sigmond.Buslog.Algo","--output",output};
        foreach(var file in ElmParity.Cases.Single(c=>c.Id=="business").Sources){args.Add("--source");args.Add(System.IO.Path.Combine(Support.Root,file));}
        var result=await Cli(args.ToArray());Assert.True(result.Exit==0,result.Text);
        Assert.Null(StructuralComparison.Compare(ElmParity.Expected("business"),File.ReadAllText(output)));
        var previous=File.ReadAllBytes(output);var bad=System.IO.Path.Combine(folder,"Input.cs");
        foreach(var code in new[]{"invalid C#","namespace Sigmond.Buslog.Algo;public class Generic<T>{}","namespace Sigmond.Buslog.Algo;public static class Rules{public static int F(int a){a=2;return a;}}"})
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
    public async Task ProjectLoaderStillAcquiresGeneratorsAndCompilerInputs()
    {
        var project=System.IO.Path.Combine(Support.Root,"samples/CurrentSupportedExample/CurrentSupportedExample.csproj");
        var loaded=await ProjectLoader.LoadAsync(project,new("net10.0"));
        Assert.True(loaded.Success,string.Join("\n",loaded.Diagnostics));
        Assert.Contains(loaded.Inputs.SelectMany(i=>i.Sources),s=>s.Generated&&s.Path.EndsWith("Rules.Generated.cs",StringComparison.Ordinal));
        Assert.Contains(loaded.Inputs.SelectMany(i=>i.AdditionalFiles),s=>s.Path.EndsWith("threshold.txt",StringComparison.Ordinal));
        Assert.NotEmpty(loaded.Inputs.SelectMany(i=>i.AnalyzerConfigs));
        var result=Frontend.Convert(loaded.Compilation!,new("Example",["Example.CurrentSupported"]),loaded.SourceProjects);
        Assert.Null(StructuralComparison.Compare(ElmParity.Expected("current-sample"),Codec.Encode(Support.Success(result))));
    }
    private static void CopyTree(string source,string target)
    {
        Directory.CreateDirectory(target);
        foreach(var file in Directory.GetFiles(source))File.Copy(file,System.IO.Path.Combine(target,System.IO.Path.GetFileName(file)),true);
        foreach(var directory in Directory.GetDirectories(source))CopyTree(directory,System.IO.Path.Combine(target,System.IO.Path.GetFileName(directory)));
    }
    private static Task<(int Exit,string Text)> Cli(string[] arguments)
    {
        var config=new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        return Run("dotnet",[System.IO.Path.Combine(Support.Root,"src/Morphir.Frontend.CSharp.Cli/bin",config,"net10.0/Morphir.Frontend.CSharp.Cli.dll"),..arguments]);
    }
    private static async Task<(int Exit,string Text)> Run(string executable,string[] arguments,string? workingDirectory=null)
    {
        var start=new ProcessStartInfo(executable){RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=workingDirectory??Support.Root};
        foreach(var arg in arguments)start.ArgumentList.Add(arg);
        using var process=Process.Start(start)!;
        var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(90));
        try{await process.WaitForExitAsync(timeout.Token);}catch{process.Kill(true);throw;}
        return(process.ExitCode,await stdout+await stderr);
    }
}
