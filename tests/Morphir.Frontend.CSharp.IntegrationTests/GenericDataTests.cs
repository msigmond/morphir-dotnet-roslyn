using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class GenericDataTests
{
    [Fact]
    public void NestedConstructedValueLayoutsRetainDefaultsAndReturnedReferenceLeaves()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Counter{public int Value;}
        public struct Pair<T>{public T Value;public Pair(T value){Value=value;}}
        public static class Rules{
          private static Pair<Pair<T>> Empty<T>()=>default;
          private static Pair<T> Identity<T>(Pair<T> value)=>value;
          public static int Default()=>Empty<int>().Value.Value;
          public static string? Text()=>Empty<string?>().Value.Value;
          public static int Alias(){var counter=new Counter{Value=1};var nested=new Pair<Pair<Counter>>(new Pair<Counter>(counter));var result=Identity(nested);result.Value.Value.Value=2;return counter.Value;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        Assert.Equal(0, Support.Call(library, "D10.Rules", "Default")); Assert.Equal(Support.Maybe(null), Support.Call(library, "D10.Rules", "Text")); Assert.Equal(2, Support.Call(library, "D10.Rules", "Alias"));
        var borrowed = code[..code.LastIndexOf('}')] + "public static int Borrowed(Counter input){if(input is null)return 0;var nested=new Pair<Pair<Counter>>(new Pair<Counter>(input));var result=Identity(nested);result.Value.Value.Value=2;return 0;}}";
        var rejected = Support.Convert(borrowed, selectors: ["Example.D10"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS3002");
    }

    [Fact]
    public void SelfInstantiatedGenericOwnersKeepCallerTypeArgumentsAndAliases()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Box<T>{public T Value;public Box(T value){Value=value;}public Box<Box<T>> Wrap()=>new(this);}
        public struct Pair<T>{public T Value;public Pair(T value){Value=value;}public Pair<Pair<T>> Wrap()=>new(this);}
        public static class Rules{
          public static int Class(int value){var box=new Box<int>(value);var nested=box.Wrap();nested.Value.Value=2;return box.Value;}
          public static int Struct(int value)=>new Pair<int>(value).Wrap().Value.Value;
          public static int Default()=>default(Pair<Pair<int>>).Value.Value;
          public static string? Text(string? value)=>new Box<string?>(value).Wrap().Value.Value;
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, int.MaxValue }) foreach (var name in new[] { "Class", "Struct" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(library, "D10.Rules", name, value));
        Assert.Equal(0, Support.Call(library, "D10.Rules", "Default"));
        foreach (var value in new string?[] { null, "", "hello" }) Assert.Equal(Support.Maybe(value), Support.Call(library, "D10.Rules", "Text", Support.Maybe(value)));
    }

    [Fact]
    public void GenericReadonlyWrapperResultsObserveCurrentReferencedValues()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Counter{public int Value;}
        public readonly record struct Box<T>(T Value);
        public static class Rules{
          public static Box<Box<Counter>> Snapshot(int value){var counter=new Counter{Value=value};var box=new Box<Box<Counter>>(new(counter));counter.Value=2;return box;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        foreach (var value in new[] { int.MinValue, 0, int.MaxValue })
        {
            var outer = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(Support.Call(library, "D10.Rules", "Snapshot", value));
            var inner = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(outer["value"]);
            var counter = Assert.IsType<Support.MaybeData>(inner["value"]);
            Assert.True(counter.Present);
            Assert.Equal(2, Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(counter.Value)["value"]);
        }
    }

    [Fact]
    public void GenericStorageAndDefaultWitnessesHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/generic-storage.json", Support.Read("fixtures/D10/csharp/GenericStorage.cs"), "Example", "Example.D10");

    [Fact]
    public void GenericStoragePreservesConstructedFieldsDefaultsCopiesAndMethodSignatures()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Cell<T>{public T Value;public Cell(T value){Value=value;}public T Read()=>Value;public T Reset{get=>default;set{Value=default;}}}
        public sealed class Empty<T>{public T Value;}
        public struct Pair<T>{public T Value;public Pair(T value){Value=value;}public T Read()=>Value;}
        public record Holder<T>(T Value);
        public static class Rules{
          private static Cell<T> Create<T>(T value)=>new(value);
          private static Empty<T> Fresh<T>()=>new();
          public static int Read(int value)=>Create(value).Read();
          public static int Alias(int value){var data=Create(value);var alias=data;alias.Value=2;return data.Value;}
          public static int Struct(int value){var data=new Pair<int>(value);var copy=data;copy.Value=2;return data.Read();}
          public static int Default()=>default(Pair<int>).Value;
          public static int Empty()=>Fresh<int>().Value;
          public static string? Text(string? value)=>Create(value).Read();
          public static string? DefaultText()=>Fresh<string?>().Value;
          public static int Setter(int value){var data=Create(value);data.Reset=value;return data.Value;}
          public static int Getter(int value)=>Create(value).Reset;
          public static int Record(int value){var data=new Holder<int>(value);var copy=data with{Value=2};return data.Value;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "Read", "Alias", "Struct", "Setter", "Getter", "Record" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(library, "D10.Rules", name, value));
        foreach (var name in new[] { "Default", "Empty" }) Assert.Equal(0, Support.Call(library, "D10.Rules", name));
        foreach (var value in new string?[] { null, "", "hello" }) Assert.Equal(Support.Maybe(value), Support.Call(library, "D10.Rules", "Text", Support.Maybe(value)));
        Assert.Equal(Support.Maybe(null), Support.Call(library, "D10.Rules", "DefaultText"));
    }

    [Fact]
    public void GenericReferenceFieldsKeepOwnedAliasesAndFiniteRecursivePayloads()
    {
        const string declarations = """
        namespace Example.D10;
        public sealed class Counter{public int Value;}
        public sealed class Cell<T>(T value){public T Value=value;public T Read()=>Value;}
        public struct Pair<T>(T value){public T Value=value;}
        public sealed class Node<T>{public T Value;public Node<T>? Next;}
        public static class Rules{
          public static int Class(int value){var counter=new Counter{Value=value};var cell=new Cell<Counter>(counter);cell.Read().Value=2;return counter.Value;}
          public static int Struct(int value){var counter=new Counter{Value=value};var pair=new Pair<Counter>(counter);var copy=pair;copy.Value.Value=2;return counter.Value;}
          public static int Finite(){var leaf=new Node<int>{Value=1};var root=new Node<int>{Next=leaf};root.Next.Value=2;return leaf.Value;}
        """;
        var code = declarations + "}"; var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, int.MaxValue }) foreach (var name in new[] { "Class", "Struct" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(library, "D10.Rules", name, value));
        Assert.Equal(2, Support.Call(library, "D10.Rules", "Finite"));
        var borrowed = Support.Convert(declarations + "public static int Borrowed(Counter input){var wrapper=new Cell<Counter>(input);wrapper.Value.Value=2;return 0;}}", selectors: ["Example.D10"]);
        Assert.Null(borrowed.Distribution); Assert.Contains(borrowed.Diagnostics, item => item.Code == "MCS3002");
        var cycle = Support.Convert(declarations + "public static int Cycle(){var node=new Node<int>();node.Next=node;return 0;}}", selectors: ["Example.D10"]);
        Assert.Null(cycle.Distribution); Assert.Contains(cycle.Diagnostics, item => item.Code == "MCS3007");
    }
}
