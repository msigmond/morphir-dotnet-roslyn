using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class ImplicitBaseTests
{
    [Fact]
    public void ImplicitOptionalBaseConstructionHasCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/implicit-base.json", Support.Read("fixtures/D10/csharp/ImplicitBase.cs"), "Example", "Example.D10");

    [Fact]
    public void ImplicitBaseCallsUseTheAccessibleCompilerSelectedOptionalOverload()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent{public int Value;private Parent(){Value=11;}protected Parent(int value=3){Value=value;}public int Read()=>Value;}
        public sealed class Leaf:Parent{}
        public sealed class PrimaryLeaf():Parent;
        public static class Rules{public static int Plain()=>new Leaf().Read();public static int Primary()=>new PrimaryLeaf().Read();}
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var name in new[] { "Plain", "Primary" }) Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, null)!, Convert.ToInt32(Support.Call(library, "D10.Rules", name)));
        Assert.Equal(3, Convert.ToInt32(Support.Call(library, "D10.Rules", "Plain")));
    }

    [Fact]
    public void OptionalAndExpandedParamsBaseOverloadsMatchNativeSelectionAndEmptyArrays()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Choice{public int Value;protected Choice(int value=5){Value=value;}protected Choice(params int[] values){Value=values?.Length??0;}}
        public sealed class Chosen:Choice{}
        public abstract class Params{public int Value;protected Params(params int[] values){Value=values?.Length??0;}}
        public sealed class Empty:Params{}
        public static class Rules{public static int Choice()=>new Chosen().Value;public static int Empty()=>new Empty().Value;}
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var name in new[] { "Choice", "Empty" }) Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, null)!, Convert.ToInt32(Support.Call(library, "D10.Rules", name)));
    }

    [Fact]
    public void ImplicitOptionalArgumentsKeepDecimalScaleEnumAndMaybeRepresentations()
    {
        const string code = """
        namespace Example.D10;
        public enum Mode{One=1}
        public abstract class Parent{public decimal Money;public Mode Mode;public string? Text;protected Parent(decimal money=1.2300m,Mode mode=Mode.One,string? text="seed"){Money=money;Mode=mode;Text=text;}}
        public sealed class Leaf:Parent{}
        public static class Rules{public static int Scale()=>new Leaf().Money.Scale;public static int Mode()=>(int)new Leaf().Mode;public static string? Text()=>new Leaf().Text;}
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var name in new[] { "Scale", "Mode" }) Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, null)!, Convert.ToInt32(Support.Call(library, "D10.Rules", name)));
        Assert.Equal(Support.Maybe("seed"), Support.Call(library, "D10.Rules", "Text"));
    }

    [Fact]
    public void OmittedGenericArgumentsPropagateDefaultsAndAbsenceWithoutStorageOfThatType()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent<T>{public int Flag;protected Parent(T value=default!){Flag=value is null?1:2;}}
        public sealed class Leaf<T>:Parent<T>{}
        public static class Rules{public static int Number()=>new Leaf<int>().Flag;public static int Text()=>new Leaf<string?>().Flag;public static int Nullable()=>new Leaf<int?>().Flag;}
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var name in new[] { "Number", "Text", "Nullable" }) Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, null)!, Convert.ToInt32(Support.Call(library, "D10.Rules", name)));
    }

    [Fact]
    public void PartialPrimaryDeclarationsResolveTheBaseDeclaredInAnotherPart()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent{public int Value;protected Parent(int value=3){Value=value;}}
        public sealed partial class Leaf(){}
        public sealed partial class Leaf:Parent{}
        public static class Rules{public static int Example()=>new Leaf().Value;}
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        Assert.Equal((int)Support.Emit(code).GetType("Example.D10.Rules")!.GetMethod("Example")!.Invoke(null, null)!, Convert.ToInt32(Support.Call(library, "D10.Rules", "Example")));
    }
}
