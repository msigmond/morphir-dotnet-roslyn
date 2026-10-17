namespace Example.D8;

public static class Results
{
    public static int Code((int Code, bool Flag) result) => result.Code;
    public static int Deconstruct(int code)
    {
        var (value, _) = (code, true);
        return value;
    }
    public static System.Tuple<int, bool> Reference(int code) => new(code, true);
    public static int Guard(System.Tuple<int, bool>? value)
    {
        if (value == null) return -1;
        return value.Item1;
    }
}
