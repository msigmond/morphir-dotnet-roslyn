namespace Example.D10;

public abstract class Parent
{
    public int Value = 3;
    protected Parent() { Value = 7; }
    public int Read() => Value;
}

public sealed class Child : Parent
{
}

public static class Rules
{
    public static int Example() => new Child().Read();
}
