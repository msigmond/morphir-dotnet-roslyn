namespace Example.D9;
public sealed class Limits
{
    public const int Minimum = 18;
    private const bool Enabled = true;
    public const string Label = "adult";
}
public static class Rules
{
    public static int Read() => Limits.Minimum;
    public static string Text() => Limits.Label;
}
