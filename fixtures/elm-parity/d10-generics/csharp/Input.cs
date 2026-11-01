namespace Example.Parity;
public record Box<T>(T Value);
public record Pair<A, B>(A First, B Second);
public static class Rules
{
    public static System.Func<int, int> AsFunction() => Identity<int>;
    public static U Apply<T, U>(System.Func<T, U> callback, T value) => callback(value);
    public static T? Optional<T>(T value) => value;
    public static Box<T?> OptionalBox<T>(T value) => new(value);
    public static T Identity<T>(T value) => value;
    public static A SelectFirst<A, B>(A first, B second) => first;
    public static Box<T> Wrap<T>(T value) => new(value);
    public static T Unwrap<T>(Box<T> box) => box.Value;
    public static int Explicit(int value) => Identity<int>(value);
    public static string Inferred(string value) => Identity(value);
    public static Box<int> IntBox(int value) => Wrap(value);
    public static int ReadBox(Box<int> box) => Unwrap(box);
    public static Pair<int, string> MakePair(int first, string second) => new(first, second);
    public static string ReadSecond(Pair<int, string> pair) => pair.Second;
}
