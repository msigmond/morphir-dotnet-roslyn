namespace Example.Parity;
public static class Container<T>
{
    public static T Identity(T value) => value;
    public record Nested<U>(T First, U Second);
}
public static class Rules
{
    public static int Use(int value) => Container<int>.Identity(value);
    public static Container<int>.Nested<string> Create(int first, string second) => new(first, second);
    public static string Read(Container<int>.Nested<string> pair) => pair.Second;
    public static T F<T>(T value) => value;
    public static T F<T, U>(T value, U unused) => value;
    public static int One(int value) => F(value);
    public static int Two(int value) => F(value, true);
    private static T Hidden<T>(T value) => value;
}
