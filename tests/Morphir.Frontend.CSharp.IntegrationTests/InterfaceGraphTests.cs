using Morphir.IR.Classic;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class InterfaceGraphTests
{
    [Fact]
    public void ReferenceInterfacePayloadsAndResultsHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/reference-interfaces.json", Support.Read("fixtures/D10/csharp/ReferenceInterfaces.cs"), "Example", "Example.D10");

    private const string Declarations = """
    namespace Example.D10;
    public sealed class Counter{public int Value;}
    public interface IBox{Counter? Read();bool Allows(int age);IBox Self();}
    public sealed class Holder:IBox{public Counter? Child;public int Bound;public IBox? Next;public Counter? Read()=>Child;public bool Allows(int age)=>age>=Bound;public IBox Self()=>this;}
    public readonly record struct ValueHolder(Counter? Child,int Bound):IBox{public Counter? Read()=>Child;public bool Allows(int age)=>age<=Bound;public IBox Self()=>this;}
    public static class Rules{
      private static Counter? Read(IBox? box){if(box is null)return null;return box.Read();}
      private static IBox Forward(IBox box)=>box;
      private static bool Allows(IBox? box,int age){if(box is null)return false;return box.Allows(age);}
    """;

    [Fact]
    public void InterfaceGraphsPreserveClassAndValuePayloadAliasesAndLiveSnapshots()
    {
        const string code = Declarations + """
          public static int ClassAlias(){var counter=new Counter{Value=1};var holder=new Holder{Child=counter};IBox box=holder;var alias=Read(box);if(alias is null)return 0;alias.Value=2;return counter.Value;}
          public static int ValueAlias(){var counter=new Counter{Value=1};IBox box=new ValueHolder(counter,18);var alias=Read(box);if(alias is null)return 0;alias.Value=2;return counter.Value;}
          public static int Forwarded(){var counter=new Counter{Value=1};IBox box=new Holder{Child=counter};var alias=Read(Forward(box).Self());if(alias is null)return 0;alias.Value=2;return counter.Value;}
          public static bool Live(int age){var holder=new Holder{Bound=0};IBox box=holder;holder.Bound=18;return Allows(box,age);}
          public static bool CastAlias(int age){IBox box=new Holder{Bound=0};var holder=(Holder)box;holder.Bound=18;return Allows(box,age);}
          public static bool Coalesced(int age){var holder=new Holder{Bound=0};IBox original=holder;IBox? absent=null;var box=absent??original;holder.Bound=18;return Allows(box,age);}
          public static IBox Snapshot(){var holder=new Holder{Bound=0};IBox box=holder;holder.Bound=2;return box;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var name in new[] { "ClassAlias", "ValueAlias", "Forwarded" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(library, "D10.Rules", name));
        foreach (var age in new[] { int.MinValue, 0, 17, 18, 19, int.MaxValue }) foreach (var name in new[] { "Live", "CastAlias", "Coalesced" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [age]), Support.Call(library, "D10.Rules", name, age));
        var snapshot = Assert.IsType<Support.MaybeData>(Support.Call(library, "D10.Rules", "Snapshot")); var represented = Assert.IsType<Support.CustomData>(snapshot.Value);
        Assert.Equal(Name.From("HolderBox").Key, represented.Constructor.Local.Key); Assert.Equal(2, Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(Assert.Single(represented.Arguments))["bound"]);
    }

    [Fact]
    public void BorrowedPolymorphicChildrenAndClassCyclesCannotAcquireOwnership()
    {
        foreach (var body in new[]{
          "public static int Borrowed(IBox input){if(input is null)return 0;var counter=input.Read();if(counter is null)return 0;counter.Value=2;return 0;}",
          "public static int Borrowed(Counter input){if(input is null)return 0;IBox box=new Holder{Child=input};var counter=Read(box);if(counter is null)return 0;counter.Value=2;return 0;}"})
        {
            var result = Support.Convert(Declarations + body + "}", selectors: ["Example.D10"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == "MCS3002");
        }
        var cyclic = Support.Convert(Declarations + "public static int Cycle(){var holder=new Holder();IBox box=holder;holder.Next=box;return 0;}}", selectors: ["Example.D10"]); Assert.Null(cyclic.Distribution); Assert.Contains(cyclic.Diagnostics, item => item.Code == "MCS3007");
    }

    [Fact]
    public void UnknownInputDispatchCanReturnAnIndependentFreshAllocation()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Counter{public int Value;}
        public interface IFactory{Counter Create();}
        public sealed class ClassFactory:IFactory{public Counter Create()=>new Counter{Value=1};}
        public readonly record struct ValueFactory(int Ignored):IFactory{public Counter Create()=>new Counter{Value=1};}
        public static class Rules{public static int Fresh(IFactory? input){if(input is null)return 0;var data=input.Create();data.Value=2;return data.Value;}}
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        foreach (var (name, payload) in new[] { ("ClassFactoryFactory", new Dictionary<string, object>()), ("ValueFactoryFactory", new Dictionary<string, object> { { "ignored", 0 } }) })
        {
            var represented = new Support.CustomData(new FQName(Morphir.IR.Classic.Path.From("Example"), Morphir.IR.Classic.Path.From("D10"), Name.From(name)), [payload]);
            Assert.Equal(2, Support.Call(library, "D10.Rules", "Fresh", Support.Maybe(represented)));
        }
    }

    [Fact]
    public void GenericCallsAndStoredInterfaceViewsKeepOwnedChildrenAndAbsence()
    {
        const string code = Declarations + """
          private static Counter? GenericRead<T>(T box)where T:IBox{if(box is null)return null;return box.Read();}
          private static Counter? ValueRead<T>(T box)where T:struct,IBox=>GenericRead(box);
          public static int GenericClass(){var counter=new Counter{Value=1};var box=new Holder{Child=counter};var alias=GenericRead(box);if(alias is null)return 0;alias.Value=2;return counter.Value;}
          public static int GenericValue(){var counter=new Counter{Value=1};var box=new ValueHolder(counter,18);var alias=ValueRead(box);if(alias is null)return 0;alias.Value=2;return counter.Value;}
          public static int Stored(){var counter=new Counter{Value=1};var holder=new Holder{Child=counter};var wrapper=new Holder{Next=holder};var box=wrapper.Next;if(box is null)return 0;var alias=box.Read();if(alias is null)return 0;alias.Value=2;return counter.Value;}
          public static int GenericAbsent(){var alias=GenericRead<IBox>(null);if(alias is null)return 3;return alias.Value;}
          public static bool FieldLive(int age){var holder=new Holder{Bound=0};var wrapper=new Holder{Next=holder};holder.Bound=18;return Allows(wrapper.Next,age);}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var name in new[] { "GenericClass", "GenericValue", "Stored", "GenericAbsent" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(library, "D10.Rules", name));
        foreach (var age in new[] { int.MinValue, 0, 17, 18, 19, int.MaxValue }) Assert.Equal(rules.GetMethod("FieldLive")!.Invoke(null, [age]), Support.Call(library, "D10.Rules", "FieldLive", age));
    }

    [Fact]
    public void ReferenceBearingValueAndConstructedInterfaceCapturesKeepTheD13Gate()
    {
        const string declarations = """
        namespace Example.D10;
        public sealed class Counter{public int Value;}
        public interface IValue<T>{T Read();}
        public readonly record struct Holder<U>(U Value):IValue<U>{public U Read()=>Value;}
        public readonly record struct Wrapper(IValue<Counter>? Value){public IValue<Counter>? Read()=>Value;}
        public static class Rules{
        """;
        foreach (var body in new[]{
          "public static int Capture(){var child=new Counter{Value=1};IValue<Counter> box=new Holder<Counter>(child);System.Func<Counter> function=box.Read;child.Value=2;return function().Value;}",
          "public static int Capture(){var child=new Counter{Value=1};var box=new Holder<Counter>(child);System.Func<Counter> function=box.Read;child.Value=2;return function().Value;}",
          "public static IValue<Counter>? Capture(){var child=new Counter{Value=1};var box=new Wrapper(new Holder<Counter>(child));System.Func<IValue<Counter>?> function=box.Read;child.Value=2;return function();}"})
        {
            var result = Support.Convert(declarations + body + "}", selectors: ["Example.D10"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == "MCS2802");
        }
    }
}
