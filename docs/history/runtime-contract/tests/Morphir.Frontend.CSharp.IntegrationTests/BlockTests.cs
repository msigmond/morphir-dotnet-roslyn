using Microsoft.CodeAnalysis.CSharp;
using Morphir.Frontend.CSharp;
using Morphir.IR.Classic;
using Morphir.IR.Json;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D3")]
public class BlockTests
{
    [Fact]
    public void CategoryHasIndependentWholeDistributionParityAndBoundaryBehavior()
    {
        var code = Support.Read("fixtures/D3/csharp/Category.cs");
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D3"]));
        var difference = StructuralComparison.Compare(Support.Read("fixtures/D3/expected/blocks.json"), Codec.Encode(lib)); Assert.True(difference is null, difference?.ToString());
        var assembly = Support.Emit(code); var method = assembly.GetType("Example.D3.Rules")!.GetMethod("Category")!;
        foreach (var age in new[] { int.MinValue, 0, 17, 18, 19, int.MaxValue })
            foreach (var active in new[] { false, true })
                Assert.Equal(method.Invoke(null, [age, active]), Support.Call(lib, "D3.Rules", "Category", age, active));
        Assert.Equal(0, Support.Call(lib, "D3.Rules", "Category", 18, false));
        Assert.Equal(1, Support.Call(lib, "D3.Rules", "Category", 17, true));
        Assert.Equal(2, Support.Call(lib, "D3.Rules", "Category", 18, true));
        var changed = Support.Success(Support.Convert(code.Replace("category = 2", "category = 3", StringComparison.Ordinal), selectors: ["Example.D3"]));
        Assert.Equal(3, Support.Call(changed, "D3.Rules", "Category", 18, true)); Assert.NotNull(StructuralComparison.Compare(Codec.Encode(lib), Codec.Encode(changed)));
    }
    [Fact]
    public void CumulativeBlockFormsMatchIndependentCodecAndCompiledCSharp()
    {
        var compilation = SampleCompilation();
        var lib = Support.Success(Morphir.Frontend.CSharp.Frontend.Convert(compilation, new("Example", ["Example.CurrentSupported"])));
        // This standalone snapshot is the complete original D1 class plus D3 forms.
        // Project-mode D3 parity also retains all D2 generated/reference definitions.
        var assembly = Support.Emit(compilation); var rules = assembly.GetType("Example.CurrentSupported.CurrentSupportedExample")!;
        foreach (var age in new[] { int.MinValue, -1, 0, 17, 18, 19, int.MaxValue })
        {
            foreach (var active in new[] { false, true })
                foreach (var method in new[] { "Category", "Early" })
                    Assert.Equal(rules.GetMethod(method)!.Invoke(null, [age, active]), Support.Call(lib, "CurrentSupported.CurrentSupportedExample", method, age, active));
            foreach (var method in new[] { "Reassign", "Scoped", "AssignmentOrder", "ArgumentAssignment" })
                Assert.Equal(rules.GetMethod(method)!.Invoke(null, [age]), Support.Call(lib, "CurrentSupported.CurrentSupportedExample", method, age));
            Assert.Equal(1, Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "NestedAssignment", age, age));
            var customer = Activator.CreateInstance(assembly.GetType("Example.CurrentSupported.Customer")!, age, true);
            Assert.Equal(age, rules.GetMethod("ValueCopy")!.Invoke(null, [customer]));
            Assert.Equal(age, Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "ValueCopy", new Dictionary<string, object> { { "age", age }, { "active", true } }));
        }
        foreach (var a in new[] { false, true })
        {
            Assert.Equal(rules.GetMethod("InitLater")!.Invoke(null, [a]), Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "InitLater", a));
            Assert.Equal(rules.GetMethod("KnownLabel")!.Invoke(null, [a]), Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "KnownLabel", a));
            Assert.Equal(a ? 1 : 2, Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "ConditionalAssignment", a, 99));
            foreach (var b in new[] { false, true }) Assert.Equal(rules.GetMethod("ShortAssignment")!.Invoke(null, [a, b]), Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "ShortAssignment", a, b));
            foreach (var b in new[] { false, true }) Assert.Equal(rules.GetMethod("Boolean")!.Invoke(null, [a, b]), Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "Boolean", a, b));
        }
        Assert.Equal(3, Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "ConstantBranch"));
    }
    [Theory]
    [InlineData("Unassigned", "MCS0001")]
    [InlineData("ExternalUpdate", "MCS2404")]
    [InlineData("Effects", "MCS2107")]
    public void UnprovedOrExternalStateIsRejected(string fixture, string code)
    {
        var result = Support.Convert(Support.Read($"fixtures/D3/rejected/{fixture}.cs"), selectors: ["Example.D3"]);
        Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, d => d.Code == code && d.Line > 0 && d.Column > 0);
    }
    [Fact]
    public void AssignmentExpressionsPreserveSnapshotsOrderAndShortCircuit()
    {
        const string code = """
        namespace Example.D3;
        public static class Rules {
          public static int Nested(int x,int y) { x = (y = 1) + 0; return y; }
          public static int Order(int x) { return x + (x = 0); }
          public static bool Short(bool a,bool b) { bool result = a && (b = false); return b; }
          public static int Decision(bool flag,int value) { return flag ? (value=1) : (value=2); }
          public static int Arguments(int x) { return First(x,x=0); }
          private static int First(int a,int b) => a;
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D3"])); var assembly = Support.Emit(code); var rules = assembly.GetType("Example.D3.Rules")!;
        foreach (var x in new[] { int.MinValue, -1, 0, 1, int.MaxValue })
        {
            foreach (var method in new[] { "Order", "Arguments" }) Assert.Equal(rules.GetMethod(method)!.Invoke(null, [x]), Support.Call(lib, "D3.Rules", method, x));
            Assert.Equal(1, Support.Call(lib, "D3.Rules", "Nested", x, x));
        }
        foreach (var a in new[] { false, true }) foreach (var b in new[] { false, true }) Assert.Equal(rules.GetMethod("Short")!.Invoke(null, [a, b]), Support.Call(lib, "D3.Rules", "Short", a, b));
        foreach (var flag in new[] { false, true }) Assert.Equal(flag ? 1 : 2, Support.Call(lib, "D3.Rules", "Decision", flag, 99));
        // The earlier operand is an arbitrary input snapshot, not the later zero assignment.
        var unsafeCode = code.Replace("return x + (x = 0);", "return x + ((x = 0) + 1);", StringComparison.Ordinal);
        var wrapped = Support.Success(Support.Convert(unsafeCode, selectors: ["Example.D3"]));
        var changedRules = Support.Emit(unsafeCode).GetType("Example.D3.Rules")!;
        foreach (var input in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) Assert.Equal(changedRules.GetMethod("Order")!.Invoke(null, [input]), Support.Call(wrapped, "D3.Rules", "Order", input));
    }
    [Fact]
    public void ScopeAndUnreachableUnsupportedSourceCannotBypassValidation()
    {
        var outOfScope = Support.Convert("namespace Example.D3; public static class Rules { public static int F(){ { int value=1; } return value; }}", selectors: ["Example.D3"]);
        Assert.Contains(outOfScope.Diagnostics, d => d.Code == "MCS0001");
        var effect = Support.Convert("namespace Example.D3; public static class Rules { public static int F(){ if(false) System.Console.WriteLine(1); return 0; }}", selectors: ["Example.D3"]);
        Assert.Contains(effect.Diagnostics, d => d.Code == "MCS2202");
        var numeric = Support.Convert("namespace Example.D3; public static class Rules { public static int F(int value){ if(false) return value+1; return 0; }}", selectors: ["Example.D3"]);
        Support.Success(numeric); // D5 now supplies exact unchecked semantics.
        var unsupported = Support.Convert("namespace Example.D3; public static class Rules { public static int F(int value){ if(false) return value*2; return 0; }}", selectors: ["Example.D3"]);
        Support.Success(unsupported);
        var checkedCode = Support.Convert("namespace Example.D3; public static class Rules { public static int F(){ if(false) return checked(1+2); return 0; }}", selectors: ["Example.D3"]);
        Assert.Contains(checkedCode.Diagnostics, d => d.Code == "MCS2001");
    }
    private static CSharpCompilation SampleCompilation() => SourceCompilation.Create(new[] { "CurrentSupportedExample.cs", "Blocks.cs" }.Select(name => (name, Support.Read("samples/CurrentSupportedExample/" + name))));
}
