namespace Example.D10;

public interface IValue
{
    int Read() => 3;
}

public interface IRequired : IValue
{
    abstract int IValue.Read();
}

public readonly record struct ValueHolder(int Value) : IRequired
{
    public int Read() => Value;
}
