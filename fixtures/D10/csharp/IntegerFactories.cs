namespace Example.D10;

public static class Rules
{
    public static byte Small(long value) => byte.CreateSaturating(value);
    public static int Wrapped(System.Numerics.BigInteger value) => int.CreateTruncating(value);
    public static long Widen(byte value) => long.CreateChecked(value);
}
