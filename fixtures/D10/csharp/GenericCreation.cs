namespace Example.D10;

public static class Creation
{
    public static T Create<T>() where T : new() => new T();
    public static T Forward<T>() where T : new() => Create<T>();
}
