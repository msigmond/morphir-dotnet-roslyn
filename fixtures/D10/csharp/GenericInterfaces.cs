namespace Example.D10;

public interface IValue<T>
{
    T Read();
}

public readonly record struct ValueHolder<U>(U Value) : IValue<U>
{
    public U Read() => Value;
}

public readonly record struct DefaultHolder<U>(int Ignored) : IValue<U>
{
    public U Read() => default;
}
