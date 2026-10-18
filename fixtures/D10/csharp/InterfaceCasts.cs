namespace Example.D10;

public interface IRule
{
    bool Allows(int age);
}

public readonly record struct MinimumAge(int Minimum) : IRule
{
    public bool Allows(int age) => age >= Minimum;
}

public readonly record struct MaximumAge(int Maximum) : IRule
{
    public bool Allows(int age) => age <= Maximum;
}

public static class Casts
{
    public static bool IsMinimum(IRule? rule) => rule is MinimumAge;
    public static MinimumAge? Minimum(IRule? rule) => rule as MinimumAge?;
    public static int Bound(IRule? rule)
    {
        if (rule is MinimumAge)
            return ((MinimumAge)rule).Minimum;
        return 0;
    }
}
