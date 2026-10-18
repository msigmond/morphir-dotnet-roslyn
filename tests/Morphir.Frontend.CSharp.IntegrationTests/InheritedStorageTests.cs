using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class InheritedStorageTests
{
    [Fact]
    public void HiddenStorageHasCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/hidden-storage.json", Support.Read("fixtures/D10/csharp/HiddenStorage.cs"), "Example", "Example.D10");

    [Fact]
    public void HiddenFieldsKeepDistinctSourceOwnerSlotsAndBaseViewUpdates()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent{public int Value;protected Parent(int value){Value=value;}public int Read()=>Value;}
        public sealed class Leaf:Parent{public new int Value;public Leaf(int root,int own):base(root){Value=own;}public int Own()=>Value;}
        public static class Rules{
          public static int Root(int value)=>new Leaf(value,11).Read();
          public static int Own(int value)=>new Leaf(value,11).Own();
          public static int RootWrite(int value){var leaf=new Leaf(value,11);Parent parent=leaf;parent.Value=7;return leaf.Read();}
          public static int OwnAfterRootWrite(int value){var leaf=new Leaf(value,11);Parent parent=leaf;parent.Value=7;return leaf.Own();}
          public static int RootAfterOwnWrite(int value){var leaf=new Leaf(value,11);leaf.Value=7;return leaf.Read();}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "Root", "Own", "RootWrite", "OwnAfterRootWrite", "RootAfterOwnWrite" })
                Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value)));
    }

    [Fact]
    public void MultipleHiddenInitializersAndPrimaryCapturesKeepTheirActualOwners()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent{public int Value=3;public int Root()=>Value;}
        public abstract class Middle:Parent{public new int Value=11;public int Mid()=>Value;}
        public sealed class Leaf:Middle{public new int Value=17;public int Own()=>Value;}
        public abstract class CapturedParent(int value){public int Read()=>value;}
        public sealed class CapturedLeaf(int value):CapturedParent(3){public int Own()=>value;}
        public static class Rules{
          public static int Root()=>new Leaf().Root();
          public static int Middle()=>new Leaf().Mid();
          public static int Leaf()=>new Leaf().Own();
          public static int CaptureRoot(int value)=>new CapturedLeaf(value).Read();
          public static int CaptureLeaf(int value)=>new CapturedLeaf(value).Own();
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var name in new[] { "Root", "Middle", "Leaf" }) Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, null)!, Convert.ToInt32(Support.Call(library, "D10.Rules", name)));
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "CaptureRoot", "CaptureLeaf" })
                Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value)));
    }

    [Fact]
    public void HiddenGenericFieldsKeepMaybeArgumentsChildAliasesAndBorrowedReachability()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Box{public int Value;public Box(int value){Value=value;}}
        public abstract class Parent<T>{public T Value;protected Parent(T value){Value=value;}public T Read()=>Value;}
        public sealed class Leaf<T>:Parent<T>{public new T Value;public Leaf(T root,T own):base(root){Value=own;}public T Own()=>Value;}
        public static class Rules{
          public static string? Root(string? value)=>new Leaf<string?>(value,"own").Read();
          public static string? Own(string? value)=>new Leaf<string?>(value,"own").Own();
          public static int RootAlias(int value){var first=new Box(value);var second=new Box(2);var leaf=new Leaf<Box>(first,second);var result=leaf.Read();first.Value=7;second.Value=11;if(result is null)return 0;return result.Value;}
          public static int OwnAlias(int value){var first=new Box(value);var second=new Box(2);var leaf=new Leaf<Box>(first,second);var result=leaf.Own();first.Value=7;second.Value=11;if(result is null)return 0;return result.Value;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new string?[] { null, "", "sample" })
        {
            Assert.Equal(Support.Maybe(value), Support.Call(library, "D10.Rules", "Root", Support.Maybe(value)));
            Assert.Equal(Support.Maybe("own"), Support.Call(library, "D10.Rules", "Own", Support.Maybe(value)));
        }
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "RootAlias", "OwnAlias" })
                Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value)));
        var rejected = Support.Convert(code.Replace("public static string? Root(string? value)", "public static int Unsafe(Box first){var leaf=new Leaf<Box>(first,new Box(2));var result=leaf.Read();if(result is null)return 0;result.Value=1;return result.Value;}public static string? Root(string? value)"), selectors: ["Example.D10"]);
        Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS3002");
    }

    [Fact]
    public void HiddenFieldProofsDoNotConflateDistinctSlotsOrNormalizeSameOwnerCollisions()
    {
        const string prefix = "namespace Example.D10;public abstract class Parent{public int Value;protected Parent(int value){Value=value;}}public sealed class Leaf:Parent{public new int Value;public Leaf(int root,int own):base(root){Value=own;}}public static class Rules{";
        var rejected = Support.Convert(prefix + "public static int Unsafe(int root,int own){var leaf=new Leaf(root,own);Parent parent=leaf;if(parent.Value!=0)return 100/leaf.Value;return 0;}}", selectors: ["Example.D10"]);
        Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS2601");
        var code = prefix + "public static int Safe(int root,int own){var leaf=new Leaf(root,own);Parent parent=leaf;if(parent.Value!=0&&leaf.Value!=0)return 100/leaf.Value;return 0;}}";
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var root in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var own in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) Assert.Equal((int)rules.GetMethod("Safe")!.Invoke(null, [root, own])!, Convert.ToInt32(Support.Call(library, "D10.Rules", "Safe", root, own)));
        var collision = Support.Convert("namespace Example.D10;public sealed class Cell{public int Value;public int value;}", selectors: ["Example.D10"]);
        Assert.Null(collision.Distribution); Assert.True(collision.Diagnostics.Count(item => item.Code == "MCS1004") >= 2);
    }

    [Fact]
    public void RepeatedOwnedFieldReadsRetainVersionedNonzeroFactsAndWritesInvalidateThem()
    {
        const string prefix = "namespace Example.D10;public sealed class Cell{public int Value;public Cell(int value){Value=value;}}public static class Rules{";
        var library = Support.Success(Support.Convert(prefix + "public static int Safe(int value){var cell=new Cell(value);if(cell.Value==0)return 0;return 100/cell.Value;}}", selectors: ["Example.D10"]));
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) Assert.Equal(value == 0 ? 0 : 100 / value, Convert.ToInt32(Support.Call(library, "D10.Rules", "Safe", value)));
        var rejected = Support.Convert(prefix + "public static int Unsafe(int value){var cell=new Cell(value);if(cell.Value==0)return 0;cell.Value=0;return 100/cell.Value;}}", selectors: ["Example.D10"]);
        Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS2601");
    }
}
