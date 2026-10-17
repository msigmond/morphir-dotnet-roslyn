namespace Example.D8;

[System.Flags]
public enum Access : int { None = 0, Read = 1, Alias = 1, Write = 2, All = 3 }
public static class Rules
{
    public static int Flags(Access a, Access b) => (int)(a | b);
}
