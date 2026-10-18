using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class GenericDispatchTests
{
    [Fact]
    public void GenericPresenceViewsHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/generic-view.json", Support.Read("fixtures/D10/csharp/GenericView.cs"), "Example", "Example.D10");

    [Fact]
    public void GenericPresenceViewsRetainWholeArgumentsAndGuardClosedDispatch()
    {
        const string code = """
        namespace Example.D10;
        public interface IRule{bool Allows(int age);}
        public readonly record struct Minimum(int Bound):IRule{public bool Allows(int age)=>age>=Bound;}
        public readonly record struct Maximum(int Bound):IRule{public bool Allows(int age)=>age<=Bound;}
        public static class Rules{
          public static bool Apply<T>(T rule,int age)where T:IRule{if(rule is null)return false;return rule.Allows(age);}
          public static bool Boolean<T>(T rule,int age)where T:IRule{var present=rule is not null;return present&&rule.Allows(age);}
          private static bool Forward<T>(T rule,int age)where T:IRule=>Apply(rule,age);
          private static bool Struct<T>(T rule,int age)where T:struct,IRule=>Forward(rule,age);
          public static bool Min(int bound,int age)=>Struct(new Minimum(bound),age);
          public static bool Max(int bound,int age)=>Forward(new Maximum(bound),age);
          public static bool Interface(int bound,int age)=>Apply<IRule>(new Minimum(bound),age);
          public static bool Absent(int age)=>Apply<IRule>(null,age);
          public static bool Reference(int bound,int age){System.Func<IRule,int,bool> function=Boolean<IRule>;return function(new Maximum(bound),age);}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var bound in new[] { int.MinValue, 0, 18, int.MaxValue }) foreach (var age in new[] { int.MinValue, 0, 17, 18, 19, int.MaxValue }) foreach (var name in new[] { "Min", "Max", "Interface", "Reference" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [bound, age]), Support.Call(library, "D10.Rules", name, bound, age));
        foreach (var age in new[] { int.MinValue, 0, int.MaxValue }) Assert.Equal(false, Support.Call(library, "D10.Rules", "Absent", age));
        var invalid = code.Replace("return rule.Allows(age);", "rule=default;return rule.Allows(age);");
        var rejected = Support.Convert(invalid, selectors: ["Example.D10"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS2501");
    }

    [Fact]
    public void ConstrainedDispatchHasCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/generic-dispatch.json", Support.Read("fixtures/D10/csharp/GenericDispatch.cs"), "Example", "Example.D10");

    [Fact]
    public void StructConstraintsUseResolvedMemberWitnessesWithoutBlanketSpecialization()
    {
        const string code = """
        namespace Example.D10;
        public interface IRule{bool Allows(int age);}
        public readonly record struct Minimum(int Bound):IRule{public bool Allows(int age)=>age>=Bound;}
        public readonly record struct Maximum(int Bound):IRule{public bool Allows(int age)=>age<=Bound;}
        public static class Container<T>where T:struct,IRule{public static bool Apply(T rule,int age)=>rule.Allows(age);}
        public static class Rules{
          public static bool Apply<T>(T rule,int age)where T:struct,IRule=>rule.Allows(age);
          private static bool Forward<T>(T rule,int age)where T:struct,IRule=>Apply(rule,age);
          public static bool Min(int bound,int age)=>Forward(new Minimum(bound),age);
          public static bool Max(int bound,int age)=>Container<Maximum>.Apply(new Maximum(bound),age);
          public static bool Reference(int bound,int age){System.Func<Minimum,int,bool> function=Apply<Minimum>;return function(new Minimum(bound),age);}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var bound in new[] { int.MinValue, 0, 18, int.MaxValue }) foreach (var age in new[] { int.MinValue, 0, 17, 18, 19, int.MaxValue }) foreach (var name in new[] { "Min", "Max", "Reference" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [bound, age]), Support.Call(library, "D10.Rules", name, bound, age));
    }

    [Fact]
    public void CreationWitnessesCarryTheirConstructorsConstrainedMemberRequirements()
    {
        const string code = """
        namespace Example.D10;
        public interface IRule{bool Allows(int age);}
        public readonly record struct Minimum(int Bound):IRule{public bool Allows(int age)=>age>=Bound;}
        public readonly record struct Maximum(int Bound):IRule{public bool Allows(int age)=>age<=Bound;}
        public sealed class Probe<T>where T:struct,IRule{public bool Value;public Probe(){T rule=default;Value=rule.Allows(18);}}
        public static class Rules{
          private static T Create<T>()where T:new()=>new T();
          private static Probe<T> Build<T>()where T:struct,IRule=>Create<Probe<T>>();
          public static bool Min()=>Build<Minimum>().Value;
          public static bool Max()=>Build<Maximum>().Value;
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var name in new[] { "Min", "Max" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(library, "D10.Rules", name));
    }
}
