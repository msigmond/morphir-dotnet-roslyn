namespace Example.D10;

public abstract class Parent
{
    public int Value;
    private Parent() { Value = 11; }
    protected Parent(int value = 3) { Value = value; }
}

public sealed class Child : Parent
{
}

public static class Rules
{
    public static int Example() => new Child().Value;
}
