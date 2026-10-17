namespace Example.CurrentSupported;

public readonly record struct OptionalCustomer(int? Age, string? Region);
public readonly record struct AnnotatedReference(string Region);
public static partial class CurrentSupportedExample
{
    public static bool MissingAdult(OptionalCustomer data) { if (!data.Age.HasValue) return false; return data.Age.Value >= 18; }
    public static string RegionOrUnknown(OptionalCustomer data) => data.Region ?? "unknown";
    public static int RegionLength(OptionalCustomer data) => data.Region?.Length ?? 99;
    public static int GuardedAnnotation(AnnotatedReference data) { if (data.Region == null) return 0; return data.Region.Length; }
    public static int? NullableAddZero(int? value) => value + 0;
    public static bool NullableEqual(int? a, int? b) => a == b;
    public static bool NullableGreater(int? a, int? b) => a >= b;
    public static bool? NullableAnd(bool? a, bool? b) => a & b;
    public static bool? NullableOr(bool? a, bool? b) => a | b;
    public static bool? NullableXor(bool? a, bool? b) => a ^ b;
    public static bool? NullableNot(bool? value) => !value;
    public static OptionalCustomer DefaultOptional() => default;
    public static OptionalCustomer ConstructOptional() => new(18, "UK");
    public static int ConstructedAge() { var data = new OptionalCustomer(18, "UK"); return data.Age!.Value; }
    public static int NullableLocal() { int? value = 18; return value.Value; }
    public static string? NullResult() => null;
    public static string EchoReference(string value) => value;
    public static string ReferenceCall(string value) => EchoReference(value) ?? "unknown";
    public static int GuardCopy(OptionalCustomer data) { var copy = data; if (data.Age.HasValue) { data = default; return copy.Age!.Value; } return 0; }
    public static int DefaultFromMember(int? value) => value.GetValueOrDefault(99);
    public static int InitializeMissing(int? value) { value ??= 18; return value.Value; }
    public static OptionalCustomer DefaultRecord() => new OptionalCustomer();
    public static int CoalesceState(int? value) { int result = 3; int actual = value ?? (result = 4); return result; }
}
