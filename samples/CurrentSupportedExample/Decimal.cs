namespace Example.CurrentSupported;

public static partial class CurrentSupportedExample
{
    public static decimal DecimalPrice(int units)
    {
        if (units < 0 || units > 1000) return 0m;
        return units * 0.1m;
    }
    public static decimal DecimalExact() => 0.1m;
    public static decimal DecimalScaled() => 1.2300m;
    public static decimal DecimalRound(decimal value) => decimal.Round(value, 2);
    public static byte DecimalScale(decimal value) => value.Scale;
    public static decimal DecimalNegate(decimal value) => -value;
    public static bool DecimalLess(decimal left, decimal right) => left < right;
    public static decimal DecimalFromLong(long value) => value;
}
