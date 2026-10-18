namespace Example.D10;

public abstract class Parent
{
    public int Value;
}

public sealed class Child : Parent
{
    public new bool Value;
}

public static class Rules
{
    public static bool Own() => new Child().Value;
    public static int Root() => ((Parent)new Child()).Value;
}
