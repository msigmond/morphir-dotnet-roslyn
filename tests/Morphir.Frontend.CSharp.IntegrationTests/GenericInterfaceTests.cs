using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class GenericInterfaceTests
{
    [Fact]
    public void GenericInterfaceLayoutsAndDefaultWitnessesHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/generic-interfaces.json", Support.Read("fixtures/D10/csharp/GenericInterfaces.cs"), "Example", "Example.D10");

    [Fact]
    public void GenericInterfaceMembersHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/generic-members.json", Support.Read("fixtures/D10/csharp/GenericMembers.cs"), "Example", "Example.D10");

    [Fact]
    public void ParametricInterfaceLayoutsSubstituteArgumentsAndConstrainedCallbacks()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue<T>{T Read();}
        public readonly record struct ValueHolder<U>(U Value):IValue<U>{public U Read()=>Value;}
        public sealed class ReferenceHolder<U>:IValue<U>{public U Value;public U Read()=>Value;}
        public static class Rules{
          private static T Read<T>(IValue<T>? value,T missing){if(value is null)return missing;return value.Read();}
          private static TResult Constrained<TResult,TSource>(TSource source)where TSource:struct,IValue<TResult> =>source.Read();
          private static TResult Forward<TResult,TSource>(TSource source)where TSource:struct,IValue<TResult> =>Constrained<TResult,TSource>(source);
          public static int Integer(int value)=>Read<int>(new ValueHolder<int>(value),0);
          public static bool Boolean(bool value)=>Read<bool>(new ReferenceHolder<bool>{Value=value},false);
          public static string? Text(string? value)=>Read<string?>(new ValueHolder<string?>(value),null);
          public static int Callback(int value)=>Forward<int,ValueHolder<int>>(new ValueHolder<int>(value));
          public static int Missing()=>Read<int>(null,3);
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, 18, int.MaxValue }) foreach (var name in new[] { "Integer", "Callback" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(library, "D10.Rules", name, value));
        foreach (var value in new[] { false, true }) Assert.Equal(value, Support.Call(library, "D10.Rules", "Boolean", value));
        foreach (var value in new string?[] { null, "", "hello" }) Assert.Equal(Support.Maybe(value), Support.Call(library, "D10.Rules", "Text", Support.Maybe(value)));
        Assert.Equal(3, Support.Call(library, "D10.Rules", "Missing"));
    }

    [Fact]
    public void InterfaceTargetsForwardRequiredDefaultWitnesses()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue<T>{T Read();}
        public readonly record struct DefaultHolder<U>(int Ignored):IValue<U>{public U Read()=>default;}
        public static class Rules{
          private static T Read<T>(IValue<T>? value,T missing){if(value is null)return missing;return value.Read();}
          private static T Callback<T,TSource>(TSource source)where TSource:struct,IValue<T> =>source.Read();
          public static int Integer()=>Read<int>(new DefaultHolder<int>(0),2);
          public static string? Text()=>Read<string?>(new DefaultHolder<string?>(0),"missing");
          public static int Constrained()=>Callback<int,DefaultHolder<int>>(new DefaultHolder<int>(0));
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        Assert.Equal(0, Support.Call(library, "D10.Rules", "Integer")); Assert.Equal(0, Support.Call(library, "D10.Rules", "Constrained")); Assert.Equal(Support.Maybe(null), Support.Call(library, "D10.Rules", "Text"));
    }

    [Fact]
    public void GenericInterfaceResultsKeepReferenceOwnershipThroughTypeProofs()
    {
        const string declarations = """
        namespace Example.D10;
        public sealed class Counter{public int Value;}
        public interface IValue<T>{T Read();}
        public readonly record struct ValueHolder<U>(U Value):IValue<U>{public U Read()=>Value;}
        public static class Rules{
          private static T Read<T>(IValue<T>? box,T missing){if(box is null)return missing;return box.Read();}
          private static T Cast<T>(IValue<T>? box,T missing){if(box is ValueHolder<T> holder)return holder.Read();return missing;}
        """;
        var code = declarations + """
          public static int Own(){var child=new Counter{Value=1};IValue<Counter> box=new ValueHolder<Counter>(child);var alias=Read(box,null);if(alias is null)return 0;alias.Value=2;return child.Value;}
          public static int Proven(){var child=new Counter{Value=1};IValue<Counter> box=new ValueHolder<Counter>(child);var alias=Cast(box,null);if(alias is null)return 0;alias.Value=2;return child.Value;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var name in new[] { "Own", "Proven" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, null), Support.Call(library, "D10.Rules", name));
        var rejected = Support.Convert(declarations + "public static int Borrowed(IValue<Counter> input){if(input is null)return 0;var child=input.Read();if(child is null)return 0;child.Value=2;return 0;}}", selectors: ["Example.D10"]);
        Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS3002");
    }

    [Fact]
    public void GenericInterfaceCreationTargetsForwardFreshAllocationWitnesses()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Counter{public int Value;}
        public interface IFactory<T>where T:new(){T Create();}
        public readonly record struct Factory<U>(int Ignored):IFactory<U>where U:new(){public U Create()=>new U();}
        public static class Rules{
          private static T Create<T>(IFactory<T>? factory)where T:new(){if(factory is null)return new T();return factory.Create();}
          public static int Fresh(){IFactory<Counter> factory=new Factory<Counter>(0);var first=Create(factory);var second=Create(factory);first.Value=2;return second.Value;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        Assert.Equal(rules.GetMethod("Fresh")!.Invoke(null, null), Support.Call(library, "D10.Rules", "Fresh"));
    }

    [Fact]
    public void GenericMembersRetainMethodArgumentsAndDistinctConstrainedCallbacks()
    {
        const string code = """
        namespace Example.D10;
        public interface IMapper{T Echo<T>(T value);T Zero<T>();}
        public readonly record struct Mapper(int Ignored):IMapper{public U Echo<U>(U value)=>value;public U Zero<U>()=>default;}
        public static class Rules{
          private static T Echo<T>(IMapper? mapper,T value){if(mapper is null)return value;return mapper.Echo(value);}
          private static T Zero<T>(IMapper? mapper){if(mapper is null)return default;return mapper.Zero<T>();}
          private static T Callback<T,TSource>(TSource mapper,T value)where TSource:struct,IMapper=>mapper.Echo<T>(value);
          private static bool Two<TSource>(TSource mapper,int value,bool flag)where TSource:struct,IMapper=>mapper.Echo<int>(value)==value&&mapper.Echo<bool>(flag)==flag;
          public static int Integer(int value)=>Echo<int>(new Mapper(0),value);
          public static string? Text(string? value)=>Echo<string?>(new Mapper(0),value);
          public static int Default()=>Zero<int>(new Mapper(0));
          public static int Constrained(int value)=>Callback<int,Mapper>(new Mapper(0),value);
          public static bool Distinct(int value,bool flag)=>Two(new Mapper(0),value,flag);
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, 18, int.MaxValue }) foreach (var name in new[] { "Integer", "Constrained" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(library, "D10.Rules", name, value));
        foreach (var value in new string?[] { null, "", "hello" }) Assert.Equal(Support.Maybe(value), Support.Call(library, "D10.Rules", "Text", Support.Maybe(value)));
        foreach (var value in new[] { int.MinValue, 0, int.MaxValue }) foreach (var flag in new[] { false, true }) Assert.Equal(true, Support.Call(library, "D10.Rules", "Distinct", value, flag));
        Assert.Equal(0, Support.Call(library, "D10.Rules", "Default"));
    }

    [Fact]
    public void ExplicitInterfaceTargetsKeepTheirSourceOwnersAndPrivateAccess()
    {
        const string code = """
        namespace Example.D10;
        public interface IValue<T>{T Read();}
        public readonly record struct ValueHolder<U>(U Value):IValue<U>{U IValue<U>.Read()=>Value;}
        public sealed class ReferenceHolder<U>:IValue<U>{public U Value;U IValue<U>.Read()=>Value;}
        public static class Rules{
          private static T Read<T>(IValue<T>? box,T missing){if(box is null)return missing;return box.Read();}
          public static int Value(int value)=>Read<int>(new ValueHolder<int>(value),0);
          public static bool Reference(bool value)=>Read<bool>(new ReferenceHolder<bool>{Value=value},false);
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, 0, 18, int.MaxValue }) Assert.Equal(rules.GetMethod("Value")!.Invoke(null, [value]), Support.Call(library, "D10.Rules", "Value", value));
        foreach (var value in new[] { false, true }) Assert.Equal(value, Support.Call(library, "D10.Rules", "Reference", value));
        foreach (var owner in new[] { "ValueHolder", "ReferenceHolder" })
        {
            var module = library.Definition.Modules.Single(item => item.Key.Key == Morphir.IR.Classic.Path.From("D10." + owner).Key).Value.Value;
            Assert.Contains(module.Values, item => item.Value.Access == Morphir.IR.Classic.Access.Private);
        }
    }

    [Fact]
    public void GenericDefaultTargetsResolveAcrossAcquiredSourceCompilations()
    {
        var contracts = Support.Compile("namespace Example.D10;public interface IValue<T>{T Read();T Alias()=>Read();}").WithAssemblyName("GenericContracts");
        var consumer = Support.Compile("""
        namespace Example.D10;
        public readonly record struct Holder<U>(U Value):IValue<U>{public U Read()=>Value;}
        public static class Rules{public static int Read(int value){IValue<int> box=new Holder<int>(value);return box.Alias();}}
        """).WithAssemblyName("GenericConsumer").AddReferences(contracts.ToMetadataReference());
        var library = Support.Success(Morphir.Frontend.CSharp.Frontend.Convert(consumer, new("Example", ["Example.D10"]), [contracts]));
        foreach (var value in new[] { int.MinValue, 0, 18, int.MaxValue }) Assert.Equal(value, Support.Call(library, "D10.Rules", "Read", value));
    }
}
