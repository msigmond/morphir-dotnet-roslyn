using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class GenericPresenceTests
{
    [Fact]
    public void AbsenceWitnessesHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/generic-presence.json", Support.Read("fixtures/D10/csharp/GenericPresence.cs"), "Example", "Example.D10");

    [Fact]
    public void AbsenceWitnessesPreserveValueNullableAndReferenceArgumentRepresentations()
    {
        const string code = """
        namespace Example.D10;
        public static class Rules{
          private static bool Missing<T>(T value)=>value is null;
          private static bool Forward<T>(T value)=>Missing(value);
          private static bool StructForward<T>(T value)where T:struct=>Missing(value);
          private static bool EqualAny<T>(T value)=>value==null;
          private static bool Equal<T>(T value)where T:class?=>value==null;
          private static bool Present<T>(T value)where T:class?=>value is not null;
          private static T Or<T>(T value,T fallback)where T:class?=>value??fallback;
          private static T Assign<T>(T value,T fallback)where T:class?{value??=fallback;return value;}
          public static bool Integer(int value)=>Forward(value);
          public static bool Struct(int value)=>StructForward(value);
          public static bool AnyInteger(int value)=>EqualAny(value);
          public static bool AnyNullable(int? value)=>EqualAny(value);
          public static bool Nullable(int? value)=>Forward(value);
          public static bool Text(string? value)=>Equal(value);
          public static bool TextPresent(string? value)=>Present(value);
          public static string? Coalesce(string? value,string? fallback)=>Or(value,fallback);
          public static string? Assignment(string? value,string? fallback)=>Assign(value,fallback);
          public static bool Reference(string? value){System.Func<string?,bool> function=Missing<string?>;return function(value);}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, int.MaxValue }) foreach (var name in new[] { "Integer", "Struct", "AnyInteger" }) Assert.False((bool)Support.Call(library, "D10.Rules", name, value));
        foreach (var value in new int?[] { null, int.MinValue, 0, int.MaxValue }) foreach (var name in new[] { "Nullable", "AnyNullable" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(library, "D10.Rules", name, Support.Maybe(value)));
        foreach (var value in new string?[] { null, "", "hello" }) foreach (var name in new[] { "Text", "TextPresent", "Reference" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(library, "D10.Rules", name, Support.Maybe(value)));
        foreach (var value in new string?[] { null, "", "hello" }) foreach (var fallback in new string?[] { null, "", "fallback" }) foreach (var name in new[] { "Coalesce", "Assignment" }) Assert.Equal(Support.Maybe((string?)rules.GetMethod(name)!.Invoke(null, [value, fallback])), Support.Call(library, "D10.Rules", name, Support.Maybe(value), Support.Maybe(fallback)));
    }

    [Fact]
    public void GenericCoalescingCreationRetainsLazinessAndOwnedReferenceReturns()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Counter{public int Value=18;}
        public static class Rules{
          private static T ExistingOrNew<T>(T? value)where T:class,new()=>value??new T();
          public static int Read(bool present){Counter? input=present?new Counter{Value=1}:null;return ExistingOrNew(input).Value;}
          public static int Alias(){var first=new Counter{Value=1};var alias=ExistingOrNew(first);alias.Value=2;return first.Value;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var present in new[] { false, true }) Assert.Equal(rules.GetMethod("Read")!.Invoke(null, [present]), Support.Call(library, "D10.Rules", "Read", present));
        Assert.Equal(2, Support.Call(library, "D10.Rules", "Alias"));
    }
}
