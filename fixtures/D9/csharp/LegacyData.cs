namespace Example.CurrentSupported;

public sealed class ClassData { public int Value; }
public struct StructData { public int Value; public static bool operator !(StructData value) => value.Value == 0; public static explicit operator int(StructData value) => value.Value; }
public readonly struct ReadonlyData { public int Value { get; init; } }
public sealed record RecordData { public int Value { get; init; } }
public record struct RecordValue { public int Value { get; set; } }
public readonly record struct ReadonlyRecord { public int Value { get; init; } }
public sealed class Owner
{
    private struct Hidden { private bool Flag { get; set; } }
    public struct Nested { public int Count; }
    private sealed class Node { public Node? Next { get; set; } }
}
public static partial class CurrentSupportedExample
{
    public static ClassData MakeClass(int value) => new ClassData { Value = value };
    public static StructData MakeStruct(int value) => new StructData { Value = value };
    public static ReadonlyData MakeReadonly(int value) => new ReadonlyData { Value = value };
    public static RecordData MakeRecord(int value) => new RecordData { Value = value };
    public static RecordValue MakeValue(int value) => new RecordValue { Value = value };
    public static ReadonlyRecord MakeReadonlyRecord(int value) => new ReadonlyRecord { Value = value };
    public static int ReadClass(ClassData? value) { if (value is null) return 0; return value.Value; }
    public static int ReadStruct(StructData value) => value.Value;
    public static int ReadReadonly(ReadonlyData value) => value.Value;
    public static int ReadRecord(RecordData? value) { if (value is null) return 0; return value.Value; }
    public static int ReadValue(RecordValue value) => value.Value;
    public static int ReadReadonlyRecord(ReadonlyRecord value) => value.Value;
}

public sealed class SampleCounter { public int Value; }
public sealed class ConstructedData
{
    public int Value;
    public ConstructedData(int value) { Value = value; }
    public int Read => Value;
}
public static partial class CurrentSupportedExample
{
    public static int DataAlias()
    {
        var counter = new SampleCounter { Value = 1 };
        var alias = counter;
        alias.Value = 2;
        return counter.Value;
    }
    public static int DataConstructed(int value) => new ConstructedData(value).Read;
    public static bool DataRecordsEqual(int a, int b) => new RecordValue { Value = a } == new RecordValue { Value = b };
    public static int DataWith(int value)
    {
        var original = new RecordValue { Value = value };
        var copy = original with { Value = 2 };
        return copy.Value;
    }
    public static bool DataOperator(int value) => !new StructData { Value = value };
    public static int DataConvert(int value) => (int)new StructData { Value = value };
}
