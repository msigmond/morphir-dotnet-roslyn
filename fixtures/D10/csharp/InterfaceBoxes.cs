namespace Example.D10;

public interface IValue
{
    int Value { get; set; }
}

public struct Counter : IValue
{
    public int Value { get; set; }
}

public static class Rules
{
    public static int Example(int value)
    {
        IValue box = new Counter();
        box.Value = value;
        return box.Value;
    }
}
