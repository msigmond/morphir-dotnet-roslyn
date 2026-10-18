using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class PolymorphicPropertyTests
{
    [Fact]
    public void PropertyDispatchHasCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/polymorphic-properties.json", Support.Read("fixtures/D10/csharp/PolymorphicProperties.cs"), "Example", "Example.D10");

    [Fact]
    public void VirtualAndAbstractGettersSelectTheRuntimeOverride()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent{public int Stored;public abstract int Required{get;}public virtual int Value=>3;protected Parent(int value){Stored=value;}}
        public abstract class Middle:Parent{protected Middle(int value):base(value){}public override int Value=>5;}
        public sealed class Leaf:Middle{public Leaf(int value):base(value){}public override int Required=>Stored;}
        public sealed class Other:Parent{public Other(int value):base(value){}public override int Required=>7;public override int Value=>11;}
        public static class Rules{
          public static int Required(int value,bool first){Parent parent=first?new Leaf(value):new Other(value);return parent.Required;}
          public static int Value(int value,bool first){Parent parent=first?new Leaf(value):new Other(value);return parent.Value;}
        }
        """;
        Compare(code, ["Required", "Value"]);
    }

    [Fact]
    public void InterfaceGettersUseAutomaticComputedAndPositionalImplementations()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue{int Value{get;}}
        public readonly record struct Position(int Value):IValue;
        public sealed class Stored:IValue{public int Value{get;}public Stored(int value){Value=value;}}
        public readonly struct Computed:IValue{public int Value=>7;}
        public static class Rules{
          public static int Automatic(int value,bool first){IValue item=first?new Position(value):new Stored(value);return item.Value;}
          public static int Computed(int value,bool first){IValue item=first?new Position(value):new Computed();return item.Value;}
          private static int Read<T>(T item)where T:IValue{if(item is null)return 0;return item.Value;}
          public static int Generic(int value,bool first)=>first?Read(new Position(value)):Read(new Stored(value));
        }
        """;
        Compare(code, ["Automatic", "Computed", "Generic"]);
    }

    [Fact]
    public void InterfaceGettersFollowInheritedVirtualClassOverrides()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue{int Value{get;}}
        public abstract class Parent:IValue{public virtual int Value=>3;}
        public sealed class Leaf:Parent{public override int Value=>7;}
        public static class Rules{public static int Read(int value,bool first){IValue item=new Leaf();return item.Value;}}
        """;
        Compare(code, ["Read"]);
    }

    [Fact]
    public void DefaultAndReabstractedInterfaceGettersRetainTheirSourceTargets()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue{int Value=>3;}
        public interface IRequired:IValue{abstract int IValue.Value{get;}}
        public interface IRestored:IRequired{int IValue.Value=>5;}
        public readonly struct Default:IValue;
        public readonly struct Restored:IRestored;
        public readonly struct Explicit:IRequired{int IValue.Value=>7;}
        public static class Rules{
          public static int Default(int value,bool first){IValue item=first?new Default():new Explicit();return item.Value;}
          public static int Restored(int value,bool first){IValue item=first?new Restored():new Explicit();return item.Value;}
        }
        """;
        Compare(code, ["Default", "Restored"]);
    }

    [Fact]
    public void PolymorphicGettersKeepNullableGenericArgumentsAndOwnedAliases()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue<T>{T Value{get;}}
        public sealed class Box{public int Value;public Box(int value){Value=value;}}
        public sealed class Holder<T>:IValue<T>{public T Value{get;}public Holder(T value){Value=value;}}
        public static class Rules{
          public static int Alias(int value,bool first){var child=new Box(value);IValue<Box> item=new Holder<Box>(child);var alias=item.Value;if(alias is null)return 0;alias.Value=7;return child.Value;}
          public static string? Text(string? value){IValue<string?> item=new Holder<string?>(value);return item.Value;}
        }
        """;
        Compare(code, ["Alias"]);
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        foreach (var value in new string?[] { null, "", "test" }) Assert.Equal(Support.Maybe(value), Support.Call(library, "D10.Rules", "Text", Support.Maybe(value)));
        var rejected = Support.Convert(code.Replace("public static int Alias(int value,bool first)", "public static int Unsafe(IValue<Box> item){if(item is null)return 0;var child=item.Value;if(child is null)return 0;child.Value=7;return 0;}public static int Alias(int value,bool first)"), selectors: ["Example.D10"]);
        Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS3002");
    }

    [Fact]
    public void UnusedDefaultPropertyBodiesStillRejectEffectsAndCheckedSyntax()
    {
        foreach (var (body, diagnostic) in new[] { ("get{System.Console.WriteLine(0);return 3;}", "MCS2202"), ("get=>checked(3);", "MCS2001") })
        {
            var code = "namespace Example.D10;public interface IValue{int Value{" + body + "}}public readonly struct Item:IValue{public int Value=>7;}";
            var result = Support.Convert(code, selectors: ["Example.D10"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == diagnostic && item.File == "Case.cs");
        }
    }

    [Fact]
    public void AutomaticVirtualGettersKeepSeparateBaseStorageAndDirectBaseReads()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent{public virtual int Value{get;}=3;public virtual int Dynamic=>Value;}
        public sealed class Leaf:Parent{public override int Value{get;}=7;public int Direct=>base.Value;public int ThroughBase=>base.Dynamic;}
        public sealed class Other:Parent{}
        public static class Rules{
          public static int Virtual(int value,bool first){Parent item=first?new Leaf():new Other();return item.Value;}
          public static int Direct(int value,bool first)=>new Leaf().Direct;
          public static int Dynamic(int value,bool first)=>new Leaf().ThroughBase;
        }
        """;
        Compare(code, ["Virtual", "Direct", "Dynamic"]);
    }

    [Fact]
    public void AbstractAndDefaultIndexersKeepArgumentOrderAndBaseTargets()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue{int this[int first,int second]=>first;}
        public abstract class Parent:IValue{public abstract int this[int first,int second]{get;}}
        public sealed class Leaf:Parent{public override int this[int first,int second]=>second;}
        public readonly struct Default:IValue;
        public static class Rules{
          public static int Class(int value,bool first){Parent item=new Leaf();int local=value;return item[second:(local=7),first:local];}
          public static int Interface(int value,bool first){IValue item=first?new Leaf():new Default();int local=value;return item[second:(local=7),first:local];}
        }
        """;
        Compare(code, ["Class", "Interface"]);
    }

    private static void Compare(string code, string[] names)
    {
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var first in new[] { false, true }) foreach (var name in names)
                    Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value, first])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value, first)));
    }
}
