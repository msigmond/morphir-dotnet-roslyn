using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D9")]
public class DataTests
{
    [Fact]
    public void CounterHasCompleteIndependentCodecParity() => Support.Parity("fixtures/D9/expected/counter.json", Support.Read("fixtures/D9/csharp/Counter.cs"), "Example", "Example.D9");
    [Fact]
    public void AllDataFormsAndNestedRecursivePrivateTypesHaveCompleteIndependentParity() => Support.Parity("fixtures/D9/expected/data-forms.json", Support.Read("fixtures/D9/csharp/DataForms.cs"), "Example", "Example.D9");
    [Fact]
    public void SourceConstantsHaveCompleteIndependentCodecParity() => Support.Parity("fixtures/D9/expected/constants.json", Support.Read("fixtures/D9/csharp/Constants.cs"), "Example", "Example.D9");
    [Fact]
    public void LocalCounterAliasesObserveTheSameFunctionalUpdates()
    {
        const string code = """
        namespace Example.D9;
        public sealed class Counter { public int Value; }
        public static class Rules {
          public static int BuildAndUpdate(){var counter=new Counter{Value=1};var alias=counter;alias.Value=2;return counter.Value;}
          public static int AliasRead(){var counter=new Counter{Value=1};var alias=counter;counter.Value=2;return alias.Value;}
          public static int Rebinding(){var first=new Counter{Value=1};var alias=first;first=new Counter{Value=3};alias.Value=2;return first.Value;}
          public static int Branch(bool change){var counter=new Counter{Value=1};var alias=counter;if(change)alias.Value=2;return counter.Value;}
          public static int EarlierReceiver(){var first=new Counter{Value=1};var alias=first;alias.Value=(alias=new Counter{Value=3}).Value;return first.Value;}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var name in new[] { "BuildAndUpdate", "AliasRead", "Rebinding", "EarlierReceiver" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(lib, "D9.Rules", name));
        foreach (var value in new[] { false, true }) Assert.Equal(rules.GetMethod("Branch")!.Invoke(null, [value]), Support.Call(lib, "D9.Rules", "Branch", value));
    }
    [Fact]
    public void StructAssignmentsCopyValuesAndRebindOnlyTheirLocalOrByValueRoot()
    {
        const string code = """
        namespace Example.D9;public struct Counter{public int Value;}
        public static class Rules{
          public static int Copy(int value){var first=new Counter{Value=value};var copy=first;copy.Value=2;return first.Value;}
          public static int Update(int value){var first=new Counter{Value=value};first.Value=2;return first.Value;}
          public static int Parameter(Counter value){value.Value=2;return value.Value;}
          public static int Default()=>default(Counter).Value;
          public static int RhsRebind(int value){var first=new Counter{Value=value};first.Value=(first=new Counter{Value=3}).Value;return first.Value;}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "Copy", "Update", "RhsRebind" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D9.Rules", name, value));
        Assert.Equal(0, Support.Call(lib, "D9.Rules", "Default")); Assert.Equal(2, Support.Call(lib, "D9.Rules", "Parameter", new Dictionary<string, object> { { "value", 18 } }));
    }
    [Fact]
    public void AutoPropertyStoragePreservesOwnedAliasesAndValueCopies()
    {
        const string code = """
        namespace Example.D9;
        public sealed class Counter{public int Value{get;set;}}
        public struct Amount{public int Value{get;set;}}
        public static class Rules{
          public static int Class(){var value=new Counter{Value=1};var alias=value;alias.Value=2;return value.Value;}
          public static int Struct(){var value=new Amount{Value=1};var alias=value;alias.Value=2;return value.Value;}
          public static int StructUpdate(){var value=new Amount{Value=1};value.Value=2;return value.Value;}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var name in new[] { "Class", "Struct", "StructUpdate" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(lib, "D9.Rules", name));
    }
    [Fact]
    public void PureDataInstanceMethodsReceiveTheCurrentPresentValue()
    {
        const string code = """
        namespace Example.D9;
        public sealed class Counter{public int Value;public int Read()=>Value;public bool Minimum(int minimum=18)=>Value>=minimum;public int WithIgnored(int ignored)=>Value;}
        public struct Amount{public int Value;public int Read()=>Value;}
        public static class Rules{
          private static int ReadArgument(Counter? first,int ignored){if(first==null)return 0;return first.Value;}
          public static int ArgumentOrder(int value){var data=new Counter{Value=value};return ReadArgument(data,data.Value=2);}
          public static int ReceiverOrder(int value){var data=new Counter{Value=value};return data.WithIgnored(data.Value=2);}
          public static int Read(int value){var data=new Counter{Value=value};var alias=data;alias.Value=2;return data.Read();}
          public static bool Optional(int value){var data=new Counter{Value=value};return data.Minimum();}
          public static int Struct(int value){var data=new Amount{Value=value};return data.Read();}
          public static int Guard(Counter? value){if(value==null)return -1;return value.Read();}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, 17, 18, int.MaxValue }) foreach (var name in new[] { "Read", "Optional", "Struct", "ArgumentOrder", "ReceiverOrder" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D9.Rules", name, value));
        Assert.Equal(-1, Support.Call(lib, "D9.Rules", "Guard", Support.Maybe(null))); Assert.Equal(18, Support.Call(lib, "D9.Rules", "Guard", Support.Maybe(new Dictionary<string, object> { { "value", 18 } })));
    }
    [Fact]
    public void OwnedNestedReferencesKeepTheirAliasesAndRecursiveDeclarationsStayFinite()
    {
        const string code = """
        namespace Example.D9;
        public sealed class Counter{public int Value;}
        public sealed class Holder{public Counter? Child;}
        public sealed class Node{public int Value;public Node? Next;}
        public static class Rules{
          public static int Nested(){var child=new Counter{Value=1};var holder=new Holder{Child=child};holder.Child.Value=2;return child.Value;}
          public static Holder ReturnGraph(){var child=new Counter{Value=1};var holder=new Holder{Child=child};child.Value=2;return holder;}
          public static int Finite(){var leaf=new Node{Value=1};var root=new Node{Next=leaf};root.Next.Value=2;return leaf.Value;}
          public static int Replaced(){var a=new Node();var b=new Node();a.Next=b;a.Next=null;b.Next=a;return b.Next.Value;}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var name in new[] { "Nested", "Finite", "Replaced" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(lib, "D9.Rules", name));
        var graph = (IReadOnlyDictionary<string, object>)((Support.MaybeData)Support.Call(lib, "D9.Rules", "ReturnGraph")).Value!;
        Assert.Equal(2, ((IReadOnlyDictionary<string, object>)((Support.MaybeData)graph["child"]).Value!)["value"]);
        foreach (var member in new[] { "public static int F(){var a=new Node();a.Next=a;return 0;}", "public static int F(){var a=new Node();var b=new Node();a.Next=b;b.Next=a;return 0;}" })
        { var result = Support.Convert("namespace Example.D9;public sealed class Node{public Node? Next;}public static class Rules{" + member + "}", selectors: ["Example.D9"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, d => d.Code == "MCS3007"); }
        var borrowed = Support.Convert("namespace Example.D9;public sealed class Counter{public int Value;}public sealed class Holder{public Counter? Child;}public static class Rules{public static int F(Counter input){var own=new Holder{Child=input};if(own.Child==null)return 0;own.Child.Value=2;return 0;}}", selectors: ["Example.D9"]);
        Assert.Null(borrowed.Distribution); Assert.Contains(borrowed.Diagnostics, d => d.Code == "MCS3002");
    }
    [Fact]
    public void SourceFactoriesAndAliasReturnsRetainInterproceduralReferenceReachability()
    {
        const string code = """
        namespace Example.D9;
        public sealed class Counter{public int Value;public Counter Self=>this;}
        public sealed class Holder{public Counter? Child;}
        public struct Pair{public Counter? Child;}
        public static class Rules{
          private static Counter Id(Counter value)=>value;
          private static Counter Fresh(int value)=>new Counter{Value=value};
          private static Holder Wrap(Counter value)=>new Holder{Child=value};
          private static Pair ValueWrap(Counter value)=>new Pair{Child=value};
          private static Counter Choose(bool first,Counter a,Counter b){if(first)return a;return b;}
          public static int Alias(){var value=Fresh(1);var alias=Id(value);value.Value=2;return alias.Value;}
          public static int Getter(){var value=Fresh(1);var alias=value.Self;alias.Value=2;return value.Value;}
          public static int Wrapped(){var value=Fresh(1);var own=Wrap(value);own.Child.Value=2;return value.Value;}
          public static int Struct(){var value=Fresh(1);var own=ValueWrap(value);value.Value=2;return own.Child.Value;}
          public static int Conditional(bool first){var a=Fresh(1);var b=Fresh(2);var alias=Choose(first,a,b);alias.Value=3;return a.Value;}
          public static int Ordered(){var value=Fresh(1);return Id(value).Value;}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var name in new[] { "Alias", "Getter", "Wrapped", "Struct", "Ordered" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(lib, "D9.Rules", name));
        foreach (var first in new[] { false, true }) Assert.Equal(rules.GetMethod("Conditional")!.Invoke(null, [first]), Support.Call(lib, "D9.Rules", "Conditional", first));
        foreach (var path in new[] { "Id(input)", "Wrap(input).Child" })
        {
            var result = Support.Convert("namespace Example.D9;public sealed class Counter{public int Value;}public sealed class Holder{public Counter? Child;}public static class Rules{private static Counter Id(Counter value)=>value;private static Holder Wrap(Counter value)=>new Holder{Child=value};public static int F(Counter input){var alias=" + path + ";if(alias==null)return 0;alias.Value=2;return 0;}}", selectors: ["Example.D9"]);
            Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, d => d.Code == "MCS3002");
        }
    }
    [Fact]
    public void ExplicitClassAndStructConstructorsRetainBodiesChainsAndAliases()
    {
        const string code = """
        namespace Example.D9;
        public sealed class Counter{public int Value;public Counter(int value){static int Identity(int input)=>input;Value=Identity(value);}public Counter():this(2){}public Counter(int value,bool use){if(use)Value=value;else Value=3;}}
        public struct Amount{public int Value;public Amount(int value){Value=value;}}
        public static class Rules{
          public static int Class(int value){var data=new Counter(value);var alias=data;alias.Value=2;return data.Value;}
          public static int Read(int value)=>new Counter(value).Value;
          public static int Chain()=>new Counter().Value;
          public static int Branch(int value,bool flag)=>new Counter(value,flag).Value;
          public static int Struct(int value){var data=new Amount(value);var copy=data;copy.Value=2;return data.Value;}
          public static int Order(int value)=>new Counter(use:true,value:value=2).Value;
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, 1, int.MaxValue })
        {
            foreach (var name in new[] { "Class", "Read", "Struct", "Order" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D9.Rules", name, value));
            foreach (var flag in new[] { false, true }) Assert.Equal(rules.GetMethod("Branch")!.Invoke(null, [value, flag]), Support.Call(lib, "D9.Rules", "Branch", value, flag));
        }
        Assert.Equal(2, Support.Call(lib, "D9.Rules", "Chain"));
    }
    [Fact]
    public void ConditionalOwnedAliasesAndConstructorEarlyReturnsKeepTheirActualCells()
    {
        const string code = """
        namespace Example.D9;public sealed class Counter{public int Value;public Counter(){}public Counter(int value,bool use){if(use){Value=value;return;}Value=3;}}
        public static class Rules{
          public static int Alias(bool first){var a=new Counter{Value=1};var b=new Counter{Value=2};var alias=first?a:b;alias.Value=3;return a.Value;}
          public static int Construct(bool first){var data=first?new Counter(1,true):new Counter(2,true);data.Value=3;return data.Value;}
          public static int Early(int value,bool use)=>new Counter(value,use).Value;
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var flag in new[] { false, true }) foreach (var name in new[] { "Alias", "Construct" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [flag]), Support.Call(lib, "D9.Rules", name, flag));
        foreach (var flag in new[] { false, true }) foreach (var value in new[] { int.MinValue, 0, 1, int.MaxValue }) Assert.Equal(rules.GetMethod("Early")!.Invoke(null, [value, flag]), Support.Call(lib, "D9.Rules", "Early", value, flag));
    }
    [Fact]
    public void FieldAndAutoPropertyInitializersRunInSourceOrderAndOnlyForActualConstruction()
    {
        const string code = """
        namespace Example.D9;
        public sealed class Counter{public int Value=1;public int Other{get;set;}=2;public Counter(){}public Counter(int value):this(){Value=Value+value;}}
        public sealed class DefaultCounter{public int Value=3;public string Name{get;}="hello";}
        public struct Amount{public int Value=1;public Amount(int value){Value=Value+value;}}
        public static class Rules{
          public static int Implicit()=>new DefaultCounter().Value;
          public static int String()=>new DefaultCounter().Name.Length;
          public static int Class(int value)=>new Counter(value).Value;
          public static int Property()=>new Counter().Other;
          public static int Override()=>new Counter{Value=4}.Value;
          public static int Struct(int value)=>new Amount(value).Value;
          public static int Zero()=>new Amount().Value;
          public static int Default()=>default(Amount).Value;
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var name in new[] { "Implicit", "String", "Property", "Override", "Zero", "Default" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(lib, "D9.Rules", name));
        foreach (var value in new[] { int.MinValue, 0, 1, int.MaxValue }) foreach (var name in new[] { "Class", "Struct" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D9.Rules", name, value));
        var hidden = Support.Convert("namespace Example.D9;public sealed class Hidden{public int Value=System.Environment.TickCount;}public static class Rules{public static int F()=>1;}", selectors: ["Example.D9"]);
        Assert.Null(hidden.Distribution); Assert.NotEmpty(hidden.Diagnostics);
    }
    [Fact]
    public void PureCustomGettersAndIndexersUseResolvedBodiesAndRejectHiddenEffects()
    {
        const string code = """
        namespace Example.D9;public sealed class Counter{
          private int value;
          public Counter(int input){value=input;}
          public int Value=>value;
          public bool Adult{get{return value>=18;}}
          public int this[bool choose]{get=>choose?value:0;}
          public static int Constant=>18;
        }
        public static class Rules{
          public static int Read(int input)=>new Counter(input).Value;
          public static bool Adult(int input)=>new Counter(input).Adult;
          public static int Index(int input,bool choose)=>new Counter(input)[choose];
          public static int Static()=>Counter.Constant;
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, 17, 18, int.MaxValue })
        {
            foreach (var name in new[] { "Read", "Adult" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D9.Rules", name, value));
            foreach (var choose in new[] { false, true }) Assert.Equal(rules.GetMethod("Index")!.Invoke(null, [value, choose]), Support.Call(lib, "D9.Rules", "Index", value, choose));
        }
        Assert.Equal(18, Support.Call(lib, "D9.Rules", "Static"));
        var hidden = Support.Convert("namespace Example.D9;public sealed class Hidden{private int value;private int Effect=>System.Environment.TickCount;public int Read()=>value;}public static class Rules{public static int F()=>1;}", selectors: ["Example.D9"]);
        Assert.Null(hidden.Distribution); Assert.NotEmpty(hidden.Diagnostics);
    }
    [Fact]
    public void PrimaryConstructorsInitializeFieldsAndRetainCapturedParameterValues()
    {
        const string code = """
        namespace Example.D9;
        public sealed class Counter(int initial){public int Value{get;}=initial;public int Captured=>initial;public int Read()=>initial;}
        public struct Amount(int initial){public int Value=initial;public int Captured=>initial;}
        public static class Rules{
          public static int Class(int value)=>new Counter(value).Value;
          public static int Capture(int value)=>new Counter(value).Captured;
          public static int Method(int value)=>new Counter(value).Read();
          public static int Struct(int value)=>new Amount(value).Value;
          public static int StructCapture(int value)=>new Amount(value).Captured;
          public static int Zero()=>default(Amount).Captured;
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, 1, int.MaxValue }) foreach (var name in new[] { "Class", "Capture", "Method", "Struct", "StructCapture" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D9.Rules", name, value));
        Assert.Equal(0, Support.Call(lib, "D9.Rules", "Zero"));
    }
    [Fact]
    public void CustomSettersInitAndFieldBackedAccessorsPreserveOwnedStateAndSourceOrder()
    {
        const string code = """
        namespace Example.D9;
        public sealed class Counter{private int stored;public int Value{get=>stored;set{if(value<0){stored=0;return;}stored=value;}}public int this[bool keep]{get=>stored;set{if(keep)stored=value;}}public int Initial{get;init{field=value;}}public int Backed{get;set{field=value;}}}
        public struct Amount{private int stored;public int Value{get=>stored;set=>stored=value;}}
        public static class Rules{
          public static int Class(int value){var data=new Counter();var alias=data;alias.Value=value;return data.Value;}
          public static int Init(int value)=>new Counter{Initial=value}.Initial;
          public static int Backed(int value){var data=new Counter{Backed=value};data.Backed=2;return data.Backed;}
          public static int Index(int value,bool keep){var data=new Counter{Value=1};data[keep]=value;return data.Value;}
          public static int Struct(int value){var data=new Amount{Value=1};var copy=data;copy.Value=value;return data.Value;}
          public static int StructUpdate(int value){var data=new Amount();data.Value=value;return data.Value;}
          public static int EarlierReceiver(){var data=new Counter{Value=1};var original=data;data.Value=(data=new Counter{Value=3}).Value;return original.Value;}
          public static int StructRhs(){var data=new Amount{Value=1};data.Value=(data=new Amount{Value=3}).Value;return data.Value;}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, 1, int.MaxValue })
        {
            foreach (var name in new[] { "Class", "Init", "Backed", "Struct", "StructUpdate" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D9.Rules", name, value));
            foreach (var keep in new[] { false, true }) Assert.Equal(rules.GetMethod("Index")!.Invoke(null, [value, keep]), Support.Call(lib, "D9.Rules", "Index", value, keep));
        }
        foreach (var name in new[] { "EarlierReceiver", "StructRhs" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(lib, "D9.Rules", name));
        foreach (var member in new[] { "private int Effect{set{stored=System.Environment.TickCount;}}", "public static int F(Counter input){if(input==null)return 0;input.Value=2;return 0;}" })
        {
            var rejected = Support.Convert("namespace Example.D9;public sealed class Counter{private int stored;public int Value{get=>stored;set=>stored=value;}" + member + "}", selectors: ["Example.D9"]); Assert.Null(rejected.Distribution); Assert.NotEmpty(rejected.Diagnostics);
        }
    }
    [Fact]
    public void RecordClassesStructsReadonlyFormsAndWithUpdatesPreserveShallowCopies()
    {
        const string code = """
        namespace Example.D9;
        public sealed record Counter(int Value){public int Extra{get;init;}=3;}
        public record struct Amount(int Value);
        public readonly record struct ReadonlyAmount(int Value);
        public sealed class Cell{public int Value;}
        public sealed record Holder{public Cell? Child{get;init;}}
        public readonly record struct ReadonlyHolder(Cell? Child);
        public static class Rules{
          public static int Class(int value)=>new Counter(value).Value;
          public static int Extra()=>new Counter(1).Extra;
          public static int Struct(int value){var a=new Amount(value);a.Value=2;return a.Value;}
          public static int Copy(int value){var a=new Amount(value);var b=a with{Value=2};return a.Value;}
          public static int Readonly(int value)=>new ReadonlyAmount(value).Value;
          public static int With(int value){var data=new Counter(value) with{Value=2};return data.Value;}
          public static int Alias(int value){var a=new Counter(value);var b=a with{Value=2};return a.Value;}
          public static int LegacyWith(int value){var a=new ReadonlyAmount(value);var b=a with{Value=2};return b.Value;}
          public static int Nested(){var child=new Cell{Value=1};var holder=new Holder{Child=child};var copy=holder with{};copy.Child.Value=2;return child.Value;}
          public static int ReadonlyNested(){var child=new Cell{Value=1};var holder=new ReadonlyHolder(child);var copy=holder with{};child.Value=2;return copy.Child.Value;}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, 1, int.MaxValue }) foreach (var name in new[] { "Class", "Struct", "Copy", "Readonly", "With", "Alias", "LegacyWith" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D9.Rules", name, value));
        foreach (var name in new[] { "Extra", "Nested", "ReadonlyNested" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(lib, "D9.Rules", name));
    }
    [Fact]
    public void ResolvedPureOperatorsConversionsAndLiftedFormsUseTheirSourceBodies()
    {
        const string code = """
        namespace Example.D9;
        public struct Amount{
          public int Value;public Amount(int value){Value=value;}
          public static Amount operator +(Amount a,Amount b)=>new Amount(a.Value+b.Value);
          public static Amount operator -(Amount a)=>new Amount(-a.Value);
          public static bool operator <(Amount a,Amount b)=>a.Value<b.Value;
          public static bool operator >(Amount a,Amount b)=>a.Value>b.Value;
          public static implicit operator Amount(int value)=>new Amount(value);
          public static explicit operator int(Amount value)=>value.Value;
        }
        public sealed class Cell{public int Value;public static Cell operator +(Cell a,int ignored)=>a;}
        public static class Rules{
          public static int Add(int a,int b)=>(int)(new Amount(a)+new Amount(b));
          public static int Negate(int value)=>(int)-new Amount(value);
          public static int Convert(int value){Amount amount=value;return (int)amount;}
          public static Amount? Lift(Amount? a,Amount? b)=>a+b;
          public static bool Less(Amount? a,Amount? b)=>a<b;
          public static int Alias(){var a=new Cell{Value=1};var b=a+0;b.Value=2;return a.Value;}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var assembly = Support.Emit(code); var rules = assembly.GetType("Example.D9.Rules")!; var amount = assembly.GetType("Example.D9.Amount")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "Negate", "Convert" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D9.Rules", name, value));
        foreach (var a in new[] { int.MinValue, 0, int.MaxValue }) foreach (var b in new[] { -1, 0, 1 }) Assert.Equal(rules.GetMethod("Add")!.Invoke(null, [a, b]), Support.Call(lib, "D9.Rules", "Add", a, b));
        foreach (int? a in new int?[] { null, -1, 0, int.MaxValue }) foreach (int? b in new int?[] { null, 0, 1 })
            {
                object? Native(int? value) => value.HasValue ? Activator.CreateInstance(amount, [value.Value]) : null;
                object Ir(int? value) => Support.Maybe(value.HasValue ? new Dictionary<string, object> { { "value", value.Value } } : null);
                Assert.Equal(rules.GetMethod("Less")!.Invoke(null, [Native(a), Native(b)]), Support.Call(lib, "D9.Rules", "Less", Ir(a), Ir(b)));
                var native = rules.GetMethod("Lift")!.Invoke(null, [Native(a), Native(b)]); var actual = (Support.MaybeData)Support.Call(lib, "D9.Rules", "Lift", Ir(a), Ir(b));
                Assert.Equal(native is null, null == actual.Value); if (native is not null) Assert.Equal(amount.GetField("Value")!.GetValue(native), ((IReadOnlyDictionary<string, object>)actual.Value!)["value"]);
            }
        Assert.Equal(2, Support.Call(lib, "D9.Rules", "Alias"));
        var hidden = Support.Convert("namespace Example.D9;public struct Hidden{public static Hidden operator +(Hidden a,Hidden b){var ignored=System.Environment.TickCount;return a;}}", selectors: ["Example.D9"]); Assert.Null(hidden.Distribution); Assert.NotEmpty(hidden.Diagnostics);
    }
    [Fact]
    public void RecordAndDefaultStructValueEqualityPreservesClrEqualsBoundaries()
    {
        const string code = """
        namespace Example.D9;
        public sealed record Counter(int Value);
        public record struct Amount(double Value,decimal Money);
        public struct Plain{public double Value;public decimal Money;public (double,int) Pair;}
        public readonly record struct Composite(Amount Amount,(double,int) Pair,System.Numerics.Complex Complex);
        public static class Rules{
          public static bool Record(int a,int b)=>new Counter(a)==new Counter(b);
          public static bool Null(Counter? a)=>a==null;
          public static bool Equals(Counter? a,Counter? b){if(a is null)return b is null;return a.Equals(b);}
          public static bool Numeric(double a,double b,decimal x,decimal y)=>new Amount(a,x)==new Amount(b,y);
          public static bool Plain(double a,double b,decimal x,decimal y){var first=new Plain{Value=a,Money=x,Pair=(a,1)};var second=new Plain{Value=b,Money=y,Pair=(b,1)};return first.Equals(second);}
          public static bool Nested(double a,double b,decimal x,decimal y)=>new Composite(new Amount(a,x),(a,1),new System.Numerics.Complex(a,0))==new Composite(new Amount(b,y),(b,1),new System.Numerics.Complex(b,0));
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var a in new[] { int.MinValue, 0, int.MaxValue }) foreach (var b in new[] { int.MinValue, 0, int.MaxValue }) Assert.Equal(rules.GetMethod("Record")!.Invoke(null, [a, b]), Support.Call(lib, "D9.Rules", "Record", a, b));
        foreach (var a in new[] { double.NaN, double.NegativeInfinity, -0d, 0d, 1d, double.PositiveInfinity }) foreach (var b in new[] { double.NaN, -0d, 0d, 1d, double.PositiveInfinity }) foreach (var money in new[] { (0m, 0.00m), (1m, 1.00m), (decimal.MaxValue, decimal.MinValue) }) foreach (var name in new[] { "Numeric", "Plain", "Nested" })
                        Assert.Equal(rules.GetMethod(name)!.Invoke(null, [a, b, money.Item1, money.Item2]), Support.Call(lib, "D9.Rules", name, a, b, DecimalData(money.Item1), DecimalData(money.Item2)));
        Assert.True((bool)Support.Call(lib, "D9.Rules", "Null", Support.Maybe(null))); Assert.False((bool)Support.Call(lib, "D9.Rules", "Null", Support.Maybe(new Dictionary<string, object> { { "value", 0 } })));
        var identity = Support.Convert("namespace Example.D9;public sealed class Cell{public int Value;}public sealed record Holder(Cell Value);public static class Rules{public static bool F(Holder a,Holder b)=>a==b;}", selectors: ["Example.D9"]); Assert.Null(identity.Distribution); Assert.Contains(identity.Diagnostics, d => d.Code == "MCS3003");
    }
    [Fact]
    public void MixedClassStructAndTupleLocationsPreserveValueCopiesAndChildAliases()
    {
        const string code = """
        namespace Example.D9;
        public sealed class Cell{public int Value;}
        public struct Amount{public int Value;public Cell? Child;public int Property{get=>Value;set=>Value=value;}}
        public sealed class Holder{public Amount Amount;public (Amount,int) Pair;}
        public struct Outer{public Amount Amount;public (Amount,int) Pair;}
        public static class Rules{
          public static int ClassField(){var holder=new Holder();holder.Amount.Value=2;return holder.Amount.Value;}
          public static int ClassSetter(){var holder=new Holder();holder.Amount.Property=2;return holder.Amount.Value;}
          public static int TupleField(){var holder=new Holder();holder.Pair.Item1.Value=2;return holder.Pair.Item1.Value;}
          public static int TupleElement(){var holder=new Holder();holder.Pair.Item2=2;return holder.Pair.Item2;}
          public static int TupleSetter(){var holder=new Holder();holder.Pair.Item1.Property=2;return holder.Pair.Item1.Value;}
          public static int StructTuple(){var outer=new Outer();outer.Pair.Item1.Value=2;return outer.Pair.Item1.Value;}
          public static int LocalTuple(){var pair=(new Amount(),0);pair.Item1.Value=2;return pair.Item1.Value;}
          public static int Copy(){var outer=new Outer();var copy=outer;copy.Amount.Value=2;return outer.Amount.Value;}
          public static int ChildAlias(){var child=new Cell{Value=1};var outer=new Outer{Amount=new Amount{Child=child}};var copy=outer;copy.Amount.Value=3;child.Value=2;return copy.Amount.Child.Value;}
          public static int TupleAlias(){var child=new Cell{Value=1};var pair=(new Amount{Child=child},0);pair.Item1.Value=3;pair.Item1.Child.Value=2;return child.Value;}
          public static int ReceiverOrder(){var old=new Holder();var current=old;current.Amount.Value=(current=new Holder{Amount=new Amount{Value=3}}).Amount.Value;return old.Amount.Value;}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var name in new[] { "ClassField", "ClassSetter", "TupleField", "TupleElement", "TupleSetter", "StructTuple", "LocalTuple", "Copy", "ChildAlias", "TupleAlias", "ReceiverOrder" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(lib, "D9.Rules", name));
        var rejected = Support.Convert("namespace Example.D9;public struct Amount{public int Value;}public sealed class Holder{public Amount Amount;}public static class Rules{public static int F(Holder input){if(input==null)return 0;input.Amount.Value=2;return 0;}}", selectors: ["Example.D9"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, d => d.Code == "MCS3002");
    }
    [Fact]
    public void ConditionalUserOperatorsKeepShortCircuitStateAndCustomNullEqualityIsNotAGuard()
    {
        const string code = """
        namespace Example.D9;
        public struct Flag{
          public int Value;public Flag(int value){Value=value;}
          public static bool operator true(Flag value)=>value.Value!=0;
          public static bool operator false(Flag value)=>value.Value==0;
          public static Flag operator &(Flag a,Flag b)=>new Flag(a.Value & b.Value);
          public static Flag operator |(Flag a,Flag b)=>new Flag(a.Value | b.Value);
        }
        public static class Rules{
          public static int And(int value){var right=1;var result=new Flag(value)&&new Flag(right=2);return right;}
          public static int Or(int value){var right=1;var result=new Flag(value)||new Flag(right=2);return right;}
          public static int Value(int value){var result=new Flag(value)&&new Flag(2);return result.Value;}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var value in new[] { 0, 1, 2, 3 }) foreach (var name in new[] { "And", "Or", "Value" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D9.Rules", name, value));
        var rejected = Support.Convert("namespace Example.D9;public sealed class Cell{public int Value;public static bool operator ==(Cell? a,Cell? b)=>false;public static bool operator !=(Cell? a,Cell? b)=>true;}public static class Rules{public static int F(Cell? input){if(input==null)return 0;return input.Value;}}", selectors: ["Example.D9"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, d => d.Code == "MCS2501");
    }
    [Fact]
    public void RecursiveRecordEqualityAndResolvedCustomValueEqualityAreExact()
    {
        const string code = """
        namespace Example.D9;
        public sealed record Node(int Value){public Node? Next{get;init;}}
        public readonly record struct Parity(int Value){public bool Equals(Parity other)=>Value%2==other.Value%2;}
        public static class Rules{
          public static bool Recursive(int a,int b)=>new Node(1){Next=new Node(a)}==new Node(1){Next=new Node(b)};
          public static bool Custom(int a,int b)=>new Parity(a)==new Parity(b);
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var a in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var b in new[] { -1, 0, 1, 2 }) foreach (var name in new[] { "Recursive", "Custom" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [a, b]), Support.Call(lib, "D9.Rules", name, a, b));
    }
    [Fact]
    public void NestedMemberInitializersUseExistingOwnedChildrenAndStructLocations()
    {
        const string code = """
        namespace Example.D9;
        public sealed class Cell{public int Value;public int Property{get=>Value;set=>Value=value;}}
        public struct Amount{public int Value;public int Property{get=>Value;set=>Value=value;}}
        public sealed class Holder{public Cell Child=new Cell{Value=1};public Amount Amount;public (int,Amount) Pair;public System.Tuple<Cell> Tuple=System.Tuple.Create(new Cell{Value=1});public Cell Exposed=>Child;}
        public static class Rules{
          public static int Class()=>new Holder{Child={Value=2}}.Child.Value;
          public static int Setter()=>new Holder{Child={Property=2}}.Child.Value;
          public static int Getter()=>new Holder{Exposed={Value=2}}.Child.Value;
          public static int Struct()=>new Holder{Amount={Value=2}}.Amount.Value;
          public static int StructSetter()=>new Holder{Amount={Property=2}}.Amount.Value;
          public static int Tuple()=>new Holder{Pair={Item1=2}}.Pair.Item1;
          public static int NestedTuple()=>new Holder{Pair={Item2={Value=2}}}.Pair.Item2.Value;
          public static int ReferenceTuple()=>new Holder{Tuple={Item1={Value=2}}}.Tuple.Item1.Value;
          public static int With(){var a=new Amount{Value=1};var b=a with{Value=2};return a.Value;}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var name in new[] { "Class", "Setter", "Getter", "Struct", "StructSetter", "Tuple", "NestedTuple", "ReferenceTuple", "With" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(lib, "D9.Rules", name));
        var rejected = Support.Convert("namespace Example.D9;public sealed class Cell{public int Value;}public sealed class Holder{public Cell? Child;public Holder(Cell input){Child=input;}}public static class Rules{public static int F(Cell input)=>new Holder(input){Child={Value=2}}.Child.Value;}", selectors: ["Example.D9"]); Assert.Null(rejected.Distribution); Assert.NotEmpty(rejected.Diagnostics);
    }
    [Fact]
    public void PrimaryCaptureAssignmentsUpdateConstructorStateAndRejectInputReceiverMutation()
    {
        const string code = """
        namespace Example.D9;
        public sealed class Counter(int initial){private int written=(initial=2);public int Value=>initial;public int Captured{get=>initial;set=>initial=value;}public int Echo(int initial){initial=3;return initial;}}
        public struct Amount(int initial){private int written=(initial=2);public int Value=>initial;}
        public static class Rules{
          public static int Class(int value)=>new Counter(value).Value;
          public static int Setter(int value){var counter=new Counter(value);counter.Captured=4;return counter.Value;}
          public static int Shadow(int value)=>new Counter(value).Echo(5);
          public static int Struct(int value)=>new Amount(value).Value;
          public static int Default()=>default(Amount).Value;
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, int.MaxValue }) foreach (var name in new[] { "Class", "Struct", "Setter", "Shadow" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D9.Rules", name, value));
        foreach (var name in new[] { "Default" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(lib, "D9.Rules", name));
        var rejected = Support.Convert("namespace Example.D9;public sealed class Counter(int initial){public int Change(){initial=2;return initial;}}", selectors: ["Example.D9"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, d => d.Code == "MCS3002");
    }
    [Fact]
    public void HistoricalD1WithFixtureNowCopiesTheInputValueUnderD9()
    {
        var source = Support.Read("fixtures/D1/rejected/InputUpdate.cs"); var lib = Support.Success(Support.Convert(source)); var input = new Dictionary<string, object> { { "age", 18 }, { "active", true } };
        var result = (IReadOnlyDictionary<string, object>)Support.Call(lib, "D1.Rules", "Update", input); Assert.Equal(20, result["age"]); Assert.Equal(true, result["active"]); Assert.Equal(18, input["age"]);
        var assembly = Support.Emit(source); var customer = assembly.GetType("Example.D1.Customer")!; var native = assembly.GetType("Example.D1.Rules")!.GetMethod("Update")!.Invoke(null, [Activator.CreateInstance(customer, [18, true])]); Assert.Equal(customer.GetProperty("Age")!.GetValue(native), result["age"]);
    }
    [Fact]
    public void CumulativeDataSampleMatchesNativeConstructionReadsUpdatesAndOperators()
    {
        var source = Support.Read("samples/CurrentSupportedExample/Data.cs"); var lib = Support.Success(Support.Convert(source, selectors: ["Example.CurrentSupported"])); var assembly = Support.Emit(source); var rules = assembly.GetType("Example.CurrentSupported.CurrentSupportedExample")!;
        foreach (var value in new[] { int.MinValue, 0, int.MaxValue })
        {
            foreach (var name in new[] { "DataConstructed", "DataWith", "DataOperator", "DataConvert" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "CurrentSupported.CurrentSupportedExample", name, value));
            foreach (var suffix in new[] { "Class", "Struct", "Readonly", "Record", "Value", "ReadonlyRecord" })
            {
                var native = rules.GetMethod("Make" + suffix)!.Invoke(null, [value])!; var actual = Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "Make" + suffix, value); var map = (IReadOnlyDictionary<string, object>)(actual is Support.MaybeData maybe ? maybe.Value! : actual);
                Assert.Equal(value, map["value"]); Assert.Equal(rules.GetMethod("Read" + suffix)!.Invoke(null, [native]), Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "Read" + suffix, actual));
            }
            foreach (var other in new[] { int.MinValue, 0, int.MaxValue }) Assert.Equal(rules.GetMethod("DataRecordsEqual")!.Invoke(null, [value, other]), Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "DataRecordsEqual", value, other));
        }
        Assert.Equal(2, Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "DataAlias"));
    }
    [Fact]
    public void StaticPurePropertiesAndClosedOverridesUseTheirResolvedImplementations()
    {
        const string code = """
        namespace Example.D9;
        public struct Amount{public int Value;public override int GetHashCode()=>Value;public override string ToString()=>"amount";}
        public sealed class Cell{public int Value;public override int GetHashCode()=>Value;}
        public sealed record Named(int Value){public bool Equals(Named? other)=>other is not null&&Value==other.Value;}
        public static class Rules{
          private static int Constant=>18;
          private static Cell Fresh=>new Cell{Value=1};
          public static int Static()=>Constant;
          public static int Factory(){var value=Fresh;value.Value=2;return value.Value;}
          public static int Struct(int value)=>new Amount{Value=value}.GetHashCode();
          public static int Class(int value)=>new Cell{Value=value}.GetHashCode();
          public static int String()=>new Amount().ToString().Length;
          public static bool Custom(int a,int b)=>new Named(a).Equals(new Named(b));
          private static bool Identity(bool value)=>value;
          public static bool NestedGuard(Cell? value)=>Identity(value is not null&&value.Value>0);
          public static bool NegatedGuard(Cell? value)=>!(value is null||value.Value<=0);
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var name in new[] { "Static", "Factory", "String" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(lib, "D9.Rules", name));
        foreach (var value in new[] { int.MinValue, 0, int.MaxValue })
        { foreach (var name in new[] { "Struct", "Class" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D9.Rules", name, value)); foreach (var other in new[] { int.MinValue, 0, int.MaxValue }) Assert.Equal(rules.GetMethod("Custom")!.Invoke(null, [value, other]), Support.Call(lib, "D9.Rules", "Custom", value, other)); }
        foreach (var name in new[] { "NestedGuard", "NegatedGuard" })
        { Assert.False((bool)Support.Call(lib, "D9.Rules", name, Support.Maybe(null))); Assert.True((bool)Support.Call(lib, "D9.Rules", name, Support.Maybe(new Dictionary<string, object> { { "value", 1 } }))); }
        var hidden = Support.Convert("namespace Example.D9;public static class Rules{private static int Hidden=>System.Environment.TickCount;public static int F()=>1;}", selectors: ["Example.D9"]); Assert.Null(hidden.Distribution); Assert.NotEmpty(hidden.Diagnostics);
    }
    [Fact]
    public void RecordWithRunsExplicitCopyConstructorsAndTheirActualInitializationPolicy()
    {
        const string code = """
        namespace Example.D9;
        public sealed record Counter{
          public int Value=1;public int Other=2;
          public Counter(){}
          private Counter(Counter original){if(original is not null)Value=original.Value+1;}
        }
        public static class Rules{
          public static int Copy(int value){var original=new Counter{Value=value};var copy=original with{};return copy.Value;}
          public static int Original(int value){var original=new Counter{Value=value};var copy=original with{};return original.Value;}
          public static int Override(int value){var original=new Counter{Value=value};var copy=original with{Value=3};return copy.Value;}
          public static int Initializers(){var original=new Counter();var copy=original with{};return copy.Other;}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, int.MaxValue }) foreach (var name in new[] { "Copy", "Original", "Override" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D9.Rules", name, value));
        Assert.Equal(rules.GetMethod("Initializers")!.Invoke(null, null), Support.Call(lib, "D9.Rules", "Initializers"));
    }
    [Fact]
    public void WholeStructConstructionAndSetterUpdatesRetainTheirActualValueLocations()
    {
        const string code = """
        namespace Example.D9;
        public struct Amount{public int Value;public Amount(int value){this=default;Value=value;}public Amount(Amount other){this=other;}public int Property{get=>Value;set{this=new Amount(value);}}}
        public readonly struct ReadonlyAmount{public readonly int Value;public ReadonlyAmount(int value){this=default;Value=value;}}
        public static class Rules{
          public static int Construct(int value)=>new Amount(value).Value;
          public static int Copy(int value){var first=new Amount(value);var copy=new Amount(first);first.Value=2;return copy.Value;}
          public static int Setter(int value){var first=new Amount(1);first.Property=value;return first.Value;}
          public static int Readonly(int value)=>new ReadonlyAmount(value).Value;
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, int.MaxValue }) foreach (var name in new[] { "Construct", "Copy", "Setter", "Readonly" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D9.Rules", name, value));
        var collisions = Support.Convert("namespace Example.D9;public sealed class Cell{public int Value;public int value;}", selectors: ["Example.D9"]); Assert.Null(collisions.Distribution); Assert.True(collisions.Diagnostics.Count(d => d.Code == "MCS1004") >= 2); Assert.True(collisions.Diagnostics.Where(d => d.Code == "MCS1004").Select(d => d.Column).Distinct().Count() >= 2);
    }
    [Fact]
    public void DefaultValueEqualityDistinguishesBoxedTypesAndComparesReferenceTuplesByValue()
    {
        const string code = """
        namespace Example.D9;
        public struct Amount{public int Value;}
        public struct Other{public int Value;}
        public readonly record struct TupleHolder(System.Tuple<double,int>? Value);
        public static class Rules{
          public static bool Integer(int value)=>new Amount{Value=value}.Equals(value);
          public static bool Other(int value)=>new Amount{Value=value}.Equals(new Other{Value=value});
          public static bool Nullable(Amount value,Amount? other)=>value.Equals(other);
          public static bool Tuple(double a,double b)=>new TupleHolder(System.Tuple.Create(a,1))==new TupleHolder(System.Tuple.Create(b,1));
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var assembly = Support.Emit(code); var rules = assembly.GetType("Example.D9.Rules")!; var amount = assembly.GetType("Example.D9.Amount")!;
        foreach (var value in new[] { int.MinValue, 0, int.MaxValue }) foreach (var name in new[] { "Integer", "Other" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D9.Rules", name, value));
        foreach (int? other in new int?[] { null, 0, 1 })
        { var native = Activator.CreateInstance(amount)!; amount.GetField("Value")!.SetValue(native, 0); object? rhs = null; if (other.HasValue) { rhs = Activator.CreateInstance(amount)!; amount.GetField("Value")!.SetValue(rhs, other.Value); } Assert.Equal(rules.GetMethod("Nullable")!.Invoke(null, [native, rhs]), Support.Call(lib, "D9.Rules", "Nullable", new Dictionary<string, object> { { "value", 0 } }, Support.Maybe(other.HasValue ? new Dictionary<string, object> { { "value", other.Value } } : null))); }
        foreach (var a in new[] { double.NaN, 0d, 1d }) foreach (var b in new[] { double.NaN, -0d, 1d }) Assert.Equal(rules.GetMethod("Tuple")!.Invoke(null, [a, b]), Support.Call(lib, "D9.Rules", "Tuple", a, b));
    }
    [Fact]
    public void EqualityReadsCurrentReferenceGraphsAfterAllArgumentsAreEvaluated()
    {
        const string code = """
        namespace Example.D9;
        public sealed record Cell{public int Value{get;set;}}
        public record struct Holder(Cell? Child);
        public static class Rules{
          public static bool Record(){var first=new Cell{Value=1};return first==new Cell{Value=(first.Value=2)};}
          public static bool Wrapper(){var child=new Cell{Value=1};var first=new Holder(child);return first==new Holder(new Cell{Value=(child.Value=2)});}
          public static bool Call(){var child=new Cell{Value=1};var first=new Holder(child);return first.Equals(new Holder(new Cell{Value=(child.Value=2)}));}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var name in new[] { "Record", "Wrapper", "Call" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(lib, "D9.Rules", name));
        var rejected = Support.Convert("namespace Example.D9;public struct Amount{public int Value;}public sealed class Cell{public int Value;}public static class Rules{public static bool F(Amount value,Cell? input)=>value.Equals(input.Value);}", selectors: ["Example.D9"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, d => d.Code == "MCS2501");
    }
    [Fact]
    public void SelectedConstantsRetainSourceDefinitionsExactDomainsAndNamespaceDependencies()
    {
        const string code = """
        namespace Example.D9;
        public sealed class Limits{
          public const int Minimum=18;private const bool Enabled=true;
          public const string Label="adult";public const string? Missing=null;
          public const decimal Money=1.00m;public const long Wide=long.MaxValue;public const double Nan=double.NaN;
        }
        public static class Rules{
          public const int One=1;
          public static int Age()=>Limits.Minimum;
          public static int Length()=>Limits.Label.Length;
          public static int Missing()=>Limits.Missing is null?1:0;
          public static int Safe()=>100/Limits.Minimum;
          public static int Adjusted()=>One+Limits.Minimum;
          public static long Wide()=>Limits.Wide;
          public static decimal Money()=>Limits.Money;
          public static bool Nan()=>double.IsNaN(Limits.Nan);
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var name in new[] { "Age", "Length", "Missing", "Safe", "Adjusted", "Nan" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(lib, "D9.Rules", name));
        Assert.Equal(new System.Numerics.BigInteger(long.MaxValue), Support.IntegerResult(Support.Call(lib, "D9.Rules", "Wide")));
        var money = (IReadOnlyDictionary<string, object>)Support.Call(lib, "D9.Rules", "Money"); Assert.Equal(2, money["scale"]); Assert.Equal(1.00m, money["value"]);
        var limits = lib.Definition.Modules.Single(module => module.Key.Key == Morphir.IR.Classic.Path.From("D9.Limits").Key).Value.Value; Assert.Contains(limits.Values, value => value.Key.Key == Morphir.IR.Classic.Name.From("Enabled").Key && value.Value.Access == Morphir.IR.Classic.Access.Private);
        const string dependency = "namespace Other{public static class Limits{public const int Age=18;}}namespace Example.D9{public static class Rules{public static int F()=>Other.Limits.Age;}}";
        var missing = Support.Convert(dependency, selectors: ["Example.D9"]); Assert.Null(missing.Distribution); Assert.Contains(missing.Diagnostics, d => d.Code == "MCS2201" && d.Message.Contains("Other"));
        Assert.Equal(18, Support.Call(Support.Success(Support.Convert(dependency, selectors: ["Example.D9", "Other"])), "D9.Rules", "F"));
    }
    [Fact]
    public void GuardedBorrowedRecursiveAndTupleMembersKeepStablePresenceProofs()
    {
        const string code = """
        namespace Example.D9;
        public sealed class Node{public int Value;public Node? Next;}
        public static class Rules{
          public static int Recursive(Node? node){if(node is null||node.Next is null)return 0;return node.Next.Value;}
          public static int Tuple((Node?,int) pair){if(pair.Item1 is null)return 0;return pair.Item1.Value;}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"]));
        var leaf = Support.Maybe(Support.Enum("D9", "Node", new Dictionary<string, object> { { "value", 7 }, { "next", Support.Maybe(null) } }));
        var root = Support.Maybe(Support.Enum("D9", "Node", new Dictionary<string, object> { { "value", 1 }, { "next", leaf } }));
        Assert.Equal(7, Support.Call(lib, "D9.Rules", "Recursive", root)); Assert.Equal(0, Support.Call(lib, "D9.Rules", "Recursive", leaf)); Assert.Equal(0, Support.Call(lib, "D9.Rules", "Recursive", Support.Maybe(null)));
        Assert.Equal(7, Support.Call(lib, "D9.Rules", "Tuple", (object)new object[] { leaf, 0 })); Assert.Equal(0, Support.Call(lib, "D9.Rules", "Tuple", (object)new object[] { Support.Maybe(null), 0 }));
    }
    [Fact]
    public void ExplicitClassValueEqualityOperatorsPreserveNullAndValueSemantics()
    {
        const string code = """
        namespace Example.D9;
        public sealed class Cell{
          public int Value;
          public static bool operator ==(Cell? a,Cell? b){if(a is null)return b is null;if(b is null)return false;return a.Value==b.Value;}
          public static bool operator !=(Cell? a,Cell? b)=>!(a==b);
        }
        public static class Rules{public static bool Equal(Cell? a,Cell? b)=>a==b;public static bool Unequal(Cell? a,Cell? b)=>a!=b;}
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var assembly = Support.Emit(code); var cell = assembly.GetType("Example.D9.Cell")!; var rules = assembly.GetType("Example.D9.Rules")!;
        foreach (int? a in new int?[] { null, 0, 1 }) foreach (int? b in new int?[] { null, 0, 1 }) foreach (var name in new[] { "Equal", "Unequal" })
                {
                    object? Native(int? value) { if (!value.HasValue) return null; var instance = Activator.CreateInstance(cell)!; cell.GetField("Value")!.SetValue(instance, value.Value); return instance; }
                    object Ir(int? value) => Support.Maybe(value.HasValue ? new Dictionary<string, object> { { "value", value.Value } } : null);
                    Assert.Equal(rules.GetMethod(name)!.Invoke(null, [Native(a), Native(b)]), Support.Call(lib, "D9.Rules", name, Ir(a), Ir(b)));
                }
    }
    [Fact]
    public void DirectSynthesizedRecordCopyCallsKeepIndependentRootsAndSharedChildren()
    {
        const string code = """
        namespace Example.D9;
        public sealed record Cell{
          public int Value;public Cell? Child;
          public static Cell Copy(Cell? source){if(source is null)return new Cell();return new Cell(source);}
        }
        public static class Rules{
          public static int Copy(int value){var original=new Cell{Value=value};var copy=Cell.Copy(original);copy.Value=2;return original.Value;}
          public static int Read(int value)=>Cell.Copy(new Cell{Value=value}).Value;
          public static int Alias(){var child=new Cell{Value=1};var original=new Cell{Child=child};var copy=Cell.Copy(original);copy.Child.Value=2;return child.Value;}
          public static int Null()=>Cell.Copy(null).Value;
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D9"])); var rules = Support.Emit(code).GetType("Example.D9.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, int.MaxValue }) foreach (var name in new[] { "Copy", "Read" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D9.Rules", name, value));
        foreach (var name in new[] { "Alias", "Null" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(lib, "D9.Rules", name));
    }
    private static object DecimalData(decimal value)
    {
        var bits = decimal.GetBits(value); var coefficient = (System.Numerics.BigInteger)(uint)bits[0] + ((System.Numerics.BigInteger)(uint)bits[1] << 32) + ((System.Numerics.BigInteger)(uint)bits[2] << 64);
        return new Dictionary<string, object> { { "coefficient", Support.Integer(coefficient) }, { "scale", (bits[3] >> 16) & 255 }, { "negative", bits[3] < 0 }, { "value", value } };
    }
    [Theory]
    [InlineData("public static int F(Counter input){if(input==null)return 0;input.Value=2;return input.Value;}", "MCS3002")]
    [InlineData("public static int F(Counter input){var alias=input;if(alias==null)return 0;alias.Value=2;return alias.Value;}", "MCS3002")]
    [InlineData("public static int F(Counter input)=>input.Value;", "MCS2501")]
    [InlineData("public static int F(Counter input){if(false)input.Value=2;return 0;}", "MCS3002")]
    [InlineData("public static bool F(Counter first,Counter second)=>first==second;", "MCS3003")]
    public void BorrowedMutationsUnsafeReadsAndIdentityReject(string method, string code)
    {
        var result = Support.Convert("namespace Example.D9;public sealed class Counter{public int Value;}public static class Rules{" + method + "}", selectors: ["Example.D9"]);
        Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, d => d.Code == code && d.File.Length > 0 && d.Line > 0);
    }
}
