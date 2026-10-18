using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class InheritedPropertyTests
{
    [Fact]
    public void InheritedPropertiesHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/inherited-properties.json", Support.Read("fixtures/D10/csharp/InheritedProperties.cs"), "Example", "Example.D10");

    [Fact]
    public void BaseAutomaticFieldBackedAndCustomPropertiesKeepTheirStorageAndUpdates()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent{
          private int _custom;
          public int Stored{get;set;}=3;
          public int ReadOnly{get;}=11;
          public int Custom{get=>_custom;set{_custom=value;}}
          public int Field{get=>field;set=>field=value;}=5;
          protected Parent(int value){Custom=value;}
        }
        public sealed class Leaf:Parent{public Leaf(int value):base(value){}}
        public static class Rules{
          public static int Stored(int value)=>new Leaf(value).Stored;
          public static int ReadOnly(int value)=>new Leaf(value).ReadOnly;
          public static int Custom(int value)=>new Leaf(value).Custom;
          public static int Field(int value)=>new Leaf(value).Field;
          public static int StoredWrite(int value){Parent parent=new Leaf(value);var alias=parent;parent.Stored=value;return alias.Stored;}
          public static int CustomWrite(int value){Parent parent=new Leaf(value);parent.Custom=7;return parent.Custom;}
          public static int FieldWrite(int value){Parent parent=new Leaf(value);parent.Field=7;return parent.Field;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "Stored", "ReadOnly", "Custom", "Field", "StoredWrite", "CustomWrite", "FieldWrite" })
                Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value)));
    }

    [Fact]
    public void BaseGetterResultsKeepAliasesAndActualFreshAllocations()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Box{public int Value;public Box(int value){Value=value;}}
        public abstract class Parent{public Box? Stored{get;set;}public Box? Alias=>Stored;public Box Fresh=>new Box(5);protected Parent(Box? child){Stored=child;}}
        public sealed class Leaf:Parent{public Leaf(Box? child):base(child){}}
        public static class Rules{
          public static int Alias(int value){var child=new Box(value);Parent parent=new Leaf(child);var result=parent.Alias;child.Value=7;if(result is null)return 0;result.Value=11;return child.Value;}
          public static int Fresh(int value){var child=new Box(value);Parent parent=new Leaf(child);var result=parent.Fresh;if(result is null)return 0;result.Value=7;return child.Value;}
          public static int Separate(){Parent parent=new Leaf(null);var first=parent.Fresh;var second=parent.Fresh;if(first is null||second is null)return 0;first.Value=7;return second.Value;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "Alias", "Fresh" })
                Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value)));
        Assert.Equal(5, Convert.ToInt32(Support.Call(library, "D10.Rules", "Separate")));
        foreach (var body in new[] { "parent.Stored=new Box(1);return 0;", "var result=parent.Alias;if(result is null)return 0;result.Value=1;return 0;" })
        {
            var rejected = Support.Convert(code.Replace("public static int Alias(int value)", "public static int Unsafe(Parent parent){if(parent is null)return 0;" + body + "}public static int Alias(int value)"), selectors: ["Example.D10"]);
            Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS3002");
        }
    }

    [Fact]
    public void GenericBaseGettersUseConstrainedViewsAndWholeMaybeArguments()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent<T>{public T Value;public T Read=>Value;protected Parent(T value){Value=value;}}
        public sealed class Leaf<T>:Parent<T>{public Leaf(T value):base(value){}}
        public static class Rules{
          private static U Read<P,U>(P parent,U fallback)where P:Parent<U>{if(parent is null)return fallback;return parent.Read;}
          public static int Number(int value)=>Read<Leaf<int>,int>(new Leaf<int>(value),0);
          public static string? Text(string? value)=>Read<Leaf<string?>,string?>(new Leaf<string?>(value),null);
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) Assert.Equal((int)rules.GetMethod("Number")!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", "Number", value)));
        foreach (var value in new string?[] { null, "", "sample" }) Assert.Equal(Support.Maybe(value), Support.Call(library, "D10.Rules", "Text", Support.Maybe(value)));
    }

    [Fact]
    public void GenericBaseSettersBindNumericWitnessesInTheCallerScope()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent<T> where T:System.Numerics.INumberBase<T>{public T Stored;public T Read=>Stored;public long Number{set{Stored=T.CreateSaturating(value);}}}
        public sealed class Leaf<T>:Parent<T> where T:System.Numerics.INumberBase<T>{}
        public static class Rules{
          private static T Convert<T>(long value)where T:System.Numerics.INumberBase<T>{Parent<T> parent=new Leaf<T>();parent.Number=value;return parent.Read;}
          public static int Number(long value)=>Convert<int>(value);
          public static byte Byte(long value)=>Convert<byte>(value);
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { long.MinValue, -1, 0, 1, 255, 256, long.MaxValue }) foreach (var name in new[] { "Number", "Byte" })
                Assert.Equal(Convert.ToInt64(rules.GetMethod(name)!.Invoke(null, [value])!), Convert.ToInt64(Support.Call(library, "D10.Rules", name, Support.Integer(value))));
    }

    [Fact]
    public void HiddenPropertyStorageAndAutomaticSettersWithCustomGettersStayDistinct()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent{public int Value{get;set;}=3;public int Custom{get=>field;set;}public string? Text{get;set;}protected Parent(int value){Custom=value;}}
        public sealed class Leaf:Parent{public new int Value{get;set;}=11;public Leaf(int value):base(value){}}
        public static class Rules{
          public static int Root(int value){Parent parent=new Leaf(value);return parent.Value;}
          public static int Own(int value)=>new Leaf(value).Value;
          public static int WriteRoot(int value){var leaf=new Leaf(value);Parent parent=leaf;parent.Value=7;return leaf.Value;}
          public static int Custom(int value)=>new Leaf(value).Custom;
          public static string? Coalesce(){Parent parent=new Leaf(0);parent.Text??="value";return parent.Text;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "Root", "Own", "WriteRoot", "Custom" })
            {
                var expected = (int)rules.GetMethod(name)!.Invoke(null, [value])!;
                var actual = Convert.ToInt32(Support.Call(library, "D10.Rules", name, value));
                Assert.True(expected == actual, $"{name}({value}): expected {expected}, actual {actual}");
            }
        Assert.Equal(Support.Maybe("value"), Support.Call(library, "D10.Rules", "Coalesce"));
    }

    [Fact]
    public void InheritedPropertyBodiesRejectEffectsCheckedSyntaxAndBorrowedSetters()
    {
        foreach (var (member, diagnostic) in new[] {
            ("public int Value{get{System.Console.WriteLine(0);return 0;}}", "MCS2202"),
            ("public int Value=>checked(0);", "MCS2001"),
            ("public int Value{get=>field;set{System.Console.WriteLine(value);field=value;}}", "MCS2202")})
        {
            var result = Support.Convert("namespace Example.D10;public abstract class Parent{" + member + "}public sealed class Leaf:Parent{}", selectors: ["Example.D10"]);
            Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == diagnostic && item.File == "Case.cs");
        }
        const string borrowed = "namespace Example.D10;public abstract class Parent{public int Value{get=>field;set=>field=value;}}public sealed class Leaf:Parent{}public static class Rules{public static int Unsafe(Parent parent){if(parent is null)return 0;parent.Value=1;return parent.Value;}}";
        var rejected = Support.Convert(borrowed, selectors: ["Example.D10"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS3002");
    }

    [Fact]
    public void OwnedStorageCoalescingKeepsLazyRhsReceiverIdentityAndStructCopies()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent{public int? Value{get;set;}}
        public sealed class Leaf:Parent{}
        public struct Amount{public int? Value;}
        public static class Rules{
          public static int Skip(){Parent parent=new Leaf{Value=1};int side=0;parent.Value??=(side=7);return side;}
          public static int Fill(){Parent parent=new Leaf();int side=0;parent.Value??=(side=7);return side;}
          public static int Receiver(){Parent first=new Leaf();Parent current=first;current.Value??=(current=new Leaf{Value=9}).Value;return first.Value??0;}
          public static int Copy(){var first=default(Amount);var second=first;first.Value??=7;return second.Value??0;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var name in new[] { "Skip", "Fill", "Receiver", "Copy" }) Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, null)!, Convert.ToInt32(Support.Call(library, "D10.Rules", name)));
        var rejected = Support.Convert(code.Replace("public static int Skip()", "public static int Unsafe(Parent parent){if(parent is null)return 0;parent.Value??=7;return parent.Value??0;}public static int Skip()"), selectors: ["Example.D10"]);
        Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS3002");
    }
}
