using Morphir.IR.Classic;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class GenericDefaultsTests
{
    [Fact]
    public void GenericDefaultsUseExplicitWitnessesAndActualClosedRepresentations()
    {
        const string code = """
        namespace Example.D10;
        public readonly record struct Box<T>(T Value);
        public sealed class Counter{public int Value;}
        public static class Rules{
          public static T Default<T>()=>default;
          private static T Forward<T>()=>Default<T>();
          private static T Reference<T>() where T:class?=>default;
          private static T Null<T>() where T:class?=>null;
          private static T Collision<T>(T default_t)=>default;
          public static int Integer()=>Default<int>();
          public static bool Boolean()=>Forward<bool>();
          public static int Struct()=>Default<Box<int>>().Value;
          public static string? Text()=>Reference<string?>();
          public static Counter? Missing()=>Forward<Counter>();
          public static string? LiteralNull()=>Null<string?>();
          public static int NameCollision()=>Collision(18);
          public static string? Delegate(){System.Func<string?> value=Default<string?>;return value();}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var name in new[] { "Integer", "Boolean", "Struct", "NameCollision" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(library, "D10.Rules", name));
        foreach (var name in new[] { "Text", "Missing", "LiteralNull", "Delegate" }) Assert.Equal(Support.Maybe(null), Support.Call(library, "D10.Rules", name));
        var definition = Lookup.Value(library, new(Morphir.IR.Classic.Path.From("Example"), Morphir.IR.Classic.Path.From("D10.Rules"), Name.From("Default")))!;
        var input = Assert.Single(definition.Inputs); Assert.IsType<VariableType<Unit>>(input.Type); Assert.IsType<VariableValue<Unit, IrType<Unit>>>(definition.Body);
    }
}
