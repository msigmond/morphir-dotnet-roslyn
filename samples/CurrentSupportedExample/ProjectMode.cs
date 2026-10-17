namespace Example.CurrentSupported;

public static partial class CurrentSupportedExample
{
    private static bool ProjectAdult(int age) => Shared.AgeRule.Adult(age);
    public static bool ProjectEligible(int age) => ProjectAdult(age);
}
