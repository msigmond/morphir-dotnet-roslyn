namespace Example.D10;

public interface IRule
{
    bool Check(int age);
    bool Allows(int age) => Check(age);
}

public readonly record struct Minimum(int Bound) : IRule
{
    public bool Check(int age) => age >= Bound;
}

public readonly record struct Maximum(int Bound) : IRule
{
    public bool Check(int age) => age <= Bound;
}
