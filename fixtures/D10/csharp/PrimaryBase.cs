namespace Example.D10;

public abstract class Parent
{
    public int Value;
    protected Parent(int value) { Value = value; }
}

public sealed class Child(int value) : Parent(value);

public static class Rules
{
    public static int Example(int value) => new Child(value).Value;
}
