namespace Example.Parity;
public static class Rules
{
    public static int Use(int value)
    {
        T Echo<T>(T item) => item;
        return Echo(value);
    }
    public static T Forward<T>(T value)
    {
        U Echo<U>(U item) => item;
        return Echo(value);
    }
    public static int Through(int value)
    {
        U Echo<U>(U item) => item;
        V Forward<V>(V item) => Echo(item);
        return Forward(value);
    }
}
