using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class ConcreteBaseTests
{
    [Fact]
    public void ConcreteBaseDomainsHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/concrete-base.json", Support.Read("fixtures/D10/csharp/ConcreteBase.cs"), "Example", "Example.D10");

    [Fact]
    public void ConcreteBaseInstancesAndDerivedLeavesRetainDifferentDispatchAndStorage()
    {
        const string code = """
        namespace Example.D10;
        public class Parent{public int Value;public Parent(int value){Value=value;}public virtual int Read()=>Value;}
        public sealed class Leaf:Parent{public Leaf(int value):base(value){}public override int Read()=>7;}
        public static class Rules{
          public static int Base(int value,bool first)=>new Parent(value).Read();
          public static int Either(int value,bool first){Parent item=first?new Parent(value):new Leaf(value);return item.Read();}
          public static int Alias(int value,bool first){var parent=new Parent(value);var alias=parent;parent.Value=11;return alias.Read();}
        }
        """;
        Compare(code, ["Base", "Either", "Alias"]);
    }

    [Fact]
    public void ConcreteIntermediateDomainsKeepInheritedFieldsAndBaseBodies()
    {
        const string code = """
        namespace Example.D10;
        public class Root{public int Value=3;public virtual int Read()=>Value;}
        public class Middle:Root{public new int Value=5;public override int Read()=>Value;public int Base()=>base.Read();}
        public sealed class Leaf:Middle{public override int Read()=>7;}
        public static class Rules{
          public static int Root(int value,bool first){Root item=first?new Root():new Middle();return item.Read();}
          public static int Middle(int value,bool first){Middle item=first?new Middle():new Leaf();return item.Read();}
          public static int Base(int value,bool first)=>new Middle().Base();
          public static int Leaf(int value,bool first)=>new Leaf().Base();
        }
        """;
        Compare(code, ["Root", "Middle", "Base", "Leaf"]);
    }

    [Fact]
    public void ConcreteBaseViewsCastsAndInterfaceOverridesKeepTheActualReceiver()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue{int Read();}
        public class Parent:IValue{public int Value;public Parent(int value){Value=value;}public virtual int Read()=>Value;}
        public sealed class Leaf:Parent{public Leaf(int value):base(value){}public override int Read()=>7;}
        public static class Rules{
          public static int Interface(int value,bool first){IValue item=first?new Parent(value):new Leaf(value);return item.Read();}
          public static int Upcast(int value,bool first){Parent parent=first?new Parent(value):new Leaf(value);IValue item=parent;return item.Read();}
          public static int Downcast(int value,bool first){Parent parent=first?new Parent(value):new Leaf(value);if(parent is Leaf)return ((Leaf)parent).Value;return parent.Value;}
          public static int Optional(int value,bool first){Parent parent=first?new Parent(value):new Leaf(value);var leaf=parent as Leaf;if(leaf is null)return 0;return leaf.Read();}
          public static int ProvedBase(int value,bool first){IValue item=first?new Parent(value):new Leaf(value);if(item is Parent)return ((Parent)item).Read();return 0;}
        }
        """;
        Compare(code, ["Interface", "Upcast", "Downcast", "Optional", "ProvedBase"]);
    }

    [Fact]
    public void ConcreteBasePropertiesAndConstructorsUseTheSameOwnedLeaf()
    {
        const string code = """
        namespace Example.D10;
        public class Parent{public int Stored;public int Seen;public virtual int Value{get=>Stored;set{Stored=value;}}public Parent(int value){Value=value;Seen=Value;}}
        public sealed class Leaf:Parent{public int Other;public Leaf(int value):base(value){}public override int Value{get=>Other;set{Other=7;}}}
        public static class Rules{
          public static int Value(int value,bool first){Parent item=first?new Parent(value):new Leaf(value);return item.Value;}
          public static int Seen(int value,bool first){Parent item=first?new Parent(value):new Leaf(value);return item.Seen;}
          public static int Alias(int value,bool first){Parent item=first?new Parent(value):new Leaf(value);var alias=item;item.Value=11;return alias.Value;}
        }
        """;
        Compare(code, ["Value", "Seen", "Alias"]);
    }

    [Fact]
    public void GenericConcreteBaseDomainsKeepMaybeArgumentsAndReturnedChildren()
    {
        const string code = """
        namespace Example.D10;
        public class Parent<T>{public T Value;public Parent(T value){Value=value;}public virtual T Read()=>Value;}
        public sealed class Leaf<T>:Parent<T>{public Leaf(T value):base(value){}}
        public sealed class Box{public int Value;public Box(int value){Value=value;}}
        public static class Rules{
          public static string? Text(string? value){Parent<string?> item=new Parent<string?>(value);return item.Read();}
          public static int Child(int value,bool first){var child=new Box(value);Parent<Box> parent=first?new Parent<Box>(child):new Leaf<Box>(child);var alias=parent.Read();if(alias is null)return 0;alias.Value=7;return child.Value;}
        }
        """;
        Compare(code, ["Child"]);
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        foreach (var value in new string?[] { null, "", "test" }) Assert.Equal(Support.Maybe(value), Support.Call(library, "D10.Rules", "Text", Support.Maybe(value)));
    }

    [Fact]
    public void ConcreteBaseClosureAndBorrowedMutationRemainFailures()
    {
        const string prefix = "namespace Example.D10{public class Parent{public int Value;public virtual int Read()=>Value;}public static class Rules{public static int Unsafe(Parent item){if(item is null)return 0;item.Value=7;return item.Read();}}}";
        var result = Support.Convert(prefix + "namespace Example.D10{public sealed class Leaf:Parent{}}", selectors: ["Example.D10"]);
        Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == "MCS3002");
        var closure = Support.Convert(prefix + "namespace Other{public sealed class Leaf:Example.D10.Parent{}}", selectors: ["Example.D10"]);
        Assert.Null(closure.Distribution); Assert.Contains(closure.Diagnostics, item => item.Code == "MCS3111");
    }

    [Fact]
    public void NonvirtualBaseMembersKeepPrivateOwnersAndHiddenStorage()
    {
        const string code = """
        namespace Example.D10;
        public class Parent{public int Value;private int ReadOwn()=>Value;public int Read()=>ReadOwn();public Parent(int value){Value=value;}}
        public sealed class Leaf:Parent{public new int Value=7;public Leaf(int value):base(value){}}
        public static class Rules{
          public static int Read(int value,bool first){Parent item=first?new Parent(value):new Leaf(value);return item.Read();}
          public static int Hidden(int value,bool first){var leaf=new Leaf(value);Parent item=leaf;item.Value=11;return leaf.Value;}
        }
        """;
        Compare(code, ["Read", "Hidden"]);
    }

    [Fact]
    public void ConcretePrimaryBaseAllocationAndImplicitOptionalArgumentsStayExact()
    {
        const string code = """
        namespace Example.D10;
        public class Parent(int value=3){public int Stored=value;public virtual int Read()=>Stored;}
        public sealed class Leaf:Parent{}
        public static class Rules{
          public static int Primary(int value,bool first)=>new Parent(value).Read();
          public static int Optional(int value,bool first){Parent item=first?new Parent():new Leaf();return item.Read();}
        }
        """;
        Compare(code, ["Primary", "Optional"]);
    }

    [Fact]
    public void RecursiveConcreteDomainsPreserveAcyclicAliasesAndRejectCyclesOrBorrowedChildren()
    {
        const string code = """
        namespace Example.D10;
        public class Parent{public int Value;public Parent? Next;public virtual int Read()=>Value;}
        public sealed class Leaf:Parent{}
        public static class Rules{
          public static int Alias(int value,bool first){var child=new Parent{Value=value};Parent item=first?new Parent():new Leaf();item.Next=child;var alias=item.Next;if(alias is null)return 0;alias.Value=7;return child.Read();}
        }
        """;
        Compare(code, ["Alias"]);
        foreach (var (body, diagnostic) in new[] { ("var item=new Parent();item.Next=item;return 0;", "MCS3007"), ("Parent item=new Parent{Next=input};var child=item.Next;if(child is null)return 0;child.Value=7;return 0;", "MCS3002") })
        {
            var rejected = Support.Convert(code.Replace("public static int Alias(int value,bool first)", "public static int Unsafe(Parent? input){" + body + "}public static int Alias(int value,bool first)"), selectors: ["Example.D10"]);
            Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == diagnostic);
        }
    }

    private static void Compare(string code, string[] names)
    {
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var first in new[] { false, true }) foreach (var name in names)
                    Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value, first])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value, first)));
    }
}
