namespace Example.D10;

public interface IRule
{
    bool Allows(int age);
}

public struct MinimumAge : IRule
{
    public int Minimum;
    public bool Allows(int age) => age >= Minimum;
}

public readonly struct MaximumAge : IRule
{
    public readonly int Maximum;
    public bool Allows(int age) => age <= Maximum;
}
