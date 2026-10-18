namespace Example.D10;

public abstract class Parent
{
    public int Value { get; set; } = 3;
}

public sealed class Child : Parent
{
}

public static class Rules
{
    public static int Example() => new Child().Value;
}
