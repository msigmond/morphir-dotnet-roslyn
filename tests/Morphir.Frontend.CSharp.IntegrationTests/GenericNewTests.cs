using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class GenericNewTests
{
    [Fact]
    public void FiniteNestedCreationWitnessesDoNotBecomeSourceRecursion()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Box<T> where T:new(){public T Value;public Box(){Value=new T();}}
        public static class Rules{
          public static int Nested(){var first=new Box<Box<int>>();var second=new Box<Box<int>>();first.Value.Value=2;return second.Value.Value;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        Assert.Equal(Support.Emit(code).GetType("Example.D10.Rules")!.GetMethod("Nested")!.Invoke(null, null), Support.Call(library, "D10.Rules", "Nested"));
    }

    [Fact]
    public void CreationWitnessesCannotHideActualRecursiveConstruction()
    {
        const string code = "namespace Example.D10;public sealed class Box{public Box(){var ignored=Rules.Create<Box>();}}public static class Rules{public static T Create<T>()where T:new()=>new T();}";
        var result = Support.Convert(code, selectors: ["Example.D10"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == "MCS2203" && item.File == "Case.cs");
    }

    [Fact]
    public void CreationWitnessesHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/generic-creation.json", Support.Read("fixtures/D10/csharp/GenericCreation.cs"), "Example", "Example.D10");

    [Fact]
    public void CreationWitnessesPreserveActualConstructorsDefaultsAndFreshOwnership()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Counter{public int Value;public Counter(){Value=18;}}
        public struct Amount{public int Value;public Amount(){Value=19;}}
        public sealed class Initial{public int Value=20;}
        public static class Rules{
          public static T Create<T>() where T:new()=>new T();
          private static T Forward<T>() where T:new()=>Create<T>();
          public static int Class()=>Create<Counter>().Value;
          public static int Struct()=>Forward<Amount>().Value;
          public static int Initialized()=>Create<Initial>().Value;
          public static int Integer()=>Create<int>();
          public static int DefaultStruct()=>default(Amount).Value;
          public static int Update(){var first=Create<Counter>();var second=Create<Counter>();first.Value=2;return second.Value;}
          public static int Alias(){var data=Create<Counter>();var alias=data;alias.Value=2;return data.Value;}
          public static int Reference(){System.Func<Amount> factory=Create<Amount>;return factory().Value;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var name in new[] { "Class", "Struct", "Initialized", "Integer", "DefaultStruct", "Update", "Alias", "Reference" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(library, "D10.Rules", name));
    }

    [Fact]
    public void CreationWitnessesDoNotHideEffectsInResolvedConstructors()
    {
        const string code = "namespace Example.D10;public sealed class Counter{public int Value=System.Environment.TickCount;}public static class Rules{public static T Create<T>()where T:new()=>new T();public static Counter F()=>Create<Counter>();}";
        var result = Support.Convert(code, selectors: ["Example.D10"]); Assert.Null(result.Distribution); Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void CreationWitnessesCarryTransitiveDefaultAndAbsenceRequirements()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Payload<T,U>{
          public T Value;public int Flag;
          private static V Default<V>()=>default;
          public Payload(){U ignored=Default<U>();if(Value is null)Flag=1;else Flag=2;}
        }
        public static class Rules{
          private static T Create<T>()where T:new()=>new T();
          private static Payload<T,bool> Build<T>()=>Create<Payload<T,bool>>();
          public static int Text()=>Build<string?>().Flag;
          public static int Integer()=>Build<int>().Flag;
          public static int Nullable()=>Build<int?>().Flag;
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var name in new[] { "Text", "Integer", "Nullable" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(library, "D10.Rules", name));
    }
}
