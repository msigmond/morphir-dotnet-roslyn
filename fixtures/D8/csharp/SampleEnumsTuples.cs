namespace Example.CurrentSupported;

[System.Flags]
public enum SampleAccess : int
{
    None = 0,
    Read = 1,
    Alias = 1,
    Write = 2,
    All = 3
}
public static partial class CurrentSupportedExample
{
    public static (int Code, bool IsReadOnly) EnumDescribe(SampleAccess access) =>
        ((int)access, access == SampleAccess.Read);
    public static int EnumFlags(SampleAccess a, SampleAccess b) => (int)(a | b);
    public static int TupleCode((int Code, bool Flag) result) => result.Code;
    public static int TupleDeconstruct(int code)
    {
        var (value, _) = (code, true);
        return value;
    }
    public static System.Tuple<int, bool> TupleReference(int code) => new(code, true);
    public static int TupleGuard(System.Tuple<int, bool>? value)
    {
        if (value == null) return -1;
        return value.Item1;
    }
}
