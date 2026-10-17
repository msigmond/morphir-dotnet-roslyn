namespace Example.D2.Business;
public static partial class Rules
{
    private static bool Adult(int age) => Example.D2.Shared.AgeRule.Adult(age);
    public static bool Eligible(int age) => Adult(age);
#if D2_FEATURE
    public static int Conditional() => 18;
#else
    public static int Conditional() => 21;
#endif
}
