using System.Numerics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Morphir.IR.Classic;
using Morphir.IR.Json;
using Morphir.Tests;
namespace Morphir.Frontend.CSharp.IntegrationTests;

[Trait("Delivery", "D5")]
public class NumericFixtureTests
{
    private static BigInteger Whole(object value) => value is BigInteger big ? big : System.Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
    private static void SameFloat(double expected, object actual)
    {
        if (double.IsNaN(expected)) Assert.True(double.IsNaN((double)actual)); else Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits((double)actual));
    }
    [Theory]
    [InlineData("wrap", "Wrap")]
    [InlineData("numeric", "Numeric")]
    public void IndependentCompleteSourceAndCodecDistributionsMatch(string expected, string source)
    {
        var code = Support.Read($"fixtures/D5/csharp/{source}.cs"); Support.Parity($"fixtures/D5/expected/{expected}.json", code, "Example", "Example.D5");
    }
    [Fact]
    public void EveryNumericFamilyHasBoundaryBehaviorInTheCumulativeExample()
    {
        var code = Support.Read("fixtures/D5/csharp/Numeric.cs"); var lib = Support.Success(Support.Convert(code, selectors: ["Example.D5"])); var rules = Support.Emit(code).GetType("Example.D5.Rules")!;
        object Call(string method, params object[] args) => Support.Call(lib, "D5.Rules", method, args);
        foreach (var value in new[] { int.MinValue, -65537, -1, 0, 1, 65536, int.MaxValue })
        {
            Assert.Equal(unchecked(value + 1), Call("NumericNext", value));
            foreach (var method in new[] { "NumericByte", "NumericSByte", "NumericShort", "NumericUShort", "NumericChar" }) Assert.Equal(Whole(rules.GetMethod(method)!.Invoke(null, [value])!), Whole(Call(method, value)));
            foreach (var count in new[] { -1, 0, 31, 32 }) Assert.Equal((value ^ -1) >>> count, Call("NumericBits", value, count));
        }
        foreach (var a in new byte[] { 0, 1, byte.MaxValue }) foreach (var b in new sbyte[] { sbyte.MinValue, 0, sbyte.MaxValue }) Assert.Equal(a + b, Call("NumericPromotion", (int)a, (int)b));
        foreach (var a in new uint[] { 0, 1, uint.MaxValue }) foreach (var b in new uint[] { 0, 1, uint.MaxValue }) Assert.Equal(new BigInteger(unchecked(a + b)), Whole(Call("NumericUInt", new BigInteger(a), new BigInteger(b))));
        foreach (var a in new long[] { long.MinValue, -9007199254740993L, 0, 9007199254740993L, long.MaxValue }) foreach (var b in new long[] { -1, 0, 1, long.MaxValue })
            {
                Assert.Equal(new BigInteger(unchecked(a * b)), Support.IntegerResult(Call("NumericLong", Support.Integer(a), Support.Integer(b))));
                Assert.Equal(new BigInteger(unchecked((long)((nint)a + (nint)b))), Support.IntegerResult(Call("NumericNative", Support.Integer(a), Support.Integer(b))));
                var ua = unchecked((ulong)a); var ub = unchecked((ulong)b);
                foreach (var method in new[] { "NumericULong", "NumericUNative" }) Assert.Equal(new BigInteger(unchecked(ua + ub)), Support.IntegerResult(Call(method, Support.Integer(ua), Support.Integer(ub))));
            }
        var max128 = (BigInteger.One << 127) - 1; Assert.Equal(-max128 - 1, Support.IntegerResult(Call("NumericInt128", Support.Integer(max128), Support.Integer(1))));
        var unsigned128 = (BigInteger.One << 128) - 1; Assert.Equal(unsigned128 - 1, Support.IntegerResult(Call("NumericUInt128", Support.Integer(unsigned128), Support.Integer(2))));
        var huge = (BigInteger.One << 300) + 17; Assert.Equal(huge * huge, Support.IntegerResult(Call("NumericBigInteger", Support.Integer(huge), Support.Integer(huge))));
        Assert.Equal(new BigInteger(9007199254740993L), Support.IntegerResult(Call("NumericLargeLong"))); Assert.Equal(unsigned128, Support.IntegerResult(Call("NumericMaximum128")));
        Assert.Equal(-BigInteger.One, Support.IntegerResult(Call("NumericConstruct128", Support.Integer(ulong.MaxValue), Support.Integer(ulong.MaxValue))));
        SameFloat((double)Half.Epsilon, Call("NumericHalfMinimum")); SameFloat(double.NaN, Call("NumericNaN")); SameFloat(double.PositiveInfinity, Call("NumericInfinity")); SameFloat(-0.0, Call("NumericNegativeZero"));
        foreach (var value in new[] { 0.0, -0.0, 1.0000000596046448, double.PositiveInfinity, double.NaN, 65520.0 })
        {
            SameFloat((float)value * (float)value, Call("NumericSingle", (double)(float)value, (double)(float)value));
            SameFloat(value / 3, Call("NumericDouble", value, 3.0)); SameFloat((double)((Half)value + (Half)1), Call("NumericHalf", (double)(Half)value, 1.0));
            Assert.Equal((int)unchecked((byte)value), Call("NumericDoubleToByte", value));
        }
        foreach (var value in new[] { BigInteger.Zero, (BigInteger.One << 100) + 1, max128, -max128 - 1 }) SameFloat((double)(Int128)value, Call("NumericIntegerToDouble", Support.Integer(value)));
        var result = (Dictionary<string, object>)Call("NumericComplex", new Dictionary<string, object> { { "real", 1.0 }, { "imaginary", 2.0 } }, new Dictionary<string, object> { { "real", 3.0 }, { "imaginary", 4.0 } });
        var complex = new Complex(1, 2) / new Complex(3, 4); SameFloat(complex.Real, result["real"]); SameFloat(complex.Imaginary, result["imaginary"]);
        foreach (int? value in new int?[] { null, int.MinValue, int.MaxValue })
        {
            var nullable = (Support.MaybeData)Call("NumericNullable", Support.Maybe(value)); Assert.Equal(value.HasValue, nullable.Present); if (nullable.Present) Assert.Equal(new BigInteger(value!.Value), Support.IntegerResult(nullable.Value!));
        }
        // The bound BigInteger comparison overload takes Int64, so constants use the exact wide representation.
        var compilation = Support.Compile(code); var tree = compilation.SyntaxTrees.Single(); var model = compilation.GetSemanticModel(tree);
        var comparison = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single(node => node.ToString() == "value < int.MinValue");
        Assert.Equal(SpecialType.System_Int64, ((IBinaryOperation)model.GetOperation(comparison)!).OperatorMethod!.Parameters[1].Type.SpecialType);
    }
}
