namespace Example.D10;

public interface IValue
{
    int Value { get; }
}

public readonly record struct Item(int Value) : IValue;

public static class Rules
{
    public static int Read(IValue? item)
    {
        if (item is null)
            return 0;
        return item.Value;
    }
}
