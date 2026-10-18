using Morphir.IR.Classic;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class DispatchTests
{
    [Fact]
    public void ClosedRulesHaveCompleteIndependentCodecParity() => Support.Parity("fixtures/D10/expected/closed-rules.json", Support.Read("fixtures/D10/csharp/Rules.cs"), "Example", "Example.D10");

    [Fact]
    public void ClosedValueInterfacesDispatchToTheirResolvedSourceImplementations()
    {
        var code = Support.Read("fixtures/D10/csharp/Rules.cs"); var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var assembly = Support.Emit(code); var rules = assembly.GetType("Example.D10.Rules")!;
        Assert.False((bool)Support.Call(library, "D10.Rules", "Test", Support.Maybe(null), 18));
        foreach (var name in new[] { "MinimumAge", "MaximumAge" }) foreach (var bound in new[] { int.MinValue, 0, 18, int.MaxValue }) foreach (var age in new[] { int.MinValue, -1, 0, 17, 18, 19, int.MaxValue })
                {
                    var value = Activator.CreateInstance(assembly.GetType("Example.D10." + name)!, bound)!;
                    var constructor = new FQName(Morphir.IR.Classic.Path.From("Example"), Morphir.IR.Classic.Path.From("D10"), Name.From(name + "Rule"));
                    var represented = new Support.CustomData(constructor, [new Dictionary<string, object> { { name == "MinimumAge" ? "minimum" : "maximum", bound } }]);
                    Assert.Equal(rules.GetMethod("Test")!.Invoke(null, [value, age]), Support.Call(library, "D10.Rules", "Test", Support.Maybe(represented), age));
                }
        var types = library.Definition.Modules.Single(entry => entry.Key.Key == Morphir.IR.Classic.Path.From("D10").Key).Value.Value.Types;
        var contract = Assert.IsType<CustomDefinition<Unit>>(types.Single(entry => entry.Key.Key == Name.From("IRule").Key).Value.Value.Value);
        Assert.Equal(2, contract.Constructors.Value.Count);
        Assert.Contains(library.Definition.Modules, entry => entry.Key.Key == Morphir.IR.Classic.Path.From("D10.IRule").Key);
        Assert.Contains(library.Definition.Modules, entry => entry.Key.Key == Morphir.IR.Classic.Path.From("D10.MinimumAge").Key);
    }

    [Fact]
    public void BoxingConversionsUseExplicitRuntimeAlternativesAndKeepMaybe()
    {
        const string code = """
        namespace Example.D10;
        public interface IRule{bool Allows(int age);}
        public readonly record struct MinimumAge(int Minimum):IRule{public bool Allows(int age)=>age>=Minimum;}
        public readonly record struct MaximumAge(int Maximum):IRule{public bool Allows(int age)=>age<=Maximum;}
        public static class Rules{
          private static bool Test(IRule? rule,int age){if(rule==null)return false;return rule.Allows(age);}
          public static bool Minimum(int bound,int age)=>Test(new MinimumAge(bound),age);
          public static bool Maximum(int bound,int age)=>Test(new MaximumAge(bound),age);
          public static bool Missing(int age){MinimumAge? rule=null;return Test(rule,age);}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var bound in new[] { int.MinValue, 0, 18, int.MaxValue }) foreach (var age in new[] { int.MinValue, 0, 17, 18, 19, int.MaxValue }) foreach (var name in new[] { "Minimum", "Maximum" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [bound, age]), Support.Call(library, "D10.Rules", name, bound, age));
        Assert.False((bool)Support.Call(library, "D10.Rules", "Missing", 18));
    }

    [Theory]
    [InlineData("namespace Example.D10{public interface IRule{bool Allows(int age);}}", "MCS3111")]
    [InlineData("namespace Example.D10{public interface IRule{bool Allows(int age);}}namespace Other{public readonly record struct Rule(int Bound):Example.D10.IRule{public bool Allows(int age)=>age>=Bound;}}", "MCS3111")]
    [InlineData("namespace Example.D10;public interface IRule{bool Allows(int age);}public readonly record struct Rule(int Bound):IRule{public bool Allows(int age)=>System.Environment.TickCount>=age;}", "MCS2107")]
    [InlineData("namespace Example.D10;public interface IRule{bool Allows(int age);}public readonly record struct Rule(int Bound):IRule{public bool Allows(int age)=>age>=Bound;}public static class Rules{public static bool Unsafe(IRule rule,int age)=>rule.Allows(age);}", "MCS2501")]
    [InlineData("namespace Example.D10{public interface IRule{bool Allows(int age);}}namespace Example.A{public readonly record struct Rule(int Bound):D10.IRule{public bool Allows(int age)=>age>=Bound;}}namespace Example.B{public readonly record struct Rule(int Bound):D10.IRule{public bool Allows(int age)=>age<=Bound;}}", "MCS3111")]
    public void UnknownIncompleteImpureOrUnsafeDispatchFailsWithSourceEvidence(string code, string diagnostic)
    {
        var result = Support.Convert(code, selectors: ["Example.D10"]); Assert.Null(result.Distribution);
        Assert.Contains(result.Diagnostics, item => item.Code == diagnostic && item.File == "Case.cs" && item.Line > 0 && item.Column > 0);
    }

    [Fact]
    public void AmbiguousAlternativeConstructorNamesRejectWithoutLosingImplementations()
    {
        const string code = """
        namespace Example.D10{public interface IRule{bool Allows(int age);}}
        namespace Example.A{public readonly record struct Rule(int Bound):D10.IRule{public bool Allows(int age)=>age>=Bound;}}
        namespace Example.B{public readonly record struct Rule(int Bound):D10.IRule{public bool Allows(int age)=>age<=Bound;}}
        """;
        var result = Support.Convert(code, selectors: ["Example"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == "MCS3112");
    }

    [Fact]
    public void DispatchPatternBindingsDoNotShadowSourceArguments()
    {
        const string code = """
        namespace Example.D10;
        public interface IRule{bool Allows(int rule);}
        public readonly record struct Minimum(int Bound):IRule{public bool Allows(int rule)=>rule>=Bound;}
        public static class Rules{public static bool Test(int bound,int rule){IRule value=new Minimum(bound);return value.Allows(rule);}}
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var bound in new[] { 0, 18, int.MaxValue }) foreach (var rule in new[] { int.MinValue, 0, 17, 18, int.MaxValue }) Assert.Equal(rules.GetMethod("Test")!.Invoke(null, [bound, rule]), Support.Call(library, "D10.Rules", "Test", bound, rule));
    }

    [Fact]
    public void InterfaceClosureResolvesAcrossSelectedSourceCompilations()
    {
        var contracts = Support.Compile("namespace Example.D10;public interface IRule{bool Allows(int age);}").WithAssemblyName("Contracts");
        var consumer = Support.Compile("""
        namespace Example.D10;
        public readonly record struct Minimum(int Bound):IRule{public bool Allows(int age)=>age>=Bound;}
        public static class Rules{public static bool Test(int bound,int age){IRule rule=new Minimum(bound);return rule.Allows(age);}}
        """).WithAssemblyName("Consumer").AddReferences(contracts.ToMetadataReference());
        var library = Support.Success(Morphir.Frontend.CSharp.Frontend.Convert(consumer, new("Example", ["Example.D10"]), [contracts]));
        foreach (var age in new[] { int.MinValue, 0, 17, 18, 19, int.MaxValue }) Assert.Equal(age >= 18, Support.Call(library, "D10.Rules", "Test", 18, age));
    }

    [Fact]
    public void ReferenceReturningDispatchPreservesAnAbsentResult()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Counter{public int Value;}
        public interface IRule{Counter? Read();}
        public readonly record struct Minimum(int Bound):IRule{public Counter? Read()=>null;}
        public static class Rules{public static Counter? Test(IRule? rule){if(rule==null)return null;return rule.Read();}}
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        Assert.Equal(Support.Maybe(null), Support.Call(library, "D10.Rules", "Test", Support.Maybe(null)));
    }
    [Fact]
    public void BinaryOnlyImplementationsCannotDisappearFromTheRuntimeClosure()
    {
        var contracts = Support.Compile("namespace Example.D10;public interface IRule{bool Allows(int age);}").WithAssemblyName("Contracts");
        var binary = Support.Compile("namespace Other;public readonly record struct Hidden(int Bound):Example.D10.IRule{public bool Allows(int age)=>age>=Bound;}").WithAssemblyName("BinaryRules").AddReferences(contracts.ToMetadataReference());
        var consumer = Support.Compile("namespace Example.D10;public readonly record struct Minimum(int Bound):IRule{public bool Allows(int age)=>age>=Bound;}").WithAssemblyName("Consumer").AddReferences(contracts.ToMetadataReference(), binary.ToMetadataReference());
        var result = Morphir.Frontend.CSharp.Frontend.Convert(consumer, new("Example", ["Example.D10"]), [contracts]);
        Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == "MCS3111" && item.File == "Case.cs");
    }

}
