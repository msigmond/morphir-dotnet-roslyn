namespace Example.D9;

public sealed class ClassData { public int Value; }
public struct StructData { public int Value; }
public readonly struct ReadonlyData { public int Value { get; init; } }
public sealed record RecordData { public int Value { get; init; } }
public record struct RecordValue { public int Value { get; set; } }
public readonly record struct ReadonlyRecord { public int Value { get; init; } }
public sealed class Owner
{
    private struct Hidden { private bool flag; }
    public struct Nested { public int Count; }
    private sealed class Node { public Node? Next; }
}
public static class Rules
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
