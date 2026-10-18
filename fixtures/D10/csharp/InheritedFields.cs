namespace Example.D10;

public abstract class Parent
{
    public int Value;
    public int Read() => Value;
}

public sealed class Child : Parent
{
}

public static class Rules
{
    public static int Example() => new Child().Read();
}
