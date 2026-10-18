namespace Example.D10;

public interface IMapper
{
    T Echo<T>(T value);
    T Zero<T>();
}

public readonly record struct Mapper(int Ignored) : IMapper
{
    public U Echo<U>(U value) => value;
    public U Zero<U>() => default;
}
