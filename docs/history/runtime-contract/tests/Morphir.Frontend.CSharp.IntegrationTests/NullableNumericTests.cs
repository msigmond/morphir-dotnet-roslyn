using System.Numerics;
using Morphir.Tests;
namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D5")]
public class NullableNumericTests
{
    [Fact]
    public void NullableNumericConversionsPreserveAbsenceAndRequireExtractionProof()
    {
        const string code = """
        namespace Example.D5;
        public static class Rules {
          public static long? Wide(int? value)=>(long?)value;
          public static byte? Narrow(int? value)=>unchecked((byte?)value);
          public static long Extract(int? value){if(value.HasValue)return (long)value;return 99;}
          public static long? Add(long? a,long? b)=>unchecked(a+b);
          public static double? Multiply(double? a,double? b)=>a*b;
          public static long? Divide(long? a,long? b){if(b.HasValue && b.Value>0)return a/b;return null;}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D5"]));
        foreach (int? input in new int?[] { null, int.MinValue, -1, 0, 1, int.MaxValue })
        {
            var widened = (Support.MaybeData)Support.Call(lib, "D5.Rules", "Wide", Support.Maybe(input));
            Assert.Equal(input.HasValue, widened.Present); if (input.HasValue) Assert.Equal(new BigInteger(input.Value), Support.IntegerResult(widened.Value!));
            Assert.Equal(Support.Maybe(input.HasValue ? (int)unchecked((byte)input.Value) : null), Support.Call(lib, "D5.Rules", "Narrow", Support.Maybe(input)));
            Assert.Equal(new BigInteger(input ?? 99), Support.IntegerResult(Support.Call(lib, "D5.Rules", "Extract", Support.Maybe(input))));
        }
        foreach (long? a in new long?[] { null, long.MinValue, 0, long.MaxValue }) foreach (long? b in new long?[] { null, -1, 0, 1, 3 })
            {
                var left = Support.Maybe(a.HasValue ? Support.Integer(a.Value) : null); var right = Support.Maybe(b.HasValue ? Support.Integer(b.Value) : null);
                var add = (Support.MaybeData)Support.Call(lib, "D5.Rules", "Add", left, right);
                Assert.Equal(a.HasValue && b.HasValue, add.Present); if (add.Present) Assert.Equal(new BigInteger(unchecked(a!.Value + b!.Value)), Support.IntegerResult(add.Value!));
                var quotient = (Support.MaybeData)Support.Call(lib, "D5.Rules", "Divide", left, right);
                Assert.Equal(a.HasValue && b > 0, quotient.Present); if (quotient.Present) Assert.Equal(new BigInteger(a!.Value / b!.Value), Support.IntegerResult(quotient.Value!));
            }
        foreach (double? a in new double?[] { null, -0.0, 3, double.PositiveInfinity }) foreach (double? b in new double?[] { null, -1, 0, 3 })
            {
                var result = (Support.MaybeData)Support.Call(lib, "D5.Rules", "Multiply", Support.Maybe(a), Support.Maybe(b));
                Assert.Equal(a.HasValue && b.HasValue, result.Present); if (result.Present) { var expected = a!.Value * b!.Value; if (double.IsNaN(expected)) Assert.True(double.IsNaN((double)result.Value!)); else Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits((double)result.Value!)); }
            }
        var invalid = Support.Convert("namespace Example.D5;public static class Rules{public static long F(int? value)=>(long)value;}", selectors: ["Example.D5"]);
        Assert.Contains(invalid.Diagnostics, d => d.Code == "MCS2501");
    }
    [Fact]
    public void FiniteFloatingGuardsPermitExactBigIntegerConstructionAndInvalidateOnAssignment()
    {
        const string code = """
        using System.Numerics;
        namespace Example.D5;
        public static class Rules {
          public static BigInteger Cast(double value){if(!double.IsFinite(value))return 0;return (BigInteger)value;}
          public static BigInteger Construct(double value){if(double.IsFinite(value))return new BigInteger(value);return 0;}
        }
        """;
        var lib = Support.Success(Support.Convert(code, selectors: ["Example.D5"]));
        foreach (var value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -0.0, 0.0, -3.9, 3.9, Math.ScaleB(1, 128), double.MaxValue }) foreach (var method in new[] { "Cast", "Construct" })
                Assert.Equal(double.IsFinite(value) ? new BigInteger(value) : BigInteger.Zero, Support.IntegerResult(Support.Call(lib, "D5.Rules", method, value)));
        foreach (var member in new[] { "public static System.Numerics.BigInteger F(double value)=>(System.Numerics.BigInteger)value;", "public static System.Numerics.BigInteger F(double value){if(double.IsFinite(value)){value=double.NaN;return (System.Numerics.BigInteger)value;}return 0;}" })
        {
            var invalid = Support.Convert("namespace Example.D5;public static class Rules{" + member + "}", selectors: ["Example.D5"]); Assert.Contains(invalid.Diagnostics, d => d.Code == "MCS2604");
        }
    }
}
