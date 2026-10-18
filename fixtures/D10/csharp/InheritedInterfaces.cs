namespace Example.D10;

public interface IRule
{
    bool Allows(int age);
}

public interface IChecked : IRule
{
    bool Enabled();
}

public readonly record struct MinimumAge(int Minimum) : IChecked
{
    public bool Allows(int age) => age >= Minimum;
    public bool Enabled() => true;
}

public readonly record struct MaximumAge(int Maximum) : IRule
{
    public bool Allows(int age) => age <= Maximum;
}
