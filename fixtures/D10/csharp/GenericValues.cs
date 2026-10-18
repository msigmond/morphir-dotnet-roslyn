namespace Example.D10;

public readonly record struct Box<T>(T Value)
{
    public T Read() => Value;
}

public static class Rules
{
    public static T Identity<T>(T value) => value;
    public static Box<T> Wrap<T>(T value) => new(value);
    public static T Reference<T>(T value) where T : class? => value;
}
