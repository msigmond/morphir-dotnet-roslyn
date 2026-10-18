namespace Example.D10;

public interface IFactory
{
    static abstract int Value { get; }
}

public sealed class Provider : IFactory
{
    public static int Value => 7;
}

public static class Rules
{
    public static int Read<T>() where T : IFactory => T.Value;
    public static int Run() => Read<Provider>();
}
