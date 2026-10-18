namespace Example.D10;

public static class Presence
{
    public static bool Missing<T>(T value) => value is null;
    public static T Or<T>(T value, T fallback) where T : class? => value ?? fallback;
}
