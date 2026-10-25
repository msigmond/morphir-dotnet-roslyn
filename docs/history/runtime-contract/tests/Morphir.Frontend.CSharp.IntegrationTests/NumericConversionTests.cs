using System.Numerics;
using Morphir.Tests;

namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D5")]
public class NumericConversionTests
{
    [Fact]
    public void WideAndFloatingConversionsUseThePinnedClrRoundingAndSaturation()
    {
        const string code = """
        namespace Example.D5;
        public static class Rules {
          public static float LongSingle(long a)=>(float)a;
          public static System.Half LongHalf(long a)=>(System.Half)a;
          public static double UnsignedDouble(ulong a)=>(double)a;
          public static float Int128Single(System.Int128 a)=>(float)a;
          public static System.Half Int128Half(System.Int128 a)=>(System.Half)a;
          public static float BigSingle(System.Numerics.BigInteger a)=>(float)a;
          public static System.Numerics.BigInteger Logical(System.Numerics.BigInteger a,int count)=>a>>>count;
          public static double BigDouble(System.Numerics.BigInteger a)=>(double)a;
          public static long DoubleLong(double a)=>unchecked((long)a);
          public static ulong DoubleUnsigned(double a)=>unchecked((ulong)a);
          public static System.Int128 DoubleInt128(double a)=>unchecked((System.Int128)a);
          public static System.UInt128 DoubleUInt128(double a)=>unchecked((System.UInt128)a);
          public static long UnsignedSigned(ulong a)=>unchecked((long)a);
          public static ulong SignedUnsigned(long a)=>unchecked((ulong)a);
          public static System.Numerics.BigInteger RealInteger(System.Numerics.Complex a){if(a.Imaginary!=0 || !double.IsFinite(a.Real))return 0;return (System.Numerics.BigInteger)a;}
          public static int RoundTrip(int a){System.Numerics.BigInteger b=a;return (int)b;}
          public static long RoundWide(long a){System.Numerics.BigInteger b=a;return (long)b;}
          public static System.Numerics.BigInteger Constructed(){var a=new System.Numerics.Complex(3.5,0);return (System.Numerics.BigInteger)a;}
          public static System.Int128? Negate(System.Int128? a)=>-a;
          public static double? Negative(double? a)=>-a;
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D5"]));
        foreach (var a in new[] { int.MinValue, 0, int.MaxValue }) Assert.Equal(a, Support.Call(lib, "D5.Rules", "RoundTrip", a));
        Assert.Equal(new BigInteger(3), Support.IntegerResult(Support.Call(lib, "D5.Rules", "Constructed")));
        void Same(double expected, object actual) { var value = (double)actual; if (double.IsNaN(expected)) Assert.True(double.IsNaN(value)); else Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(value)); }
        foreach (var a in new[] { long.MinValue, -9007199254740993L, -16777217L, -65520L, 0, 65519L, 16777217L, 9007199254740993L, long.MaxValue })
        {
            Assert.Equal(new BigInteger(a), Support.IntegerResult(Support.Call(lib, "D5.Rules", "RoundWide", Support.Integer(a))));
            Same((float)a, Support.Call(lib, "D5.Rules", "LongSingle", Support.Integer(a))); Same((double)(Half)a, Support.Call(lib, "D5.Rules", "LongHalf", Support.Integer(a)));
            var u = unchecked((ulong)a); Same((double)u, Support.Call(lib, "D5.Rules", "UnsignedDouble", Support.Integer(u)));
            Assert.Equal(new BigInteger(unchecked((long)u)), Support.IntegerResult(Support.Call(lib, "D5.Rules", "UnsignedSigned", Support.Integer(u))));
            Assert.Equal(new BigInteger(unchecked((ulong)a)), Support.IntegerResult(Support.Call(lib, "D5.Rules", "SignedUnsigned", Support.Integer(a))));
        }
        foreach (var a in new[] { -(BigInteger.One << 127), (BigInteger.One << 100) + (BigInteger.One << 76) + 1, (BigInteger.One << 127) - 1 })
        {
            Same((float)(Int128)a, Support.Call(lib, "D5.Rules", "Int128Single", Support.Integer(a))); Same((double)(Half)(Int128)a, Support.Call(lib, "D5.Rules", "Int128Half", Support.Integer(a)));
            Same((float)a, Support.Call(lib, "D5.Rules", "BigSingle", Support.Integer(a))); Same((double)a, Support.Call(lib, "D5.Rules", "BigDouble", Support.Integer(a)));
            var result = (Support.MaybeData)Support.Call(lib, "D5.Rules", "Negate", Support.Maybe(Support.Integer(a))); Assert.Equal(BigInteger.Parse(unchecked(-(Int128)a).ToString()), Support.IntegerResult(result.Value!));
        }
        foreach (var a in new[] { double.NaN, double.NegativeInfinity, double.PositiveInfinity, -0.0, 0.0, -3.9, 3.9, -Math.ScaleB(1, 127), Math.ScaleB(1, 128), double.MaxValue })
        {
            foreach (var pair in new[] { ("DoubleLong", new BigInteger(unchecked((long)a))), ("DoubleUnsigned", new BigInteger(unchecked((ulong)a))), ("DoubleInt128", BigInteger.Parse(unchecked((Int128)a).ToString())), ("DoubleUInt128", BigInteger.Parse(unchecked((UInt128)a).ToString())) })
                Assert.Equal(pair.Item2, Support.IntegerResult(Support.Call(lib, "D5.Rules", pair.Item1, a)));
            var result = (Support.MaybeData)Support.Call(lib, "D5.Rules", "Negative", Support.Maybe(a)); Same(-a, result.Value!);
        }
        foreach (var a in new BigInteger[] { -1, -2147483648L, -2147483649L, -4294967295L, -4294967296L, -4294967297L, -(BigInteger.One << 128), 1 }) foreach (var count in new[] { -1, 0, 1, 31, 32, 33, 64, 128, 129 })
                Assert.Equal(a >>> count, Support.IntegerResult(Support.Call(lib, "D5.Rules", "Logical", Support.Integer(a), count)));
        Assert.Equal(Support.Maybe(null), Support.Call(lib, "D5.Rules", "Negate", Support.Maybe(null)));
        Assert.Equal(Support.Maybe(null), Support.Call(lib, "D5.Rules", "Negative", Support.Maybe(null)));
        foreach (var real in new[] { double.NaN, double.PositiveInfinity, 0.0, 3.9, -3.9 }) foreach (var imaginary in new[] { 0.0, -0.0, 1.0, double.NaN })
            {
                var input = new Dictionary<string, object> { { "real", real }, { "imaginary", imaginary } };
                Assert.Equal(imaginary == 0 && double.IsFinite(real) ? new BigInteger(real) : BigInteger.Zero, Support.IntegerResult(Support.Call(lib, "D5.Rules", "RealInteger", input)));
            }
        var invalid = Support.Convert("namespace Example.D5;public static class Rules{public static System.Numerics.BigInteger F(System.Numerics.Complex a)=>(System.Numerics.BigInteger)a;}", selectors: ["Example.D5"]);
        Assert.Contains(invalid.Diagnostics, d => d.Code == "MCS2606");
    }
}
