namespace Example.D6;
public static class Rules
{
    public static decimal Price(int units)
    {
        if (units < 0 || units > 1000) return 0m;
        return units * 0.1m;
    }
    public static decimal Exact() => 0.1m;
    public static decimal Scaled() => 1.2300m;
    public static decimal Round(decimal value) => decimal.Round(value, 2);
    public static byte Scale(decimal value) => value.Scale;
    public static decimal Negate(decimal value) => -value;
    public static bool Less(decimal left, decimal right) => left < right;
    public static decimal FromLong(long value) => value;
}
