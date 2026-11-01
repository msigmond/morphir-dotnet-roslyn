namespace Example.Parity;
public record Box<T>(T Value)
{
    public T Read() => Value;
    public T Current => Value;
}
public class Cell<T>(T value) { public T Read() => value; }
public class Crate<T>
{
    public T Value;
    public Crate(T value) { Value = value; }
}
public static class Rules
{
    public static int Read(Box<int> box) => box.Read();
    public static string Current(Box<string> box) => box.Current;
    public static Cell<int> CreateCell(int value) => new(value);
    public static int ReadCell(Cell<int> cell) => cell.Read();
    public static Crate<int?> CreateNullableCrate(int value) => new(value);
    public static Crate<string> CreateCrate(string value) => new(value);
    public static Box<T> Replace<T>(Box<T> box, T value) => box with { Value = value };
}
