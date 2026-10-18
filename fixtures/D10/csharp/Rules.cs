namespace Example.D10;

public readonly record struct Box<T>(T Value);

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

public static class Rules
{
    public static Box<T> Wrap<T>(T value) => new(value);

    public static bool Test(IRule? rule, int age)
    {
        if (rule is null)
            return false;
        return rule.Allows(age);
    }
}
