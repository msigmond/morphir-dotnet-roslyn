using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class InterfaceBoxTests
{
    [Fact]
    public void BoxedInterfaceUpdatesHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/interface-boxes.json", Support.Read("fixtures/D10/csharp/InterfaceBoxes.cs"), "Example", "Example.D10");

    [Fact]
    public void BoxAliasesShareUpdatesWhileSeparateBoxesAndUnboxingRemainValueCopies()
    {
        const string code = """
        namespace Example.D10;
        public interface IRead{int Value{get;}}
        public interface IValue:IRead{new int Value{get;set;}}
        public struct Counter:IValue{public int Value{get;set;}}
        public static class Rules{
          private static int Input(Counter source){IValue box=source;box.Value=7;return source.Value;}
          public static int ThroughInput(int value)=>Input(new Counter{Value=value});
          public static int Source(int value){var source=new Counter{Value=value};IValue box=source;box.Value=7;return source.Value;}
          public static int Alias(int value){IValue box=new Counter{Value=value};var alias=box;box.Value=7;return alias.Value;}
          public static int Separate(int value){var source=new Counter{Value=value};IValue first=source;IValue second=source;first.Value=7;return second.Value;}
          public static int Unbox(int value){IValue box=new Counter{Value=value};var copy=(Counter)box;box.Value=7;return copy.Value;}
          public static int UnboxWrite(int value){IValue box=new Counter{Value=value};var copy=(Counter)box;copy.Value=11;return box.Value;}
          public static int Rebox(int value){IValue first=new Counter{Value=value};IValue second=(Counter)first;first.Value=7;return second.Value;}
          public static int View(int value){IValue box=new Counter{Value=value};IRead view=box;box.Value=7;return view.Value;}
          public static int Pattern(int value){IValue box=new Counter{Value=value};if(box is Counter copy){box.Value=7;return copy.Value;}return 0;}
        }
        """;
        Compare(code, ["Source", "Alias", "Separate", "Unbox", "UnboxWrite", "Rebox", "View", "Pattern", "ThroughInput"]);
    }

    [Fact]
    public void NullableBoxingAndReturnedBoxesRetainPresenceAndFreshAllocations()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue{int Value{get;set;}}
        public struct Counter:IValue{public int Value{get;set;}}
        public static class Rules{
          private static IValue Make(int value)=>new Counter{Value=value};
          public static int Fresh(int value){var first=Make(value);var second=Make(value);if(first is null||second is null)return 0;first.Value=7;return second.Value;}
          public static int Present(int value){Counter? source=new Counter{Value=value};IValue? box=source;if(box is null)return 0;box.Value=7;return box.Value;}
          public static int Absent(int value){Counter? source=null;IValue? box=source;if(box is null)return 0;return box.Value;}
        }
        """;
        Compare(code, ["Fresh", "Present", "Absent"]);
    }

    [Fact]
    public void BoxedGenericStorageKeepsWholeMaybeArgumentsAndChildAliases()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue<T>{T Value{get;set;}}
        public struct Holder<T>:IValue<T>{public T Value{get;set;}}
        public sealed class Box{public int Value;public Box(int value){Value=value;}}
        public static class Rules{
          public static int Child(int value){var child=new Box(value);IValue<Box> box=new Holder<Box>{Value=new Box(0)};box.Value=child;var result=box.Value;if(result is null)return 0;result.Value=7;return child.Value;}
          public static int CopyChild(int value){var child=new Box(value);IValue<Box> box=new Holder<Box>{Value=child};var copy=(Holder<Box>)box;box.Value=new Box(11);var original=copy.Value;if(original is null)return 0;original.Value=7;return child.Value;}
          public static string? Text(string? value){IValue<string?> box=new Holder<string?>{Value=null};box.Value=value;return box.Value;}
        }
        """;
        Compare(code, ["Child", "CopyChild"]);
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        foreach (var value in new string?[] { null, "", "test" }) Assert.Equal(Support.Maybe(value), Support.Call(library, "D10.Rules", "Text", Support.Maybe(value)));
    }

    [Fact]
    public void BoxedCoalescingUsesTheOwnedBoxAndLazyRhs()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue{int? Value{get;set;}}
        public struct Counter:IValue{public int? Value{get;set;}}
        public static class Rules{
          public static int Skip(int value){IValue box=new Counter{Value=value};int side=0;box.Value??=(side=7);return side;}
          public static int Fill(int value){IValue box=new Counter();box.Value??=7;return box.Value??0;}
        }
        """;
        Compare(code, ["Skip", "Fill"]);
    }

    [Fact]
    public void BorrowedBoxesBorrowedChildrenAndCyclesRemainRejected()
    {
        const string declarations = "namespace Example.D10;public interface IValue{IValue? Value{get;set;}}public struct Holder:IValue{public IValue? Value{get;set;}}public static class Rules{";
        foreach (var (body, diagnostic) in new[] { ("public static int Unsafe(IValue input){if(input is null)return 0;input.Value=null;return 0;}", "MCS3002"), ("public static int Unsafe(IValue input){IValue box=new Holder{Value=input};var child=box.Value;if(child is null)return 0;child.Value=null;return 0;}", "MCS3002"), ("public static int Unsafe(){IValue box=new Holder();box.Value=box;return 0;}", "MCS3007") })
        {
            var result = Support.Convert(declarations + body + "}", selectors: ["Example.D10"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == diagnostic);
        }
    }

    private static void Compare(string code, string[] names)
    {
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in names)
                Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value)));
    }
}
