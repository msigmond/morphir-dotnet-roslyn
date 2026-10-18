namespace Example.D10;

public interface IValue
{
    int Value { get; set; }
}

public sealed class Item : IValue
{
    public int Value { get; set; }
}
