namespace Example.D10;

public sealed class Cell<T>
{
    public T Value;
}

public struct Pair<T>
{
    public T Value;
}

public static class Storage
{
    public static T Default<T>() => default;
    public static Cell<T> Create<T>(T value) => new() { Value = value };
    public static Pair<T> NewPair<T>() => default;
}
