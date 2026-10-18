namespace Example.D10;

public class Parent
{
    public virtual int Read() => 3;
}

public sealed class Leaf : Parent
{
    public override int Read() => 7;
}
