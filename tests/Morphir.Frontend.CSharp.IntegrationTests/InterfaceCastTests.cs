using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class InterfaceCastTests
{
    [Fact]
    public void ClosedTypeTestsAndCastsHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/interface-casts.json", Support.Read("fixtures/D10/csharp/InterfaceCasts.cs"), "Example", "Example.D10");

    [Fact]
    public void InheritedInterfacesHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/inherited-interfaces.json", Support.Read("fixtures/D10/csharp/InheritedInterfaces.cs"), "Example", "Example.D10");

    [Fact]
    public void OrdinaryStructAlternativesHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/value-alternatives.json", Support.Read("fixtures/D10/csharp/ValueAlternatives.cs"), "Example", "Example.D10");

    private const string Declarations = """
    namespace Example.D10;
    public interface IRule{bool Allows(int age);}
    public readonly record struct Minimum(int Bound):IRule{public bool Allows(int age)=>age>=Bound;}
    public readonly record struct Maximum(int Bound):IRule{public bool Allows(int age)=>age<=Bound;}
    public static class Rules{
    """;

    [Fact]
    public void InheritedInterfaceMembersAndCompatibleRuntimeViewsKeepOwners()
    {
        const string code = """
        namespace Example.D10;
        public interface IRule{bool Allows(int age);}
        public interface IChecked:IRule{bool Enabled();}
        public readonly record struct Minimum(int Bound):IChecked{public bool Allows(int age)=>age>=Bound;public bool Enabled()=>true;}
        public readonly record struct Maximum(int Bound):IRule{public bool Allows(int age)=>age<=Bound;}
        public static class Rules{
          private static bool Apply(IChecked? rule,int age){if(rule is null)return false;return rule.Allows(age)&&rule.Enabled();}
          private static bool Parent(IRule? rule,int age){if(rule is null)return false;return rule.Allows(age);}
          private static bool Reference(IChecked? rule,int age){if(rule is null)return false;System.Func<int,bool> function=rule.Allows;return function(age);}
          private static bool Cast(IRule? rule,int age){var check=rule as IChecked;if(check is null)return false;return check.Enabled()&&check.Allows(age);}
          private static bool Type(IRule? rule,int age){if(rule is IChecked)return ((IChecked)rule).Allows(age);return false;}
          private static bool Generic<T>(T rule,int age)where T:IChecked{if(rule is null)return false;return rule.Allows(age)&&rule.Enabled();}
          private static bool GenericBoolean<T>(T rule,int age)where T:IChecked{var present=rule is not null;return present&&rule.Allows(age)&&rule.Enabled();}
          public static bool Min(int bound,int age)=>Apply(new Minimum(bound),age);
          public static bool Upcast(int bound,int age){IChecked rule=new Minimum(bound);return Parent(rule,age);}
          public static bool Captured(int bound,int age)=>Reference(new Minimum(bound),age);
          public static bool Narrow(int bound,int age)=>Cast(new Minimum(bound),age);
          public static bool Other(int bound,int age)=>Cast(new Maximum(bound),age);
          public static bool Tested(int bound,int age)=>Type(new Minimum(bound),age);
          public static bool GenericMin(int bound,int age)=>Generic(new Minimum(bound),age);
          public static bool GenericInterface(int bound,int age)=>GenericBoolean<IChecked>(new Minimum(bound),age);
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var bound in new[] { int.MinValue, 0, 18, int.MaxValue }) foreach (var age in new[] { int.MinValue, 0, 17, 18, 19, int.MaxValue }) foreach (var name in new[] { "Min", "Upcast", "Captured", "Narrow", "Other", "Tested", "GenericMin", "GenericInterface" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [bound, age]), Support.Call(library, "D10.Rules", name, bound, age));
    }

    [Fact]
    public void ClosedTypeTestsSafeUnboxingAndNullableAsPreserveRuntimeAlternatives()
    {
        const string code = Declarations + """
          private static int Bound(IRule? rule){if(rule is Minimum)return ((Minimum)rule).Bound;return 0;}
          private static int Pattern(IRule? rule){if(rule is Minimum minimum)return minimum.Bound;return 0;}
          private static int Negated(IRule? rule){if(rule is not Minimum)return 0;return ((Minimum)rule).Bound;}
          private static bool Boolean(IRule? rule){var minimum=rule is Minimum;return minimum&&((Minimum)rule).Allows(18);}
          private static int Optional(IRule? rule){var minimum=rule as Minimum?;return minimum.HasValue?minimum.Value.Bound:0;}
          private static int Complement(IRule? rule){if(rule is null)return 0;if(rule is Minimum)return 0;return ((Maximum)rule).Bound;}
          public static int Min(int bound)=>Bound(new Minimum(bound));
          public static int Max(int bound)=>Bound(new Maximum(bound));
          public static int Null()=>Bound(null);
          public static int PatternMin(int bound)=>Pattern(new Minimum(bound));
          public static int NegatedMin(int bound)=>Negated(new Minimum(bound));
          public static bool BooleanMin(int bound)=>Boolean(new Minimum(bound));
          public static bool BooleanMax(int bound)=>Boolean(new Maximum(bound));
          public static int AsMin(int bound)=>Optional(new Minimum(bound));
          public static int AsMax(int bound)=>Optional(new Maximum(bound));
          public static int AsNull()=>Optional(null);
          public static int Known(int bound){IRule rule=new Minimum(bound);return ((Minimum)rule).Bound;}
          public static int ComplementMax(int bound)=>Complement(new Maximum(bound));
          public static int ComplementMin(int bound)=>Complement(new Minimum(bound));
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var bound in new[] { int.MinValue, 0, 18, int.MaxValue }) foreach (var name in new[] { "Min", "Max", "PatternMin", "NegatedMin", "BooleanMin", "BooleanMax", "AsMin", "AsMax", "Known", "ComplementMax", "ComplementMin" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [bound]), Support.Call(library, "D10.Rules", name, bound));
        foreach (var name in new[] { "Null", "AsNull" }) Assert.Equal(0, Support.Call(library, "D10.Rules", name));
    }

    [Fact]
    public void NullGuardsAndOldTypePredicatesCannotProveAnUnsafeCast()
    {
        foreach (var body in new[] { "return ((Minimum)rule).Bound;", "if(rule is null)return 0;return ((Minimum)rule).Bound;", "if(rule is Minimum){rule=new Maximum(0);return ((Minimum)rule).Bound;}return 0;", "var minimum=rule is Minimum;rule=new Maximum(0);if(minimum)return ((Minimum)rule).Bound;return 0;" })
        {
            var result = Support.Convert(Declarations + "public static int Unsafe(IRule? rule){" + body + "}}", selectors: ["Example.D10"]);
            Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == "MCS3113" && item.File == "Case.cs");
        }
    }

    [Fact]
    public void OrdinaryStructAlternativesBoxValueCopiesAndResolvePureMembers()
    {
        const string code = """
        namespace Example.D10;
        public interface IRule{bool Allows(int age);}
        public struct Minimum:IRule{public int Bound;public bool Allows(int age)=>age>=Bound;}
        public readonly struct Maximum:IRule{public readonly int Bound;public Maximum(int bound){Bound=bound;}public bool Allows(int age)=>age<=Bound;}
        public static class Rules{
          private static bool Test(IRule? rule,int age){if(rule is null)return false;return rule.Allows(age);}
          private static bool Constrained<T>(T rule,int age)where T:struct,IRule=>rule.Allows(age);
          public static bool Min(int bound,int age)=>Test(new Minimum{Bound=bound},age);
          public static bool Max(int bound,int age)=>Constrained(new Maximum(bound),age);
          public static bool Copy(int age){var data=new Minimum{Bound=18};IRule boxed=data;data.Bound=0;return Test(boxed,age);}
          public static bool UnboxCopy(int age){IRule boxed=new Minimum{Bound=18};var data=(Minimum)boxed;data.Bound=0;return Test(boxed,age);}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var bound in new[] { int.MinValue, 0, 18, int.MaxValue }) foreach (var age in new[] { int.MinValue, 0, 17, 18, 19, int.MaxValue }) foreach (var name in new[] { "Min", "Max" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [bound, age]), Support.Call(library, "D10.Rules", name, bound, age));
        foreach (var age in new[] { int.MinValue, 0, 17, 18, 19, int.MaxValue }) foreach (var name in new[] { "Copy", "UnboxCopy" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [age]), Support.Call(library, "D10.Rules", name, age));
        var mutation = code.Replace("public bool Allows(int age)=>age>=Bound;", "public bool Allows(int age){Bound=2;return age>=Bound;}");
        var rejected = Support.Convert(mutation, selectors: ["Example.D10"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS3002" || item.Code == "MCS2404");
    }
}
