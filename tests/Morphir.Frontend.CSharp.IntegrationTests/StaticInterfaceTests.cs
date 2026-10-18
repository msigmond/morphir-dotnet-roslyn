using System.Numerics;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D10")]
public class StaticInterfaceTests
{
    [Fact]
    public void StaticInterfacesHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/static-interfaces.json", Support.Read("fixtures/D10/csharp/StaticInterfaces.cs"), "Example", "Example.D10");

    [Fact]
    public void StaticPropertiesHaveCompleteIndependentParity() => Support.Parity("fixtures/D10/expected/static-properties.json", Support.Read("fixtures/D10/csharp/StaticProperties.cs"), "Example", "Example.D10");

    [Fact]
    public void StaticAbstractAndDefaultMethodsUseResolvedTypeDirectedCallbacks()
    {
        const string code = """
        namespace Example.D10;
        public interface IFactory {
          static abstract int Make(int value);
          static abstract int Zero();
          static virtual bool Negative(int value)=>value<0;
          public static int Constant()=>7;
        }
        public readonly record struct Keep(int Ignored):IFactory {
          public static int Make(int value)=>value;
          public static int Zero()=>0;
        }
        public sealed class Reverse:IFactory {
          public static int Make(int value)=>-value;
          public static int Zero()=>7;
          public static bool Negative(int value)=>value>=0;
        }
        public static class Rules {
          public static int Call<T>(int value) where T:IFactory=>T.Make(value);
          public static int Forward<T>(int value) where T:IFactory=>Call<T>(value);
          public static int Zero<T>() where T:IFactory=>T.Zero();
          public static bool Negative<T>(int value) where T:IFactory=>T.Negative(value);
          public static int Reference<T>(int value) where T:IFactory {System.Func<int,int> function=T.Make;return function(value);}
          public static int ZeroReference<T>() where T:IFactory {System.Func<int> function=T.Zero;return function();}
          public static int A(int value)=>Forward<Keep>(value);
          public static int B(int value)=>Forward<Reverse>(value);
          public static int RA(int value)=>Reference<Keep>(value);
          public static int RB(int value)=>Reference<Reverse>(value);
          public static bool NA(int value)=>Negative<Keep>(value);
          public static bool NB(int value)=>Negative<Reverse>(value);
          public static int ZA()=>Zero<Keep>();
          public static int ZB()=>Zero<Reverse>();
          public static int ZRA()=>ZeroReference<Keep>();
          public static int ZRB()=>ZeroReference<Reverse>();
          public static int Constant()=>IFactory.Constant();
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "A", "B", "RA", "RB", "NA", "NB" })
            {
                var expected = rules.GetMethod(name)!.Invoke(null, [value]); var actual = Support.Call(library, "D10.Rules", name, value);
                if (expected is bool flag) Assert.Equal(flag, actual); else Assert.Equal(new BigInteger((int)expected!), new BigInteger(Convert.ToInt64(actual)));
            }
        foreach (var name in new[] { "ZA", "ZB", "ZRA", "ZRB", "Constant" }) Assert.Equal(new BigInteger((int)rules.GetMethod(name)!.Invoke(null, null)!), new BigInteger(Convert.ToInt64(Support.Call(library, "D10.Rules", name))));
    }

    [Fact]
    public void StaticConstraintTargetsKeepEffectOwnershipRecursionAndClosureGates()
    {
        foreach (var (source, diagnostic) in new[] {
          ("namespace Example.D10;public interface IFactory{static abstract int Make(int value);}public readonly record struct Maker(int Ignored):IFactory{public static int Make(int value){System.Console.WriteLine(value);return value;}}", "MCS2202"),
          ("namespace Example.D10;public interface IFactory{static abstract int Make(int value);}public readonly record struct Maker(int Ignored):IFactory{public static int Make(int value)=>Rules.Call<Maker>(value);}public static class Rules{public static int Call<T>(int value) where T:IFactory=>T.Make(value);}", "MCS2203"),
          ("namespace Example.D10{public interface IFactory{static abstract int Make(int value);}}namespace Outside{public readonly record struct Maker(int Ignored):Example.D10.IFactory{public static int Make(int value)=>value;}}", "MCS3111"),
          ("namespace Example.D10;public sealed class Box{public int Value;}public interface IFactory{static abstract Box Make(Box value);}public readonly record struct Maker(int Ignored):IFactory{public static Box Make(Box value){value.Value=1;return value;}}", "MCS3002")})
        {
            var result = Support.Convert(source, selectors: ["Example.D10"]); Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, item => item.Code == diagnostic);
        }
    }

    [Fact]
    public void GenericStaticMembersContractsPrivateTargetsAndDefaultWitnessesRetainTheirTypes()
    {
        const string code = """
        namespace Example.D10;
        public interface IMapper {
          static abstract U Echo<U>(U value);
          static virtual U Zero<U>()=>default;
        }
        public readonly record struct Mapper(int Ignored):IMapper {
          static U IMapper.Echo<U>(U value)=>value;
        }
        public interface IReader<T> {static abstract T Read(T value);}
        public readonly record struct Reader<T>(T Ignored):IReader<T> {public static T Read(T value)=>value;}
        public static class Rules {
          public static int Echo<P>(int value) where P:IMapper=>P.Echo<int>(value);
          public static string? Text<P>(string? value) where P:IMapper=>P.Echo<string?>(value);
          public static int Zero<P>() where P:IMapper=>P.Zero<int>();
          public static int Read<P>(int value) where P:IReader<int> =>P.Read(value);
          public static string? ReadText<P>(string? value) where P:IReader<string?> =>P.Read(value);
          public static int Number(int value)=>Echo<Mapper>(value);
          public static string? Identity(string? value)=>Text<Mapper>(value);
          public static int Default()=>Zero<Mapper>();
          public static int Generic(int value)=>Read<Reader<int>>(value);
          public static string? GenericText(string? value)=>ReadText<Reader<string?>>(value);
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"]));
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) foreach (var name in new[] { "Number", "Generic" }) Assert.Equal(new BigInteger(value), new BigInteger(Convert.ToInt64(Support.Call(library, "D10.Rules", name, value))));
        foreach (var value in new string?[] { null, "", "sample" }) foreach (var name in new[] { "Identity", "GenericText" }) Assert.Equal(Support.Maybe(value), Support.Call(library, "D10.Rules", name, Support.Maybe(value)));
        Assert.Equal(0, Convert.ToInt32(Support.Call(library, "D10.Rules", "Default")));
    }

    [Fact]
    public void ReadonlyStaticInterfacePropertiesResolvePrivateDefaultAndInheritedGetters()
    {
        const string code = """
        namespace Example.D10;
        public interface IFactory {static abstract int Value {get;} static virtual bool Ready=>true;}
        public interface IChild:IFactory {}
        public sealed class First:IChild {public static int Value=>7;}
        public sealed class Second:IFactory {static int IFactory.Value=>9;public static bool Ready=>false;}
        public static class Rules {
          public static int Read<T>() where T:IFactory=>T.Value;
          public static int Child<T>() where T:IChild=>T.Value;
          public static bool Ready<T>() where T:IFactory=>T.Ready;
          public static int A()=>Read<First>();
          public static int B()=>Read<Second>();
          public static int C()=>Child<First>();
          public static bool RA()=>Ready<First>();
          public static bool RB()=>Ready<Second>();
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var name in new[] { "A", "B", "C" }) Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, null)!, Convert.ToInt32(Support.Call(library, "D10.Rules", name)));
        foreach (var name in new[] { "RA", "RB" }) Assert.Equal((bool)rules.GetMethod(name)!.Invoke(null, null)!, Support.Call(library, "D10.Rules", name));
        var rejected = Support.Convert("namespace Example.D10;public interface IFactory{static abstract int Value{get;set;}}public sealed class Maker:IFactory{public static int Value{get;set;}}", selectors: ["Example.D10"]);
        Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS3110");
    }

    [Fact]
    public void StaticSourceResultsPreserveReturnedAliasesFreshnessAndOwnedWrapperGraphs()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Box {public int Value;public Box(int value){Value=value;}}
        public readonly record struct Wrapper(Box Value);
        public interface IFactory {
          static abstract Box Select(Box value);
          static abstract Box Fresh {get;}
          static abstract Wrapper Wrap(Box value);
        }
        public readonly record struct Keep(int Ignored):IFactory {
          public static Box Select(Box value)=>value;
          public static Box Fresh=>new Box(7);
          public static Wrapper Wrap(Box value)=>new Wrapper(value);
        }
        public static class Rules {
          public static Box Select<T>(Box value) where T:IFactory=>T.Select(value);
          public static Box Fresh<T>() where T:IFactory=>T.Fresh;
          public static Wrapper Wrap<T>(Box value) where T:IFactory=>T.Wrap(value);
          public static int Alias(int value){var a=new Box(value);var b=Select<Keep>(a);a.Value=7;if(b is null)return 0;return b.Value;}
          public static int Forward(int value){var a=new Box(value);var b=Select<Keep>(a);if(b is null)return 0;b.Value=9;return a.Value;}
          public static int FreshResult(){var a=Fresh<Keep>();if(a is null)return 0;a.Value=9;return a.Value;}
          public static int Wrapped(int value){var a=new Box(value);var wrapper=Wrap<Keep>(a);a.Value=9;if(wrapper.Value is null)return 0;return wrapper.Value.Value;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var name in new[] { "Alias", "Forward", "Wrapped" }) foreach (var value in new[] { int.MinValue, 0, int.MaxValue }) Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, value)));
        Assert.Equal(9, Convert.ToInt32(Support.Call(library, "D10.Rules", "FreshResult")));
        foreach (var body in new[] {
          "var result=Select<Keep>(value);if(result is null)return 0;result.Value=1;return result.Value;",
          "var wrapper=Wrap<Keep>(value);if(wrapper.Value is null)return 0;wrapper.Value.Value=1;return 1;"})
        {
            var source = code.Replace("public static int FreshResult()", "public static int Unsafe(Box value){" + body + "}public static int FreshResult()");
            var rejected = Support.Convert(source, selectors: ["Example.D10"]); Assert.Null(rejected.Distribution); Assert.Contains(rejected.Diagnostics, item => item.Code == "MCS3002");
        }
        var unknownAlias = code.Replace("public static int FreshResult()", "public static int Unknown<T>() where T:IFactory {var a=new Box(0);var b=T.Select(a);a.Value=1;if(b is null)return 0;return b.Value;}public static int FreshResult()");
        var unknown = Support.Convert(unknownAlias, selectors: ["Example.D10"]); Assert.Null(unknown.Distribution); Assert.Contains(unknown.Diagnostics, item => item.Code == "MCS3002");
    }

    [Fact]
    public void ConstructedStaticContractsRetainCallerArgumentsThroughOwnedSourceCalls()
    {
        const string code = """
        namespace Example.D10;
        public sealed class Product {public int Value;public int Flag;public Product(int value){Value=value;}}
        public interface IReader<U> where U:System.Numerics.INumberBase<U> {static abstract int Read(U value);static abstract int Flag{get;}}
        public readonly record struct Reader<U>(U Ignored):IReader<U> where U:System.Numerics.INumberBase<U> {public static int Read(U value)=>int.CreateSaturating(value);public static int Flag=>7;}
        public static class Rules{
          public static Product Make<T,U>(U value) where T:IReader<U> where U:System.Numerics.INumberBase<U> {var product=new Product(T.Read(value));product.Flag=T.Flag;return product;}
          public static int Number(long value){var product=Make<Reader<long>,long>(value);if(product is null)return 0;return product.Value;}
          public static int Flag(long value){var product=Make<Reader<long>,long>(value);if(product is null)return 0;return product.Flag;}
        }
        """;
        var library = Support.Success(Support.Convert(code, selectors: ["Example.D10"])); var rules = Support.Emit(code).GetType("Example.D10.Rules")!;
        foreach (var value in new[] { long.MinValue, (long)int.MinValue - 1, -1, 0, 1, (long)int.MaxValue + 1, long.MaxValue }) foreach (var name in new[] { "Number", "Flag" }) Assert.Equal((int)rules.GetMethod(name)!.Invoke(null, [value])!, Convert.ToInt32(Support.Call(library, "D10.Rules", name, Support.Integer(value))));
    }
}
