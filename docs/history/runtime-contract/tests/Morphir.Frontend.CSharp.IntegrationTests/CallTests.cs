using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D7")]
public class CallTests
{
    [Theory]
    [InlineData("optional-source", "Optional")]
    [InlineData("calls", "Calls")]
    public void CompleteIndependentCallDistributionsMatch(string expected, string source)
    {
        Support.Parity("fixtures/D7/expected/" + expected + ".json", Support.Read("fixtures/D7/csharp/" + source + ".cs"), "Example", "Example.D7");
    }
    [Fact]
    public void ResolvedOverloadsNamedDefaultsAndInstanceReceiversPreserveSourceOrder()
    {
        const string code = """
        namespace Example.D7;
        public readonly record struct Customer(int Age) {
          public bool Eligible(int minimum=18)=>Age>=minimum;
          public bool ViaHelper()=>Eligible();
        }
        public static class Rules {
          private static bool Meets(int age,int minimum=18)=>age>=minimum;
          private static bool Meets(bool approved)=>approved;
          private static int Pick(int first,int second)=>first;
          private static decimal DecimalDefault(decimal value=0.10m)=>value;
          public static bool Eligible(int age)=>Meets(minimum:18,age:age);
          public static bool Approved(bool approved)=>Meets(approved);
          public static bool DefaultEligible(int age)=>Meets(age);
          public static int Order(int value)=>Pick(second:value,first:value=2);
          public static int EarlierRead(int value)=>Pick(second:value=2,first:value);
          public static bool Instance(Customer value)=>value.ViaHelper();
          public static decimal DefaultDecimal()=>DecimalDefault();
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D7"]));
        var assembly = Support.Emit(code); var rules = assembly.GetType("Example.D7.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, 17, 18, 19, int.MaxValue })
        {
            foreach (var method in new[] { "Eligible", "DefaultEligible", "Order", "EarlierRead" }) Assert.Equal(rules.GetMethod(method)!.Invoke(null, [value]), Support.Call(lib, "D7.Rules", method, value));
            Assert.Equal(value >= 18, Support.Call(lib, "D7.Rules", "Instance", new Dictionary<string, object> { { "age", value } }));
        }
        foreach (var value in new[] { false, true }) Assert.Equal(value, Support.Call(lib, "D7.Rules", "Approved", value));
        var result = (Dictionary<string, object>)Support.Call(lib, "D7.Rules", "DefaultDecimal"); Assert.Equal(2, result["scale"]);
    }
    [Fact]
    public void NoncapturingLocalFunctionsHaveSeparateOwnersBodiesAndDefaults()
    {
        const string code = """
        namespace Example.D7;
        public static class Rules {
          public static bool Local(int age){static bool Meets(int value,int minimum=18)=>value>=minimum;return Meets(value:age);}
          public static bool Nested(bool approved){bool Outer(bool value){static bool Inner(bool result)=>result;return Inner(value);}return Outer(approved);}
          public static bool Scopes(bool value){if(value){bool Same(bool a)=>a;return Same(value);}else{bool Same(bool a)=>!a;return Same(value);}}
          public static int WithLocal(int value){int Identity(int a){var b=a;return b;}return Identity(value);}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D7"])); var rules = Support.Emit(code).GetType("Example.D7.Rules")!;
        foreach (var age in new[] { int.MinValue, 17, 18, int.MaxValue }) foreach (var name in new[] { "Local", "WithLocal" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [age]), Support.Call(lib, "D7.Rules", name, age));
        foreach (var value in new[] { false, true }) foreach (var name in new[] { "Nested", "Scopes" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D7.Rules", name, value));
        Assert.Contains(lib.Definition.Modules, module => module.Key.Segments.Count > 2);
        var captured = Support.Convert("namespace Example.D7;public static class Rules{public static int F(int value){int Local()=>value;return Local();}}", selectors: ["Example.D7"]);
        Assert.Null(captured.Distribution); Assert.Contains(captured.Diagnostics, d => d.Code == "MCS2802");
    }

    [Fact]
    public void PureMethodReferencesSnapshotReceiversAndRetainPresenceAndEffectsProofs()
    {
        const string code = """
        namespace Example.D7;
        public readonly record struct Customer(int Age){public bool Adult()=>Age>=18;public bool Minimum(int age)=>Age>=age;}
        public static class Rules {
          private static bool Adult(int age)=>age>=18;
          private static int Number()=>42;
          public static bool Static(int age){System.Func<int,bool> f=Adult;return f(age);}
          public static bool Local(int age){static bool Test(int value)=>value>=18;System.Func<int,bool> f=Test;return f(age);}
          public static bool Receiver(int age){var value=new Customer(age);System.Func<bool> f=value.Adult;value=new Customer(100);return f();}
          public static bool ReceiverArgument(int age){var value=new Customer(age);System.Func<int,bool> f=value.Minimum;return f(18);}
          public static System.Func<int> Reference()=>Number;
          public static int Returned()=>Reference()();
          public static bool Conditional(int age){System.Func<int,bool>? f=Adult;return f?.Invoke(age)??false;}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D7"])); var rules = Support.Emit(code).GetType("Example.D7.Rules")!;
        foreach (var age in new[] { int.MinValue, 0, 17, 18, 100, int.MaxValue }) foreach (var name in new[] { "Static", "Local", "Receiver", "ReceiverArgument", "Conditional" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [age]), Support.Call(lib, "D7.Rules", name, age));
        Assert.Equal(42, Support.Call(lib, "D7.Rules", "Returned"));
        foreach (var member in new[] { "public static bool F(System.Func<int,bool> f)=>f(18);", "public static bool F(System.Func<int,bool>? f){if(f==null)return false;return f(18);}" })
        { var result = Support.Convert("namespace Example.D7;public static class Rules{" + member + "}", selectors: ["Example.D7"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, d => d.Code is "MCS2501" or "MCS2804"); }
    }

    [Fact]
    public void ParamsPacksRetainNullEmptyValuesBoundsAndElementEvaluationOrder()
    {
        const string code = """
        namespace Example.D7;
        public static class Rules {
          private static int Count(params int[] values)=>values?.Length??-1;
          private static int First(params int[] values){if(values==null || values.Length==0)return -1;return values[0];}
          private static int Two(params int[] values){if(values==null || values.Length<2)return -1;return values[1L];}
          private static int Forward(params int[] values)=>Count(values);
          public static int Empty()=>Count();
          public static int Null()=>Count(null);
          public static int Three()=>Count(1,2,3);
          public static int Explicit()=>First(new[]{17,18});
          public static int Named()=>Two(values:new[]{17,18});
          public static int Forwarded()=>Forward(1,2,3);
          public static int Order(int value)=>First(value,value=2);
          public static int AfterAssignment(int value)=>First(value=2,value);
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D7"])); var rules = Support.Emit(code).GetType("Example.D7.Rules")!;
        foreach (var name in new[] { "Empty", "Null", "Three", "Explicit", "Named", "Forwarded" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(lib, "D7.Rules", name));
        foreach (var value in new[] { int.MinValue, 0, 18, int.MaxValue }) foreach (var name in new[] { "Order", "AfterAssignment" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D7.Rules", name, value));
        foreach (var source in new[] { "public static int F(params int[] values)=>values.Length;", "public static int F(params int[] values){if(values==null)return 0;return values[0];}", "public static int F(params int[] values){if(values==null || values.Length==0)return 0;values=null;return values[0];}" })
        { var result = Support.Convert("namespace Example.D7;public static class Rules{" + source + "}", selectors: ["Example.D7"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, d => d.Code is "MCS2501" or "MCS2808"); }
    }

    [Fact]
    public void MethodReferencesReuseExactCatalogMappings()
    {
        const string code = """
        namespace Example.D7;
        public static class Rules {
          public static bool Finite(double value){System.Func<double,bool> f=double.IsFinite;return f(value);}
          public static decimal Absolute(){System.Func<decimal,decimal> f=decimal.Abs;return f(-1.2300m);}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D7"]));
        foreach (var value in new[] { double.NaN, double.NegativeInfinity, double.PositiveInfinity, -0.0, 0.0, 1.5, double.MaxValue }) Assert.Equal(double.IsFinite(value), Support.Call(lib, "D7.Rules", "Finite", value));
        var absolute = (Dictionary<string, object>)Support.Call(lib, "D7.Rules", "Absolute"); Assert.Equal(1.2300m, absolute["value"]); Assert.Equal(4, absolute["scale"]); Assert.False((bool)absolute["negative"]);
    }
    [Fact]
    public void RemainingNormalizedCollisionsReportBothDeclarationsAndUnknownBodiesReject()
    {
        var collision = Support.Convert("namespace Example.D7;public static class Rules{public static int FooBar()=>1;public static int Foo_Bar()=>2;}", selectors: ["Example.D7"]);
        Assert.Null(collision.Distribution); var diagnostics = collision.Diagnostics.Where(d => d.Code == "MCS1004").ToArray(); Assert.Equal(2, diagnostics.Length); Assert.NotEqual(diagnostics[0].Column, diagnostics[1].Column);
        foreach (var code in new[]{
            "public readonly record struct Hidden(int Value){private int Getter=>System.Environment.TickCount;public int Read()=>Getter;}",
            "public readonly record struct Hidden(int Value){public Hidden():this(System.Environment.TickCount){}}",
            "public readonly record struct Hidden(int Value){public static Hidden operator +(Hidden a,Hidden b)=>new(System.Environment.TickCount);}",
            "public static class Rules{private static int Hidden()=>System.Environment.TickCount;public static int F()=>Hidden();}"})
        { var result = Support.Convert("namespace Example.D7;" + code, selectors: ["Example.D7"]); Assert.Null(result.Distribution); Assert.NotEmpty(result.Diagnostics); Assert.All(result.Diagnostics, d => Assert.True(d.File.Length > 0)); }
    }

    [Fact]
    public void CumulativeCallAdditionsMatchCompiledCSharpAtBoundaries()
    {
        var code = Support.Read("samples/CurrentSupportedExample/Calls.cs");
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.CurrentSupported"])); var assembly = Support.Emit(code); var rules = assembly.GetType("Example.CurrentSupported.CurrentSupportedExample")!; var customer = assembly.GetType("Example.CurrentSupported.CallCustomer")!;
        foreach (var value in new[] { int.MinValue, 0, 17, 18, 100, int.MaxValue })
        {
            foreach (var name in new[] { "CallEligible", "CallDefaultEligible", "CallNamedOrder", "CallPacked", "CallLocal", "CallReference", "CallSnapshot" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "CurrentSupported.CurrentSupportedExample", name, value));
            Assert.Equal(rules.GetMethod("CallInstance")!.Invoke(null, [Activator.CreateInstance(customer, [value])]), Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "CallInstance", new Dictionary<string, object> { { "age", value } }));
        }
        foreach (var value in new[] { false, true }) Assert.Equal(value, Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "CallApproved", value));
        foreach (var name in new[] { "CallEmpty", "CallMissing" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(lib, "CurrentSupported.CurrentSupportedExample", name));
    }

}
