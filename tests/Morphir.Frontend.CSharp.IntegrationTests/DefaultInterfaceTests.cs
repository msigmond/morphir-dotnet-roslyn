using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class DefaultInterfaceTests
{
    [Fact]
    public void DefaultInterfaceBodiesHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/default-interfaces.json", Support.Read("fixtures/D10/csharp/DefaultRules.cs"), "Example", "Example.D10");

    [Fact]
    public void ReabstractionHasCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/reabstraction.json", Support.Read("fixtures/D10/csharp/Reabstraction.cs"), "Example", "Example.D10");

    [Fact]
    public void ResolvedDefaultsAndPrivateHelpersDispatchThroughTheOriginalReceiver()
    {
        const string code = """
        namespace Example.D10;
        public interface IRule{bool Check(int age);bool Allows(int age)=>Helper(age);private bool Helper(int age)=>Check(age);}
        public readonly record struct Minimum(int Bound):IRule{public bool Check(int age)=>age>=Bound;}
        public readonly record struct Maximum(int Bound):IRule{public bool Check(int age)=>age<=Bound;public bool Allows(int age)=>age<=Bound;}
        public static class Rules{
          private static bool Apply(IRule? rule,int age){if(rule is null)return false;return rule.Allows(age);}
          private static bool Callback<T>(T rule,int age)where T:struct,IRule=>rule.Allows(age);
          public static bool Min(int bound,int age)=>Apply(new Minimum(bound),age);
          public static bool Max(int bound,int age)=>Apply(new Maximum(bound),age);
          public static bool Constrained(int bound,int age)=>Callback(new Minimum(bound),age);
          public static bool Reference(int bound,int age){IRule rule=new Minimum(bound);System.Func<int,bool> function=rule.Allows;return function(age);}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var bound in new[] { int.MinValue, 0, 18, int.MaxValue }) foreach (var age in new[] { int.MinValue, 0, 17, 18, 19, int.MaxValue }) foreach (var name in new[] { "Min", "Max", "Constrained", "Reference" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [bound, age]), Support.Call(library, "D10.Rules", name, bound, age));
    }

    [Fact]
    public void DefaultReferenceResultsPreserveOwnedAndBorrowedChildren()
    {
        const string declarations = """
        namespace Example.D10;
        public sealed class Counter{public int Value;}
        public interface ICounter{Counter? Read();Counter? Alias()=>Read();}
        public sealed class Holder:ICounter{public Counter? Child;public Counter? Read()=>Child;}
        public static class Rules{
        """;
        var code = declarations + "public static int Own(){var child=new Counter{Value=1};ICounter box=new Holder{Child=child};var alias=box.Alias();if(alias is null)return 0;alias.Value=2;return child.Value;}}";
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); Assert.Equal(2, Support.Call(library, "D10.Rules", "Own"));
        var rejected = Support.Convert(declarations + "public static int Borrowed(ICounter box){if(box is null)return 0;var alias=box.Alias();if(alias is null)return 0;alias.Value=2;return 0;}}", selectors: ["Example.D10"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS3002");
    }

    [Fact]
    public void InheritedDefaultsSelectTheMostSpecificSourceImplementation()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue<T>{T Read(T value)=>default;}
        public interface IChild<T>:IValue<T>{T IValue<T>.Read(T value)=>value;}
        public readonly record struct ChildHolder<U>(int Ignored):IChild<U>;
        public readonly record struct ParentHolder<U>(int Ignored):IValue<U>;
        public static class Rules{
          private static T Read<T>(IValue<T>? box,T value){if(box is null)return default;return box.Read(value);}
          public static int Child(int value)=>Read<int>(new ChildHolder<int>(0),value);
          public static int Parent(int value)=>Read<int>(new ParentHolder<int>(0),value);
          public static string? Text(string? value)=>Read<string?>(new ChildHolder<string?>(0),value);
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, 18, int.MaxValue }) foreach (var name in new[] { "Child", "Parent" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(library, "D10.Rules", name, value));
        foreach (var value in new string?[] { null, "", "hello" }) Assert.Equal(Support.Maybe(value), Support.Call(library, "D10.Rules", "Text", Support.Maybe(value)));
    }

    [Fact]
    public void DefaultBodiesRetainCheckedSyntaxAndRecursionRejections()
    {
        foreach (var (method, diagnostic) in new[] { ("int Read()=>checked(0);", "MCS2001"), ("int Read()=>Read();", "MCS2203") })
        {
            var code = "namespace Example.D10;public interface IValue{" + method + "}public readonly record struct ValueHolder(int Ignored):IValue;";
            var result = Support.Convert(code, selectors: ["Example.D10"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == diagnostic);
        }
    }

    [Fact]
    public void ReabstractedInheritedDefaultsResolveConcreteAndRestoredMostSpecificBodies()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue<T>{T Echo(T value)=>default;}
        public interface IRequired<T>:IValue<T>{abstract T IValue<T>.Echo(T value);}
        public interface IRestored<T>:IRequired<T>{T IValue<T>.Echo(T value)=>value;}
        public readonly record struct Required<U>(int Ignored):IRequired<U>{public U Echo(U value)=>value;}
        public readonly record struct Restored<U>(int Ignored):IRestored<U>;
        public readonly record struct Default<U>(int Ignored):IValue<U>;
        public static class Rules{
          private static T Apply<T>(IValue<T>? item,T value){if(item is null)return default;return item.Echo(value);}
          public static int Required(int value)=>Apply<int>(new Required<int>(0),value);
          public static int Restored(int value)=>Apply<int>(new Restored<int>(0),value);
          public static int Default(int value)=>Apply<int>(new Default<int>(0),value);
          public static string? Text(string? value)=>Apply<string?>(new Required<string?>(0),value);
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "Required", "Restored", "Default" })
                Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value)));
        foreach (var value in new string?[] { null, "", "sample" }) Assert.Equal(Support.Maybe(value), Support.Call(library, "D10.Rules", "Text", Support.Maybe(value)));
    }

    [Fact]
    public void ReabstractionRetainsUnknownClosureEffectsAndCompilerAmbiguityFailures()
    {
        foreach (var (code, diagnostic) in new[] {
            ("namespace Example.D10{public interface IValue{int Read()=>0;}public interface IRequired:IValue{abstract int IValue.Read();}}namespace Outside{public readonly record struct ValueHolder(int Value):Example.D10.IRequired{public int Read()=>Value;}}", "MCS3111"),
            ("namespace Example.D10;public interface IValue{int Read()=>0;}public interface IRequired:IValue{abstract int IValue.Read();}public readonly record struct ValueHolder(int Value):IRequired{public int Read(){System.Console.WriteLine(Value);return Value;}}", "MCS2202"),
            ("namespace Example.D10;public interface IValue{int Read()=>0;}public interface IRequired:IValue{abstract int IValue.Read();}public readonly record struct ValueHolder(int Value):IRequired;", "MCS0001"),
            ("namespace Example.D10;public interface IValue{int Read()=>0;}public interface ILeft:IValue{int IValue.Read()=>1;}public interface IRight:IValue{int IValue.Read()=>2;}public readonly record struct ValueHolder(int Value):ILeft,IRight;", "MCS0001")})
        {
            var result = Support.Convert(code, selectors: ["Example.D10"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == diagnostic && item.File == "Case.cs");
        }
    }

    [Fact]
    public void OverriddenDefaultsAreStillEmittedAndTheirUnusedBodiesAreValidated()
    {
        const string prefix = "namespace Example.D10;public interface IValue{";
        const string suffix = "}public interface IRequired:IValue{abstract int IValue.Read();}public readonly record struct ValueHolder(int Value):IRequired{public int Read()=>Value;}";
        var library = Support.Success(Support.Convert(prefix + "int Read()=>3;" + suffix, selectors: ["Example.D10"]));
        var original = library.Definition.Modules.Single(module => module.Key.Key == Morphir.IR.Classic.Path.From("D10.IValue").Key).Value.Value;
        Assert.Contains(original.Values, value => value.Key.Key == Morphir.IR.Classic.Name.From("ReadDefaultBody").Key && value.Value.Access == Morphir.IR.Classic.Access.Private);
        foreach (var (body, diagnostic) in new[] { ("int Read()=>checked(0);", "MCS2001"), ("int Read(){System.Console.WriteLine(0);return 0;}", "MCS2202") })
        {
            var rejected = Support.Convert(prefix + body + suffix, selectors: ["Example.D10"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == diagnostic);
        }
    }

    [Fact]
    public void ReabstractedStaticMethodsAndPropertiesUseTheResolvedSourceImplementation()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue{static virtual int Read()=>3;static virtual int Value=>4;}
        public interface IRequired:IValue{static abstract int IValue.Read();static abstract int IValue.Value{get;}}
        public readonly record struct Required(int Ignored):IRequired{public static int Read()=>7;public static int Value=>11;}
        public interface IRestored:IRequired{static int IValue.Read()=>13;static int IValue.Value=>17;}
        public readonly record struct Restored(int Ignored):IRestored;
        public static class Rules{
          private static int Read<T>()where T:IValue=>T.Read();
          private static int Value<T>()where T:IValue=>T.Value;
          public static int RequiredRead()=>Read<Required>();
          public static int RequiredValue()=>Value<Required>();
          public static int RestoredRead()=>Read<Restored>();
          public static int RestoredValue()=>Value<Restored>();
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var name in new[] { "RequiredRead", "RequiredValue", "RestoredRead", "RestoredValue" })
            Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, null)!, Convert.ToInt32(Support.Call(library, "D10.Rules", name)));
    }

    [Fact]
    public void ReabstractedGenericMethodsRetainArgumentsAndReturnedAliases()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Box{public int Value;public Box(int value){Value=value;}}
        public interface IValue{U Echo<U>(U value)=>default;}
        public interface IRequired:IValue{abstract U IValue.Echo<U>(U value);}
        public readonly record struct Holder(int Ignored):IRequired{public U Echo<U>(U value)=>value;}
        public static class Rules{
          public static int Number(int value){IValue holder=new Holder(0);return holder.Echo<int>(value);}
          public static string? Text(string? value){IValue holder=new Holder(0);return holder.Echo<string?>(value);}
          public static int Alias(int value){var child=new Box(value);IValue holder=new Holder(0);var result=holder.Echo<Box>(child);child.Value=7;if(result is null)return 0;return result.Value;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "Number", "Alias" })
                Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value)));
        foreach (var value in new string?[] { null, "", "sample" }) Assert.Equal(Support.Maybe(value), Support.Call(library, "D10.Rules", "Text", Support.Maybe(value)));
        var rejected = Support.Convert(code.Replace("public static int Number(int value)", "public static int Unsafe(Box child){IValue holder=new Holder(0);var result=holder.Echo<Box>(child);if(result is null)return 0;result.Value=1;return result.Value;}public static int Number(int value)"), selectors: ["Example.D10"]);
        Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS3002");
    }
}
