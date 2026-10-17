namespace Example.D8;

[System.Flags]
public enum Access : int
{
    None = 0,
    Read = 1,
    Alias = 1,
    Write = 2,
    All = 3
}
public static class Rules
{
    public static (int Code, bool IsReadOnly) Describe(Access access) =>
        ((int)access, access == Access.Read);
}
