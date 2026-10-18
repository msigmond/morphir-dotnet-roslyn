namespace Example.D10;

public sealed class Counter
{
    public int Value;
}

public interface IRead
{
    Counter? Read();
}

public sealed class Holder : IRead
{
    public Counter? Value;
    public Counter? Read() => Value;
}

public struct ValueHolder : IRead
{
    public Counter? Value;
    public Counter? Read() => Value;
}
