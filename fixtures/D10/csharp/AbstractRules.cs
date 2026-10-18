namespace Example.D10;

public abstract class Rule
{
    public abstract bool Allows(int age);
}

public sealed class MinimumAge : Rule
{
    public override bool Allows(int age) => age >= 18;
}

public sealed class MaximumAge : Rule
{
    public override bool Allows(int age) => age <= 65;
}

public static class Rules
{
    public static bool Test(Rule? rule, int age)
    {
        if (rule is null)
            return false;
        return rule.Allows(age);
    }

    public static bool Example(int age) => Test(new MinimumAge(), age);
}
