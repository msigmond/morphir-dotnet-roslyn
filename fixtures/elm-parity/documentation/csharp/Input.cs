namespace Example.Parity;
/// <summary>Written data.</summary>
public record Data(int Value);
public static class Rules{
/// <summary>Written operation.</summary>
public static int Read(Data data)=>data.Value;
}
