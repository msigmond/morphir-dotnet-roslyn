using System.Diagnostics;
using Morphir.Frontend.CSharp;
using Morphir.IR.Json;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D1")]
public class IntegrationTests
{
    [Fact] public void CumulativeSampleHasCompleteIndependentCodecParity() => Support.Parity("fixtures/D1/expected/sample.json", Support.Read("samples/CurrentSupportedExample/CurrentSupportedExample.cs"), "Example", "Example.CurrentSupported");
    [Fact]
    public void CumulativeBehaviorMatchesCompiledCSharpAtBoundaries()
    {
        var code = Support.Read("samples/CurrentSupportedExample/CurrentSupportedExample.cs");
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.CurrentSupported"]));
        var assembly = Support.Emit(code);
        var rules = assembly.GetType("Example.CurrentSupported.CurrentSupportedExample")!;
        var customer = assembly.GetType("Example.CurrentSupported.Customer")!;
        var methods = new[] { "Eligible", "ActiveOrAdult", "Below", "AtMost", "Exactly", "Different" };
        foreach (int age in new[] { int.MinValue, -1, 0, 17, 18, 19, int.MaxValue })
            foreach (bool active in new[] { false, true })
            {
                var c = Activator.CreateInstance(customer, age, active);
                var ir = new Dictionary<string, object> { ["age"] = age, ["active"] = active };
                foreach (var method in methods) Assert.Equal(rules.GetMethod(method)!.Invoke(null, [c]), Support.Call(lib, "CurrentSupported.CurrentSupportedExample", method, ir));
                foreach (var method in new[] { "Create", "TargetTyped" })
                {
                    var actual = rules.GetMethod(method)!.Invoke(null, [age, active]);
                    var result = (Dictionary<string, object>)Support.Call(lib, "CurrentSupported.CurrentSupportedExample", method, age, active);
                    Assert.Equal(customer.GetProperty("Age")!.GetValue(actual), result["age"]); Assert.Equal(customer.GetProperty("Active")!.GetValue(actual), result["active"]);
                }
            }
        foreach (var method in new[] { "Addition", "Negative", "EligibleLabel", "Always" }) Assert.Equal(rules.GetMethod(method)!.Invoke(null, []), Support.Call(lib, "CurrentSupported.CurrentSupportedExample", method));
        foreach (var x in new[] { int.MinValue, -1, 0, 1, int.MaxValue })
            foreach (var method in new[] { "IdentityAddition", "IdentityCast" }) Assert.Equal(x, Support.Call(lib, "CurrentSupported.CurrentSupportedExample", method, x));
        var named = (Dictionary<string, object>)Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "Named");
        Assert.Equal(18, named["age"]); Assert.Equal(true, named["active"]);
        Assert.Equal(1, Support.Call(lib, "CurrentSupported.Customer", "DataConstant"));
        Assert.True((bool)Support.Call(lib, "CurrentSupported.CurrentSupportedExample.Tools", "Echo", true));
        Assert.Equal(int.MaxValue, Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "Echo0NoneGlobalSystemInt32", int.MaxValue));
        Assert.False((bool)Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "Echo0NoneGlobalSystemBoolean", false));
        Assert.True((bool)Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "Eligible", new Dictionary<string, object> { ["active"] = true, ["age"] = 18 }));
        // Missing age intentionally verifies short circuiting: the right side must not be evaluated.
        Assert.False((bool)Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "Eligible", new Dictionary<string, object> { ["active"] = false }));
        Assert.True((bool)Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "ActiveOrAdult", new Dictionary<string, object> { ["active"] = true }));
    }
    [Fact]
    public void CliProducesParityAndFailurePreservesExistingArtifact()
    {
        var folder = System.IO.Path.Combine(Support.Root, "artifacts", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        var output = System.IO.Path.Combine(folder, "ir.json");
        var source = System.IO.Path.Combine(folder, "Input.cs"); File.WriteAllText(source, Support.Read("fixtures/D1/csharp/Eligibility.cs"));
        var arguments = new[] { "--package", "Example", "--namespace", "Example.D1", "--output", output, "--source", source };
        Assert.Equal(0, RunCli(arguments).Exit); var bytes = File.ReadAllBytes(output);
        Assert.Null(StructuralComparison.Compare(Support.Read("fixtures/D1/expected/eligibility.json"), File.ReadAllText(output)));
        File.WriteAllText(source, "namespace Example.D1; public static class Rules { public static int F(int a,int b)=>a+b; }");
        var rejected = RunCli(arguments); Assert.Equal(1, rejected.Exit); Assert.Contains("MCS2302", rejected.Error); Assert.Equal(bytes, File.ReadAllBytes(output));
        var fresh = System.IO.Path.Combine(folder, "fresh.json"); var freshArgs = (string[])arguments.Clone(); freshArgs[5] = fresh;
        Assert.Equal(1, RunCli(freshArgs).Exit); Assert.False(File.Exists(fresh));
        File.WriteAllText(source, "invalid C#"); Assert.Equal(1, RunCli(arguments).Exit); Assert.Equal(bytes, File.ReadAllBytes(output));
        Assert.Equal(1, RunCli(["--namespace", "Example.D1", "--output", output, "--source", source]).Exit);
        Assert.Equal(2, RunCli(["--bad", "x"]).Exit);
    }
    [Fact]
    public void HistoricalUnboundedAdditionIsExplicitlyRejectedUntilD5()
    {
        var code = Support.Read("docs/references/prototype-addition/Add.cs");
        var r = Support.Convert(code, "Arithmetic", ["Arithmetic"]);
        Assert.Contains(r.Diagnostics, d => d.Code == "MCS2302");
        // The model-backed safe specialization retains both operands and the archived owner.
        Support.Parity("fixtures/D1/expected/addition.json", code.Replace("a + b", "a + 0"), "Arithmetic", "Arithmetic");
    }
    private static (int Exit, string Error) RunCli(string[] args)
    {
        var config = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = System.IO.Path.Combine(Support.Root, "src", "Morphir.Frontend.CSharp.Cli", "bin", config, "net10.0", "Morphir.Frontend.CSharp.Cli.dll");
        var start = new ProcessStartInfo("dotnet") { RedirectStandardError = true, RedirectStandardOutput = true }; start.ArgumentList.Add(dll); foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start)!; var err = process.StandardError.ReadToEnd(); Assert.True(process.WaitForExit(30000)); return (process.ExitCode, err);
    }
}
