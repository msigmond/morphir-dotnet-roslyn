using System.Numerics;
using System.Runtime.CompilerServices;
using Morphir.IR.Classic;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D8")]
public class EnumTupleTests
{
    [Theory]
    [InlineData("enums-source", "Enums")]
    [InlineData("tuples", "Tuples")]
    [InlineData("flags", "Flags")]
    public void IndependentCompleteSourceDistributionsMatch(string expected, string source) => Support.Parity("fixtures/D8/expected/" + expected + ".json", Support.Read("fixtures/D8/csharp/" + source + ".cs"), "Example", "Example.D8");
    [Fact]
    public void CumulativeAdditionsMatchNativeCSharp()
    {
        var code = Support.Read("samples/CurrentSupportedExample/EnumsTuples.cs"); var lib = Support.Success(Support.Convert(code, selectors: ["Example.CurrentSupported"])); var rules = Support.Emit(code).GetType("Example.CurrentSupported.CurrentSupportedExample")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, 3, 8, int.MaxValue })
        {
            Assert.Equal(value, Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "TupleDeconstruct", value));
            Assert.Equal(value, Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "TupleCode", (object)new object[] { value, true }));
            var result = (object[])Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "EnumDescribe", Support.Enum("CurrentSupported", "SampleAccess", value));
            Assert.Equal(value, result[0]); Assert.Equal(value == 1, result[1]);
            Assert.Equal(value | 3, Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "EnumFlags", Support.Enum("CurrentSupported", "SampleAccess", value), Support.Enum("CurrentSupported", "SampleAccess", 3)));
            Assert.Equal(value, Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "TupleGuard", Support.Maybe(new object[] { value, true })));
        }
        Assert.Equal(-1, Support.Call(lib, "CurrentSupported.CurrentSupportedExample", "TupleGuard", Support.Maybe(null)));
    }
    [Fact]
    public void EnumsRetainDomainsAliasesFlagsConversionsAndSourceOwners()
    {
        const string code = """
        namespace Example.D8;
        [System.Flags] public enum Access:int{None=0,Read=1,Alias=1,Write=2,All=3,Minimum=int.MinValue,Maximum=int.MaxValue}
        public enum ByteAccess:byte{None=0,All=255}
        public enum Wide:ulong{None=0,Top=ulong.MaxValue}
        public static class Rules{
          public static (int Code,bool IsReadOnly) Describe(Access access)=>((int)access,access==Access.Read);
          public static int Flags(Access a,Access b)=>(int)(a|b);
          public static int Mask(Access a,Access b)=>(int)(a&b);
          public static int Xor(Access a,Access b)=>(int)(a^b);
          public static int Complement(Access a)=>(int)~a;
          public static int Add(Access a,int offset)=>(int)(a+offset);
          public static int Difference(Access a,Access b)=>a-b;
          public static int Roundtrip(int value)=>(int)(Access)value;
          public static int Byte(int value)=>(byte)(ByteAccess)value;
          public static ulong WideRoundtrip(ulong value)=>(ulong)(Wide)value;
          public static int Default()=> (int)default(Access);
          public static int? Lifted(Access? a,Access? b)=>(int?)(a|b);
          public static bool Equal(Access? a,Access? b)=>a==b;
          public static int? FromNullable(int? a)=>(int?)(Access?)a;
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D8"])); var assembly = Support.Emit(code); var rules = assembly.GetType("Example.D8.Rules")!; var enumType = assembly.GetType("Example.D8.Access")!;
        foreach (var a in new[] { int.MinValue, -1, 0, 1, 2, 3, 8, int.MaxValue })
        {
            var input = Support.Enum("D8", "Access", a); var native = System.Enum.ToObject(enumType, a);
            var tuple = (ITuple)rules.GetMethod("Describe")!.Invoke(null, [native])!; var result = (object[])Support.Call(lib, "D8.Rules", "Describe", input);
            Assert.Equal(tuple[0], result[0]); Assert.Equal(tuple[1], result[1]);
            foreach (var name in new[] { "Complement" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [native]), Support.Call(lib, "D8.Rules", name, input));
            foreach (var name in new[] { "Roundtrip", "Byte" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [a]), Support.Call(lib, "D8.Rules", name, a));
            foreach (var b in new[] { int.MinValue, -1, 0, 1, 3, 8, int.MaxValue }) foreach (var name in new[] { "Flags", "Mask", "Xor", "Difference", "Add" })
                { var nativeB = System.Enum.ToObject(enumType, b); Assert.Equal(rules.GetMethod(name)!.Invoke(null, [native, name == "Add" ? b : nativeB]), Support.Call(lib, "D8.Rules", name, input, name == "Add" ? b : Support.Enum("D8", "Access", b))); }
        }
        foreach (var n in new[] { 0UL, 1UL, (ulong)long.MaxValue, 1UL << 63, ulong.MaxValue }) Assert.Equal(n, (ulong)Support.IntegerResult(Support.Call(lib, "D8.Rules", "WideRoundtrip", Support.Integer(n))));
        foreach (var a in new int?[] { null, 0, 1, 8 }) foreach (var b in new int?[] { null, 0, 1, 8 })
            {
                object? Native(int? value) => value is null ? null : System.Enum.ToObject(enumType, value.Value);
                var left = Support.Maybe(a is null ? null : Support.Enum("D8", "Access", a.Value)); var right = Support.Maybe(b is null ? null : Support.Enum("D8", "Access", b.Value));
                Assert.Equal(Support.Maybe(rules.GetMethod("Lifted")!.Invoke(null, [Native(a), Native(b)])), Support.Call(lib, "D8.Rules", "Lifted", left, right));
                Assert.Equal(rules.GetMethod("Equal")!.Invoke(null, [Native(a), Native(b)]), Support.Call(lib, "D8.Rules", "Equal", left, right));
                Assert.Equal(Support.Maybe(a), Support.Call(lib, "D8.Rules", "FromNullable", Support.Maybe(a)));
            }
        var data = lib.Definition.Modules.Single(m => m.Key.Key == Morphir.IR.Classic.Path.From("D8").Key).Value.Value;
        Assert.IsType<CustomDefinition<Unit>>(data.Types.Single(t => t.Key.Key == Name.From("Access").Key).Value.Value.Value);
        var constants = lib.Definition.Modules.Single(m => m.Key.Key == Morphir.IR.Classic.Path.From("D8.Access").Key).Value.Value.Values;
        Assert.Equal(7, constants.Count); Assert.Equal(Support.Enum("D8", "Access", 1).Constructor.Local.Key, ((Support.CustomData)Support.Call(lib, "D8.Access", "Alias")).Constructor.Local.Key);
        Assert.Equal(0, Support.Call(lib, "D8.Rules", "Default"));
    }
    [Theory]
    [InlineData("sbyte", -128L, 127L, false)]
    [InlineData("byte", 0L, 255L, false)]
    [InlineData("short", -32768L, 32767L, false)]
    [InlineData("ushort", 0L, 65535L, false)]
    [InlineData("int", -2147483648L, 2147483647L, false)]
    [InlineData("uint", 0L, 4294967295L, false)]
    [InlineData("long", long.MinValue, long.MaxValue, true)]
    [InlineData("ulong", 0L, -1L, true)]
    public void AllEightEnumUnderlyingDomainsPreserveBoundariesAndUnlistedValues(string underlying, long minimum, long maximum, bool wide)
    {
        var high = underlying == "ulong" ? new BigInteger(ulong.MaxValue) : new BigInteger(maximum); var low = new BigInteger(minimum);
        var code = "namespace Example.D8;public static class Outer{public enum Bits:" + underlying + "{Minimum=" + underlying + ".MinValue,Maximum=" + underlying + ".MaxValue,Alias=" + underlying + ".MaxValue}public static " + underlying + " Identity(" + underlying + " value)=>(" + underlying + ")(Bits)value;public static " + underlying + " Complement(" + underlying + " value)=>(" + underlying + ")~(Bits)value;}";
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D8"])); var outer = Support.Emit(code).GetType("Example.D8.Outer")!; var method = outer.GetMethod("Identity")!; var clrType = method.GetParameters()[0].ParameterType;
        foreach (var value in new[] { low, high, BigInteger.Zero, BigInteger.One, high - 1 }.Distinct()) foreach (var name in new[] { "Identity", "Complement" })
            {
                var native = System.Convert.ChangeType(value.ToString(), clrType, System.Globalization.CultureInfo.InvariantCulture); var expected = BigInteger.Parse(outer.GetMethod(name)!.Invoke(null, [native])!.ToString()!);
                var actual = Support.Call(lib, "D8.Outer", name, wide ? Support.Integer(value) : value >= int.MinValue && value <= int.MaxValue ? (object)(int)value : value); Assert.Equal(expected, wide ? Support.IntegerResult(actual) : actual is BigInteger big ? big : new BigInteger((int)actual));
            }
        Assert.Contains(lib.Definition.Modules.Single(module => module.Key.Key == Morphir.IR.Classic.Path.From("D8.Outer").Key).Value.Value.Types, t => t.Key.Key == Name.From("Bits").Key);
        Assert.Equal(3, lib.Definition.Modules.Single(module => module.Key.Key == Morphir.IR.Classic.Path.From("D8.Outer.Bits").Key).Value.Value.Values.Count);
    }
    [Fact]
    public void TuplesPreserveNamesDefaultsDeconstructionNullsAndEvaluationOrder()
    {
        const string code = """
        namespace Example.D8;
        public static class Rules{
          public static (int Code,bool Flag) Pair(int value)=>(value,value>=18);
          public static int Named(int value){var pair=Pair(value);return pair.Code;}
          public static int Deconstruct(int value){var (code,flag)=Pair(value);return flag?code:0;}
          public static int Swap(int value){var a=value;var b=2;(a,b)=(b,a);return b;}
          public static int Nested(int value){var ((a,b),_) = ((value,2),true);return a;}
          public static int Discard(int value){var (_,b)=(2,value);return b;}
          public static int Reference(int value){var tuple=System.Tuple.Create(value,true);return tuple.Item1;}
          public static int Guard(System.Tuple<int,bool>? tuple){if(tuple==null)return -1;return tuple.Item1;}
          public static int Conditional(System.Tuple<int,bool>? tuple)=>tuple?.Item1??-1;
          public static string? DefaultString()=>default((int,string)).Item2;
          public static int DefaultInt()=>default((int,bool)).Item1;
          public static int Order(int value){var pair=(value,value=2);return pair.Item1;}
          public static int After(int value){var pair=(value=2,value);return pair.Item2;}
          public static long Convert(int value){(long Value,bool Flag) pair=(value,true);return pair.Value;}
          public static int Copy(int value){var original=(value,true);var copy=original;copy.Item1=2;return original.Item1;}
          public static int Updated(int value){var pair=(value,true);pair.Item1=2;return pair.Item1;}
          public static int NestedUpdate(int value){var pair=((value,true),false);pair.Item1.Item1=2;return pair.Item1.Item1;}
          public static int RebindRhs(int value){var pair=(value,1);pair.Item1=(pair=(2,3)).Item2;return pair.Item2;}
          public static long? NullableWiden((int Value,bool Flag)? pair){(long Value,bool Flag)? wide=pair;return wide?.Value;}
          public static long WideDeconstruct(int value){(long a,bool b)=(value,true);return a;}
          public static int KnownString(){var pair=("value",1);return pair.Item1.Length;}
          public static int Many(int value){var pair=(1,2,3,4,5,6,7,value,9);return pair.Item8;}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D8"])); var rules = Support.Emit(code).GetType("Example.D8.Rules")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 17, 18, int.MaxValue }) foreach (var name in new[] { "Named", "Deconstruct", "Swap", "Nested", "Discard", "Reference", "Order", "After", "Many", "Copy", "Updated", "NestedUpdate", "RebindRhs" }) Assert.Equal(rules.GetMethod(name)!.Invoke(null, [value]), Support.Call(lib, "D8.Rules", name, value));
        foreach (var value in new[] { int.MinValue, 0, int.MaxValue }) foreach (var name in new[] { "Convert", "WideDeconstruct" }) Assert.Equal(new BigInteger(value), Support.IntegerResult(Support.Call(lib, "D8.Rules", name, value)));
        foreach (var name in new[] { "Guard", "Conditional" }) foreach (var value in new int?[] { null, 0, 18 }) Assert.Equal(value ?? -1, Support.Call(lib, "D8.Rules", name, Support.Maybe(value is null ? null : new object[] { value.Value, true })));
        foreach (var value in new int?[] { null, int.MinValue, 0, int.MaxValue })
        {
            var result = (Support.MaybeData)Support.Call(lib, "D8.Rules", "NullableWiden", Support.Maybe(value is null ? null : new object[] { value.Value, true }));
            Assert.Equal(value is not null, result.Present); if (value is not null) Assert.Equal(new BigInteger(value.Value), Support.IntegerResult(result.Value!));
        }
        Assert.Equal(5, Support.Call(lib, "D8.Rules", "KnownString")); Assert.Equal(0, Support.Call(lib, "D8.Rules", "DefaultInt")); Assert.Equal(Support.Maybe(null), Support.Call(lib, "D8.Rules", "DefaultString"));
    }
    [Fact]
    public void TupleEqualityAndRestConstructionRetainElementSemantics()
    {
        const string code = """
        namespace Example.D8;public static class Rules{
          public static bool Equality(int a,long b,double x,double y)=>(a,x)==(b,y);
          public static bool Nullable((int,bool)? a,(int,bool)? b)=>a!=b;
          public static int Rest(int value)=>System.Tuple.Create(1,2,3,4,5,6,7,value).Rest.Item1;
          public static int GuardRest(System.Tuple<int,int,int,int,int,int,int,System.Tuple<int>>? value){if(value==null)return -1;return value.Rest.Item1;}
          public static int One(int value)=>new System.ValueTuple<int>(value).Item1;
          public static int Constructed(int value)=>new System.Tuple<int,bool>(value,true).Item1;
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D8"])); var rules = Support.Emit(code).GetType("Example.D8.Rules")!;
        foreach (var a in new[] { int.MinValue, 0, 1, int.MaxValue })
        {
            foreach (var b in new[] { long.MinValue, (long)a, long.MaxValue }) foreach (var x in new[] { double.NaN, -0d, 1d, double.PositiveInfinity }) foreach (var y in new[] { double.NaN, 0d, 1d, double.PositiveInfinity }) Assert.Equal(rules.GetMethod("Equality")!.Invoke(null, [a, b, x, y]), Support.Call(lib, "D8.Rules", "Equality", a, Support.Integer(b), x, y));
            foreach (var name in new[] { "Rest", "One", "Constructed" }) Assert.Equal(a, Support.Call(lib, "D8.Rules", name, a));
        }
        Assert.Equal(-1, Support.Call(lib, "D8.Rules", "GuardRest", Support.Maybe(null)));
        Assert.Equal(8, Support.Call(lib, "D8.Rules", "GuardRest", Support.Maybe(new object[] { 1, 2, 3, 4, 5, 6, 7, new object[] { 8 } })));
        foreach (var a in new (int, bool)?[] { null, (0, false), (1, true) }) foreach (var b in new (int, bool)?[] { null, (0, false), (1, true) }) Assert.Equal(a != b, Support.Call(lib, "D8.Rules", "Nullable", Support.Maybe(a is null ? null : new object[] { a.Value.Item1, a.Value.Item2 }), Support.Maybe(b is null ? null : new object[] { b.Value.Item1, b.Value.Item2 })));
        foreach (var member in new[] { "public static object F()=>new System.Tuple<int,int,int,int,int,int,int,int>(1,2,3,4,5,6,7,8);", "public static int F()=>new System.ValueTuple<int,int,int,int,int,int,int,int>(1,2,3,4,5,6,7,8).Item1;" })
        { var result = Support.Convert("namespace Example.D8;public static class Rules{" + member + "}", selectors: ["Example.D8"]); Assert.Null(result.Distribution); }
    }
    [Theory]
    [InlineData("public static int F(System.Tuple<int,bool> value)=>value.Item1;", "MCS2501")]
    [InlineData("public static int F(System.Tuple<int,bool>? value){if(value==null)return 0;value=null;return value.Item1;}", "MCS2501")]
    [InlineData("public static int F(System.Tuple<int,bool> value)=>true?0:value.Item1;", "MCS2501")]
    [InlineData("public static int F(System.Tuple<int,bool> value){return 0;return value.Item1;}", "MCS2501")]
    [InlineData("public static int F(int value)=>checked((int)(Access)value);", "MCS2001")]
    public void UnsafeAccessAndCheckedConversionsRejectWithoutDistribution(string member, string diagnostic)
    {
        var result = Support.Convert("namespace Example.D8;public enum Access{None=0}public static class Rules{" + member + "}", selectors: ["Example.D8"]);
        Assert.Null(result.Distribution); Assert.Contains(result.Diagnostics, d => d.Code == diagnostic && d.Line > 0 && d.File.Length > 0);
    }
}
