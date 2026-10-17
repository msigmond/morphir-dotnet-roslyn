using Morphir.Frontend.CSharp;
using Morphir.IR.Json;
using Morphir.Tests;
namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D4")]
public class PresenceTests
{
    [Fact]
    public void SeedHasGuardedExtractionAndExplicitFallback()
    {
        var code = Support.Read("fixtures/D4/csharp/Maybe.cs"); var lib = Support.Success(Support.Convert(code, selectors: ["Example.D4"]));
        var difference = StructuralComparison.Compare(Support.Read("fixtures/D4/expected/maybe.json"), Codec.Encode(lib)); Assert.True(difference is null, difference?.ToString());
        foreach (int? age in new int?[] { null, int.MinValue, 17, 18, int.MaxValue }) foreach (string? region in new string?[] { null, "", "UK" })
            {
                var data = new Dictionary<string, object> { { "age", Support.Maybe(age) }, { "region", Support.Maybe(region) } };
                Assert.Equal(age >= 18, Support.Call(lib, "D4.Rules", "Adult", data));
                Assert.Equal(region ?? "unknown", Support.Call(lib, "D4.Rules", "RegionOrDefault", data));
            }
    }
    [Fact]
    public void DefaultsGuardsConstructionCopiesAndConditionalAccess()
    {
        const string code = """
        namespace Example.D4;
        public readonly record struct Customer(int? Age,string Region);
        public static class Rules {
          public static int GuardAlias(int? age) { bool present=age.HasValue; if(present) return age.Value; return 0; }
          public static int Guard(int? age) { if(age is null) return 0; return age.Value; }
          public static int Conditional(string? region) => region?.Length ?? 99;
          public static int Annotated(string region) { if(region != null) return region.Length; return 99; }
          public static int Local() { int? age=18; return age.Value; }
          public static int Constructed() { var data=new Customer(18,"yes"); return data.Age.Value; }
          public static string DefaultRegion() { var data=default(Customer); return data.Region??"unknown"; }
          public static int Copy(Customer data) { var copy=data; if(data.Age.HasValue) { data=default; return copy.Age.Value; } return 0; }
          public static int Short(int? age) => age.HasValue && age.Value>=18 ? 1:0;
          public static string? Echo(string value) => value;
          public static string ThroughCall(string value) => Echo(value)??"unknown";
          public static int DefaultMember(int? value) => value.GetValueOrDefault(99);
          public static int DefaultMemberZero(int? value) => value.GetValueOrDefault();
          public static int CoalesceAssignment(int? value) { value??=18; return value.Value; }
          public static int CoalesceOrder(int? value) { int result=3; int actual=value??(result=4); return result; }
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D4"]));
        foreach (var method in new[] { "Local", "Constructed" }) Assert.Equal(18, Support.Call(lib, "D4.Rules", method));
        Assert.Equal("unknown", Support.Call(lib, "D4.Rules", "DefaultRegion"));
        foreach (int? age in new int?[] { null, int.MinValue, 0, 17, 18, int.MaxValue })
        {
            Assert.Equal(age ?? 0, Support.Call(lib, "D4.Rules", "Guard", Support.Maybe(age)));
            Assert.Equal(age ?? 0, Support.Call(lib, "D4.Rules", "GuardAlias", Support.Maybe(age)));
            Assert.Equal(age ?? 99, Support.Call(lib, "D4.Rules", "DefaultMember", Support.Maybe(age)));
            Assert.Equal(age ?? 0, Support.Call(lib, "D4.Rules", "DefaultMemberZero", Support.Maybe(age)));
            Assert.Equal(age ?? 18, Support.Call(lib, "D4.Rules", "CoalesceAssignment", Support.Maybe(age)));
            Assert.Equal(age >= 18 ? 1 : 0, Support.Call(lib, "D4.Rules", "Short", Support.Maybe(age)));
            Assert.Equal(age ?? 0, Support.Call(lib, "D4.Rules", "Copy", new Dictionary<string, object> { { "age", Support.Maybe(age) }, { "region", Support.Maybe(null) } }));
            Assert.Equal(age.HasValue ? 3 : 4, Support.Call(lib, "D4.Rules", "CoalesceOrder", Support.Maybe(age)));
        }
        foreach (string? value in new string?[] { null, "", "a", "😀" })
        {
            foreach (var method in new[] { "Conditional", "Annotated" }) Assert.Equal(value?.Length ?? 99, Support.Call(lib, "D4.Rules", method, Support.Maybe(value)));
            Assert.Equal(value ?? "unknown", Support.Call(lib, "D4.Rules", "ThroughCall", Support.Maybe(value)));
        }
    }
    [Fact]
    public void LiftedOperatorsMatchClrIncludingThreeValuedBooleanLogic()
    {
        const string code = """
        namespace Example.D4;
        public static class Rules {
          public static bool Equal(int? a,int? b)=>a==b;
          public static bool RelationalNull(int? a)=>a>=null;
          public static bool Different(int? a,int? b)=>a!=b;
          public static bool Greater(int? a,int? b)=>a>=b;
          public static int? AddZero(int? a)=>a+0;
          public static bool? And(bool? a,bool? b)=>a&b;
          public static bool? Or(bool? a,bool? b)=>a|b;
          public static bool? Xor(bool? a,bool? b)=>a^b;
          public static bool? Not(bool? a)=>!a;
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D4"])); var assembly = Support.Emit(code); var rules = assembly.GetType("Example.D4.Rules")!;
        foreach (int? a in new int?[] { null, int.MinValue, 0, int.MaxValue }) foreach (int? b in new int?[] { null, int.MinValue, 0, int.MaxValue }) foreach (var method in new[] { "Equal", "Different", "Greater" })
                    Assert.Equal(rules.GetMethod(method)!.Invoke(null, [a, b]), Support.Call(lib, "D4.Rules", method, Support.Maybe(a), Support.Maybe(b)));
        foreach (int? a in new int?[] { null, int.MinValue, 0, int.MaxValue }) Assert.False((bool)Support.Call(lib, "D4.Rules", "RelationalNull", Support.Maybe(a)));
        foreach (int? a in new int?[] { null, int.MinValue, 0, int.MaxValue }) Assert.Equal(Support.Maybe(a), Support.Call(lib, "D4.Rules", "AddZero", Support.Maybe(a)));
        foreach (bool? a in new bool?[] { null, false, true })
        {
            Assert.Equal(Support.Maybe(!a), Support.Call(lib, "D4.Rules", "Not", Support.Maybe(a)));
            foreach (bool? b in new bool?[] { null, false, true }) foreach (var method in new[] { "And", "Or", "Xor" }) Assert.Equal(Support.Maybe(rules.GetMethod(method)!.Invoke(null, [a, b])), Support.Call(lib, "D4.Rules", method, Support.Maybe(a), Support.Maybe(b)));
        }
    }
    [Fact]
    public void CumulativeSampleBehaviorCoversEveryD4Addition()
    {
        var compilation = SourceCompilation.Create(new[] { "CurrentSupportedExample.cs", "Blocks.cs", "Presence.cs" }.Select(name => (name, Support.Read("samples/CurrentSupportedExample/" + name))));
        var lib = Support.Success(Morphir.Frontend.CSharp.Frontend.Convert(compilation, new("Example", ["Example.CurrentSupported"])));
        var assembly = Support.Emit(compilation); var rules = assembly.GetType("Example.CurrentSupported.CurrentSupportedExample")!;
        object Call(string name, params object[] args) => Support.Call(lib, "CurrentSupported.CurrentSupportedExample", name, args);
        foreach (int? age in new int?[] { null, int.MinValue, 0, 17, 18, int.MaxValue })
        {
            foreach (string? region in new string?[] { null, "", "UK", "😀" })
            {
                var data = new Dictionary<string, object> { { "age", Support.Maybe(age) }, { "region", Support.Maybe(region) } };
                Assert.Equal(age >= 18, Call("MissingAdult", data)); Assert.Equal(region ?? "unknown", Call("RegionOrUnknown", data));
                Assert.Equal(region?.Length ?? 99, Call("RegionLength", data)); Assert.Equal(age ?? 0, Call("GuardCopy", data));
                Assert.Equal(region?.Length ?? 0, Call("GuardedAnnotation", new Dictionary<string, object> { { "region", Support.Maybe(region) } }));
                Assert.Equal(region ?? "unknown", Call("ReferenceCall", Support.Maybe(region)));
                Assert.Equal(Support.Maybe(region), Call("EchoReference", Support.Maybe(region)));
            }
            Assert.Equal(Support.Maybe(age), Call("NullableAddZero", Support.Maybe(age)));
            Assert.Equal(age ?? 99, Call("DefaultFromMember", Support.Maybe(age))); Assert.Equal(age ?? 18, Call("InitializeMissing", Support.Maybe(age)));
            Assert.Equal(age.HasValue ? 3 : 4, Call("CoalesceState", Support.Maybe(age)));
            foreach (int? other in new int?[] { null, int.MinValue, 0, int.MaxValue }) foreach (var method in new[] { "NullableEqual", "NullableGreater" })
                    Assert.Equal(rules.GetMethod(method)!.Invoke(null, [age, other]), Call(method, Support.Maybe(age), Support.Maybe(other)));
        }
        foreach (bool? a in new bool?[] { null, false, true })
        {
            Assert.Equal(Support.Maybe(!a), Call("NullableNot", Support.Maybe(a)));
            foreach (bool? b in new bool?[] { null, false, true }) foreach (var method in new[] { "NullableAnd", "NullableOr", "NullableXor" })
                    Assert.Equal(Support.Maybe(rules.GetMethod(method)!.Invoke(null, [a, b])), Call(method, Support.Maybe(a), Support.Maybe(b)));
        }
        foreach (var method in new[] { "NullableLocal", "ConstructedAge" }) Assert.Equal(18, Call(method));
        foreach (var method in new[] { "DefaultOptional", "DefaultRecord" }) Assert.Equal(Support.Maybe(null), ((Dictionary<string, object>)Call(method))["region"]);
        Assert.Equal(Support.Maybe(18), ((Dictionary<string, object>)Call("ConstructOptional"))["age"]);
        Assert.Equal(Support.Maybe(null), Call("NullResult"));
    }
    [Theory]
    [InlineData("public static int F(int? value)=>value.Value;", "MCS2501")]
    [InlineData("public static int F(int? value){bool present=value.HasValue;value=null;if(present)return value.Value;return 0;}", "MCS2501")]
    [InlineData("public static int F(){string value=default!;return value.Length;}", "MCS2501")]
    [InlineData("public static int F(string value)=>value!.Length;", "MCS2501")]
    [InlineData("public static int F(int? value){if(value.HasValue){value=null;return value.Value;}return 0;}", "MCS2501")]
    [InlineData("public static int F(int? value){try{return value.Value;}catch{return 0;}}", "MCS2403")]
    [InlineData("public static int F(int? value){if(value.HasValue){System.Console.WriteLine(1);return value.Value;}return 0;}", "MCS2202")]
    public void MissingProofAndUnsupportedEffectsFailWholeConversion(string member, string diagnostic)
    {
        var result = Support.Convert("namespace Example.D4; public static class Rules {" + member + "}", selectors: ["Example.D4"]);
        Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, d => d.Code == diagnostic && d.Line > 0 && d.Column > 0);
    }
}
