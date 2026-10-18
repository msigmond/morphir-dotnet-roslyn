using Morphir.IR.Classic;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class GenericsTests
{
    [Fact]
    public void NestedOwnerParametersRemainDistinctInCanonicalOverloadSignatures()
    {
        const string code = """
        namespace Example.D10;
        public static class Outer<T>{public static class Inner<U>{public static T Choose(T value)=>value;public static U Choose(U value)=>value;}}
        public static class Rules{
          public static int Integer(int value)=>Outer<int>.Inner<bool>.Choose(value);
          public static bool Boolean(bool value)=>Outer<int>.Inner<bool>.Choose(value);
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        foreach (var value in new[] { int.MinValue, 0, int.MaxValue }) Assert.Equal(value, Support.Call(library, "D10.Rules", "Integer", value));
        foreach (var value in new[] { false, true }) Assert.Equal(value, Support.Call(library, "D10.Rules", "Boolean", value));
    }

    [Fact]
    public void OrdinaryGenericsHaveCompleteIndependentCodecParity() => Support.Parity("fixtures/D10/expected/generic-values.json", Support.Read("fixtures/D10/csharp/GenericValues.cs"), "Example", "Example.D10");

    [Fact]
    public void OrdinaryGenericValuesKeepVariablesAndInstantiateMaybeArguments()
    {
        const string code = """
        namespace Example.D10;
        public readonly record struct Box<T>(T Value){public T Read()=>Value;}
        public static class Rules{
          public static T Identity<T>(T value)=>value;
          public static Box<T> Wrap<T>(T value)=>new(value);
          public static int Integer(int value)=>Wrap(Identity(value)).Read();
          public static string? Text(string? value)=>Wrap(Identity(value)).Value;
          public static Box<string?> Optional(string? value)=>Wrap(value);
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        var module = library.Definition.Modules.Single(entry => entry.Key.Key == Morphir.IR.Classic.Path.From("D10").Key).Value.Value;
        var box = Assert.IsType<AliasDefinition<Unit>>(module.Types.Single(entry => entry.Key.Key == "box").Value.Value.Value);
        Assert.Equal("t", Assert.Single(box.Parameters).Key);
        Assert.IsType<VariableType<Unit>>(Assert.Single(Assert.IsType<RecordType<Unit>>(box.Body).Fields).Value);
        var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) Assert.Equal(rules.GetMethod("Integer")!.Invoke(null, [value]), Support.Call(library, "D10.Rules", "Integer", value));
        foreach (var value in new string?[] { null, "", "hello" })
        {
            var actual = Assert.IsType<Support.MaybeData>(Support.Call(library, "D10.Rules", "Text", Support.Maybe(value)));
            Assert.Equal(value, actual.Value); Assert.Equal(value is not null, actual.Present);
            var wrapped = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(Support.Call(library, "D10.Rules", "Optional", Support.Maybe(value)));
            Assert.Equal(Support.Maybe(value), wrapped["value"]);
        }
    }

    [Fact]
    public void ParametricConstraintsAndGenericMethodReferencesRetainClosedSignatures()
    {
        const string code = """
        namespace Example.D10;
        public readonly record struct Box<T>(T Value) where T:struct;
        public static class Rules{
          private static T Identity<T>(T value) where T:unmanaged=>value;
          private static T Reference<T>(T value) where T:class?=>value;
          private static T Constructible<T>(T value) where T:new()=>value;
          public static int Integer(int value){System.Func<int,int> function=Identity<int>;return function(value);}
          public static string? Text(string? value)=>Reference(value);
          public static int Box(int value)=>Constructible(new Box<int>(value)).Value;
          public static int DefaultBox()=>default(Box<int>).Value;
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "Integer", "Box" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(library, "D10.Rules", name, value));
        Assert.Equal(0, Support.Call(library, "D10.Rules", "DefaultBox"));
        foreach (var value in new string?[] { null, "", "hello" }) Assert.Equal(Support.Maybe(value), Support.Call(library, "D10.Rules", "Text", Support.Maybe(value)));
    }

    [Fact]
    public void NestedAndShadowedGenericOwnersHaveDistinctVariablesAndArityPaths()
    {
        const string code = """
        namespace Example.D10;
        public readonly record struct Box(int Value);
        public readonly record struct Box<T>(T Value);
        public static class Outer<T>{
          public readonly record struct Pair<U>(T First,U Second);
          public static U Select<U>(Pair<U> pair)=>pair.Second;
          public static T Shadow<T>(T value)=>value;
          public static T Local<T>(T value){static T Id<T>(T input)=>input;return Id(value);}
        }
        public static class Rules{
          public static int Integer(int value)=>Outer<string>.Select(new Outer<string>.Pair<int>(null,value));
          public static int Shadow(int value)=>Outer<string>.Shadow(value);
          public static int Local(int value)=>Outer<string>.Local(value);
          public static int Plain(int value)=>new Box(value).Value;
          public static int Generic(int value)=>new Box<int>(value).Value;
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, int.MaxValue }) foreach (var name in new[] { "Integer", "Shadow", "Local", "Plain", "Generic" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(library, "D10.Rules", name, value));
        var declarations = library.Definition.Modules.Single(entry => entry.Key.Key == Morphir.IR.Classic.Path.From("D10").Key).Value.Value.Types;
        Assert.Contains(declarations, entry => entry.Key.Key == Name.From("Box__Arity0").Key); Assert.Contains(declarations, entry => entry.Key.Key == Name.From("Box__Arity1").Key);
    }

    [Fact]
    public void UnprovedConstrainedReferenceDispatchRequiresPresence()
    {
        const string code = "namespace Example.D10;public interface IRule{bool Allows(int age);}public readonly record struct Minimum(int Bound):IRule{public bool Allows(int age)=>age>=Bound;}public static class Rules{public static bool F<T>(T rule,int age)where T:IRule=>rule.Allows(age);}";
        var result = Support.Convert(code, selectors: ["Example.D10"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == "MCS2501" && item.File == "Case.cs");
    }

    [Fact]
    public void GenericAliasAndWrapperReturnsPreserveOwnedAndBorrowedReachability()
    {
        const string declarations = """
        namespace Example.D10;
        public sealed class Counter{public int Value;}
        public readonly record struct Box<T>(T Value);
        public static class Rules{
          private static T Identity<T>(T value)=>value;
          private static T Forward<T>(T value)=>Identity(value);
          private static Box<T> Wrap<T>(T value)=>new(value);
          public static int Alias(int value){var data=new Counter{Value=value};var alias=Identity(data);alias.Value=2;return data.Value;}
          public static int Wrapped(int value){var data=new Counter{Value=value};var box=Wrap(data);box.Value.Value=2;return data.Value;}
          public static int Nested(int value){var data=new Counter{Value=value};var alias=Identity(Identity(data));alias.Value=2;return data.Value;}
          public static int Forwarded(int value){var data=new Counter{Value=value};var alias=Forward(data);alias.Value=2;return data.Value;}
        """;
        var code = declarations + "}"; var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, int.MaxValue }) foreach (var name in new[] { "Alias", "Wrapped", "Nested", "Forwarded" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(library, "D10.Rules", name, value));
        foreach (var expression in new[] { "Identity(input)", "Wrap(input).Value" })
        {
            var result = Support.Convert(declarations + "public static int Borrowed(Counter input){var alias=" + expression + ";if(alias==null)return 0;alias.Value=2;return 0;}}", selectors: ["Example.D10"]);
            Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == "MCS3002");
        }
    }
}
