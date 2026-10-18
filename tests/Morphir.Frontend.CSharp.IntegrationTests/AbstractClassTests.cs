using Morphir.Tests;
using Morphir.IR.Classic;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class AbstractClassTests
{
    [Fact]
    public void AbstractClassesHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/abstract-classes.json", Support.Read("fixtures/D10/csharp/AbstractRules.cs"), "Example", "Example.D10");

    [Fact]
    public void DirectBaseBodiesHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/base-bodies.json", Support.Read("fixtures/D10/csharp/BaseBodies.cs"), "Example", "Example.D10");

    [Fact]
    public void InheritedFieldsHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/inherited-fields.json", Support.Read("fixtures/D10/csharp/InheritedFields.cs"), "Example", "Example.D10");

    [Fact]
    public void BaseConstructionHasCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/base-construction.json", Support.Read("fixtures/D10/csharp/BaseConstruction.cs"), "Example", "Example.D10");

    [Fact]
    public void PrimaryBaseConstructionHasCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/primary-base.json", Support.Read("fixtures/D10/csharp/PrimaryBase.cs"), "Example", "Example.D10");

    [Fact]
    public void ClosedFieldlessAbstractBasesDispatchOverridesDefaultBodiesAndInheritedCalls()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Rule {
          public abstract bool Allows(int age);
          public bool Twice(int age)=>Allows(age)&&Allows(age);
          public virtual bool Extra(int age)=>age>=0;
        }
        public sealed class MinimumAge:Rule {
          public int Minimum;
          public MinimumAge(int minimum){Minimum=minimum;}
          public override bool Allows(int age)=>age>=Minimum;
          public bool Own(int age)=>Twice(age);
        }
        public sealed class MaximumAge:Rule {
          public int Maximum;
          public MaximumAge(int maximum){Maximum=maximum;}
          public override bool Allows(int age)=>age<=Maximum;
          public override bool Extra(int age)=>age<Maximum;
        }
        public static class Rules {
          public static bool Test(Rule? rule,int age){if(rule is null)return false;return rule.Twice(age);}
          public static bool Example(bool maximum,int bound,int age){Rule rule=maximum?new MaximumAge(bound):new MinimumAge(bound);return Test(rule,age);}
          public static bool Extra(bool maximum,int bound,int age){Rule rule=maximum?new MaximumAge(bound):new MinimumAge(bound);return rule.Extra(age);}
          public static bool Own(int bound,int age)=>new MinimumAge(bound).Own(age);
          public static int Pattern(Rule? rule){if(rule is MinimumAge minimum)return minimum.Minimum;return 0;}
          public static int Cast(int bound){Rule rule=new MinimumAge(bound);if(rule is MinimumAge)return ((MinimumAge)rule).Minimum;return 0;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var maximum in new[] { false, true }) foreach (var bound in new[] { int.MinValue, -1, 0, 18, int.MaxValue }) foreach (var age in new[] { int.MinValue, -1, 0, 18, 65, int.MaxValue }) foreach (var name in new[] { "Example", "Extra" }) Assert.Equal((bool)rules.GetMethod(name)!.Invoke(null, [maximum, bound, age])!, Support.Call(library, "D10.Rules", name, maximum, bound, age));
        foreach (var bound in new[] { int.MinValue, 0, 18, int.MaxValue })
        {
            Assert.Equal(bound, Convert.ToInt32(Support.Call(library, "D10.Rules", "Cast", bound)));
            foreach (var age in new[] { int.MinValue, 0, 18, int.MaxValue }) Assert.Equal((bool)rules.GetMethod("Own")!.Invoke(null, [bound, age])!, Support.Call(library, "D10.Rules", "Own", bound, age));
        }
        Assert.Equal(false, Support.Call(library, "D10.Rules", "Test", Support.Maybe(null), 18));
    }

    [Fact]
    public void AbstractClassClosureStillRejectsUnmappedBaseStorageConstructionAndOpenRuntimeTypes()
    {
        foreach (var (source, diagnostic) in new[] {
          ("namespace Example.D10;public abstract class Base{public static int State;public abstract int Read();}public sealed class Leaf:Base{public override int Read()=>State;}", "MCS3110"),
          ("namespace Example.D10;public abstract class Base{public virtual int Value{get;set;}=System.Console.Read();public abstract int Read();}public sealed class Leaf:Base{public override int Read()=>Value;}", "MCS2202"),
          ("namespace Example.D10;public abstract class Base{public abstract int Read();}public class Leaf:Base{public override int Read()=>System.Console.Read();}", "MCS2202"),
          ("namespace Example.D10{public abstract class Base{public abstract int Read();}}namespace Outside{public sealed class Leaf:Example.D10.Base{public override int Read()=>0;}}", "MCS3111"),
          ("namespace Example.D10;public abstract class Base{public abstract int Read();}", "MCS3111")})
        {
            var result = Support.Convert(source, selectors: ["Example.D10"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == diagnostic);
        }
    }

    [Fact]
    public void ClassAndInterfaceViewsResolveTheSameInheritedOverrideWithDistinctConstructors()
    {
        const string code = """
        namespace Example.D10;
        public interface IRule{bool Allows(int age);}
        public abstract class Rule:IRule{public abstract bool Allows(int age);}
        public sealed class MinimumAge:Rule{public override bool Allows(int age)=>age>=18;}
        public static class Rules{
          public static bool AsInterface(IRule? rule,int age){if(rule is null)return false;return rule.Allows(age);}
          public static bool Test(int age){Rule rule=new MinimumAge();return AsInterface(rule,age);}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var age in new[] { int.MinValue, -1, 0, 17, 18, 19, int.MaxValue }) Assert.Equal((bool)rules.GetMethod("Test")!.Invoke(null, [age])!, Support.Call(library, "D10.Rules", "Test", age));
        var constructors = library.Definition.Modules.Single(module => module.Key.Key == Morphir.IR.Classic.Path.From("D10").Key).Value.Value.Types.SelectMany(type => type.Value.Value.Value is CustomDefinition<Unit> custom ? custom.Constructors.Value.Select(constructor => constructor.Key.Key) : []);
        Assert.Equal(constructors.Count(), constructors.Distinct().Count());
    }

    [Fact]
    public void ExplicitBaseBodiesRetainVirtualSelfCallsAndReturnedReferenceAliases()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Box{public int Value;public Box(int value){Value=value;}}
        public abstract class Parent {
          public abstract int Compute();
          public virtual int Read()=>Compute();
          public virtual Box Select(Box value)=>value;
        }
        public sealed class Child:Parent {
          public int Value;public Child(int value){Value=value;}
          public override int Compute()=>Value;
          public override int Read()=>base.Read();
          public override Box Select(Box value)=>base.Select(value);
        }
        public static class Rules{
          public static int Read(int value){Parent parent=new Child(value);return parent.Read();}
          public static int Alias(int value){var input=new Box(value);Parent parent=new Child(7);var result=parent.Select(input);input.Value=42;if(result is null)return 0;return result.Value;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "Read", "Alias" }) Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value)));
        var source = code.Replace("public static int Read(int value)", "public static int Unsafe(Box value){Parent parent=new Child(7);var result=parent.Select(value);if(result is null)return 0;result.Value=1;return result.Value;}public static int Read(int value)");
        var rejected = Support.Convert(source, selectors: ["Example.D10"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS3002");
    }

    [Fact]
    public void GenericAbstractDomainsRetainWholeArgumentsConstrainedViewsAndReturnedAliases()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Box{public int Value;public Box(int value){Value=value;}}
        public abstract class Parent<T>{public abstract T Echo(T value);}
        public sealed class Child<T>:Parent<T>{public override T Echo(T value)=>value;}
        public static class Rules{
          public static U Call<P,U>(P? parent,U value) where P:Parent<U>{if(parent is null)return value;return parent.Echo(value);}
          public static int Number(int value)=>Call<Child<int>,int>(new Child<int>(),value);
          public static string? Text(string? value)=>Call<Child<string?>,string?>(new Child<string?>(),value);
          public static int Alias(int value){var input=new Box(value);var result=Call<Child<Box>,Box>(new Child<Box>(),input);input.Value=7;if(result is null)return 0;return result.Value;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "Number", "Alias" }) Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value)));
        foreach (var value in new string?[] { null, "", "sample" }) Assert.Equal(Support.Maybe(value), Support.Call(library, "D10.Rules", "Text", Support.Maybe(value)));
    }

    [Fact]
    public void InheritedDefaultFieldsKeepLeafStorageAliasesAndBorrowedReachability()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Box{public int Value;public Box(int value){Value=value;}}
        public abstract class Parent{public int Value;public Box? Child;public int Read()=>Value;public Box? Select()=>Child;}
        public sealed class Leaf:Parent{public Leaf(int value,Box? child){Value=value;Child=child;}}
        public sealed class Default:Parent{}
        public static class Rules{
          public static int Read(int value){Parent parent=new Leaf(value,null);return parent.Read();}
          public static int Alias(int value){var box=new Box(value);Parent parent=new Leaf(value,box);var result=parent.Select();box.Value=7;if(result is null)return 0;return result.Value;}
          public static int Update(int value){Parent parent=new Leaf(value,null);var alias=parent;parent.Value=7;return alias.Read();}
          public static int Default()=>new Default().Read();
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "Read", "Alias", "Update" }) Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value)));
        Assert.Equal(0, Convert.ToInt32(Support.Call(library, "D10.Rules", "Default")));
        foreach (var member in new[] { "parent.Value=1;return 1;", "if(parent.Child is null)return 0;parent.Child.Value=1;return 1;" })
        {
            var rejected = Support.Convert(code.Replace("public static int Read(int value)", "public static int Unsafe(Parent parent){if(parent is null)return 0;" + member + "}public static int Read(int value)"), selectors: ["Example.D10"]);
            Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS3002");
        }
    }

    [Fact]
    public void BaseConstructionRunsInitializersAndChainedBodiesInClrOrderWithVirtualSelf()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent {
          public int Root=3;public int Seen;
          protected Parent(int value):this(value,true){}
          private Parent(int value,bool marker){Seen=Read();Root=value;}
          public abstract int Read();
        }
        public abstract class Middle:Parent {
          public int MiddleValue=11;
          protected Middle(int value):base(value){MiddleValue=Root;}
        }
        public sealed class Leaf:Middle {
          public int Value=17;
          public Leaf(int value):this(value,true){}
          private Leaf(int value,bool marker):base(value){Value=value;}
          public override int Read()=>Value;
        }
        public abstract class DefaultParent{public int Value=23;public int Read()=>Value;}
        public sealed class DefaultLeaf:DefaultParent{}
        public static class Rules {
          public static int Seen(int value)=>new Leaf(value).Seen;
          public static int Root(int value)=>new Leaf(value).Root;
          public static int Middle(int value)=>new Leaf(value).MiddleValue;
          public static int Value(int value){Parent leaf=new Leaf(value);return leaf.Read();}
          public static int Default()=>new DefaultLeaf().Read();
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, 42, int.MaxValue })
            foreach (var name in new[] { "Seen", "Root", "Middle", "Value" })
                Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value)));
        Assert.Equal(17, Convert.ToInt32(Support.Call(library, "D10.Rules", "Seen", 42)));
        Assert.Equal(23, Convert.ToInt32(Support.Call(library, "D10.Rules", "Default")));
    }

    [Fact]
    public void BaseConstructorArgumentsKeepOwnedChildAliasesAndRejectBorrowedMutationsAndEffects()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Box{public int Value;public Box(int value){Value=value;}}
        public abstract class Parent {
          public Box Child;public Box Initialized=new Box(5);
          protected Parent(Box child){Child=child;}
          public Box Select()=>Child;
        }
        public sealed class Leaf:Parent {
          public Leaf(Box child):base(child){}
        }
        public static class Rules {
          public static int Alias(int value){var box=new Box(value);Parent leaf=new Leaf(box);var result=leaf.Select();box.Value=7;if(result is null)return 0;return result.Value;}
          public static int Default(int value){Parent leaf=new Leaf(new Box(value));if(leaf.Initialized is null)return 0;return leaf.Initialized.Value;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "Alias", "Default" })
                Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value)));
        foreach (var mutation in new[] { "if(child is not null)child.Value=1;", "if(Child is not null)Child.Value=1;" })
        {
            var rejected = Support.Convert(code.Replace("Child=child;", "Child=child;" + mutation), selectors: ["Example.D10"]);
            Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS3002");
        }
        foreach (var body in new[] { "System.Console.WriteLine(value);", "var ignored=checked(value+1);" })
        {
            var rejected = Support.Convert("namespace Example.D10;public abstract class Parent{protected Parent(int value){" + body + "}}public sealed class Leaf:Parent{public Leaf(int value):base(value){}}", selectors: ["Example.D10"]);
            Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == (body.Contains("checked") ? "MCS2001" : "MCS2202"));
        }
    }

    [Fact]
    public void GenericBaseConstructionKeepsDefaultWitnessesWholeMaybeArgumentsAndChildAliases()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Box{public int Value;public Box(int value){Value=value;}}
        public abstract class Parent<T>{public T Value=default!;protected Parent(T value){Value=value;}public T Read()=>Value;}
        public sealed class Leaf<T>:Parent<T>{public Leaf(T value):base(value){}}
        public static class Rules{
          public static int Number(int value){Parent<int> parent=new Leaf<int>(value);return parent.Read();}
          public static string? Text(string? value){Parent<string?> parent=new Leaf<string?>(value);return parent.Read();}
          public static int Alias(int value){var child=new Box(value);Parent<Box> parent=new Leaf<Box>(child);var result=parent.Read();child.Value=7;if(result is null)return 0;return result.Value;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "Number", "Alias" })
                Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value)));
        foreach (var value in new string?[] { null, "", "sample" }) Assert.Equal(Support.Maybe(value), Support.Call(library, "D10.Rules", "Text", Support.Maybe(value)));
    }

    [Fact]
    public void ImplicitGenericBaseInitializersPropagateCreationAndAbsenceWitnesses()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Box{public int Value=27;}
        public abstract class Parent<T> where T:new(){public T Value=new T();public bool Present()=>Value is not null;public T Read()=>Value;}
        public sealed class Leaf<T>:Parent<T> where T:new(){}
        public static class Rules{
          public static bool Present()=>new Leaf<int>().Present();
          public static int Read(){Parent<Box> parent=new Leaf<Box>();var result=parent.Read();if(result is null)return 0;return result.Value;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        Assert.Equal(true, Support.Call(library, "D10.Rules", "Present"));
        Assert.Equal(27, Convert.ToInt32(Support.Call(library, "D10.Rules", "Read")));
        Assert.Equal(27, Support.Emit(code).GetType("Example.D10.Rules")!.GetMethod("Read")!.Invoke(null, null));
    }

    [Fact]
    public void GenericVirtualConstructionResultsRetainTheirActualStoredValue()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent<T>{public T Seen=default!;protected Parent(){Seen=Read();}public abstract T Read();}
        public sealed class Leaf<T>:Parent<T>{public T Value=default!;public override T Read()=>Value;}
        public static class Rules{
          public static int Number()=>new Leaf<int>().Seen;
          public static string? Text()=>new Leaf<string?>().Seen;
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        Assert.Equal(0, Convert.ToInt32(Support.Call(library, "D10.Rules", "Number")));
        Assert.Equal(Support.Maybe(null), Support.Call(library, "D10.Rules", "Text"));
    }

    [Fact]
    public void PrimaryBaseArgumentsAndCapturesUseTheBoundBaseConstructor()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent{public int Value;public int Seen;protected Parent(int value){Value=value;Seen=ReadCaptured();}public abstract int ReadCaptured();}
        public sealed class Captured(int value):Parent(Rules.Pass(value)){public override int ReadCaptured()=>value;}
        public sealed class Plain(int value):Parent(Rules.Pass(value)){public override int ReadCaptured()=>7;}
        public static class Rules{
          public static int Pass(int value)=>value;
          public static int Value(int value)=>new Captured(value).Value;
          public static int Seen(int value)=>new Captured(value).Seen;
          public static int Plain(int value)=>new Plain(value).Value;
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "Value", "Seen", "Plain" })
                Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value)));
    }

    [Fact]
    public void PrimaryBaseArgumentCallsCarryGenericNumericWitnesses()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent<T> where T:System.Numerics.INumberBase<T>{public T Value;protected Parent(T value){Value=value;}}
        public sealed class Leaf<T>(long value):Parent<T>(Rules.Convert<T>(value)) where T:System.Numerics.INumberBase<T>;
        public static class Rules{
          public static T Convert<T>(long value)where T:System.Numerics.INumberBase<T> =>T.CreateSaturating(value);
          public static int Number(long value)=>new Leaf<int>(value).Value;
          public static byte Byte(long value)=>new Leaf<byte>(value).Value;
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { long.MinValue, -1, 0, 1, 255, 256, int.MaxValue, long.MaxValue }) foreach (var name in new[] { "Number", "Byte" })
                Assert.Equal(Convert.ToInt64(rules.GetMethod(name)!.Invoke(null, [value])!), Convert.ToInt64(Support.Call(library, "D10.Rules", name, Support.Integer(value))));
    }

    [Fact]
    public void InheritedPrimaryCapturesRetainGenericMaybeValuesAndReferenceAliases()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Box{public int Value;public Box(int value){Value=value;}}
        public abstract class Parent<T>(T seed){public T Read()=>seed;}
        public sealed class Leaf<T>(T value):Parent<T>(value);
        public static class Rules{
          public static int Number(int value)=>new Leaf<int>(value).Read();
          public static string? Text(string? value)=>new Leaf<string?>(value).Read();
          public static int Alias(int value){var child=new Box(value);Parent<Box> parent=new Leaf<Box>(child);var result=parent.Read();child.Value=7;if(result is null)return 0;return result.Value;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "Number", "Alias" })
                Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value)));
        foreach (var value in new string?[] { null, "", "sample" }) Assert.Equal(Support.Maybe(value), Support.Call(library, "D10.Rules", "Text", Support.Maybe(value)));
        var rejected = Support.Convert(code.Replace("public static int Number(int value)", "public static int Unsafe(Box child){Parent<Box> parent=new Leaf<Box>(child);var result=parent.Read();if(result is null)return 0;result.Value=1;return result.Value;}public static int Number(int value)"), selectors: ["Example.D10"]);
        Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS3002");
    }

    [Fact]
    public void PrimaryBaseNamedArgumentsEvaluateInSourceOrderBeforeTheBaseBody()
    {
        const string code = """
        namespace Example.D10;
        public abstract class Parent{public int First;public int Second;public int Seen;protected Parent(int first,int second){First=first;Second=second;Seen=ReadCaptured();}public abstract int ReadCaptured();}
        public sealed class Leaf(int value):Parent(second:value,first:value=7){public override int ReadCaptured()=>value;}
        public static class Rules{
          public static int First(int value)=>new Leaf(value).First;
          public static int Second(int value)=>new Leaf(value).Second;
          public static int Seen(int value)=>new Leaf(value).Seen;
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "First", "Second", "Seen" })
                Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value)));
    }

    [Fact]
    public void PrimaryBaseArgumentsRejectEffectsAndUnsafeNumericConversionsEvenWithoutAllocations()
    {
        foreach (var (argument, diagnostic) in new[] { ("System.Console.Read()", "MCS2202"), ("false?System.Console.Read():0", "MCS2202"), ("checked((int)value)", "MCS2001"), ("byte.CreateChecked(value)", "MCS2610"), ("false?byte.CreateChecked(value):0", "MCS2610") })
        {
            var source = "namespace Example.D10;public abstract class Parent{public int Value;protected Parent(int value){Value=value;}}public sealed class Leaf(long value):Parent(" + argument + ");";
            var result = Support.Convert(source, selectors: ["Example.D10"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == diagnostic && item.File == "Case.cs");
        }
    }

    [Fact]
    public void ValueOnlyVirtualResultsDoNotInventReceiverAliasesOrCycles()
    {
        foreach (var (declaration, type, initial) in new[] {
            ("", "long", "5L"),
            ("", "System.Int128", "(System.Int128)5"),
            ("", "decimal", "5m"),
            ("", "System.Numerics.Complex", "new System.Numerics.Complex(5,2)"),
            ("public enum Kind{Five=5}", "Kind", "Kind.Five"),
            ("", "(int Number,bool Flag)", "(5,true)"),
            ("public readonly record struct Scalar<T>(T Value);", "Scalar<int>", "new Scalar<int>(5)")})
        {
            var observation = type.StartsWith("(") ? "leaf.Seen.Number==5&&leaf.Seen.Flag" : type.StartsWith("Scalar") ? "leaf.Seen.Value==5" : "leaf.Seen==" + initial;
            var code = "namespace Example.D10;" + declaration + "public abstract class Parent{public " + type + " Seen;protected Parent(){Seen=Read();}public abstract " + type + " Read();}public sealed class Leaf:Parent{public " + type + " Value=" + initial + ";public override " + type + " Read()=>Value;}public static class Rules{public static bool Test(){var leaf=new Leaf();return " + observation + ";}}";
            var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
            Assert.Equal((bool)Support.Emit(code).GetType("Example.D10.Rules")!.GetMethod("Test")!.Invoke(null, null)!, Support.Call(library, "D10.Rules", "Test"));
        }
        const string cycle = "namespace Example.D10;public abstract class Parent{public Parent? Self;protected Parent(){Self=Select();}public abstract Parent Select();}public sealed class Leaf:Parent{public override Parent Select()=>this;}";
        var rejected = Support.Convert(cycle, selectors: ["Example.D10"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS3007");
    }
}
