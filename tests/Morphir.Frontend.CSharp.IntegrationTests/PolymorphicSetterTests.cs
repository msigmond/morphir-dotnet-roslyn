using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class PolymorphicSetterTests
{
    [Fact]
    public void SetterDispatchHasCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/polymorphic-setters.json", Support.Read("fixtures/D10/csharp/PolymorphicSetters.cs"), "Example", "Example.D10");

    [Fact]
    public void VirtualAndAbstractSettersUpdateTheRuntimeOwnerAndAliases()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent{public abstract int Value{get;set;}}
        public sealed class Leaf:Parent{public override int Value{get;set;}}
        public sealed class Other:Parent{public int Stored;public override int Value{get=>Stored;set{Stored=value;}}}
        public static class Rules{public static int Read(int value,bool first){Parent item=first?new Leaf():new Other();var alias=item;item.Value=value;return alias.Value;}}
        """;
        Compare(code, ["Read"]);
    }

    [Fact]
    public void DirectBaseSettersAndVirtualSelfCallsKeepDistinctStorage()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent{public virtual int Value{get;set;}=3;public virtual int Other=>Value;public int Seen;public int Observed{set{Value=value;Seen=Other;}}}
        public sealed class Leaf:Parent{public int Own;public override int Value{get=>Own;set{base.Value=value;Own=7;}}public int Base=>base.Value;}
        public static class Rules{
          public static int Own(int value,bool first){Parent item=new Leaf();item.Observed=value;return item.Value;}
          public static int Base(int value,bool first){var leaf=new Leaf();Parent item=leaf;item.Value=value;return leaf.Base;}
          public static int Seen(int value,bool first){Parent item=new Leaf();item.Observed=value;return item.Seen;}
        }
        """;
        Compare(code, ["Own", "Base", "Seen"]);
    }

    [Fact]
    public void InterfaceSettersUseAutomaticExplicitAndDefaultBodies()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue{int Stored{get;set;}int Value{get=>Stored;set{Stored=value;}}}
        public sealed class Default:IValue{public int Stored{get;set;}}
        public sealed class Explicit:IValue{public int Stored{get;set;}int IValue.Value{get=>Stored;set{Stored=7;}}}
        public static class Rules{public static int Read(int value,bool first){IValue item=first?new Default():new Explicit();var alias=item;item.Value=value;return alias.Stored;}}
        """;
        Compare(code, ["Read"]);
    }

    [Fact]
    public void ReabstractedAndRestoredSetterTargetsRemainComplete()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue{int Stored{get;set;}int Value{get=>Stored;set{Stored=value;}}}
        public interface IRequired:IValue{abstract int IValue.Value{get;set;}}
        public interface IRestored:IRequired{int IValue.Value{get=>Stored;set{Stored=value;}}}
        public sealed class Restored:IRestored{public int Stored{get;set;}}
        public sealed class Explicit:IRequired{public int Stored{get;set;}int IValue.Value{get=>Stored;set{Stored=7;}}}
        public static class Rules{public static int Read(int value,bool first){IValue item=first?new Restored():new Explicit();item.Value=value;return item.Value;}}
        """;
        Compare(code, ["Read"]);
    }

    [Fact]
    public void VirtualIndexersEvaluateReceiverIndicesAndValueInSourceOrder()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue{int this[int first,int second]{get;set;}int Stored{get;}}
        public abstract class Parent:IValue{public int Field;public int Stored=>Field;public abstract int this[int first,int second]{get;set;}}
        public sealed class Leaf:Parent{public override int this[int first,int second]{get=>Field;set{Field=first;}}}
        public static class Rules{
          public static int Index(int value,bool first){IValue item=new Leaf();int local=value;item[second:(local=7),first:local]=(local=11);return item.Stored;}
          public static int Receiver(int value,bool first){IValue original=new Leaf();IValue current=original;current[first:0,second:0]=(current=new Leaf()).Stored;return original.Stored;}
        }
        """;
        Compare(code, ["Index", "Receiver"]);
    }

    [Fact]
    public void GenericAutomaticSettersKeepNullablePayloadAndChildAliases()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue<T>{T Value{get;set;}}
        public sealed class Holder<T>:IValue<T>{public T Value{get;set;}public Holder(T value){Value=value;}}
        public sealed class Box{public int Value;public Box(int value){Value=value;}}
        public static class Rules{
          public static int Alias(int value,bool first){var child=new Box(value);IValue<Box> item=new Holder<Box>(new Box(0));item.Value=child;var alias=item.Value;if(alias is null)return 0;alias.Value=7;return child.Value;}
          public static string? Text(string? value){IValue<string?> item=new Holder<string?>(null);item.Value=value;return item.Value;}
        }
        """;
        Compare(code, ["Alias"]);
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        foreach (var value in new string?[] { null, "", "test" }) Assert.Equal(Support.Maybe(value), Support.Call(library, "D10.Rules", "Text", Support.Maybe(value)));
    }

    [Fact]
    public void BorrowedAndEffectfulSetterTargetsRejectWithoutArtifacts()
    {
        foreach (var member in new[] { "int Value{get;set;}", "int Value{get=>0;set{System.Console.WriteLine(value);}}" })
        {
            var code = "namespace Example.D10;public interface IValue{" + member + "}public sealed class Item:IValue{public int Value{get;set;}}public static class Rules{public static int Unsafe(IValue item){if(item is null)return 0;item.Value=7;return item.Value;}}";
            var result = Support.Convert(code, selectors: ["Example.D10"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == (member.Contains("Console") ? "MCS2202" : "MCS3002"));
        }
    }

    [Fact]
    public void NullablePropertyCoalescingKeepsLazyRhsAndTheGetterAssignmentResult()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue{int? Value{get;set;}}
        public abstract class Parent:IValue{public int? Stored;public abstract int? Value{get;set;}}
        public sealed class Leaf:Parent{public override int? Value{get=>Stored;set{Stored=7;}}}
        public struct Amount{public int? Stored;public int? Value{get=>Stored;set{Stored=value;}}}
        public static class Rules{
          public static int Result(int value,bool first){IValue item=new Leaf();return item.Value??=11;}
          public static int Stored(int value,bool first){IValue item=new Leaf();item.Value??=11;return item.Value??0;}
          public static int Skip(int value,bool first){IValue item=new Leaf{Stored=value};int side=0;item.Value??=(side=11);return side;}
          public static int Present(int value,bool first){IValue item=new Leaf{Stored=value};return item.Value??=11;}
          public static int Receiver(int value,bool first){IValue original=new Leaf();IValue current=original;current.Value??=(current=new Leaf{Stored=11}).Value;return original.Value??0;}
          public static int Struct(int value,bool first){var original=default(Amount);var copy=original;original.Value??=11;return copy.Stored??0;}
        }
        """;
        Compare(code, ["Result", "Stored", "Skip", "Present", "Receiver", "Struct"]);
        var rejected = Support.Convert(code.Replace("public static int Result(int value,bool first)", "public static int Unsafe(IValue item){if(item is null)return 0;item.Value??=11;return 0;}public static int Result(int value,bool first)"), selectors: ["Example.D10"]);
        Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS3002");
    }

    [Fact]
    public void CoalescingIndexerCapturesIndicesAndReceiverOnce()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue{int? this[int first,int second]{get;set;}int? Stored{get;}}
        public sealed class Item:IValue{public int? Field;public int? Stored=>Field;public int? this[int first,int second]{get=>Field;set{Field=first;}}}
        public static class Rules{
          public static int Index(int value,bool first){IValue item=new Item();int local=value;item[second:(local=7),first:local]??=(local=11);return item.Stored??0;}
          public static int Present(int value,bool first){IValue item=new Item{Field=3};int local=value;item[second:(local=7),first:local]??=(local=11);return local;}
        }
        """;
        Compare(code, ["Index", "Present"]);
    }

    [Fact]
    public void GenericSetterBodiesBindDefaultsInTheActualCallerRepresentation()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue<T>{T Value{get;set;}}
        public sealed class Holder<T>:IValue<T>{public T Stored;public T Value{get=>Stored;set{Stored=default!;}}public Holder(T value){Stored=value;}}
        public static class Rules{
          private static T Reset<T>(T value){IValue<T> item=new Holder<T>(value);item.Value=value;return item.Value;}
          public static int Number(int value,bool first)=>Reset(value);
          public static string? Text(string? value)=>Reset(value);
        }
        """;
        Compare(code, ["Number"]);
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        foreach (var value in new string?[] { null, "", "test" }) Assert.Equal(Support.Maybe(null), Support.Call(library, "D10.Rules", "Text", Support.Maybe(value)));
    }

    [Fact]
    public void GenericSetterBodiesForwardNumericConversionWitnesses()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue<T>where T:System.Numerics.INumberBase<T>{T Value{get;}long Number{set;}}
        public sealed class Holder<T>:IValue<T>where T:System.Numerics.INumberBase<T>{public T Stored;public T Value=>Stored;public long Number{set{Stored=T.CreateSaturating(value);}}}
        public static class Rules{
          private static T Convert<T>(long value)where T:System.Numerics.INumberBase<T>{IValue<T> item=new Holder<T>();item.Number=value;return item.Value;}
          public static int Number(long value)=>Convert<int>(value);
          public static byte Byte(long value)=>Convert<byte>(value);
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { long.MinValue, -1, 0, 1, 255, 256, long.MaxValue }) foreach (var name in new[] { "Number", "Byte" }) Assert.Equal(Convert.ToInt64(rules.GetMethod(name)!.Invoke(null, [value])!), Convert.ToInt64(Support.Call(library, "D10.Rules", name, Support.Integer(value))));
    }

    private static void Compare(string code, string[] names)
    {
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var first in new[] { false, true }) foreach (var name in names)
                    Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value, first])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value, first)));
    }
}
