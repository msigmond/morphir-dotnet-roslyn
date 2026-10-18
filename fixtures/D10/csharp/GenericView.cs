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

public static class Constrained
{
    public static bool Apply<T>(T rule, int age) where T : IRule
    {
        if (rule is null)
            return false;
        return rule.Allows(age);
    }
}
