namespace Example.D10;

public static class Rules
{
    public static decimal Saturate(System.Numerics.BigInteger value) => decimal.CreateSaturating(value);
    public static byte ToByte(decimal value) => byte.CreateTruncating(value);
    public static long ToNative(decimal value) => decimal.ConvertToIntegerNative<long>(value);
}
