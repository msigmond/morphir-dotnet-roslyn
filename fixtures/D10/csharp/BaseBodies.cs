namespace Example.D10;

public abstract class Parent
{
    public virtual int Read() => 7;
}

public sealed class Child : Parent
{
    public override int Read() => base.Read();
}

public static class Rules
{
    public static int Example() => new Child().Read();
}
