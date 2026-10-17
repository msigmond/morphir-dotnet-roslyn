using System;
using System.Numerics;
namespace Example.CurrentSupported;

public static partial class CurrentSupportedExample
{
    public static int NumericSize() => sizeof(int);
    public static int NumericNext(int value) => unchecked(value + 1);
    public static int NumericPromotion(byte left, sbyte right) => left + right;
    public static byte NumericByte(int value) => unchecked((byte)value);
    public static sbyte NumericSByte(int value) => unchecked((sbyte)value);
    public static short NumericShort(int value) => unchecked((short)value);
    public static ushort NumericUShort(int value) => unchecked((ushort)value);
    public static char NumericChar(int value) => unchecked((char)value);
    public static uint NumericUInt(uint left, uint right) => unchecked(left + right);
    public static long NumericLong(long left, long right) => unchecked(left * right);
    public static ulong NumericULong(ulong left, ulong right) => unchecked(left + right);
    public static nint NumericNative(nint left, nint right) => unchecked(left + right);
    public static nuint NumericUNative(nuint left, nuint right) => unchecked(left + right);
    public static Int128 NumericInt128(Int128 left, Int128 right) => unchecked(left + right);
    public static UInt128 NumericUInt128(UInt128 left, UInt128 right) => unchecked(left * right);
    public static BigInteger NumericBigInteger(BigInteger left, BigInteger right) => left * right;
    public static float NumericSingle(float left, float right) => left * right;
    public static double NumericDouble(double left, double right) => left / right;
    public static Half NumericHalf(Half left, Half right) => left + right;
    public static Complex NumericComplex(Complex left, Complex right) => left / right;
    public static int NumericDivide(int left, int right)
    {
        if (right == 0 || left == int.MinValue && right == -1) return 0;
        return left / right;
    }
    public static int NumericNarrow(BigInteger value)
    {
        if (value < int.MinValue || value > int.MaxValue) return 0;
        return (int)value;
    }
    public static BigInteger NumericFinite(double value)
    {
        if (!double.IsFinite(value)) return 0;
        return new BigInteger(value);
    }
    public static long NumericLargeLong() => 9007199254740993L;
    public static UInt128 NumericMaximum128() => UInt128.MaxValue;
    public static Int128 NumericConstruct128(ulong upper, ulong lower) => new Int128(upper, lower);
    public static Half NumericHalfMinimum() => Half.Epsilon;
    public static double NumericNaN() => double.NaN;
    public static double NumericInfinity() => double.PositiveInfinity;
    public static double NumericNegativeZero() => -0.0;
    public static long? NumericNullable(int? value) => (long?)value;
    public static int NumericBits(int value, int shift) => (value ^ -1) >>> shift;
    public static double NumericIntegerToDouble(Int128 value) => (double)value;
    public static byte NumericDoubleToByte(double value) => unchecked((byte)value);
    public static double NumericBigDouble(BigInteger value) => (double)value;
    public static BigInteger NumericBigLogical(BigInteger value, int count) => value >>> count;
    public static BigInteger NumericComplexInteger(Complex value)
    {
        if (value.Imaginary == 0)
        {
            if (double.IsFinite(value.Real)) return (BigInteger)value;
        }
        return 0;
    }
}
