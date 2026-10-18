namespace Example.D10;

public interface IFactory
{
    static abstract int Make(int value);
}

public readonly record struct Marker(int Ignored) : IFactory
{
    public static int Make(int value) => value;
}

public static class Rules
{
    public static int Apply<T>(int value) where T : IFactory => T.Make(value);
    public static int Run(int value) => Apply<Marker>(value);
}
